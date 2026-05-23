namespace GpsIngestClinetForm
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnl = new TableLayoutPanel();
            txtApiUrl = new TextBox();
            txtDeviceId = new TextBox();
            cmbSerialPort = new ComboBox();
            cmbPayloadType = new ComboBox();
            btnStart = new Button();
            btnStop = new Button();
            btnClear = new Button();
            btnTestSend = new Button();
            txtApiKey = new TextBox();
            cmbSource = new ComboBox();
            cmbBaud = new ComboBox();
            numFrameLen = new NumericUpDown();
            numUdpPort = new NumericUpDown();
            chkNmeaCRLF = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            lblStatus = new Label();
            txtLog = new TextBox();
            pnl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numFrameLen).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUdpPort).BeginInit();
            SuspendLayout();
            // 
            // pnl
            // 
            pnl.ColumnCount = 6;
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            pnl.Controls.Add(txtApiUrl, 1, 0);
            pnl.Controls.Add(txtDeviceId, 1, 1);
            pnl.Controls.Add(cmbSerialPort, 1, 2);
            pnl.Controls.Add(cmbPayloadType, 1, 3);
            pnl.Controls.Add(btnStart, 0, 4);
            pnl.Controls.Add(btnStop, 1, 4);
            pnl.Controls.Add(btnClear, 2, 4);
            pnl.Controls.Add(btnTestSend, 3, 4);
            pnl.Controls.Add(txtApiKey, 3, 0);
            pnl.Controls.Add(cmbSource, 3, 1);
            pnl.Controls.Add(cmbBaud, 3, 2);
            pnl.Controls.Add(numFrameLen, 3, 3);
            pnl.Controls.Add(numUdpPort, 5, 2);
            pnl.Controls.Add(chkNmeaCRLF, 5, 3);
            pnl.Controls.Add(label1, 0, 0);
            pnl.Controls.Add(label2, 0, 1);
            pnl.Controls.Add(label3, 0, 2);
            pnl.Controls.Add(label4, 0, 3);
            pnl.Controls.Add(label5, 2, 0);
            pnl.Controls.Add(label6, 2, 1);
            pnl.Controls.Add(label7, 2, 2);
            pnl.Controls.Add(label8, 2, 3);
            pnl.Controls.Add(label9, 4, 2);
            pnl.Controls.Add(label10, 4, 3);
            pnl.Controls.Add(label11, 4, 4);
            pnl.Controls.Add(lblStatus, 5, 4);
            pnl.Dock = DockStyle.Top;
            pnl.Location = new Point(0, 0);
            pnl.Margin = new Padding(2);
            pnl.Name = "pnl";
            pnl.RowCount = 5;
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnl.Size = new Size(671, 120);
            pnl.TabIndex = 0;
            // 
            // txtApiUrl
            // 
            txtApiUrl.Dock = DockStyle.Fill;
            txtApiUrl.Location = new Point(86, 2);
            txtApiUrl.Margin = new Padding(2);
            txtApiUrl.Name = "txtApiUrl";
            txtApiUrl.Size = new Size(163, 23);
            txtApiUrl.TabIndex = 0;
            txtApiUrl.Text = "https://ka6j3s332k.execute-api.ap-northeast-1.amazonaws.com/prod/ingest";
            // 
            // txtDeviceId
            // 
            txtDeviceId.Dock = DockStyle.Fill;
            txtDeviceId.Location = new Point(86, 26);
            txtDeviceId.Margin = new Padding(2);
            txtDeviceId.Name = "txtDeviceId";
            txtDeviceId.Size = new Size(163, 23);
            txtDeviceId.TabIndex = 1;
            txtDeviceId.Text = "DEV-0001";
            // 
            // cmbSerialPort
            // 
            cmbSerialPort.Dock = DockStyle.Fill;
            cmbSerialPort.FormattingEnabled = true;
            cmbSerialPort.Location = new Point(86, 50);
            cmbSerialPort.Margin = new Padding(2);
            cmbSerialPort.Name = "cmbSerialPort";
            cmbSerialPort.Size = new Size(163, 23);
            cmbSerialPort.TabIndex = 2;
            // 
            // cmbPayloadType
            // 
            cmbPayloadType.Dock = DockStyle.Fill;
            cmbPayloadType.FormattingEnabled = true;
            cmbPayloadType.Items.AddRange(new object[] { "NMEA", "ID75", "IDA3" });
            cmbPayloadType.Location = new Point(86, 74);
            cmbPayloadType.Margin = new Padding(2);
            cmbPayloadType.Name = "cmbPayloadType";
            cmbPayloadType.Size = new Size(163, 23);
            cmbPayloadType.TabIndex = 3;
            // 
            // btnStart
            // 
            btnStart.Dock = DockStyle.Fill;
            btnStart.Location = new Point(2, 98);
            btnStart.Margin = new Padding(2);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(80, 20);
            btnStart.TabIndex = 4;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Dock = DockStyle.Fill;
            btnStop.Location = new Point(86, 98);
            btnStop.Margin = new Padding(2);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(163, 20);
            btnStop.TabIndex = 5;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnClear
            // 
            btnClear.Dock = DockStyle.Fill;
            btnClear.Location = new Point(253, 98);
            btnClear.Margin = new Padding(2);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(80, 20);
            btnClear.TabIndex = 6;
            btnClear.Text = "Clear Log";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // btnTestSend
            // 
            btnTestSend.Dock = DockStyle.Fill;
            btnTestSend.Location = new Point(337, 98);
            btnTestSend.Margin = new Padding(2);
            btnTestSend.Name = "btnTestSend";
            btnTestSend.Size = new Size(163, 20);
            btnTestSend.TabIndex = 7;
            btnTestSend.Text = "Test Send";
            btnTestSend.UseVisualStyleBackColor = true;
            btnTestSend.Click += btnTestSend_Click;
            // 
            // txtApiKey
            // 
            txtApiKey.Dock = DockStyle.Fill;
            txtApiKey.Location = new Point(337, 2);
            txtApiKey.Margin = new Padding(2);
            txtApiKey.Name = "txtApiKey";
            txtApiKey.Size = new Size(163, 23);
            txtApiKey.TabIndex = 8;
            txtApiKey.Text = "my-secret-api-key-2025";
            // 
            // cmbSource
            // 
            cmbSource.Dock = DockStyle.Fill;
            cmbSource.FormattingEnabled = true;
            cmbSource.Items.AddRange(new object[] { "Serial (NMEA)", "Serial (Pioneer auto)", "Serial (IDxx fixed length)", "UDP (NMEA lines)", "UDP (Pioneer auto)", "UDP (binary datagram)" });
            cmbSource.Location = new Point(337, 26);
            cmbSource.Margin = new Padding(2);
            cmbSource.Name = "cmbSource";
            cmbSource.Size = new Size(163, 23);
            cmbSource.TabIndex = 9;
            cmbSource.SelectedIndexChanged += cmbSource_SelectedIndexChanged;
            // 
            // cmbBaud
            // 
            cmbBaud.Dock = DockStyle.Fill;
            cmbBaud.FormattingEnabled = true;
            cmbBaud.Items.AddRange(new object[] { "4800", "9600", "19200", "38400", "57600", "115200" });
            cmbBaud.Location = new Point(337, 50);
            cmbBaud.Margin = new Padding(2);
            cmbBaud.Name = "cmbBaud";
            cmbBaud.Size = new Size(163, 23);
            cmbBaud.TabIndex = 10;
            // 
            // numFrameLen
            // 
            numFrameLen.Dock = DockStyle.Fill;
            numFrameLen.Location = new Point(337, 74);
            numFrameLen.Margin = new Padding(2);
            numFrameLen.Maximum = new decimal(new int[] { 4096, 0, 0, 0 });
            numFrameLen.Name = "numFrameLen";
            numFrameLen.Size = new Size(163, 23);
            numFrameLen.TabIndex = 11;
            // 
            // numUdpPort
            // 
            numUdpPort.Location = new Point(588, 50);
            numUdpPort.Margin = new Padding(2);
            numUdpPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numUdpPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numUdpPort.Name = "numUdpPort";
            numUdpPort.Size = new Size(80, 23);
            numUdpPort.TabIndex = 12;
            numUdpPort.Value = new decimal(new int[] { 10001, 0, 0, 0 });
            // 
            // chkNmeaCRLF
            // 
            chkNmeaCRLF.AutoSize = true;
            chkNmeaCRLF.Checked = true;
            chkNmeaCRLF.CheckState = CheckState.Checked;
            chkNmeaCRLF.Dock = DockStyle.Fill;
            chkNmeaCRLF.Location = new Point(588, 74);
            chkNmeaCRLF.Margin = new Padding(2);
            chkNmeaCRLF.Name = "chkNmeaCRLF";
            chkNmeaCRLF.Size = new Size(81, 20);
            chkNmeaCRLF.TabIndex = 13;
            chkNmeaCRLF.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(2, 0);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(49, 15);
            label1.TabIndex = 14;
            label1.Text = "API URL";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(2, 24);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(56, 15);
            label2.TabIndex = 15;
            label2.Text = "Device ID";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(2, 48);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(60, 15);
            label3.TabIndex = 16;
            label3.Text = "Serial Port";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(2, 72);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(76, 15);
            label4.TabIndex = 17;
            label4.Text = "Payload Type";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(253, 0);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(57, 15);
            label5.TabIndex = 18;
            label5.Text = "x-api-key";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(253, 24);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(43, 15);
            label6.TabIndex = 19;
            label6.Text = "Source";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(253, 48);
            label7.Margin = new Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new Size(34, 15);
            label7.TabIndex = 20;
            label7.Text = "Baud";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(253, 72);
            label8.Margin = new Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new Size(63, 24);
            label8.TabIndex = 21;
            label8.Text = "Frame Len (bytes)";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(504, 48);
            label9.Margin = new Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new Size(55, 15);
            label9.TabIndex = 22;
            label9.Text = "UDP Port";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(504, 72);
            label10.Margin = new Padding(2, 0, 2, 0);
            label10.Name = "label10";
            label10.Size = new Size(70, 15);
            label10.TabIndex = 23;
            label10.Text = "NMEA CRLF";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(504, 96);
            label11.Margin = new Padding(2, 0, 2, 0);
            label11.Name = "label11";
            label11.Size = new Size(39, 15);
            label11.TabIndex = 24;
            label11.Text = "Status";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Location = new Point(588, 96);
            lblStatus.Margin = new Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(81, 24);
            lblStatus.TabIndex = 25;
            lblStatus.Text = "Idle";
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtLog.Location = new Point(0, 120);
            txtLog.Margin = new Padding(2);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(671, 266);
            txtLog.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(671, 386);
            Controls.Add(txtLog);
            Controls.Add(pnl);
            Margin = new Padding(2);
            Name = "MainForm";
            Text = "GPS Ingest Client (Serial/UDP → API)";
            pnl.ResumeLayout(false);
            pnl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numFrameLen).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUdpPort).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel pnl;
        private TextBox txtApiUrl;
        private TextBox txtDeviceId;
        private ComboBox cmbSerialPort;
        private ComboBox cmbPayloadType;
        private Button btnStart;
        private Button btnStop;
        private Button btnClear;
        private Button btnTestSend;
        private TextBox txtApiKey;
        private ComboBox cmbSource;
        private ComboBox cmbBaud;
        private NumericUpDown numFrameLen;
        private NumericUpDown numUdpPort;
        private CheckBox chkNmeaCRLF;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label lblStatus;
        private TextBox txtLog;
    }
}
