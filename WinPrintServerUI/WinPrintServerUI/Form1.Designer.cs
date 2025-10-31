namespace WinPrintServerUI
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbPrinters = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.lvServices = new System.Windows.Forms.ListView();
            this.btnStopAll = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.cmbLogFilter = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbPrinters
            // 
            this.cmbPrinters.Font = new System.Drawing.Font("宋体", 16F);
            this.cmbPrinters.FormattingEnabled = true;
            this.cmbPrinters.IntegralHeight = false;
            this.cmbPrinters.ItemHeight = 27;
            this.cmbPrinters.Location = new System.Drawing.Point(196, 12);
            this.cmbPrinters.Name = "cmbPrinters";
            this.cmbPrinters.Size = new System.Drawing.Size(200, 35);
            this.cmbPrinters.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 18F);
            this.label1.Location = new System.Drawing.Point(34, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(118, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "打印机:";
            // 
            // nudPort
            // 
            this.nudPort.Font = new System.Drawing.Font("宋体", 16F);
            this.nudPort.Location = new System.Drawing.Point(196, 82);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(120, 38);
            this.nudPort.TabIndex = 2;
            this.nudPort.Value = new decimal(new int[] {
            9100,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 18F);
            this.label2.Location = new System.Drawing.Point(34, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 30);
            this.label2.TabIndex = 3;
            this.label2.Text = "端口:";
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btnStart.Font = new System.Drawing.Font("宋体", 16F);
            this.btnStart.Location = new System.Drawing.Point(514, 82);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(152, 48);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "启动服务";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(39, 420);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(798, 189);
            this.rtbLog.TabIndex = 7;
            this.rtbLog.Text = "";
            this.rtbLog.ReadOnly = true;
            // 
            // lvServices
            // 
            this.lvServices.HideSelection = false;
            this.lvServices.Location = new System.Drawing.Point(39, 150);
            this.lvServices.Name = "lvServices";
            this.lvServices.Size = new System.Drawing.Size(798, 200);
            this.lvServices.TabIndex = 8;
            this.lvServices.UseCompatibleStateImageBehavior = false;
            this.lvServices.View = System.Windows.Forms.View.Details;
            // 
            // btnStopAll
            // 
            this.btnStopAll.BackColor = System.Drawing.Color.Orange;
            this.btnStopAll.Font = new System.Drawing.Font("宋体", 16F);
            this.btnStopAll.Location = new System.Drawing.Point(689, 82);
            this.btnStopAll.Name = "btnStopAll";
            this.btnStopAll.Size = new System.Drawing.Size(148, 48);
            this.btnStopAll.TabIndex = 9;
            this.btnStopAll.Text = "停止所有";
            this.btnStopAll.UseVisualStyleBackColor = false;
            this.btnStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 12F);
            this.label3.Location = new System.Drawing.Point(39, 395);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 20);
            this.label3.TabIndex = 11;
            this.label3.Text = "日志:";
            // 
            // cmbLogFilter
            // 
            this.cmbLogFilter.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbLogFilter.FormattingEnabled = true;
            this.cmbLogFilter.Location = new System.Drawing.Point(101, 392);
            this.cmbLogFilter.Name = "cmbLogFilter";
            this.cmbLogFilter.Size = new System.Drawing.Size(150, 27);
            this.cmbLogFilter.TabIndex = 12;
            // 
            // btnClearLog
            // 
            this.btnClearLog.BackColor = System.Drawing.Color.LightGray;
            this.btnClearLog.Font = new System.Drawing.Font("宋体", 12F);
            this.btnClearLog.Location = new System.Drawing.Point(715, 615);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(122, 30);
            this.btnClearLog.TabIndex = 10;
            this.btnClearLog.Text = "清空日志";
            this.btnClearLog.UseVisualStyleBackColor = false;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(886, 669);
            this.Controls.Add(this.cmbLogFilter);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.btnStopAll);
            this.Controls.Add(this.lvServices);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nudPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbPrinters);
            this.Name = "Form1";
            this.Text = "USB转RAW打印服务器";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbPrinters;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.ListView lvServices;
        private System.Windows.Forms.Button btnStopAll;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.ComboBox cmbLogFilter;
        private System.Windows.Forms.Label label3;
    }
}

