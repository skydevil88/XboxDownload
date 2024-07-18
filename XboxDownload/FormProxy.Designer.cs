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
            groupBox1 = new GroupBox();
            checkedListBox1 = new CheckedListBox();
            groupBox2 = new GroupBox();
            textBox1 = new TextBox();
            label1 = new Label();
            cbDoh = new ComboBox();
            label2 = new Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(470, 414);
            button1.Name = "button1";
            button1.Size = new Size(262, 67);
            button1.TabIndex = 4;
            button1.Text = "保存";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkedListBox1);
            groupBox1.Location = new Point(467, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(271, 343);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "选择服务";
            // 
            // checkedListBox1
            // 
            checkedListBox1.CheckOnClick = true;
            checkedListBox1.Dock = DockStyle.Fill;
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Items.AddRange(new object[] { "Steam 商店社区", "GitHub", "Pixiv" });
            checkedListBox1.Location = new Point(3, 26);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(265, 314);
            checkedListBox1.TabIndex = 0;
            checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBox1);
            groupBox2.Location = new Point(3, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(460, 481);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "域名 (使用泛域名解析需要勾选“设置本机 DNS”)";
            // 
            // textBox1
            // 
            textBox1.BackColor = SystemColors.Window;
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(3, 26);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "*example.com | SNI(可留空) | IP(可留空)";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(454, 452);
            textBox1.TabIndex = 0;
            textBox1.WordWrap = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 487);
            label1.Name = "label1";
            label1.Size = new Size(558, 48);
            label1.TabIndex = 3;
            label1.Text = "此功能可以抵御部分网站被SNI阻断攻击无法访问的问题。\r\n*整个代理过程在用户自己的电脑上完成，并不涉及任何第三方服务。";
            // 
            // cbDoh
            // 
            cbDoh.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDoh.FormattingEnabled = true;
            cbDoh.Location = new Point(470, 376);
            cbDoh.Name = "cbDoh";
            cbDoh.Size = new Size(262, 32);
            cbDoh.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(467, 349);
            label2.Name = "label2";
            label2.Size = new Size(264, 24);
            label2.TabIndex = 5;
            label2.Text = "DoH服务器 (请使用国外服务器)";
            // 
            // FormProxy
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(743, 544);
            Controls.Add(label2);
            Controls.Add(cbDoh);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormProxy";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "本地代理服务";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private CheckedListBox checkedListBox1;
        private Button button1;
        private TextBox textBox1;
        private Label label1;
        private ComboBox cbDoh;
        private Label label2;
    }
}