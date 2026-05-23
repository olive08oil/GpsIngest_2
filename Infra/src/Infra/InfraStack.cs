using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SecretsManager;
// using Amazon.CDK.AWS.SES.Actions;  // 使っていなければ不要
// using Amazon.CDK.AWS.Synthetics;    // 使っていなければ不要
using Constructs;
using System.Collections.Generic;
using System.IO;

// エイリアス（衝突/長文回避）
using Ddb = Amazon.CDK.AWS.DynamoDB;
using Lambda = Amazon.CDK.AWS.Lambda;
using LambdaEvents = Amazon.CDK.AWS.Lambda.EventSources;

namespace Infra
{
    public class InfraStack : Stack
    {
        public InfraStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // ===== 1) DynamoDB: 履歴テーブル =====
            var historyTable = new Table(this, "TelemetryHistory", new TableProps
            {
                TableName = "TelemetryHistory",
                PartitionKey = new Attribute { Name = "pk", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "sk", Type = AttributeType.NUMBER },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                PointInTimeRecoverySpecification = new Ddb.PointInTimeRecoverySpecification
                {
                    PointInTimeRecoveryEnabled = true
                },
                RemovalPolicy = RemovalPolicy.DESTROY // 残したい場合は RETAIN
            });

            historyTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "LatestByDevice",
                PartitionKey = new Attribute { Name = "deviceId", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "ts", Type = AttributeType.NUMBER },
                ProjectionType = ProjectionType.INCLUDE,
                NonKeyAttributes = new[] { "lat", "lon", "speed", "heading", "speedDelta", "headingDelta", "accelX", "accelY", "source" }
            });

            historyTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "ByMsgType",
                PartitionKey = new Attribute { Name = "msgType", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "ts", Type = AttributeType.NUMBER },
                ProjectionType = ProjectionType.INCLUDE,
                NonKeyAttributes = new[] { "deviceId", "lat", "lon", "speed", "heading" }
            });

            // ===== 2) DynamoDB: 最新値テーブル（Streams 有効） =====
            var latestTable = new Table(this, "DeviceLatest", new TableProps
            {
                TableName = "DeviceLatest",
                PartitionKey = new Attribute { Name = "deviceId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                PointInTimeRecoverySpecification = new Ddb.PointInTimeRecoverySpecification
                {
                    PointInTimeRecoveryEnabled = true
                },
                Stream = StreamViewType.NEW_IMAGE,   // ★ Lambda トリガ用に有効化
                RemovalPolicy = RemovalPolicy.DESTROY // 残したい場合は RETAIN
            });

            // ===== 3) Ingest 用 Lambda (.NET 8) =====
            var ingestFn = new Lambda.Function(this, "IngestFn", new Lambda.FunctionProps
            {
                FunctionName = "GpsIngestFn",
                Runtime = Lambda.Runtime.DOTNET_8,
                Handler = "GPSIngest::GPSIngest.Function::FunctionHandler", // プロジェクトに合わせて
                MemorySize = 256,
                Timeout = Duration.Seconds(10),
                Environment = new Dictionary<string, string>
                {
                    ["HISTORY_TABLE_NAME"] = historyTable.TableName,
                    ["LATEST_TABLE_NAME"] = latestTable.TableName
                },
                // publish 済みフォルダをアセットとして指定
                Code = Lambda.Code.FromAsset("../GPSIngest/publish")
            });

            historyTable.GrantWriteData(ingestFn);
            latestTable.GrantReadData(ingestFn);
            latestTable.GrantWriteData(ingestFn);

            // ===== 4) API Gateway（/ingest → Lambda） =====
            var api = new RestApi(this, "GpsIngestApi", new RestApiProps
            {
                DeployOptions = new StageOptions
                {
                    ThrottlingBurstLimit = 20,
                    ThrottlingRateLimit = 50
                }
            });

            var apiKey = api.AddApiKey("ClientApiKey", new ApiKeyOptions
            {
                ApiKeyName = "gps-ingest-client"
            });

            var plan = api.AddUsagePlan("GpsUsagePlan", new UsagePlanProps
            {
                Name = "gps-clients",
                Throttle = new ThrottleSettings { BurstLimit = 20, RateLimit = 50 },
                Quota = new QuotaSettings { Limit = 100000, Period = Period.DAY }
            });
            plan.AddApiKey(apiKey);
            plan.AddApiStage(new UsagePlanPerApiStage { Api = api, Stage = api.DeploymentStage });

            var ingest = api.Root.AddResource("ingest");
            ingest.AddMethod("POST", new LambdaIntegration(ingestFn), new MethodOptions { ApiKeyRequired = true });

            // ===== 5) DynamoDB → Singular 連携（Streams トリガ Lambda） =====
            // Secrets Manager（Singular Private Token 保管）
            var singularSecret = new Secret(this, "SingularPrivateToken", new SecretProps
            {
                SecretName = "singular/datastream/privateToken",
                Description = "Singular Data Stream Private Token"
            });

            // Forwarder Lambda（publish 済み成果物を指定）
            var lambdaCodePath = Path.Combine("..", "SingularForwarderFunction", "bin", "Release", "net8.0", "publish");

            var forwarderFn = new Lambda.Function(this, "SingularForwarderFn", new Lambda.FunctionProps
            {
                Runtime = Lambda.Runtime.DOTNET_8,
                Handler = "SingularForwarderFunction::SingularForwarderFunction.Function::FunctionHandler",
                Code = Lambda.Code.FromAsset(lambdaCodePath),
                Timeout = Duration.Seconds(10),
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    ["SINGULAR_SECRET_ARN"] = singularSecret.SecretArn,
                    ["PUBLIC_STREAM_NAME"] = "GPS_LiveFeed_Test"
                }
            });

            singularSecret.GrantRead(forwarderFn);

            // DeviceLatest の Streams をイベントソースに設定
            forwarderFn.AddEventSource(new LambdaEvents.DynamoEventSource(latestTable, new LambdaEvents.DynamoEventSourceProps
            {
                StartingPosition = Lambda.StartingPosition.LATEST,
                BatchSize = 10,
                BisectBatchOnError = true,
                RetryAttempts = 2
            }));

            // ===== 6) Outputs =====
            new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = api.Url + "ingest" });
            new CfnOutput(this, "ApiKeyId", new CfnOutputProps { Value = apiKey.KeyId });
            new CfnOutput(this, "HistoryTableName", new CfnOutputProps { Value = historyTable.TableName });
            new CfnOutput(this, "LatestTableName", new CfnOutputProps { Value = latestTable.TableName });
            new CfnOutput(this, "IngestLambdaName", new CfnOutputProps { Value = ingestFn.FunctionName });
            new CfnOutput(this, "ForwarderLambdaName", new CfnOutputProps { Value = forwarderFn.FunctionName });


            // 既存の API がある前提

            //var api = /* 既に定義済みの RestApi インスタンス */;

            var remoteDdbSecretArn = new CfnParameter(this, "RemoteDdbSecretArn", new CfnParameterProps
            {
                Type = "String",
                NoEcho = true,
                Description = "Secrets Manager ARN for remote DynamoDB credentials"
            });
            // RemoteDdbProxyFunction の Lambda
            var remoteDdbFn = new Function(this, "RemoteDdbProxyFn", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8, // or 6
                MemorySize = 256,
                Timeout = Duration.Seconds(10),
                Code = Code.FromAsset("../RemoteDdbProxyFunction/bin/Release/net8.0"), // パスは環境に合わせる
                Handler = "RemoteDdbProxyFunction::RemoteDdbProxyFunction.Function::FunctionHandler",
                Environment = new Dictionary<string, string>
                {
                    // Step0 で作った SecretsManager の ARN
                    ["REMOTE_DDB_SECRET_ARN"] = remoteDdbSecretArn.ValueAsString
                }
            });

            // Secrets Manager を読む権限を付与
            // using Amazon.CDK.AWS.SecretsManager;
            var secret = Amazon.CDK.AWS.SecretsManager.Secret.FromSecretCompleteArn(
                this, "RemoteDdbSecret", remoteDdbSecretArn.ValueAsString);

            secret.GrantRead(remoteDdbFn);

            // /remote-ddb/{pk} GET 追加
            var remoteRoot = api.Root.AddResource("remote-ddb");
            var remoteByPk = remoteRoot.AddResource("{pk}");
            remoteByPk.AddMethod("GET", new LambdaIntegration(remoteDdbFn));
        }
    }
}
