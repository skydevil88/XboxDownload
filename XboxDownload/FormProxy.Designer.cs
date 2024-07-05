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
            button1.Location = new Point(390, 411);
            button1.Name = "button1";
            button1.Size = new Size(278, 67);
            button1.TabIndex = 0;
            button1.Text = "保存";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkedListBox1);
            groupBox1.Location = new Point(387, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(284, 343);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "选择服务";
            // 
            // checkedListBox1
            // 
            checkedListBox1.CheckOnClick = true;
            checkedListBox1.Dock = DockStyle.Fill;
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Items.AddRange(new object[] { "Steam 商店社区" });
            checkedListBox1.Location = new Point(3, 26);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(278, 314);
            checkedListBox1.TabIndex = 0;
            checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBox1);
            groupBox2.Location = new Point(0, 0);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(381, 481);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "域名";
            // 
            // textBox1
            // 
            textBox1.BackColor = SystemColors.Window;
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(3, 26);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(375, 452);
            textBox1.TabIndex = 0;
            textBox1.WordWrap = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 487);
            label1.Name = "label1";
            label1.Size = new Size(644, 48);
            label1.TabIndex = 3;
            label1.Text = "此功能可以改善部分网站被 SNI 阻断无法访问的问题，其它域名可以自行尝试。\r\n*所有流量直连网站，没有经过第三方中转。";
            // 
            // cbDoh
            // 
            cbDoh.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDoh.FormattingEnabled = true;
            cbDoh.Location = new Point(390, 373);
            cbDoh.Name = "cbDoh";
            cbDoh.Size = new Size(281, 32);
            cbDoh.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(390, 346);
            label2.Name = "label2";
            label2.Size = new Size(264, 24);
            label2.TabIndex = 5;
            label2.Text = "DoH服务器 (请使用国外服务器)";
            // 
            // FormProxy
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(683, 543);
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