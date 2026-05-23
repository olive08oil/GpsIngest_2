using System;

namespace GPSIngest.Parsers
{
    public sealed class TelemetryParsed
    {
        // 位置（ID75）
        public double? Lat { get; set; }           // 度, +北, -南
        public double? Lon { get; set; }           // 度, +東, -西

        // ID75 の速度/進行方向（メインとして使う値）
        public double? SpeedKmh_75 { get; set; }   // 0.1km/h 単位
        public double? Heading_75 { get; set; }    // 360/65536 度

        // IDA3 のセンサー速度/方位/積算距離
        public double? SpeedKmh_A3 { get; set; }   // 0.1km/h 単位
        public double? Heading_A3 { get; set; }    // 360/65536 度
        public long? OdoTotalMeters { get; set; } // 1m 単位
    }

    public static class TelemetryParser
    {
        private const byte DLE = 0x10;
        private const byte ID_ID75 = 0x75;
        private const byte ID_IDA3 = 0xA3;

        /// <summary>
        /// payloadType: "ID75" または "IDA3"
        /// rawBase64 : WindowsForms クライアントから来る raw (Base64)
        ///   ・10 75 ... CS 10 03 のような DLE フレーム付きでも
        ///   ・ST から始まるデータだけでも
        /// どちらでも動くようにしています。
        /// </summary>
        public static TelemetryParsed? Parse(string payloadType, string rawBase64)
        {
            if (string.IsNullOrWhiteSpace(rawBase64)) return null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(rawBase64);
            }
            catch
            {
                return null;
            }

            var span = new ReadOnlySpan<byte>(bytes);

            // 先頭が DLE なら 10 75 / 10 A3 を想定して ST 位置をずらす
            int ofs = 0;
            if (span.Length >= 2 && span[0] == DLE)
            {
                ofs = 2; // [0]=10, [1]=ID → ST は [2]
            }

            payloadType = payloadType.ToUpperInvariant();

            if (payloadType == "ID75")
            {
                return DecodeId75(span.Slice(ofs));
            }
            else if (payloadType == "IDA3")
            {
                return DecodeIdA3(span.Slice(ofs));
            }

            return null;
        }

        // ----- ID75: 10 75 ST 緯度 経度 高度 速度 方位 ... -----
        // マニュアル I-4 ID75出力フォーマット:
        // 緯度/経度 : 秒/256 単位, 32bit 符号付き, ビッグエンディアン【:contentReference[oaicite:0]{index=0}】
        // 速度     : 0.1km/h 単位, 16bit, ビッグエンディアン【:contentReference[oaicite:1]{index=1}】
        // 方位     : 360/65536 度単位, 16bit, ビッグエンディアン【:contentReference[oaicite:2]{index=2}】
        private static TelemetryParsed DecodeId75(ReadOnlySpan<byte> data)
        {
            var res = new TelemetryParsed();
            int ofs = 0;

            if (data.Length < 1 + 4 + 4 + 2 + 2 + 2)
                return res; // 安全のため

            // ST
            byte st = data[ofs];
            ofs += 1;

            // 緯度 (4B, big endian, 秒/256, 補数)
            int rawLat = ReadInt32BigEndian(data, ofs);
            ofs += 4;
            double latSeconds = rawLat / 256.0;
            res.Lat = latSeconds / 3600.0; // 度

            // 経度 (4B, big endian, 秒/256, 補数)
            int rawLon = ReadInt32BigEndian(data, ofs);
            ofs += 4;
            double lonSeconds = rawLon / 256.0;
            res.Lon = lonSeconds / 3600.0; // 度

            // 高度 (2B, big endian, m). ここでは利用しないがオフセットだけ進める
            short altRaw = ReadInt16BigEndian(data, ofs);
            ofs += 2;
            _ = altRaw;

            // 速度 (2B, big endian, 0.1km/h)
            ushort spRaw = ReadUInt16BigEndian(data, ofs);
            ofs += 2;
            res.SpeedKmh_75 = spRaw / 10.0;

            // 方位 (2B, big endian, 360/65536 度)
            ushort hdRaw = ReadUInt16BigEndian(data, ofs);
            ofs += 2;
            res.Heading_75 = hdRaw * 360.0 / 65536.0;

            return res;
        }

        // ----- IDA3: 10 A3 ST 速度 方位 オドメータ ... -----
        // マニュアル I-7 IDA3出力フォーマット:
        // 速度      : 0.1km/h 単位, 16bit, ビッグエンディアン【:contentReference[oaicite:3]{index=3}】
        // 方位      : 360/65536 度単位, 16bit, ビッグエンディアン【:contentReference[oaicite:4]{index=4}】
        // オドメータ: 1m 単位, 32bit, ビッグエンディアン【:contentReference[oaicite:5]{index=5}】
        private static TelemetryParsed DecodeIdA3(ReadOnlySpan<byte> data)
        {
            var res = new TelemetryParsed();
            int ofs = 0;

            if (data.Length < 1 + 2 + 2 + 4)
                return res;

            // ST
            byte st = data[ofs];
            ofs += 1;
            _ = st;

            // センサー速度
            ushort spRaw = ReadUInt16BigEndian(data, ofs);
            ofs += 2;
            res.SpeedKmh_A3 = spRaw / 10.0;

            // センサー方位
            ushort hdRaw = ReadUInt16BigEndian(data, ofs);
            ofs += 2;
            res.Heading_A3 = hdRaw * 360.0 / 65536.0;

            // 積算距離
            uint odoRaw = ReadUInt32BigEndian(data, ofs);
            ofs += 4;
            res.OdoTotalMeters = odoRaw;

            return res;
        }

        // ----- ヘルパ -----

        private static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static short ReadInt16BigEndian(ReadOnlySpan<byte> data, int offset)
        {
            return (short)ReadUInt16BigEndian(data, offset);
        }

        private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset)
        {
            return (uint)(
                (data[offset] << 24) |
                (data[offset + 1] << 16) |
                (data[offset + 2] << 8) |
                data[offset + 3]);
        }

        private static int ReadInt32BigEndian(ReadOnlySpan<byte> data, int offset)
        {
            return unchecked((int)ReadUInt32BigEndian(data, offset));
        }
    }
}
