using System.Globalization;

namespace GPSIngest.Models;

public static class NmeaParser
{
    public sealed class Result
    {
        public string Type { get; set; } = "UNK";
        public (double? Lat, double? Lon, double? SpeedKmh, double? HeadingDeg) Normalized { get; set; }
        public object SourceMap { get; set; } = new { raw = "", fields = Array.Empty<string>() };
    }

    public static Result Parse(string sentence)
    {
        // 例: $GPRMC,hhmmss.sss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,ddmmyy,x.x,a*hh
        var res = new Result
        {
            SourceMap = new { raw = sentence, fields = sentence.Split(',') }
        };

        if (!sentence.StartsWith("$"))
            return res;

        var type = sentence.Substring(3, 3).ToUpperInvariant(); // RMC, VTG, GGA など
        res.Type = type;

        var f = sentence.Split(',');
        switch (type)
        {
            case "RMC":
                // f[2]=A 有効 / V 無効, f[3]=lat, f[4]=N/S, f[5]=lon, f[6]=E/W
                // f[7]=speed(knots), f[8]=cog(deg)
                var lat = ParseLat(f.ElementAtOrDefault(3), f.ElementAtOrDefault(4));
                var lon = ParseLon(f.ElementAtOrDefault(5), f.ElementAtOrDefault(6));
                var spk = ParseDouble(f.ElementAtOrDefault(7));
                var cog = ParseDouble(f.ElementAtOrDefault(8));
                res.Normalized = (lat, lon, KnotsToKmh(spk), cog);
                return res;

            case "VTG":
                // f[1]=cog(T), f[5]=speed(knots)
                var heading = ParseDouble(f.ElementAtOrDefault(1));
                var vtgKnots = ParseDouble(f.ElementAtOrDefault(5));
                res.Normalized = (null, null, KnotsToKmh(vtgKnots), heading);
                return res;

                // 必要なら GGA/GSA/GSV など拡張
        }

        return res;
    }

    private static double? ParseLat(string? lat, string? hemi)
    {
        // ddmm.mmmm → dd + (mm.mmmm/60)
        if (string.IsNullOrWhiteSpace(lat)) return null;
        if (!double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return null;
        var deg = Math.Floor(v / 100);
        var min = v - deg * 100;
        var val = deg + (min / 60.0);
        if (string.Equals(hemi, "S", StringComparison.OrdinalIgnoreCase)) val = -val;
        return val;
    }

    private static double? ParseLon(string? lon, string? hemi)
    {
        if (string.IsNullOrWhiteSpace(lon)) return null;
        if (!double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return null;
        var deg = Math.Floor(v / 100);
        var min = v - deg * 100;
        var val = deg + (min / 60.0);
        if (string.Equals(hemi, "W", StringComparison.OrdinalIgnoreCase)) val = -val;
        return val;
    }

    private static double? ParseDouble(string? s)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    private static double? KnotsToKmh(double? knots)
        => knots is null ? null : knots.Value * 1.852;
}
