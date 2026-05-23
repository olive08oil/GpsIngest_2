using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GpsIngestClinetForm
{
    public enum ReceiveMode
    {
        SerialNmea,
        SerialBinaryFixed,   // 固定長フレーム
        SerialPioneerAuto,   // <DLE><ID>...<CS><DLE><ETX> を自動判別(ID75/IDA3)
        UdpNmea,
        UdpBinaryDatagram,   // そのまま1パケット=1電文
        UdpPioneerAuto       // UDPでも自動判別(ID75/IDA3)
    }

    public sealed class ReceiverService : IAsyncDisposable, IDisposable
    {
        // 通知イベント
        public event Action<string>? NmeaLineReceived;
        public event Action<byte[], string /*payloadType*/>? BinaryFrameReceived;
        public event Action<string>? Info;
        public event Action<string>? Error;

        private CancellationTokenSource? _cts;
        private Task? _worker;
        private SerialPort? _serial;
        private UdpClient? _udp;

        private byte[] _binBuf = Array.Empty<byte>(); // Serial 固定長用

        public void StartSerialNmea(string portName, int baud, bool normalizeCrLf)
            => StartCore(ReceiveMode.SerialNmea, portName, baud, 0, 0, normalizeCrLf);

        public void StartSerialBinaryFixed(string portName, int baud, int frameLen)
            => StartCore(ReceiveMode.SerialBinaryFixed, portName, baud, 0, frameLen, false);

        public void StartSerialPioneerAuto(string portName, int baud)
            => StartCore(ReceiveMode.SerialPioneerAuto, portName, baud, 0, 0, false);

        public void StartUdpNmea(int port)
            => StartCore(ReceiveMode.UdpNmea, null, 0, port, 0, false);

        public void StartUdpBinaryDatagram(int port)
            => StartCore(ReceiveMode.UdpBinaryDatagram, null, 0, port, 0, false);

        public void StartUdpPioneerAuto(int port)
            => StartCore(ReceiveMode.UdpPioneerAuto, null, 0, port, 0, false);

        private void StartCore(ReceiveMode mode, string? portName, int baud, int udpPort, int frameLen, bool normalizeCrLf)
        {
            if (_worker != null) throw new InvalidOperationException("Receiver already running.");
            _cts = new CancellationTokenSource();

            _worker = Task.Run(async () =>
            {
                try
                {
                    switch (mode)
                    {
                        case ReceiveMode.SerialNmea:
                            await RunSerialNmeaAsync(portName!, baud, normalizeCrLf, _cts.Token);
                            break;
                        case ReceiveMode.SerialBinaryFixed:
                            await RunSerialBinaryFixedAsync(portName!, baud, frameLen, _cts.Token);
                            break;
                        case ReceiveMode.SerialPioneerAuto:
                            await RunSerialPioneerAsync(portName!, baud, _cts.Token);
                            break;
                        case ReceiveMode.UdpNmea:
                            await RunUdpNmeaAsync(udpPort, _cts.Token);
                            break;
                        case ReceiveMode.UdpBinaryDatagram:
                            await RunUdpBinaryAsync(udpPort, _cts.Token);
                            break;
                        case ReceiveMode.UdpPioneerAuto:
                            await RunUdpPioneerAsync(udpPort, _cts.Token);
                            break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Error?.Invoke("Worker fatal: " + ex.Message);
                }
            }, _cts.Token);
        }

        public async Task StopAsync()
        {
            if (_worker == null) return;
            _cts!.Cancel();
            try { await _worker; } catch { }
            _worker = null;
            _cts.Dispose(); _cts = null;

            if (_udp != null) { _udp.Close(); _udp.Dispose(); _udp = null; }
            if (_serial != null)
            {
                try { if (_serial.IsOpen) _serial.Close(); } catch { }
                _serial.Dispose(); _serial = null;
            }
            _binBuf = Array.Empty<byte>();
            Info?.Invoke("Receiver stopped.");
        }

        public async ValueTask DisposeAsync() { await StopAsync(); }
        public void Dispose() { StopAsync().GetAwaiter().GetResult(); }

        // ---------- Serial (NMEA) ----------
        private async Task RunSerialNmeaAsync(string portName, int baud, bool normalizeCrLf, CancellationToken ct)
        {
            _serial = new SerialPort(portName, baud)
            {
                NewLine = "\n",
                Encoding = Encoding.ASCII,
                ReadTimeout = 200
            };
            _serial.Open();
            Info?.Invoke($"Serial OPEN: {portName}@{baud} (NMEA)");

            var buf = new byte[1024];
            var sb = new StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int n = await _serial.BaseStream.ReadAsync(buf, 0, buf.Length, ct);
                    if (n <= 0) continue;
                    var chunk = Encoding.ASCII.GetString(buf, 0, n);
                    foreach (var ch in chunk)
                    {
                        if (ch == '\r' || ch == '\n')
                        {
                            if (sb.Length == 0) continue;
                            var line = sb.ToString();
                            sb.Clear();
                            if (normalizeCrLf && !line.EndsWith("\r\n"))
                                line += "\r\n";
                            Info?.Invoke("<SER NMEA> " + line.Trim());
                            NmeaLineReceived?.Invoke(line);
                        }
                        else sb.Append(ch);
                    }
                }
                catch (TimeoutException) { }
            }
        }

        // ---------- Serial (固定長バイナリ) ----------
        private async Task RunSerialBinaryFixedAsync(string portName, int baud, int frameLen, CancellationToken ct)
        {
            if (frameLen <= 0) throw new ArgumentException("Frame length must be > 0.");

            _serial = new SerialPort(portName, baud)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 200
            };
            _serial.Open();
            Info?.Invoke($"Serial OPEN: {portName}@{baud} (fixed {frameLen} bytes)");

            var tmp = new byte[1024];
            _binBuf = Array.Empty<byte>();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int n = await _serial.BaseStream.ReadAsync(tmp, 0, tmp.Length, ct);
                    if (n <= 0) continue;

                    var oldLen = _binBuf.Length;
                    Array.Resize(ref _binBuf, oldLen + n);
                    Array.Copy(tmp, 0, _binBuf, oldLen, n);

                    while (_binBuf.Length >= frameLen)
                    {
                        var frame = new byte[frameLen];
                        Array.Copy(_binBuf, 0, frame, 0, frameLen);
                        var remain = new byte[_binBuf.Length - frameLen];
                        Array.Copy(_binBuf, frameLen, remain, 0, remain.Length);
                        _binBuf = remain;

                        Info?.Invoke($"<SER BIN> {frameLen} bytes");
                        // 種別はUIで選ぶ運用のため暫定 "IDA3" などにする場合は呼び出し側で付与
                        BinaryFrameReceived?.Invoke(frame, "BINARY");
                    }
                }
                catch (TimeoutException) { }
            }
        }

        // ---------- Serial (Pioneer 自動判別) ----------
        private async Task RunSerialPioneerAsync(string portName, int baud, CancellationToken ct)
        {
            _serial = new SerialPort(portName, baud)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 200
            };
            _serial.Open();
            Info?.Invoke($"Serial OPEN: {portName}@{baud} (Pioneer auto)");

            var buf = new byte[1024];
            var acc = new List<byte>();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int n = await _serial.BaseStream.ReadAsync(buf, 0, buf.Length, ct);
                    if (n <= 0) continue;

                    for (int i = 0; i < n; i++)
                    {
                        byte b = buf[i];
                        acc.Add(b);

                        // 終端 ... 0x10 0x03
                        if (acc.Count >= 2 && acc[^2] == 0x10 && acc[^1] == 0x03)
                        {
                            int start = acc.FindIndex(x => x == 0x10);
                            if (start >= 0 && acc.Count - start >= 5)
                            {
                                var frame = acc.Skip(start).ToArray();
                                if (TryDetectPayloadType(frame, out var pt) && pt != "NMEA")
                                {
                                    if (TryUnstuffAndVerify(frame, out var _, out var _, out var _))
                                    {
                                        Info?.Invoke($"<SER Pioneer> {frame.Length} bytes → {pt}");
                                        BinaryFrameReceived?.Invoke(frame, pt);
                                    }
                                    else
                                    {
                                        Error?.Invoke("SER(Pioneer): checksum/frame invalid.");
                                    }
                                }
                                else
                                {
                                    Error?.Invoke("SER(Pioneer): unknown frame.");
                                }
                            }
                            acc.Clear();
                        }
                    }
                }
                catch (TimeoutException) { }
            }
        }

        // ---------- UDP (NMEA) ----------
        private async Task RunUdpNmeaAsync(int port, CancellationToken ct)
        {
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Info?.Invoke($"UDP BIND: {port} (NMEA lines)");

            while (!ct.IsCancellationRequested)
            {
                var res = await _udp.ReceiveAsync(ct);
                var line = Encoding.ASCII.GetString(res.Buffer).TrimEnd('\r', '\n', '\0');
                if (string.IsNullOrWhiteSpace(line)) continue;
                Info?.Invoke("<UDP NMEA> " + line);
                NmeaLineReceived?.Invoke(line);
            }
        }

        // ---------- UDP (binary そのまま) ----------
        private async Task RunUdpBinaryAsync(int port, CancellationToken ct)
        {
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Info?.Invoke($"UDP BIND: {port} (binary datagram)");
            while (!ct.IsCancellationRequested)
            {
                var res = await _udp.ReceiveAsync(ct);
                var frame = res.Buffer;
                Info?.Invoke($"<UDP BIN> {frame.Length} bytes");
                BinaryFrameReceived?.Invoke(frame, "BINARY");
            }
        }

        // ---------- UDP (Pioneer 自動判別) ----------
        private async Task RunUdpPioneerAsync(int port, CancellationToken ct)
        {
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Info?.Invoke($"UDP BIND: {port} (Pioneer auto)");
            while (!ct.IsCancellationRequested)
            {
                var res = await _udp.ReceiveAsync(ct);
                var frame = res.Buffer;

                if (!TryDetectPayloadType(frame, out var pt) || pt == "NMEA")
                {
                    Error?.Invoke("UDP(Pioneer): unknown or NMEA frame, ignored.");
                    continue;
                }
                if (!TryUnstuffAndVerify(frame, out var _, out var _, out var _))
                {
                    Error?.Invoke("UDP(Pioneer): checksum/frame invalid.");
                    continue;
                }
                Info?.Invoke($"<UDP Pioneer> {frame.Length} bytes → {pt}");
                BinaryFrameReceived?.Invoke(frame, pt);
            }
        }

        // ---------- 判別＆検証 ----------
        private static bool TryDetectPayloadType(ReadOnlySpan<byte> data, out string payloadType)
        {
            payloadType = "";
            if (data.Length == 0) return false;
            if (data[0] == (byte)'$') { payloadType = "NMEA"; return true; }
            if (data.Length >= 2 && data[0] == 0x10)
            {
                byte id = data[1];
                if (id == 0x75) { payloadType = "ID75"; return true; }
                if (id == 0xA3) { payloadType = "IDA3"; return true; }
            }
            return false;
        }

        private static bool TryUnstuffAndVerify(ReadOnlySpan<byte> src, out byte id, out byte[] dataNoCs, out byte cs)
        {
            id = 0; cs = 0; dataNoCs = Array.Empty<byte>();
            if (src.Length < 5 || src[0] != 0x10) return false;

            id = src[1];
            if (!(src[^2] == 0x10 && src[^1] == 0x03)) return false;

            int dataLenWithStuff = src.Length - 5; // 2(ヘッダ)+1(CS)+2(終端)
            if (dataLenWithStuff < 0) return false;

            cs = src[^3];
            var stuffed = src.Slice(2, dataLenWithStuff);

            var unstuff = new List<byte>(stuffed.Length);
            for (int i = 0; i < stuffed.Length; i++)
            {
                byte b = stuffed[i];
                if (b == 0x10)
                {
                    if (i + 1 < stuffed.Length && stuffed[i + 1] == 0x10)
                    {
                        unstuff.Add(0x10); i++;
                    }
                    else
                    {
                        unstuff.Add(0x10);
                    }
                }
                else unstuff.Add(b);
            }

            byte x = 0;
            foreach (var b in unstuff) x ^= b;
            if (x != cs) return false;

            dataNoCs = unstuff.ToArray();
            return true;
        }
    }
}
