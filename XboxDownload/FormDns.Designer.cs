
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.butTest = new System.Windows.Forms.Button();
            this.cbDomainName = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Window;
            this.textBox1.Location = new System.Drawing.Point(25, 62);
            this.textBox1.Margin = new System.Windows.Forms.Padding(5);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(629, 239);
            this.textBox1.TabIndex = 8;
            this.textBox1.TabStop = false;
            // 
            // butTest
            // 
            this.butTest.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.butTest.Location = new System.Drawing.Point(554, 18);
            this.butTest.Margin = new System.Windows.Forms.Padding(5);
            this.butTest.Name = "butTest";
            this.butTest.Size = new System.Drawing.Size(100, 35);
            this.butTest.TabIndex = 6;
            this.butTest.Text = "测试";
            this.butTest.UseVisualStyleBackColor = true;
            this.butTest.Click += new System.EventHandler(this.ButTest_Click);
            // 
            // cbDomainName
            // 
            this.cbDomainName.FormattingEnabled = true;
            this.cbDomainName.ItemHeight = 18;
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
            this.cbDomainName.Location = new System.Drawing.Point(74, 21);
            this.cbDomainName.Margin = new System.Windows.Forms.Padding(5);
            this.cbDomainName.Name = "cbDomainName";
            this.cbDomainName.Size = new System.Drawing.Size(470, 26);
            this.cbDomainName.TabIndex = 5;
            this.cbDomainName.TabStop = false;
            this.cbDomainName.Validating += new System.ComponentModel.CancelEventHandler(this.CbDomainName_Validating);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(20, 26);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 18);
            this.label4.TabIndex = 7;
            this.label4.Text = "域名";
            // 
            // FormDns
            // 
            this.AcceptButton = this.butTest;
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(668, 318);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.butTest);
            this.Controls.Add(this.cbDomainName);
            this.Controls.Add(this.label4);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormDns";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "测试DNS服务器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormDns_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button butTest;
        private System.Windows.Forms.ComboBox cbDomainName;
        private System.Windows.Forms.Label label4;
    }
}