namespace GPSIngest.Models
{
    public class NormalizedTelemetry
    {
        public string DeviceId { get; set; } = default!;
        public long Ts { get; set; }

        // ---- ID75（位置付き） ----
        public double? Lat { get; set; }
        public double? Lon { get; set; }

        // ---- 共通速度 / 進行方向 ----
        public double? Speed { get; set; }
        public double? Heading { get; set; }

        // ---- 前回差分（Latest 用）----
        public double? SpeedDelta { get; set; }
        public double? HeadingDelta { get; set; }

        // ---- 加速度（必要なら） ----
        public double? AccelX { get; set; }
        public double? AccelY { get; set; }

        // ---- IDA3（位置なし） ----
        // ★ ここが今回追加するプロパティ
        public double? Speed_A3 { get; set; }
        public double? Heading_A3 { get; set; }
        public long? OdoTotalMeters { get; set; }

        // ---- 解析ソース情報 ----
        public string Source { get; set; } = default!;
        public object? SourceMap { get; set; }
        public long? SourceStRaw { get; set; }
        public object? SourceStFlags { get; set; }



    }
}
