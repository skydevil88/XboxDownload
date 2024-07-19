namespace XboxDownload
{
    partial class FormProxy
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
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(470, 431);
            button1.Name = "button1";
            button1.Size = new Size(262, 50);
            button1.TabIndex = 0;
            button1.Text = "保存";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkedListBox1);
            groupBox2.Location = new Point(467, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(271, 155);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "选择服务";
            // 
            // checkedListBox1
            // 
            checkedListBox1.Dock = DockStyle.Fill;
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Items.AddRange(new object[] { "Steam 商店社区", "GitHub", "Pixiv" });
            checkedListBox1.Location = new Point(3, 26);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(265, 126);
            checkedListBox1.TabIndex = 0;
            checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox1);
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(460, 478);
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
            textBox1.PlaceholderText = "*example.com | SNI (可留空) | IP (可留空)";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(454, 449);
            textBox1.TabIndex = 0;
            textBox1.WordWrap = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 487);
            label1.Name = "label1";
            label1.Size = new Size(673, 48);
            label1.TabIndex = 3;
            label1.Text = "此功能可以抵御部分网站被SNI阻断攻击无法访问的问题，DoH服务器请使用国外。\r\n*整个代理过程在用户自己的电脑上完成，并不涉及任何第三方服务。";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(checkedListBox2);
            groupBox3.Location = new Point(467, 164);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(271, 261);
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
            checkedListBox2.Size = new Size(265, 232);
            checkedListBox2.TabIndex = 0;
            checkedListBox2.SelectedIndexChanged += CheckedListBox2_SelectedIndexChanged;
            // 
            // FormProxy
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(743, 544);
            Controls.Add(groupBox3);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormProxy";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "本地代理服务";
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox3.ResumeLayout(false);
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
    }
}