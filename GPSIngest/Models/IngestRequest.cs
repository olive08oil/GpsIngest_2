using System.Text.Json;
using System.Text.Json.Serialization;

namespace GPSIngest.Models;

public sealed class IngestRequest
{
    [JsonPropertyName("deviceId")] public string? DeviceId { get; set; }
    [JsonPropertyName("ts")] public long Ts { get; set; } // epochMillis
    [JsonPropertyName("payloadType")] public string? PayloadType { get; set; } // "ID75" | "IDA3" | "NMEA"
    [JsonPropertyName("raw")] public string? Raw { get; set; } // IDxx=base64, NMEA=sentence
    [JsonPropertyName("nmeaType")] public string? NmeaType { get; set; }    // 任意（サーバ側で自動判定可）
    [JsonPropertyName("ttl")] public long? TtlEpochSeconds { get; set; } // 任意

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}

/*
public sealed class IngestRequest
{
    public string deviceId { get; set; } = default!;
    public long ts { get; set; }                 // UTC (ms)
    public string payloadType { get; set; } = ""; // "ID75" or "IDA3" or "NMEA" など
    public string raw { get; set; } = "";         // Base64（<DLE><ID>...<CS><DLE><ETX>）
}
*/