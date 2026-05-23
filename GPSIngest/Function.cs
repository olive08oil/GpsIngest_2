using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GPSIngest.Models;
using GPSIngest.Parsers;   // ★ TelemetryParser 用
using System.Text;
using System.Text.Json;

// Lambda シリアライザ（System.Text.Json）
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GPSIngest;

public class Function
{
    private static readonly string? HistoryTableName = Environment.GetEnvironmentVariable("HISTORY_TABLE_NAME");
    private static readonly string? LatestTableName = Environment.GetEnvironmentVariable("LATEST_TABLE_NAME");

    private readonly AmazonDynamoDBClient _ddb = new();
    private Table? _history;
    private Table? _latest;

    public Function() { }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // LOG: 受信メタ
        context.Logger.LogLine($"REQ: path={request.Path} method={request.HttpMethod} bodyLen={(request.Body?.Length ?? 0)}");

        if (string.IsNullOrWhiteSpace(HistoryTableName) || string.IsNullOrWhiteSpace(LatestTableName))
        {
            context.Logger.LogLine($"ENV MISSING: HISTORY_TABLE_NAME={HistoryTableName}, LATEST_TABLE_NAME={LatestTableName}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { ok = false, error = "env var missing: HISTORY_TABLE_NAME / LATEST_TABLE_NAME" }),
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            };
        }

        try
        {
            if (_history is null) _history = Table.LoadTable(_ddb, HistoryTableName!);
            if (_latest is null) _latest = Table.LoadTable(_ddb, LatestTableName!);

            if (string.IsNullOrWhiteSpace(request.Body))
            {
                context.Logger.LogLine("BADREQ: empty body");
                return BadRequest("empty body");
            }

            // LOG: リクエストボディ先頭だけ（最大500文字）
            context.Logger.LogLine("REQ.BODY(<=500): " + SafeTruncate(request.Body, 500));

            var ingest = JsonSerializer.Deserialize<IngestRequest>(request.Body, IngestRequest.JsonOptions);
            if (ingest is null) { context.Logger.LogLine("BADREQ: invalid json"); return BadRequest("invalid json"); }
            if (string.IsNullOrWhiteSpace(ingest.DeviceId)) { context.Logger.LogLine("BADREQ: deviceId required"); return BadRequest("deviceId required"); }
            if (ingest.Ts == 0) { context.Logger.LogLine("BADREQ: ts required"); return BadRequest("ts required (epochMillis)"); }
            if (string.IsNullOrWhiteSpace(ingest.PayloadType)) { context.Logger.LogLine("BADREQ: payloadType required"); return BadRequest("payloadType required"); }

            context.Logger.LogLine($"INGEST: deviceId={ingest.DeviceId} ts={ingest.Ts} type={ingest.PayloadType}");

            // 1) 解析 → 正規化
            NormalizedTelemetry norm;
            try
            {
                norm = await ParseAndNormalizeAsync(ingest);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("PARSE_ERROR: " + ex);
                return BadRequest("parse failed: " + ex.Message);
            }

            // LOG: 正規化結果の概況
            context.Logger.LogLine(
                $"NORM: lat={norm.Lat?.ToString() ?? "-"} " +
                $"lon={norm.Lon?.ToString() ?? "-"} " +
                $"speed={norm.Speed?.ToString() ?? "-"} " +
                $"heading={norm.Heading?.ToString() ?? "-"} " +
                $"speed_A3={norm.Speed_A3?.ToString() ?? "-"} " +
                $"heading_A3={norm.Heading_A3?.ToString() ?? "-"} " +
                $"odo_total_m={norm.OdoTotalMeters?.ToString() ?? "-"} " +
                $"src={norm.Source}"
            );

            // 2) Δ計算のため最新レコード取得（ID75/NMEA の speed/heading 用）
            var prevLatest = await _latest!.GetItemAsync(ingest.DeviceId);

            double? prevSpeed = null;
            double? prevHeading = null;

            if (prevLatest != null)
            {
                if (prevLatest.ContainsKey("speed"))
                    prevSpeed = prevLatest["speed"].AsNullableDouble();

                if (prevLatest.ContainsKey("heading"))
                    prevHeading = prevLatest["heading"].AsNullableDouble();
            }

            context.Logger.LogLine(
                $"LATEST.PREV: hasPrev={(prevLatest != null)} " +
                $"prevSpeed={(prevSpeed?.ToString() ?? "-")} " +
                $"prevHeading={(prevHeading?.ToString() ?? "-")}"
            );

            if (norm.Speed is not null && prevSpeed is not null)
                norm.SpeedDelta = norm.Speed.Value - prevSpeed.Value;
            if (norm.Heading is not null && prevHeading is not null)
                norm.HeadingDelta = HeadingDelta(norm.Heading.Value, prevHeading.Value);

            context.Logger.LogLine($"DELTA: speedDelta={(norm.SpeedDelta?.ToString() ?? "-")} headingDelta={(norm.HeadingDelta?.ToString() ?? "-")}");

            // 3) 履歴に保存（全項目）
            var historyDoc = BuildHistoryDocument(ingest, norm);
            try
            {
                await _history!.PutItemAsync(historyDoc);
                context.Logger.LogLine($"HISTORY.PUT: ok deviceId={ingest.DeviceId} ts={ingest.Ts}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("HISTORY_PUT_ERROR: " + ex);
                return BadRequest("history put failed: " + ex.Message);
            }

            // 4) 最新スナップショット 条件付き更新（ts が新しい時だけ）
            var latestDoc = BuildLatestDocument(ingest, norm);

            try
            {
                // ★ 条件付き更新をやめて常に上書き
                await _latest!.PutItemAsync(latestDoc);
                context.Logger.LogLine($"LATEST.PUT: ok deviceId={ingest.DeviceId} ts={norm.Ts}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("LATEST_PUT_ERROR: " + ex);
                return BadRequest("latest put failed: " + ex.Message);
            }

            /*var latestDoc = BuildLatestDocument(ingest, norm);
            var cfg = new PutItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionStatement = "attribute_not_exists(ts) OR :newTs > ts",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        [":newTs"] = new Primitive(norm.Ts.ToString())
                    }
                }
            };

            try
            {
                await _latest!.PutItemAsync(latestDoc, cfg);
                context.Logger.LogLine($"LATEST.PUT: ok deviceId={ingest.DeviceId} ts={norm.Ts}");
            }
            catch (ConditionalCheckFailedException)
            {
                // 古いデータ → 既知ケースなので無視
                context.Logger.LogLine($"LATEST.PUT: skipped (older ts) deviceId={ingest.DeviceId} ts={norm.Ts}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("LATEST_PUT_ERROR: " + ex);
                return BadRequest("latest put failed: " + ex.Message);
            }*/

            // 成功レスポンス
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new
                {
                    ok = true,
                    deviceId = ingest.DeviceId,
                    ts = ingest.Ts
                }),
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogLine("UNHANDLED: " + ex);
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { ok = false, error = ex.Message }),
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            };
        }
    }

    // ---------- 解析/正規化 ----------
    private static async Task<NormalizedTelemetry> ParseAndNormalizeAsync(IngestRequest ingest)
    {
        var norm = new NormalizedTelemetry
        {
            DeviceId = ingest.DeviceId!,
            Ts = ingest.Ts,
            Source = ingest.PayloadType!
        };

        var type = ingest.PayloadType!.ToUpperInvariant();

        switch (type)
        {
            case "ID75":
            case "IDA3":
                {
                    if (string.IsNullOrWhiteSpace(ingest.Raw))
                        throw new ArgumentException("raw(base64) required for ID75/IDA3");

                    // TelemetryParser に一本化
                    var parsed = TelemetryParser.Parse(type, ingest.Raw!);
                    if (parsed is null)
                        throw new ArgumentException("failed to parse ID75/IDA3 payload");

                    if (type == "ID75")
                    {
                        // 位置・代表速度/方位は ID75 を採用
                        norm.Lat = parsed.Lat;
                        norm.Lon = parsed.Lon;
                        norm.Speed = parsed.SpeedKmh_75;
                        norm.Heading = parsed.Heading_75;
                    }

                    if (type == "IDA3")
                    {
                        // IDA3 のセンサ速度/方位/ODO は別名で保持
                        norm.Speed_A3 = parsed.SpeedKmh_A3;
                        norm.Heading_A3 = parsed.Heading_A3;
                        norm.OdoTotalMeters = parsed.OdoTotalMeters;
                    }

                    // SourceMap / StRaw 等は今回は利用しないので null のまま
                    break;
                }

            case "NMEA":
                {
                    if (string.IsNullOrWhiteSpace(ingest.Raw))
                        throw new ArgumentException("raw(nmea sentence) required for NMEA");

                    var nmea = NmeaParser.Parse(ingest.Raw!);       // RMC/GGA/VTG等を自動判定
                    norm.Source = $"NMEA:{nmea.Type}";
                    norm.Lat = nmea.Normalized.Lat;
                    norm.Lon = nmea.Normalized.Lon;
                    norm.Speed = nmea.Normalized.SpeedKmh;          // knots→km/h換算済み
                    norm.Heading = nmea.Normalized.HeadingDeg;

                    norm.SourceMap = nmea.SourceMap;                // nmea.raw, fieldsなど
                    break;
                }

            default:
                throw new ArgumentException($"unsupported payloadType: {ingest.PayloadType}");
        }

        return await Task.FromResult(norm);
    }

    // ---------- 履歴ドキュメント ----------
    private static Document BuildHistoryDocument(IngestRequest ingest, NormalizedTelemetry n)
    {
        var d = new Document
        {
            ["pk"] = ingest.DeviceId,
            ["sk"] = n.Ts,
            ["deviceId"] = ingest.DeviceId,
            ["ts"] = n.Ts,
            ["msgType"] = n.Source!.Contains("NMEA") ? "NMEA" : n.Source,
            ["source"] = n.Source,
            ["ingestedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 頻繁アクセス系（ID75/NMEA）
        if (n.Lat is not null) d["lat"] = n.Lat;
        if (n.Lon is not null) d["lon"] = n.Lon;
        if (n.Speed is not null) d["speed"] = n.Speed;
        if (n.Heading is not null) d["heading"] = n.Heading;
        if (n.SpeedDelta is not null) d["speedDelta"] = n.SpeedDelta;
        if (n.HeadingDelta is not null) d["headingDelta"] = n.HeadingDelta;
        if (n.AccelX is not null) d["accelX"] = n.AccelX;
        if (n.AccelY is not null) d["accelY"] = n.AccelY;

        // IDA3 由来の追加項目
        if (n.Speed_A3 is not null) d["speed_A3"] = n.Speed_A3;
        if (n.Heading_A3 is not null) d["heading_A3"] = n.Heading_A3;
        if (n.OdoTotalMeters is not null) d["odo_total_m"] = n.OdoTotalMeters;

        // ソース固有（NMEA のみ使用）
        if (n.SourceMap is not null)
        {
            var key = n.Source!.StartsWith("NMEA") ? "nmea" : n.Source!.ToLowerInvariant();
            d[key] = Document.FromJson(JsonSerializer.Serialize(n.SourceMap));
        }

        if (n.SourceStRaw is not null)
        {
            var key = $"{(n.Source!.StartsWith("NMEA") ? "nmea" : n.Source!.ToLowerInvariant())}_st_raw";
            d[key] = n.SourceStRaw.Value;
        }

        if (n.SourceStFlags is not null)
        {
            var key = $"{(n.Source!.StartsWith("NMEA") ? "nmea" : n.Source!.ToLowerInvariant())}_st_flags";
            d[key] = Document.FromJson(JsonSerializer.Serialize(n.SourceStFlags));
        }

        // TTL（必要なら）
        if (ingest.TtlEpochSeconds is not null)
            d["ttl"] = ingest.TtlEpochSeconds.Value;

        return d;
    }

    // ---------- 最新スナップショット ----------
    private static Document BuildLatestDocument(IngestRequest ingest, NormalizedTelemetry n)
    {
        var d = new Document
        {
            ["deviceId"] = ingest.DeviceId,
            ["ts"] = n.Ts
        };

        // 位置・代表速度（ID75/NMEA）
        if (n.Lat is not null) d["lat"] = n.Lat;
        if (n.Lon is not null) d["lon"] = n.Lon;
        if (n.Speed is not null) d["speed"] = n.Speed;
        if (n.Heading is not null) d["heading"] = n.Heading;
        if (n.SpeedDelta is not null) d["speedDelta"] = n.SpeedDelta;
        if (n.HeadingDelta is not null) d["headingDelta"] = n.HeadingDelta;
        if (n.AccelX is not null) d["accelX"] = n.AccelX;
        if (n.AccelY is not null) d["accelY"] = n.AccelY;

        // IDA3 追加項目
        if (n.Speed_A3 is not null) d["speed_A3"] = n.Speed_A3;
        if (n.Heading_A3 is not null) d["heading_A3"] = n.Heading_A3;
        if (n.OdoTotalMeters is not null) d["odo_total_m"] = n.OdoTotalMeters;

        if (!string.IsNullOrWhiteSpace(n.Source)) d["source"] = n.Source;

        return d;
    }

    private static double HeadingDelta(double cur, double prev)
    {
        // ±180°の範囲に折り返し
        var diff = cur - prev;
        diff = (diff + 540.0) % 360.0 - 180.0;
        return diff;
    }

    private static APIGatewayProxyResponse BadRequest(string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 400,
            Body = JsonSerializer.Serialize(new { ok = false, error = message }),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };
    }

    // LOG: 文字列の先頭だけを安全に出す
    private static string SafeTruncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "...(truncated)";
    }
}

// 小さな拡張：null安全に double を読む
internal static class DdbExt
{
    public static double? AsNullableDouble(this DynamoDBEntry e)
    {
        try
        {
            if (e is null) return null;
            var p = e.AsPrimitive();
            if (p?.Value is null) return null;
            if (double.TryParse(p.Value.ToString(), out var v)) return v;
            return null;
        }
        catch { return null; }
    }
}
