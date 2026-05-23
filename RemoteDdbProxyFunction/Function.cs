using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

// Lambda ???/??? System.Text.Json ???????????
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RemoteDdbProxyFunction;

public class Function
{
    // Secrets Manager ??????????????????????
    private static bool _initialized = false;
    private static string? _remoteRegion;
    private static string? _remoteTableName;
    private static AmazonDynamoDBClient? _remoteDdb;

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // 1) ???? Secrets Manager ??????DynamoDB?????????
            if (!_initialized)
            {
                await InitRemoteClientAsync(context);
            }

            if (_remoteDdb is null || _remoteTableName is null)
            {
                return Response(500, new { ok = false, error = "Remote DynamoDB client not initialized" });
            }

            // 2) pk ???/??????????????deviceId ?????????
            string? pk = null;

            if (request.PathParameters != null &&
                request.PathParameters.TryGetValue("pk", out var pkFromPath))
            {
                pk = pkFromPath;
            }

            if (string.IsNullOrWhiteSpace(pk) &&
                request.PathParameters != null &&
                request.PathParameters.TryGetValue("deviceId", out var deviceIdFromPath))
            {
                pk = deviceIdFromPath;
            }

            // API Gateway uses the path parameter name verbatim; the path is defined as {DeviceId}
            if (string.IsNullOrWhiteSpace(pk) &&
                request.PathParameters != null &&
                request.PathParameters.TryGetValue("DeviceId", out var deviceIdFromPath2))
            {
                pk = deviceIdFromPath2;
            }

            if (string.IsNullOrWhiteSpace(pk) &&
                request.QueryStringParameters != null &&
                request.QueryStringParameters.TryGetValue("pk", out var pkFromQuery))
            {
                pk = pkFromQuery;
            }

            if (string.IsNullOrWhiteSpace(pk) &&
                request.QueryStringParameters != null &&
                request.QueryStringParameters.TryGetValue("deviceId", out var deviceIdFromQuery))
            {
                pk = deviceIdFromQuery;
            }

            if (string.IsNullOrWhiteSpace(pk))
            {
                return Response(400, new { ok = false, error = "pk (or deviceId) is required" });
            }

            // GSI(pk-receivedAt-index) ????receivedAt ????????1???????
            var qReq = new QueryRequest
            {
                TableName = _remoteTableName,
                IndexName = "pk-receivedAt-index",
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = pk }
                },
                // GSI ???????????????? false ??
                ConsistentRead = false,
                ScanIndexForward = false, // receivedAt ?????????????
                Limit = 1
            };

            var qRes = await _remoteDdb.QueryAsync(qReq);

            if (qRes.Items == null || qRes.Items.Count == 0)
            {
                return Response(404, new { ok = false, error = "not found", pk });
            }

            var latestItem = qRes.Items[0];

            // Convert the full DynamoDB item to a plain object for debugging / inspection
            var item = AttributeMapToSimpleObject(latestItem);

            // Extract GPS fields if present (support multiple common attribute names)
            double? lat = null;
            double? lon = null;

            if (latestItem.TryGetValue("lat", out var latAttr) || latestItem.TryGetValue("latitude", out latAttr))
            {
                lat = ParseDoubleAttribute(latAttr);
            }

            if (latestItem.TryGetValue("lon", out var lonAttr) || latestItem.TryGetValue("longitude", out lonAttr))
            {
                lon = ParseDoubleAttribute(lonAttr);
            }

            return Response(200, new
            {
                ok = true,
                deviceId = pk,
                lat,
                lon,
                item
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError("RemoteDdbProxy UNHANDLED: " + ex);
            return Response(500, new { ok = false, error = ex.Message });
        }
    }

    // ===== ????DDB??????????Secrets ?????????? =====
    private static async Task InitRemoteClientAsync(ILambdaContext context)
    {
        var secretArn = Environment.GetEnvironmentVariable("REMOTE_DDB_SECRET_ARN");
        if (string.IsNullOrWhiteSpace(secretArn))
            throw new Exception("REMOTE_DDB_SECRET_ARN is not set");

        using var sm = new AmazonSecretsManagerClient();
        var resp = await sm.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretArn
        });

        var raw = resp.SecretString ?? throw new Exception("SecretString is null");
        var node = JsonNode.Parse(raw) ?? throw new Exception("Secret JSON parse failed");

        var accessKeyId = node["accessKeyId"]?.GetValue<string>();
        var secretAccessKey = node["secretAccessKey"]?.GetValue<string>();
        _remoteRegion = node["region"]?.GetValue<string>();
        _remoteTableName = node["tableName"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(accessKeyId) ||
            string.IsNullOrWhiteSpace(secretAccessKey) ||
            string.IsNullOrWhiteSpace(_remoteRegion) ||
            string.IsNullOrWhiteSpace(_remoteTableName))
        {
            throw new Exception("Secret is missing required fields");
        }

        var creds = new BasicAWSCredentials(accessKeyId, secretAccessKey);
        _remoteDdb = new AmazonDynamoDBClient(creds, Amazon.RegionEndpoint.GetBySystemName(_remoteRegion));

        _initialized = true;
        context.Logger.LogInformation("RemoteDdbProxy: remote client initialized");
    }

    // ===== AttributeValue ? JSON ??????????? object ??? =====
    private static object AttributeMapToSimpleObject(Dictionary<string, AttributeValue> item)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kv in item)
        {
            var key = kv.Key;
            var v = kv.Value;

            if (v.S != null) dict[key] = v.S;
            else if (v.N != null) dict[key] = v.N;
            else if (v.BOOL != null) dict[key] = v.BOOL;
            else if (v.SS != null) dict[key] = v.SS;
            else if (v.NS != null) dict[key] = v.NS;
            else if (v.M != null) dict[key] = AttributeMapToSimpleObject(v.M);
            else if (v.L != null) dict[key] = v.L.Select(av => AttributeMapToSimpleObject(new Dictionary<string, AttributeValue> { ["_"] = av })).ToList();
            else dict[key] = null;
        }
        return dict;
    }

    private static double? ParseDoubleAttribute(AttributeValue? attr)
    {
        if (attr == null)
            return null;

        var raw = attr.N ?? attr.S;
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static APIGatewayProxyResponse Response(int statusCode, object bodyObj)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(bodyObj),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
    }
}
