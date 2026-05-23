using System.Text;

namespace GPSIngest.Models;

public static class IdxxParser
{
    public sealed class Parsed
    {
        public (double? Lat, double? Lon, double? SpeedKmh, double? HeadingDeg, double? AccelX, double? AccelY) Normalized { get; set; }
        public object SourceMap { get; set; } = new { }; // 全フィールド（id75/ida3）
        public long? StRaw { get; set; }
        public Dictionary<string, bool>? StFlags { get; set; }
    }

    // 例：STビット名（仮。実仕様に置き換えてください）
    private static readonly string[] ST_ID75 = new[]
    {
        "GNSS_FIX_OK","DGPS","RTK","IMU_CAL_OK","WHEEL_TICK_OK","RESERVED5","RESERVED6","RESERVED7",
        // 必要に応じてbit8以降も…
    };

    private static readonly string[] ST_IDA3 = new[]
    {
        "GNSS_FIX_OK","DIFF_APPLIED","RTK_FLOAT","RTK_FIXED","IMU_CAL_OK","WHEEL_TICK_OK","RES6","RES7",
        // …
    };

    public static Parsed Parse(byte[] raw, string type /* "ID75"|"IDA3" */)
    {
        // ▼▼ ここに実仕様を反映させる ▼▼
        // 以下は“例”。バイトオフセット、符号/スケールはあなたの仕様で置換してください
        var span = raw.AsSpan();

        // 例：緯度経度（int32: 1e-7 deg）、速度（uint16: 0.01 m/s）、方位（uint16: 0.1 deg）
        double? lat = null, lon = null, speedKmh = null, headingDeg = null, ax = null, ay = null;
        long? stRaw = null;
        try
        {
            // 例: 0..3:lat, 4..7:lon, 8..9:speed, 10..11:heading, 12..15:accX(int16*0.01), 16..17:accY, 18:ST(1byte)
            if (span.Length >= 8)
            {
                var lat_i = BitConverter.ToInt32(span.Slice(0, 4));
                var lon_i = BitConverter.ToInt32(span.Slice(4, 4));
                lat = lat_i / 1e7;
                lon = lon_i / 1e7;
            }
            if (span.Length >= 12)
            {
                var sp_u = BitConverter.ToUInt16(span.Slice(8, 2));
                // m/s→km/h
                speedKmh = sp_u * 0.01 * 3.6;
            }
            if (span.Length >= 14)
            {
                var hd_u = BitConverter.ToUInt16(span.Slice(10, 2));
                headingDeg = hd_u * 0.1;
            }
            if (span.Length >= 18)
            {
                var ax_i = BitConverter.ToInt16(span.Slice(12, 2));
                var ay_i = BitConverter.ToInt16(span.Slice(14, 2));
                ax = ax_i * 0.01; // m/s^2 仮
                ay = ay_i * 0.01;
            }
            if (span.Length >= 19)
            {
                stRaw = span[18];
            }
        }
        catch
        {
            // フォーマット不正はそのまま null で返す（上位で扱う）
        }

        // STフラグ展開
        Dictionary<string, bool>? stFlags = null;
        if (stRaw is not null)
        {
            var names = type == "ID75" ? ST_ID75 : ST_IDA3;
            stFlags = new Dictionary<string, bool>();
            for (int i = 0; i < Math.Min(names.Length, 64); i++)
            {
                bool bit = ((stRaw.Value >> i) & 0x1) == 1;
                stFlags[names[i]] = bit;
            }
        }

        // SourceMap には「すべての素の値」を詰める（GUIや検証に便利）
        var srcKey = type.ToLowerInvariant();
        var sourceMap = new Dictionary<string, object?>
        {
            ["version"] = null, // 仕様にあれば詰める
            ["seq"] = null,
            ["raw_len"] = raw.Length,
            ["st_raw"] = stRaw,
            ["fields"] = new
            {
                lat,
                lon,
                speedKmh,
                headingDeg,
                accX = ax,
                accY = ay
            }
        };

        return new Parsed
        {
            Normalized = (lat, lon, speedKmh, headingDeg, ax, ay),
            SourceMap = new Dictionary<string, object?> { [srcKey] = sourceMap },
            StRaw = stRaw,
            StFlags = stFlags
        };
    }
}
