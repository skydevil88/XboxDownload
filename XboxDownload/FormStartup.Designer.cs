namespace XboxDownload
{
    partial class FormStartup
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
            this.cbStartup = new System.Windows.Forms.CheckBox();
            this.butSubmit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbStartup
            // 
            this.cbStartup.AutoSize = true;
            this.cbStartup.Location = new System.Drawing.Point(29, 15);
            this.cbStartup.Name = "cbStartup";
            this.cbStartup.Size = new System.Drawing.Size(99, 21);
            this.cbStartup.TabIndex = 0;
            this.cbStartup.Text = "开机自动运行";
            this.cbStartup.UseVisualStyleBackColor = true;
            // 
            // butSubmit
            // 
            this.butSubmit.Location = new System.Drawing.Point(134, 12);
            this.butSubmit.Name = "butSubmit";
            this.butSubmit.Size = new System.Drawing.Size(67, 24);
            this.butSubmit.TabIndex = 1;
            this.butSubmit.Text = "保存";
            this.butSubmit.UseVisualStyleBackColor = true;
            this.butSubmit.Click += new System.EventHandler(this.ButSubmit_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(193, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "开机启动监听 + 最小化到系统托盘";
            // 
            // FormStartup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(237, 67);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.butSubmit);
            this.Controls.Add(this.cbStartup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormStartup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设置开机自动运行";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckBox cbStartup;
        private Button butSubmit;
        private Label label1;
    }
}