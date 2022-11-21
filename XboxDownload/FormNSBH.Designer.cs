namespace XboxDownload
{
    partial class FormNSBH
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
            this.tbNSHomepage = new System.Windows.Forms.TextBox();
            this.butSubmit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "主页地址：";
            // 
            // tbNSHomepage
            // 
            this.tbNSHomepage.Location = new System.Drawing.Point(77, 13);
            this.tbNSHomepage.Name = "tbNSHomepage";
            this.tbNSHomepage.Size = new System.Drawing.Size(262, 23);
            this.tbNSHomepage.TabIndex = 1;
            // 
            // butSubmit
            // 
            this.butSubmit.Location = new System.Drawing.Point(342, 11);
            this.butSubmit.Name = "butSubmit";
            this.butSubmit.Size = new System.Drawing.Size(75, 24);
            this.butSubmit.TabIndex = 2;
            this.butSubmit.Text = "保存";
            this.butSubmit.UseVisualStyleBackColor = true;
            this.butSubmit.Click += new System.EventHandler(this.ButSubmit_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(363, 68);
            this.label2.TabIndex = 3;
            this.label2.Text = "使用说明：\r\n1、进入设置中的互联网设置，把DNS设置为本机IP。\r\n2、然后选择连接到此网络，随后Switch会显示连接网络需要验证。\r\n3、点击下一步将会弹出" +
    "浏览器并且自动打开设定的主页地址。";
            // 
            // FormNSBH
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(427, 115);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.butSubmit);
            this.Controls.Add(this.tbNSHomepage);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormNSBH";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设置NS浏览器主页";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label label1;
        private TextBox tbNSHomepage;
        private Button butSubmit;
        private Label label2;
    }
}