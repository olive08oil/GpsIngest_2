using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

// Lambda のシリアライザ
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SingularForwarderFunction;

// DynamoDB Streams（Lambda イベント）側の AttributeValue 型にエイリアス
using StreamAttr = Amazon.Lambda.DynamoDBEvents.DynamoDBEvent.AttributeValue;

public class Function
{
    private static readonly HttpClient Http = new HttpClient();
    private static string? _privateToken;  // Secrets Manager から取得した Singular Private Token をキャッシュ

    public async Task FunctionHandler(DynamoDBEvent ev, ILambdaContext ctx)
    {
        // ===== 1) Singular Private Token を Secrets Manager から lazy ロード（生文字列 / JSON 両対応）=====
        if (_privateToken is null)
        {
            var secretArn = Environment.GetEnvironmentVariable("SINGULAR_SECRET_ARN")
                            ?? throw new Exception("SINGULAR_SECRET_ARN is not set.");
            using var sm = new AmazonSecretsManagerClient();
            var resp = await sm.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretArn });
            var raw = resp.SecretString ?? throw new Exception("SecretString is null.");

            // 値が {"token":"..."} 形式なら token を抜き出す。生文字列ならそのまま。
            try
            {
                var node = JsonNode.Parse(raw);
                _privateToken = node?["token"]?.GetValue<string>() ?? raw;
            }
            catch
            {
                _privateToken = raw;
            }

            ctx.Logger.LogInformation("Singular token loaded."); // 値本体は出さない
        }

        // 受信レコード数の観測ログ
        var count = ev.Records?.Count ?? 0;
        ctx.Logger.LogInformation($"DynamoDB stream records: {count}");
        if (count == 0) return;

        foreach (var rec in ev.Records)
        {
            // SDK により EventName は string のことがあるため、文字列比較で判定
            if (!string.Equals(rec.EventName, "INSERT", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(rec.EventName, "MODIFY", StringComparison.OrdinalIgnoreCase))
            {
                // 必要なら DELETE などはスキップ
                continue;
            }

            var newImg = rec.Dynamodb.NewImage;
            if (newImg is null) continue;

            var deviceId = GetString(newImg, "deviceId", "unknown");
            long newTs = GetLong(newImg, "ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            long oldTs = rec.Dynamodb.OldImage is null ? long.MinValue : GetLong(rec.Dynamodb.OldImage, "ts", long.MinValue);

            // 古い更新（時間が巻き戻る）を防ぐ
            if (newTs < oldTs)
            {
                ctx.Logger.LogInformation($"Skip older image: deviceId={deviceId} newTs={newTs} < oldTs={oldTs}");
                continue;
            }

            // ★★★ ここがポイント：DeviceLatest の speed / speed_A3 を読む ★★★
            double? speedKmh = null;

            // まず ID75 由来の speed を見る
            var primarySpeed = GetNullableDouble(newImg, "speed");
            if (primarySpeed.HasValue)
            {
                speedKmh = primarySpeed;
            }
            else
            {
                // なければ IDA3 由来の speed_A3 を代わりに使う
                var a3Speed = GetNullableDouble(newImg, "speed_A3");
                if (a3Speed.HasValue)
                    speedKmh = a3Speed;
            }

            // ペイロード作成（Singular 側は任意キーOK）
            var payload = new
            {
                deviceId,
                lat = GetDouble(newImg, "lat", double.NaN),
                lon = GetDouble(newImg, "lon", double.NaN),

                // ★ Singular 側のフィールド名は従来通り speedKmh のまま
                speedKmh = speedKmh,

                ts = newTs
            };

            ctx.Logger.LogInformation(
                $"Forwarding to Singular: deviceId={deviceId}, ts={newTs}, " +
                $"lat={payload.lat}, lon={payload.lon}, speedKmh={(speedKmh?.ToString() ?? "-")}"
            );

            var ok = await PutToSingularAsync(_privateToken!, payload, ctx);
            if (ok)
            {
                ctx.Logger.LogInformation("Singular PUT success");
            }
            else
            {
                ctx.Logger.LogError($"Singular PUT failed for deviceId={deviceId}, ts={newTs}");
            }
        }
    }

    // ===== Singular 送信（簡易リトライ＋HTTPコードをログ出力）=====
    private static async Task<bool> PutToSingularAsync(string privateToken, object payload, ILambdaContext ctx)
    {
        var url = $"https://datastream.singular.live/datastreams/{privateToken}";
        var json = JsonSerializer.Serialize(payload);
        var delays = new[] { 0, 250, 750 }; // ms

        for (int i = 0; i < delays.Length; i++)
        {
            if (delays[i] > 0) await Task.Delay(delays[i]);
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Put, url)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };

                using var res = await Http.SendAsync(req);
                var code = (int)res.StatusCode;
                ctx.Logger.LogInformation($"Singular PUT attempt {i + 1}: HTTP {code}");

                if (res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.NoContent)
                    return true;
            }
            catch (Exception ex)
            {
                ctx.Logger.LogWarning($"Singular PUT error (attempt {i + 1}): {ex.Message}");
            }
        }
        return false;
    }

    // ===== DynamoDB AttributeValue ユーティリティ（Lambda Streams 用）=====
    private static string GetString(Dictionary<string, StreamAttr> map, string key, string fallback)
        => map.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.S) ? v.S : fallback;

    private static double GetDouble(Dictionary<string, StreamAttr> map, string key, double fallback)
    {
        if (!map.TryGetValue(key, out var v)) return fallback;
        if (!string.IsNullOrEmpty(v.N) &&
            double.TryParse(v.N, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d)) return d;
        if (!string.IsNullOrEmpty(v.S) &&
            double.TryParse(v.S, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var ds)) return ds;
        return fallback;
    }

    private static double? GetNullableDouble(Dictionary<string, StreamAttr> map, string key)
    {
        if (!map.TryGetValue(key, out var v)) return null;
        if (!string.IsNullOrEmpty(v.N) &&
            double.TryParse(v.N, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d)) return d;
        if (!string.IsNullOrEmpty(v.S) &&
            double.TryParse(v.S, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var ds)) return ds;
        return null;
    }

    private static long GetLong(Dictionary<string, StreamAttr> map, string key, long fallback)
    {
        if (!map.TryGetValue(key, out var v)) return fallback;
        if (!string.IsNullOrEmpty(v.N) && long.TryParse(v.N, out var l)) return l;
        if (!string.IsNullOrEmpty(v.S) && long.TryParse(v.S, out var ls)) return ls;
        return fallback;
    }
}
