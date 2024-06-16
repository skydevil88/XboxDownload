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
            linkLabel1 = new LinkLabel();
            cbCheckAll = new CheckBox();
            label2 = new Label();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            dataGridView2 = new DataGridView();
            Col_Enable = new DataGridViewCheckBoxColumn();
            Col_Host = new DataGridViewTextBoxColumn();
            Col_DoHServer = new DataGridViewComboBoxColumn();
            Col_Remark = new DataGridViewTextBoxColumn();
            tableLayoutPanel2 = new TableLayoutPanel();
            label4 = new Label();
            butDohSave = new Button();
            butDohReset = new Button();
            linkLabel2 = new LinkLabel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // butSave
            // 
            butSave.Location = new Point(237, 21);
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
            groupBox1.Location = new Point(6, 6);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(956, 69);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "DoH 服务器";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(559, 18);
            label3.Name = "label3";
            label3.Size = new Size(391, 48);
            label3.TabIndex = 7;
            label3.Text = "DNS污染严重可以选用国外服务器，平时不建议\r\nPC用户使用此功能，需要勾选“设置本机 DNS”";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 26);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 6;
            label1.Text = "选择";
            // 
            // cbDoh
            // 
            cbDoh.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDoh.FormattingEnabled = true;
            cbDoh.Location = new Point(61, 23);
            cbDoh.Name = "cbDoh";
            cbDoh.Size = new Size(170, 32);
            cbDoh.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(dataGridView1);
            groupBox2.Controls.Add(tableLayoutPanel1);
            groupBox2.Location = new Point(8, 81);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(954, 270);
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
            dataGridView1.RowHeadersWidth = 40;
            dataGridView1.RowTemplate.Height = 32;
            dataGridView1.Size = new Size(948, 193);
            dataGridView1.TabIndex = 1;
            dataGridView1.RowPostPaint += Dgv_RowPostPaint;
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
            tableLayoutPanel1.Controls.Add(cbCheckAll, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Location = new Point(3, 219);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(948, 48);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // butTest
            // 
            butTest.Anchor = AnchorStyles.None;
            butTest.Location = new Point(417, 7);
            butTest.Name = "butTest";
            butTest.Size = new Size(112, 34);
            butTest.TabIndex = 0;
            butTest.Text = "测试";
            butTest.UseVisualStyleBackColor = true;
            butTest.Click += ButTest_Click;
            // 
            // linkLabel1
            // 
            linkLabel1.Anchor = AnchorStyles.Right;
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(791, 12);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(154, 24);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "添加自定义服务器";
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
            // 
            // cbCheckAll
            // 
            cbCheckAll.Anchor = AnchorStyles.Left;
            cbCheckAll.AutoSize = true;
            cbCheckAll.Checked = true;
            cbCheckAll.CheckState = CheckState.Checked;
            cbCheckAll.Location = new Point(3, 10);
            cbCheckAll.Name = "cbCheckAll";
            cbCheckAll.Size = new Size(72, 28);
            cbCheckAll.TabIndex = 2;
            cbCheckAll.Text = "全选";
            cbCheckAll.UseVisualStyleBackColor = true;
            cbCheckAll.CheckedChanged += CbCheckAll_CheckedChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 354);
            label2.Name = "label2";
            label2.Size = new Size(903, 48);
            label2.TabIndex = 2;
            label2.Text = "DNS over HTTPS (DoH) 是一种使用 HTTPS 加密 DNS 请求和响应的安全协议。通过使用基于 HTTPS 的 DNS，\r\n可以帮助保护您的在线隐私和安全，并确保您的 DNS 请求不被拦截或篡改。";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(978, 444);
            tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(groupBox2);
            tabPage1.Location = new Point(4, 33);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(970, 407);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "全局设置";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(dataGridView2);
            tabPage2.Controls.Add(tableLayoutPanel2);
            tabPage2.Location = new Point(4, 33);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(970, 407);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "强制加密";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView2
            // 
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Columns.AddRange(new DataGridViewColumn[] { Col_Enable, Col_Host, Col_DoHServer, Col_Remark });
            dataGridView2.Dock = DockStyle.Fill;
            dataGridView2.Location = new Point(3, 3);
            dataGridView2.MultiSelect = false;
            dataGridView2.Name = "dataGridView2";
            dataGridView2.RowHeadersWidth = 40;
            dataGridView2.RowTemplate.Height = 32;
            dataGridView2.Size = new Size(964, 353);
            dataGridView2.TabIndex = 2;
            dataGridView2.CellValueChanged += DataGridView2_CellValueChanged;
            dataGridView2.RowPostPaint += Dgv_RowPostPaint;
            dataGridView2.UserAddedRow += DataGridView2_UserAddedRow;
            // 
            // Col_Enable
            // 
            Col_Enable.DataPropertyName = "Enable";
            Col_Enable.HeaderText = "启用";
            Col_Enable.MinimumWidth = 8;
            Col_Enable.Name = "Col_Enable";
            Col_Enable.Resizable = DataGridViewTriState.False;
            Col_Enable.Width = 43;
            // 
            // Col_Host
            // 
            Col_Host.DataPropertyName = "Host";
            Col_Host.HeaderText = "域名 (无视“加密 DNS”是否勾选)";
            Col_Host.MinimumWidth = 8;
            Col_Host.Name = "Col_Host";
            Col_Host.SortMode = DataGridViewColumnSortMode.NotSortable;
            Col_Host.Width = 220;
            // 
            // Col_DoHServer
            // 
            Col_DoHServer.DataPropertyName = "DoHServer";
            Col_DoHServer.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Col_DoHServer.HeaderText = "DoH服务器";
            Col_DoHServer.MinimumWidth = 8;
            Col_DoHServer.Name = "Col_DoHServer";
            Col_DoHServer.Width = 110;
            // 
            // Col_Remark
            // 
            Col_Remark.DataPropertyName = "Remark";
            Col_Remark.HeaderText = "备注";
            Col_Remark.MinimumWidth = 8;
            Col_Remark.Name = "Col_Remark";
            Col_Remark.SortMode = DataGridViewColumnSortMode.NotSortable;
            Col_Remark.Width = 210;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 4;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel2.Controls.Add(label4, 0, 0);
            tableLayoutPanel2.Controls.Add(butDohSave, 1, 0);
            tableLayoutPanel2.Controls.Add(butDohReset, 2, 0);
            tableLayoutPanel2.Controls.Add(linkLabel2, 3, 0);
            tableLayoutPanel2.Dock = DockStyle.Bottom;
            tableLayoutPanel2.Location = new Point(3, 356);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(964, 48);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Left;
            label4.AutoSize = true;
            label4.Location = new Point(3, 12);
            label4.Name = "label4";
            label4.Size = new Size(352, 24);
            label4.TabIndex = 3;
            label4.Text = "删除单条记录点击左边序号然后按delete键";
            // 
            // butDohSave
            // 
            butDohSave.Anchor = AnchorStyles.None;
            butDohSave.Location = new Point(388, 7);
            butDohSave.Name = "butDohSave";
            butDohSave.Size = new Size(90, 34);
            butDohSave.TabIndex = 2;
            butDohSave.Text = "保存";
            butDohSave.UseVisualStyleBackColor = true;
            butDohSave.Click += ButDoHSave_Click;
            // 
            // butDohReset
            // 
            butDohReset.Anchor = AnchorStyles.None;
            butDohReset.Location = new Point(484, 7);
            butDohReset.Name = "butDohReset";
            butDohReset.Size = new Size(90, 34);
            butDohReset.TabIndex = 4;
            butDohReset.Text = "重置";
            butDohReset.UseVisualStyleBackColor = true;
            butDohReset.Click += ButDohReset_Click;
            // 
            // linkLabel2
            // 
            linkLabel2.Anchor = AnchorStyles.Right;
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(879, 12);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(82, 24);
            linkLabel2.TabIndex = 5;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "使用说明";
            linkLabel2.LinkClicked += LinkLabel2_LinkClicked;
            // 
            // FormDoH
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(978, 444);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormDoH";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "设置加密DNS";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
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
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private DataGridView dataGridView2;
        private TableLayoutPanel tableLayoutPanel2;
        private Button butDohSave;
        private Label label4;
        private Button butDohReset;
        private DataGridViewCheckBoxColumn Col_Enable;
        private DataGridViewTextBoxColumn Col_Host;
        private DataGridViewComboBoxColumn Col_DoHServer;
        private DataGridViewTextBoxColumn Col_Remark;
        private CheckBox cbCheckAll;
        private LinkLabel linkLabel2;
    }
}