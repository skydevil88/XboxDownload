namespace XboxDownload
{
    partial class FormSniProxy
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            groupBox2 = new GroupBox();
            checkedListBox1 = new CheckedListBox();
            groupBox1 = new GroupBox();
            textBox1 = new TextBox();
            label1 = new Label();
            groupBox3 = new GroupBox();
            checkedListBox2 = new CheckedListBox();
            cbSniPorxyOptimized = new CheckBox();
            cbSniProxysIPv6 = new CheckBox();
            linkTestIPv6 = new LinkLabel();
            nudSniPorxyExpired = new NumericUpDown();
            label2 = new Label();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudSniPorxyExpired).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(540, 516);
            button1.Name = "button1";
            button1.Size = new Size(265, 45);
            button1.TabIndex = 0;
            button1.Text = "保存";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkedListBox1);
            groupBox2.Location = new Point(537, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(271, 143);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "选择服务";
            // 
            // checkedListBox1
            // 
            checkedListBox1.Dock = DockStyle.Fill;
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new Point(3, 26);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(265, 114);
            checkedListBox1.TabIndex = 0;
            checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox1);
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(528, 558);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "域名 (使用泛域名解析需要勾选“设置本机 DNS”)";
            // 
            // textBox1
            // 
            textBox1.BackColor = SystemColors.Window;
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(3, 26);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "*example.com | SNI (可留空) | IP (可留空)\r\n*example.com -> example.com (使用指定域名解释IP地址)";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(522, 529);
            textBox1.TabIndex = 0;
            textBox1.TabStop = false;
            textBox1.WordWrap = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 571);
            label1.Name = "label1";
            label1.Size = new Size(673, 48);
            label1.TabIndex = 3;
            label1.Text = "此功能可以抵御部分网站被SNI阻断攻击无法访问的问题，DoH服务器请使用国外。\r\n*整个代理过程在用户自己的电脑上完成，并不涉及任何第三方服务。";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(checkedListBox2);
            groupBox3.Location = new Point(537, 152);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(271, 254);
            groupBox3.TabIndex = 3;
            groupBox3.TabStop = false;
            groupBox3.Text = "DoH服务器 (0)";
            // 
            // checkedListBox2
            // 
            checkedListBox2.Dock = DockStyle.Fill;
            checkedListBox2.FormattingEnabled = true;
            checkedListBox2.Location = new Point(3, 26);
            checkedListBox2.Name = "checkedListBox2";
            checkedListBox2.Size = new Size(265, 225);
            checkedListBox2.TabIndex = 0;
            // 
            // cbSniPorxyOptimized
            // 
            cbSniPorxyOptimized.AutoSize = true;
            cbSniPorxyOptimized.Location = new Point(540, 446);
            cbSniPorxyOptimized.Name = "cbSniPorxyOptimized";
            cbSniPorxyOptimized.Size = new Size(219, 28);
            cbSniPorxyOptimized.TabIndex = 6;
            cbSniPorxyOptimized.Text = "自动连接延迟最低的 IP";
            cbSniPorxyOptimized.UseVisualStyleBackColor = true;
            // 
            // cbSniProxysIPv6
            // 
            cbSniProxysIPv6.AutoSize = true;
            cbSniProxysIPv6.Location = new Point(540, 412);
            cbSniProxysIPv6.Name = "cbSniProxysIPv6";
            cbSniProxysIPv6.Size = new Size(149, 28);
            cbSniProxysIPv6.TabIndex = 4;
            cbSniProxysIPv6.Text = "优先使用 IPv6";
            cbSniProxysIPv6.UseVisualStyleBackColor = true;
            // 
            // linkTestIPv6
            // 
            linkTestIPv6.AutoSize = true;
            linkTestIPv6.Location = new Point(695, 413);
            linkTestIPv6.Name = "linkTestIPv6";
            linkTestIPv6.Size = new Size(82, 24);
            linkTestIPv6.TabIndex = 5;
            linkTestIPv6.TabStop = true;
            linkTestIPv6.Text = "检测IPv6";
            linkTestIPv6.Visible = false;
            linkTestIPv6.LinkClicked += LinkTestIPv6_LinkClickedAsync;
            // 
            // nudSniPorxyExpired
            // 
            nudSniPorxyExpired.Location = new Point(715, 480);
            nudSniPorxyExpired.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            nudSniPorxyExpired.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
            nudSniPorxyExpired.Name = "nudSniPorxyExpired";
            nudSniPorxyExpired.Size = new Size(90, 30);
            nudSniPorxyExpired.TabIndex = 7;
            nudSniPorxyExpired.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(537, 482);
            label2.Name = "label2";
            label2.Size = new Size(169, 24);
            label2.TabIndex = 8;
            label2.Text = "DNS缓存时间(分钟)";
            // 
            // FormSniProxy
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(813, 629);
            Controls.Add(label2);
            Controls.Add(linkTestIPv6);
            Controls.Add(nudSniPorxyExpired);
            Controls.Add(cbSniProxysIPv6);
            Controls.Add(cbSniPorxyOptimized);
            Controls.Add(groupBox3);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormSniProxy";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "本地代理服务";
            Load += FormSniProxy_Load;
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)nudSniPorxyExpired).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private GroupBox groupBox2;
        private GroupBox groupBox1;
        private CheckedListBox checkedListBox1;
        private Button button1;
        private TextBox textBox1;
        private Label label1;
        private GroupBox groupBox3;
        private CheckedListBox checkedListBox2;
        private CheckBox cbSniPorxyOptimized;
        private CheckBox cbSniProxysIPv6;
        private LinkLabel linkTestIPv6;
        private NumericUpDown nudSniPorxyExpired;
        private Label label2;
    }
}