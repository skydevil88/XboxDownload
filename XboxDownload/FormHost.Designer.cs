namespace XboxDownload
{
    partial class FormHost
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
            groupBox1 = new GroupBox();
            butConfirm = new Button();
            tbIP = new TextBox();
            label2 = new Label();
            tbHost = new TextBox();
            label1 = new Label();
            dataGridView1 = new DataGridView();
            Column1 = new DataGridViewCheckBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            Column5 = new DataGridViewTextBoxColumn();
            groupBox2 = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            butTest = new Button();
            linkLabel1 = new LinkLabel();
            panel1 = new Panel();
            rbIPv6 = new RadioButton();
            rbIPv4 = new RadioButton();
            cbCheckAll = new CheckBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            groupBox2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(butConfirm);
            groupBox1.Controls.Add(tbIP);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(tbHost);
            groupBox1.Controls.Add(label1);
            groupBox1.Dock = DockStyle.Top;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(1208, 85);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "域名解释";
            // 
            // butConfirm
            // 
            butConfirm.Enabled = false;
            butConfirm.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            butConfirm.ForeColor = Color.Green;
            butConfirm.Location = new Point(1034, 31);
            butConfirm.Name = "butConfirm";
            butConfirm.Size = new Size(112, 34);
            butConfirm.TabIndex = 4;
            butConfirm.Text = "确认";
            butConfirm.UseVisualStyleBackColor = true;
            butConfirm.Click += ButConfirm_Click;
            // 
            // tbIP
            // 
            tbIP.BackColor = SystemColors.Window;
            tbIP.Location = new Point(648, 32);
            tbIP.Name = "tbIP";
            tbIP.ReadOnly = true;
            tbIP.Size = new Size(380, 30);
            tbIP.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(575, 35);
            label2.Name = "label2";
            label2.Size = new Size(67, 24);
            label2.TabIndex = 2;
            label2.Text = "IP 地址";
            // 
            // tbHost
            // 
            tbHost.Location = new Point(64, 32);
            tbHost.Name = "tbHost";
            tbHost.Size = new Size(505, 30);
            tbHost.TabIndex = 1;
            tbHost.Validating += TbHost_Validating;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 35);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 0;
            label1.Text = "域名";
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
            dataGridView1.RowHeadersWidth = 40;
            dataGridView1.RowTemplate.Height = 32;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(1202, 385);
            dataGridView1.TabIndex = 2;
            dataGridView1.CellDoubleClick += DataGridView1_CellDoubleClick;
            dataGridView1.RowPostPaint += Dgv_RowPostPaint;
            // 
            // Column1
            // 
            Column1.HeaderText = "选择";
            Column1.MinimumWidth = 8;
            Column1.Name = "Column1";
            Column1.Resizable = DataGridViewTriState.False;
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
            Column3.HeaderText = "IP 地址";
            Column3.MinimumWidth = 8;
            Column3.Name = "Column3";
            Column3.ReadOnly = true;
            Column3.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column3.Width = 140;
            // 
            // Column4
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Column4.DefaultCellStyle = dataGridViewCellStyle1;
            Column4.HeaderText = "TLS检测";
            Column4.MinimumWidth = 8;
            Column4.Name = "Column4";
            Column4.ReadOnly = true;
            Column4.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column4.Width = 60;
            // 
            // Column5
            // 
            Column5.HeaderText = "位置（信息只供参考，不保证准确）";
            Column5.MinimumWidth = 8;
            Column5.Name = "Column5";
            Column5.ReadOnly = true;
            Column5.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column5.Width = 310;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(dataGridView1);
            groupBox2.Controls.Add(tableLayoutPanel1);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(0, 85);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1208, 459);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "解释域名IP（双击选择）";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel1.Controls.Add(butTest, 1, 0);
            tableLayoutPanel1.Controls.Add(linkLabel1, 2, 0);
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Location = new Point(3, 411);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1202, 45);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // butTest
            // 
            butTest.Anchor = AnchorStyles.None;
            butTest.Location = new Point(544, 5);
            butTest.Name = "butTest";
            butTest.Size = new Size(112, 34);
            butTest.TabIndex = 0;
            butTest.Text = "查询";
            butTest.UseVisualStyleBackColor = true;
            butTest.Click += ButTest_Click;
            // 
            // linkLabel1
            // 
            linkLabel1.Anchor = AnchorStyles.Right;
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(1050, 10);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(149, 24);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "添加 DoH 服务器";
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
            // 
            // panel1
            // 
            panel1.Controls.Add(rbIPv6);
            panel1.Controls.Add(rbIPv4);
            panel1.Controls.Add(cbCheckAll);
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(315, 39);
            panel1.TabIndex = 0;
            // 
            // rbIPv6
            // 
            rbIPv6.AutoSize = true;
            rbIPv6.Location = new Point(164, 5);
            rbIPv6.Name = "rbIPv6";
            rbIPv6.Size = new Size(71, 28);
            rbIPv6.TabIndex = 1;
            rbIPv6.Text = "IPv6";
            rbIPv6.UseVisualStyleBackColor = true;
            // 
            // rbIPv4
            // 
            rbIPv4.AutoSize = true;
            rbIPv4.Checked = true;
            rbIPv4.Location = new Point(87, 5);
            rbIPv4.Name = "rbIPv4";
            rbIPv4.Size = new Size(71, 28);
            rbIPv4.TabIndex = 0;
            rbIPv4.TabStop = true;
            rbIPv4.Text = "IPv4";
            rbIPv4.UseVisualStyleBackColor = true;
            // 
            // cbCheckAll
            // 
            cbCheckAll.Anchor = AnchorStyles.Left;
            cbCheckAll.AutoSize = true;
            cbCheckAll.Checked = true;
            cbCheckAll.CheckState = CheckState.Checked;
            cbCheckAll.Location = new Point(9, 6);
            cbCheckAll.Name = "cbCheckAll";
            cbCheckAll.Size = new Size(72, 28);
            cbCheckAll.TabIndex = 2;
            cbCheckAll.Text = "全选";
            cbCheckAll.UseVisualStyleBackColor = true;
            cbCheckAll.CheckedChanged += CbCheckAll_CheckedChanged;
            // 
            // FormHost
            // 
            AcceptButton = butTest;
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1208, 544);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormHost";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "添加域名";
            Load += FormHost_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            groupBox2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Label label1;
        private Label label2;
        private TextBox tbHost;
        private TextBox tbIP;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox2;
        private Panel panel1;
        private RadioButton rbIPv6;
        private RadioButton rbIPv4;
        private DataGridView dataGridView1;
        private Button butTest;
        private LinkLabel linkLabel1;
        private CheckBox cbCheckAll;
        private Button butConfirm;
        private DataGridViewCheckBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
    }
}