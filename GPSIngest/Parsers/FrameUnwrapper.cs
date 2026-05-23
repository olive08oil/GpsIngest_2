using System;

namespace GPSIngest.Parsers
{
    public static class FrameUnwrapper
    {
        private const byte DLE = 0x10;
        private const byte ETX = 0x03;

        /// <summary>
        /// <DLE><ID> ... <CS><DLE><ETX> を受け取り、
        ///   1) DLE/ID を除去
        ///   2) 末尾の <CS><DLE><ETX> を除去
        /// → ペイロード (ST からの本体) を返す
        /// </summary>
        public static ReadOnlySpan<byte> Unwrap(ReadOnlySpan<byte> frame)
        {
            if (frame.Length < 6)
                return ReadOnlySpan<byte>.Empty;

            // 10 xx .... cs 10 03
            if (frame[0] != DLE || frame[^2] != DLE || frame[^1] != ETX)
                return ReadOnlySpan<byte>.Empty;

            // ID = frame[1]
            int payloadLen = frame.Length - 2 - 3; // 先頭2Bと末尾3Bを引く
            return frame.Slice(2, payloadLen);
        }
    }
}
