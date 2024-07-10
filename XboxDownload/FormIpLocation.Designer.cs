namespace XboxDownload
{
    partial class FormIpLocation
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
            textBox1 = new TextBox();
            button1 = new Button();
            label2 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(33, 26);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(64, 24);
            label1.TabIndex = 0;
            label1.Text = "关键词";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(100, 22);
            textBox1.Margin = new Padding(4);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(220, 30);
            textBox1.TabIndex = 1;
            textBox1.KeyDown += TextBox1_KeyDown;
            // 
            // button1
            // 
            button1.Location = new Point(327, 22);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(100, 36);
            button1.TabIndex = 2;
            button1.Text = "提交";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 74);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(389, 24);
            label2.TabIndex = 3;
            label2.Text = "多个关键词请用空格分隔，比如输入“广东 电信”";
            // 
            // FormIpLocation
            // 
            AcceptButton = button1;
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(482, 116);
            Controls.Add(label2);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormIpLocation";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "归属地查找";
            Load += FormIpLocation_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Button button1;
        private Label label2;
    }
}