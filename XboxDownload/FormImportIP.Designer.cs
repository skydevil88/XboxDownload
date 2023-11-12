namespace XboxDownload
{
    partial class FormImportIP
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
            panel1 = new Panel();
            label4 = new Label();
            linkLabel3 = new LinkLabel();
            comboBox1 = new ComboBox();
            label3 = new Label();
            linkLabel2 = new LinkLabel();
            label2 = new Label();
            linkLabel1 = new LinkLabel();
            label1 = new Label();
            panel2 = new Panel();
            linkLabel4 = new LinkLabel();
            button1 = new Button();
            label5 = new Label();
            textBox1 = new TextBox();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(label4);
            panel1.Controls.Add(linkLabel3);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(linkLabel2);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(linkLabel1);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(975, 98);
            panel1.TabIndex = 0;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 70);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(879, 24);
            label4.TabIndex = 7;
            label4.Text = "取消全选，只选中国，然后点 Ping, 等待网页加载完毕后 Ctrl+A, Ctrl+C 复制全部内容粘贴到下面输入框提交";
            // 
            // linkLabel3
            // 
            linkLabel3.AutoSize = true;
            linkLabel3.Location = new Point(862, 39);
            linkLabel3.Margin = new Padding(4, 0, 4, 0);
            linkLabel3.Name = "linkLabel3";
            linkLabel3.Size = new Size(82, 24);
            linkLabel3.TabIndex = 6;
            linkLabel3.TabStop = true;
            linkLabel3.Text = "复制域名";
            linkLabel3.LinkClicked += LinkLabel3_LinkClicked;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "assets1.xboxlive.cn (Xbox游戏下载)", "tlu.dl.delivery.mp.microsoft.com (Xbox应用下载)", "gst.prod.dl.playstation.net  (PS5 游戏下载)", "atum.hac.lp1.d4c.nintendo.net (Switch)", "origin-a.akamaihd.net (EA)", "blzddist1-a.akamaihd.net (战网)" });
            comboBox1.Location = new Point(272, 36);
            comboBox1.Margin = new Padding(4);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(584, 32);
            comboBox1.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 39);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(231, 24);
            label3.TabIndex = 4;
            label3.Text = "查询IPv4 输入需要查询域名";
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(504, 9);
            linkLabel2.Margin = new Padding(4, 0, 4, 0);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(207, 24);
            linkLabel2.TabIndex = 3;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "http://ping.chinaz.com\r\n";
            linkLabel2.LinkClicked += LinkLabel_LinkClicked;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(418, 9);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(82, 24);
            label2.TabIndex = 2;
            label2.Text = "备用网站";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(98, 9);
            linkLabel1.Margin = new Padding(4, 0, 4, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(274, 24);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://tools.ipip.net/ping.php";
            linkLabel1.LinkClicked += LinkLabel_LinkClicked;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(82, 24);
            label1.TabIndex = 0;
            label1.Text = "打开网站";
            // 
            // panel2
            // 
            panel2.Controls.Add(linkLabel4);
            panel2.Controls.Add(button1);
            panel2.Controls.Add(label5);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 542);
            panel2.Margin = new Padding(4);
            panel2.Name = "panel2";
            panel2.Size = new Size(975, 50);
            panel2.TabIndex = 1;
            // 
            // linkLabel4
            // 
            linkLabel4.AutoSize = true;
            linkLabel4.Location = new Point(884, 15);
            linkLabel4.Margin = new Padding(4, 0, 4, 0);
            linkLabel4.Name = "linkLabel4";
            linkLabel4.Size = new Size(82, 24);
            linkLabel4.TabIndex = 2;
            linkLabel4.TabStop = true;
            linkLabel4.Text = "本地文件";
            linkLabel4.LinkClicked += LinkLabel4_LinkClicked;
            // 
            // button1
            // 
            button1.Location = new Point(430, 7);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(112, 36);
            button1.TabIndex = 1;
            button1.Text = "提交";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(20, 15);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(334, 24);
            label5.TabIndex = 0;
            label5.Text = "不在上面下拉列表中的域名同样支持测速";
            // 
            // textBox1
            // 
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(0, 98);
            textBox1.Margin = new Padding(4);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(975, 444);
            textBox1.TabIndex = 2;
            // 
            // FormImportIP
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(975, 592);
            Controls.Add(textBox1);
            Controls.Add(panel2);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormImportIP";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "手动导入IP";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Label label1;
        private LinkLabel linkLabel1;
        private Label label2;
        private LinkLabel linkLabel2;
        private Label label3;
        private ComboBox comboBox1;
        private LinkLabel linkLabel3;
        private Label label4;
        private LinkLabel linkLabel4;
        private Button button1;
        private Label label5;
        private TextBox textBox1;
    }
}