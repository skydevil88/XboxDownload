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
            this.label1 = new System.Windows.Forms.Label();
            this.cbDomainName = new System.Windows.Forms.ComboBox();
            this.butTest = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 24);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "域名";
            // 
            // cbDomainName
            // 
            this.cbDomainName.FormattingEnabled = true;
            this.cbDomainName.Items.AddRange(new object[] {
            "assets1.xboxlive.com",
            "assets1.xboxlive.cn",
            "tlu.dl.delivery.mp.microsoft.com",
            "gst.prod.dl.playstation.net",
            "atum.hac.lp1.d4c.nintendo.net",
            "origin-a.akamaihd.net",
            "blzddist1-a.akamaihd.net",
            "epicgames-download1-1251447533.file.myqcloud.com",
            "www.baidu.com"});
            this.cbDomainName.Location = new System.Drawing.Point(69, 18);
            this.cbDomainName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbDomainName.Name = "cbDomainName";
            this.cbDomainName.Size = new System.Drawing.Size(510, 32);
            this.cbDomainName.TabIndex = 1;
            this.cbDomainName.Validating += new System.ComponentModel.CancelEventHandler(this.CbDomainName_Validating);
            // 
            // butTest
            // 
            this.butTest.Location = new System.Drawing.Point(586, 17);
            this.butTest.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.butTest.Name = "butTest";
            this.butTest.Size = new System.Drawing.Size(98, 36);
            this.butTest.TabIndex = 2;
            this.butTest.Text = "测试";
            this.butTest.UseVisualStyleBackColor = true;
            this.butTest.Click += new System.EventHandler(this.ButTest_Click);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Window;
            this.textBox1.Location = new System.Drawing.Point(15, 62);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(670, 240);
            this.textBox1.TabIndex = 3;
            // 
            // FormDns
            // 
            this.AcceptButton = this.butTest;
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(698, 318);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.butTest);
            this.Controls.Add(this.cbDomainName);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormDns";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "测试DNS服务器";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label label1;
        private ComboBox cbDomainName;
        private Button butTest;
        private TextBox textBox1;
    }
}