namespace XboxDownload
{
    partial class FormDoH
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            butSave = new Button();
            groupBox1 = new GroupBox();
            label3 = new Label();
            label1 = new Label();
            cbDoh = new ComboBox();
            groupBox2 = new GroupBox();
            dataGridView1 = new DataGridView();
            Column1 = new DataGridViewCheckBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            Column5 = new DataGridViewTextBoxColumn();
            tableLayoutPanel1 = new TableLayoutPanel();
            butTest = new Button();
            label2 = new Label();
            linkLabel1 = new LinkLabel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // butSave
            // 
            butSave.Location = new Point(237, 31);
            butSave.Name = "butSave";
            butSave.Size = new Size(112, 34);
            butSave.TabIndex = 5;
            butSave.Text = "保存";
            butSave.UseVisualStyleBackColor = true;
            butSave.Click += ButSave_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(cbDoh);
            groupBox1.Controls.Add(butSave);
            groupBox1.Location = new Point(13, 11);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(763, 87);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "DoH 服务器";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(378, 23);
            label3.Name = "label3";
            label3.Size = new Size(391, 48);
            label3.TabIndex = 7;
            label3.Text = "DNS污染严重可以选用国外服务器，平时不建议\r\nPC用户使用此功能，需要勾选“设置本机 DNS”";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 36);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 6;
            label1.Text = "选择";
            // 
            // cbDoh
            // 
            cbDoh.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDoh.FormattingEnabled = true;
            cbDoh.Location = new Point(61, 33);
            cbDoh.Name = "cbDoh";
            cbDoh.Size = new Size(170, 32);
            cbDoh.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(dataGridView1);
            groupBox2.Controls.Add(tableLayoutPanel1);
            groupBox2.Location = new Point(13, 104);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(761, 245);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "测速";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5 });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 26);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.RowTemplate.Height = 32;
            dataGridView1.Size = new Size(755, 168);
            dataGridView1.TabIndex = 1;
            // 
            // Column1
            // 
            Column1.HeaderText = "选择";
            Column1.MinimumWidth = 8;
            Column1.Name = "Column1";
            Column1.Width = 43;
            // 
            // Column2
            // 
            Column2.HeaderText = "DoH 服务器";
            Column2.MinimumWidth = 8;
            Column2.Name = "Column2";
            Column2.ReadOnly = true;
            Column2.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column2.Width = 160;
            // 
            // Column3
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle1.Format = "N0";
            dataGridViewCellStyle1.NullValue = null;
            Column3.DefaultCellStyle = dataGridViewCellStyle1;
            Column3.HeaderText = "测试1";
            Column3.MinimumWidth = 8;
            Column3.Name = "Column3";
            Column3.ReadOnly = true;
            Column3.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column3.Width = 90;
            // 
            // Column4
            // 
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "N0";
            Column4.DefaultCellStyle = dataGridViewCellStyle2;
            Column4.HeaderText = "测试2";
            Column4.MinimumWidth = 8;
            Column4.Name = "Column4";
            Column4.ReadOnly = true;
            Column4.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column4.Width = 90;
            // 
            // Column5
            // 
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "N0";
            Column5.DefaultCellStyle = dataGridViewCellStyle3;
            Column5.HeaderText = "测试3";
            Column5.MinimumWidth = 8;
            Column5.Name = "Column5";
            Column5.ReadOnly = true;
            Column5.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column5.Width = 90;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Controls.Add(butTest, 1, 0);
            tableLayoutPanel1.Controls.Add(linkLabel1, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Location = new Point(3, 194);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(755, 48);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // butTest
            // 
            butTest.Anchor = AnchorStyles.None;
            butTest.Location = new Point(321, 7);
            butTest.Name = "butTest";
            butTest.Size = new Size(112, 34);
            butTest.TabIndex = 0;
            butTest.Text = "测试";
            butTest.UseVisualStyleBackColor = true;
            butTest.Click += ButTest_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 352);
            label2.Name = "label2";
            label2.Size = new Size(763, 48);
            label2.TabIndex = 2;
            label2.Text = "DNS over HTTPS (DoH) 是一种使用 HTTPS 加密 DNS 请求和响应的安全协议。通过使用基于 \r\nHTTPS 的 DNS，可以帮助保护您的在线隐私和安全，并确保您的 DNS 请求不被拦截或篡改。";
            // 
            // linkLabel1
            // 
            linkLabel1.Anchor = AnchorStyles.Right;
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(652, 12);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(100, 24);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "添加服务器";
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
            // 
            // FormDoH
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(790, 404);
            Controls.Add(label2);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormDoH";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "设置加密DNS";
            Load += FormDoH_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button butSave;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private DataGridView dataGridView1;
        private TableLayoutPanel tableLayoutPanel1;
        private Button butTest;
        private Label label2;
        private ComboBox cbDoh;
        private Label label1;
        private Label label3;
        private DataGridViewCheckBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private LinkLabel linkLabel1;
    }
}