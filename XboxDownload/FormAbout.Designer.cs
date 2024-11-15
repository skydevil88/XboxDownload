namespace XboxDownload
{
    partial class FormAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
            pictureBox1 = new PictureBox();
            label1 = new Label();
            linkLabel2 = new LinkLabel();
            label2 = new Label();
            lbVersion = new Label();
            label3 = new Label();
            linkLabel1 = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(33, 18);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(90, 90);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(33, 136);
            label1.Name = "label1";
            label1.Size = new Size(213, 25);
            label1.TabIndex = 1;
            label1.Text = "Xbox下载助手 By Devil";
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(33, 161);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(347, 20);
            linkLabel2.TabIndex = 3;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "https://github.com/skydevil88/XboxDownload";
            linkLabel2.LinkClicked += LinkLabel_LinkClicked;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(33, 111);
            label2.Name = "label2";
            label2.Size = new Size(341, 25);
            label2.TabIndex = 4;
            label2.Text = "如果软件对您有帮助，欢迎打赏原作者!\r\n";
            label2.Click += label2_Click;
            // 
            // lbVersion
            // 
            lbVersion.AutoSize = true;
            lbVersion.Location = new Point(150, 34);
            lbVersion.Name = "lbVersion";
            lbVersion.Size = new Size(62, 20);
            lbVersion.TabIndex = 7;
            lbVersion.Text = "版本 {0}";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            label3.Location = new Point(150, 9);
            label3.Name = "label3";
            label3.Size = new Size(197, 25);
            label3.TabIndex = 8;
            label3.Text = "Xbox下载助手 社区版";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(150, 54);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(465, 20);
            linkLabel1.TabIndex = 9;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://github.com/DDXDB/XboxDownload_CommunityEdition";
            // 
            // FormAbout
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(625, 195);
            Controls.Add(linkLabel1);
            Controls.Add(label3);
            Controls.Add(lbVersion);
            Controls.Add(label2);
            Controls.Add(linkLabel2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormAbout";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "关于";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Label label1;
        private LinkLabel linkLabel2;
        private Label label2;
        private Label lbVersion;
        private Label label3;
        private LinkLabel linkLabel1;
    }
}