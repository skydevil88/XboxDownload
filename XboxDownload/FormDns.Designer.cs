namespace XboxDownload
{
    partial class FormDns
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
            label1 = new Label();
            cbHostName = new ComboBox();
            butTest = new Button();
            textBox1 = new TextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 24);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 0;
            label1.Text = "域名";
            // 
            // cbHostName
            // 
            cbHostName.FormattingEnabled = true;
            cbHostName.Items.AddRange(new object[] { "assets2.xboxlive.cn", "dl.delivery.mp.microsoft.com", "gst.prod.dl.playstation.net", "atum.hac.lp1.d4c.nintendo.net", "origin-a.akamaihd.net", "blzddist1-a.akamaihd.net", "epicgames-download1-1251447533.file.myqcloud.com", "uplaypc-s-ubisoft.cdn.ubionline.com.cn", "www.baidu.com" });
            cbHostName.Location = new Point(69, 18);
            cbHostName.Margin = new Padding(4);
            cbHostName.Name = "cbHostName";
            cbHostName.Size = new Size(510, 32);
            cbHostName.TabIndex = 1;
            cbHostName.Validating += CbDomainName_Validating;
            // 
            // butTest
            // 
            butTest.Location = new Point(586, 17);
            butTest.Margin = new Padding(4);
            butTest.Name = "butTest";
            butTest.Size = new Size(98, 36);
            butTest.TabIndex = 2;
            butTest.Text = "测试";
            butTest.UseVisualStyleBackColor = true;
            butTest.Click += ButTest_Click;
            // 
            // textBox1
            // 
            textBox1.BackColor = SystemColors.Window;
            textBox1.Location = new Point(15, 62);
            textBox1.Margin = new Padding(4);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(670, 240);
            textBox1.TabIndex = 3;
            // 
            // FormDns
            // 
            AcceptButton = butTest;
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(698, 318);
            Controls.Add(textBox1);
            Controls.Add(butTest);
            Controls.Add(cbHostName);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormDns";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "测试DNS服务器";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbHostName;
        private Button butTest;
        private TextBox textBox1;
    }
}