using System;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

namespace GpsIngestClinetForm
{
    public partial class MainForm : Form
    {
        private readonly ReceiverService _rx = new ReceiverService();
        private static readonly HttpClient _http = new HttpClient();

        public MainForm()
        {
            InitializeComponent();
            WireEvents();
            LoadSerialPorts();

            UpdateControlsForSource();

        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                //btnStart.Enabled = false;   // UIガード
                await StartAsync();         // 実処理は Task を返す非同期メソッド
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "エラー");
            }
            finally
            {
                //btnStart.Enabled = true;
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                //btnStop.Enabled = false;   // UIガード
                await StopAsync();         // 実処理は Task を返す非同期メソッド
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "エラー");
            }
            finally
            {
                //btnStop.Enabled = true;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private async void btnTestSend_Click(object sender, EventArgs e)
        {
            try
            {
                //btnTestSend.Enabled = false;   // UIガード
                await SendTestAsync();         // 実処理は Task を返す非同期メソッド
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "エラー");
            }
            finally
            {
                //btnTestSend.Enabled = true;
            }


        }



        private void WireEvents()
        {
            _rx.Info += s => BeginInvoke(new Action(() => Log(s)));
            _rx.Error += s => BeginInvoke(new Action(() => Log("ERR: " + s)));

            _rx.NmeaLineReceived += line => { _ = SendNmeaAsync(line); };

            // 種別つき
            _rx.BinaryFrameReceived += (frame, pt) =>
            {
                if (pt == "BINARY")
                {
                    // UI選択の payloadType を適用
                    var uiPt = (cmbPayloadType.SelectedItem?.ToString() ?? "IDA3").ToUpperInvariant();
                    _ = SendBinaryAsync(frame, uiPt);
                }
                else
                {
                    _ = SendBinaryAsync(frame, pt); // 自動判別結果(ID75/IDA3)をそのまま使用
                }
            };
        }

        private void LoadSerialPorts()
        {
            cmbSerialPort.Items.Clear();
            cmbSerialPort.Items.AddRange(SerialPort.GetPortNames());
            if (cmbSerialPort.Items.Count > 0) cmbSerialPort.SelectedIndex = 0;
        }

        private void UpdateControlsForSource()
        {
            var s = cmbSource.SelectedItem?.ToString() ?? "";
            bool ser = s.StartsWith("Serial");
            bool udp = s.StartsWith("UDP");

            cmbSerialPort.Enabled = ser;
            cmbBaud.Enabled = ser;
            numUdpPort.Enabled = udp;

            if (s == "Serial (NMEA)")
            {
                cmbPayloadType.SelectedItem = "NMEA";
                cmbPayloadType.Enabled = false;
                numFrameLen.Enabled = false;
                chkNmeaCRLF.Enabled = true;
            }
            else if (s == "Serial (Pioneer auto)" || s == "UDP (Pioneer auto)")
            {
                cmbPayloadType.Enabled = false;   // 自動判別
                numFrameLen.Enabled = false;
                chkNmeaCRLF.Enabled = false;
            }
            else if (s == "Serial (IDxx fixed length)")
            {
                cmbPayloadType.Enabled = true;    // UIが ID75/IDA3 を選ぶ
                numFrameLen.Enabled = true;
                chkNmeaCRLF.Enabled = false;
            }
            else if (s == "UDP (NMEA lines)")
            {
                cmbPayloadType.SelectedItem = "NMEA";
                cmbPayloadType.Enabled = false;
                numFrameLen.Enabled = false;
                chkNmeaCRLF.Enabled = true;
            }
            else // UDP (binary datagram)
            {
                cmbPayloadType.Enabled = true;    // UI選択
                numFrameLen.Enabled = false;
                chkNmeaCRLF.Enabled = false;
            }
        }

        private async Task StartAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("x-api-key", txtApiKey.Text.Trim());

                var mode = cmbSource.SelectedItem?.ToString() ?? "";
                if (mode == "Serial (NMEA)")
                    _rx.StartSerialNmea(cmbSerialPort.Text, int.Parse(cmbBaud.Text), chkNmeaCRLF.Checked);
                else if (mode == "Serial (Pioneer auto)")
                    _rx.StartSerialPioneerAuto(cmbSerialPort.Text, int.Parse(cmbBaud.Text));
                else if (mode == "Serial (IDxx fixed length)")
                    _rx.StartSerialBinaryFixed(cmbSerialPort.Text, int.Parse(cmbBaud.Text), (int)numFrameLen.Value);
                else if (mode == "UDP (NMEA lines)")
                    _rx.StartUdpNmea((int)numUdpPort.Value);
                else if (mode == "UDP (Pioneer auto)")
                    _rx.StartUdpPioneerAuto((int)numUdpPort.Value);
                else
                    _rx.StartUdpBinaryDatagram((int)numUdpPort.Value);

                btnStart.Enabled = false; btnStop.Enabled = true;
                lblStatus.Text = "Running";
                Log("Receiver started.");
            }
            catch (Exception ex)
            {
                Log("Start error: " + ex.Message);
            }
        }

        private async Task StopAsync()
        {
            await _rx.StopAsync();
            btnStart.Enabled = true; btnStop.Enabled = false;
            lblStatus.Text = "Stopped";
        }

        // ===== 送信 =====
        private async Task SendNmeaAsync(string line)
        {
            var req = new
            {
                deviceId = txtDeviceId.Text.Trim(),
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payloadType = "NMEA",
                raw = line
            };
            await PostAsync(req);
        }

        private async Task SendBinaryAsync(byte[] data, string payloadType /* ID75 / IDA3 */)
        {
            var req = new
            {
                deviceId = txtDeviceId.Text.Trim(),
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                payloadType = payloadType.ToUpperInvariant(),
                raw = Convert.ToBase64String(data)
            };
            await PostAsync(req);
        }

        private async Task PostAsync(object payload)
        {
            try
            {
                var url = txtApiUrl.Text.Trim();
                var json = JsonSerializer.Serialize(payload);
                var res = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) Log($"POST NG {res.StatusCode}: {body}");
                else Log("POST OK: " + body);
            }
            catch (Exception ex) { Log("POST ERR: " + ex.Message); }
        }

        // ===== テスト送信 =====
        private async Task SendTestAsync()
        {
            try
            {
                string deviceId = txtDeviceId.Text.Trim();
                string apiUrl = txtApiUrl.Text.Trim();
                string apiKey = txtApiKey.Text.Trim();
                string payloadType = (cmbPayloadType.SelectedItem?.ToString() ?? "NMEA").ToUpperInvariant();

                object payload;
                if (payloadType == "NMEA")
                {
                    var nmea = "$GPRMC,083559.00,A,3456.789,N,13530.123,E,12.3,187.2,241025,,,A*68";
                    payload = new { deviceId, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), payloadType = "NMEA", raw = nmea };
                }
                else if (payloadType == "ID75")
                {
                    var dummy = new byte[19]; new Random().NextBytes(dummy);
                    payload = new { deviceId, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), payloadType = "ID75", raw = Convert.ToBase64String(dummy) };
                }
                else // IDA3
                {
                    var dummy = new byte[32]; new Random().NextBytes(dummy);
                    payload = new { deviceId, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), payloadType = "IDA3", raw = Convert.ToBase64String(dummy) };
                }

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
                var json = JsonSerializer.Serialize(payload);
                var res = await _http.PostAsync(apiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode) Log($"[TEST OK] {payloadType}: {body}");
                else Log($"[TEST NG] {res.StatusCode}: {body}");
            }
            catch (Exception ex) { Log("TEST ERR: " + ex.Message); }
        }

        private void Log(string s)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {s}\r\n");
        }

        private void cmbSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsForSource();
        }
    }
}
