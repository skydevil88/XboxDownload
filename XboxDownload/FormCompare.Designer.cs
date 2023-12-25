namespace XboxDownload
{
    partial class FormCompare
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
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            groupBox1 = new GroupBox();
            flowLayoutPanel1 = new FlowLayoutPanel();
            groupBox2 = new GroupBox();
            dataGridView1 = new DataGridView();
            Col_Code = new DataGridViewTextBoxColumn();
            Col_Lang = new DataGridViewTextBoxColumn();
            Col_Store = new DataGridViewTextBoxColumn();
            Col_CurrencyCode = new DataGridViewTextBoxColumn();
            Col_MSRP = new DataGridViewTextBoxColumn();
            Col_ListPrice_1 = new DataGridViewTextBoxColumn();
            Col_ListPrice_2 = new DataGridViewTextBoxColumn();
            Col_CNY = new DataGridViewTextBoxColumn();
            Col_CNYExchangeRate = new DataGridViewTextBoxColumn();
            Col_WholesalePrice_1 = new DataGridViewTextBoxColumn();
            Col_WholesalePrice_2 = new DataGridViewTextBoxColumn();
            Col_Purchase = new DataGridViewTextBoxColumn();
            panel1 = new Panel();
            button1 = new Button();
            linkLabel2 = new LinkLabel();
            linkLabel1 = new LinkLabel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(flowLayoutPanel1);
            groupBox1.Dock = DockStyle.Top;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Margin = new Padding(4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4);
            groupBox1.Size = new Size(1328, 172);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "选择商店 (0)";
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(4, 27);
            flowLayoutPanel1.Margin = new Padding(4);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1320, 141);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(dataGridView1);
            groupBox2.Controls.Add(panel1);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(0, 172);
            groupBox2.Margin = new Padding(4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4);
            groupBox2.Size = new Size(1328, 612);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "信息";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Col_Code, Col_Lang, Col_Store, Col_CurrencyCode, Col_MSRP, Col_ListPrice_1, Col_ListPrice_2, Col_CNY, Col_CNYExchangeRate, Col_WholesalePrice_1, Col_WholesalePrice_2, Col_Purchase });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(4, 27);
            dataGridView1.Margin = new Padding(4);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersWidth = 40;
            dataGridView1.Size = new Size(1320, 530);
            dataGridView1.TabIndex = 1;
            dataGridView1.CellDoubleClick += DataGridView1_CellDoubleClick;
            dataGridView1.RowPostPaint += DataGridView1_RowPostPaint;
            // 
            // Col_Code
            // 
            Col_Code.HeaderText = "Code";
            Col_Code.MinimumWidth = 8;
            Col_Code.Name = "Col_Code";
            Col_Code.ReadOnly = true;
            Col_Code.Visible = false;
            Col_Code.Width = 150;
            // 
            // Col_Lang
            // 
            Col_Lang.HeaderText = "Lang";
            Col_Lang.MinimumWidth = 8;
            Col_Lang.Name = "Col_Lang";
            Col_Lang.ReadOnly = true;
            Col_Lang.Visible = false;
            Col_Lang.Width = 150;
            // 
            // Col_Store
            // 
            Col_Store.HeaderText = "商店";
            Col_Store.MinimumWidth = 8;
            Col_Store.Name = "Col_Store";
            Col_Store.ReadOnly = true;
            Col_Store.Width = 110;
            // 
            // Col_CurrencyCode
            // 
            Col_CurrencyCode.HeaderText = "币种";
            Col_CurrencyCode.MinimumWidth = 8;
            Col_CurrencyCode.Name = "Col_CurrencyCode";
            Col_CurrencyCode.ReadOnly = true;
            Col_CurrencyCode.Width = 55;
            // 
            // Col_MSRP
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle1.Format = "N2";
            Col_MSRP.DefaultCellStyle = dataGridViewCellStyle1;
            Col_MSRP.HeaderText = "建议零售价";
            Col_MSRP.MinimumWidth = 8;
            Col_MSRP.Name = "Col_MSRP";
            Col_MSRP.ReadOnly = true;
            Col_MSRP.Width = 98;
            // 
            // Col_ListPrice_1
            // 
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "N2";
            Col_ListPrice_1.DefaultCellStyle = dataGridViewCellStyle2;
            Col_ListPrice_1.HeaderText = "普通折扣";
            Col_ListPrice_1.MinimumWidth = 8;
            Col_ListPrice_1.Name = "Col_ListPrice_1";
            Col_ListPrice_1.ReadOnly = true;
            Col_ListPrice_1.Width = 98;
            // 
            // Col_ListPrice_2
            // 
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "N2";
            Col_ListPrice_2.DefaultCellStyle = dataGridViewCellStyle3;
            Col_ListPrice_2.HeaderText = "会员折扣";
            Col_ListPrice_2.MinimumWidth = 8;
            Col_ListPrice_2.Name = "Col_ListPrice_2";
            Col_ListPrice_2.ReadOnly = true;
            Col_ListPrice_2.Width = 98;
            // 
            // Col_CNY
            // 
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle4.Format = "N2";
            Col_CNY.DefaultCellStyle = dataGridViewCellStyle4;
            Col_CNY.HeaderText = "CNY售价";
            Col_CNY.MinimumWidth = 8;
            Col_CNY.Name = "Col_CNY";
            Col_CNY.ReadOnly = true;
            Col_CNY.Width = 98;
            // 
            // Col_CNYExchangeRate
            // 
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleRight;
            Col_CNYExchangeRate.DefaultCellStyle = dataGridViewCellStyle5;
            Col_CNYExchangeRate.HeaderText = "CNY汇率";
            Col_CNYExchangeRate.MinimumWidth = 8;
            Col_CNYExchangeRate.Name = "Col_CNYExchangeRate";
            Col_CNYExchangeRate.ReadOnly = true;
            Col_CNYExchangeRate.Width = 98;
            // 
            // Col_WholesalePrice_1
            // 
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.Format = "N2";
            Col_WholesalePrice_1.DefaultCellStyle = dataGridViewCellStyle6;
            Col_WholesalePrice_1.HeaderText = "批发价";
            Col_WholesalePrice_1.MinimumWidth = 8;
            Col_WholesalePrice_1.Name = "Col_WholesalePrice_1";
            Col_WholesalePrice_1.ReadOnly = true;
            Col_WholesalePrice_1.Width = 98;
            // 
            // Col_WholesalePrice_2
            // 
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle7.Format = "N2";
            Col_WholesalePrice_2.DefaultCellStyle = dataGridViewCellStyle7;
            Col_WholesalePrice_2.HeaderText = "批发价折扣";
            Col_WholesalePrice_2.MinimumWidth = 8;
            Col_WholesalePrice_2.Name = "Col_WholesalePrice_2";
            Col_WholesalePrice_2.ReadOnly = true;
            Col_WholesalePrice_2.Width = 98;
            // 
            // Col_Purchase
            // 
            Col_Purchase.HeaderText = "官网";
            Col_Purchase.MinimumWidth = 8;
            Col_Purchase.Name = "Col_Purchase";
            Col_Purchase.ReadOnly = true;
            Col_Purchase.Width = 65;
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(linkLabel2);
            panel1.Controls.Add(linkLabel1);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(4, 557);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(1320, 51);
            panel1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(603, 9);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(112, 36);
            button1.TabIndex = 2;
            button1.Text = "查询";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(58, 16);
            linkLabel2.Margin = new Padding(4, 0, 4, 0);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(82, 24);
            linkLabel2.TabIndex = 1;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "取消选择";
            linkLabel2.LinkClicked += LinkLabel2_LinkClicked;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(9, 16);
            linkLabel1.Margin = new Padding(4, 0, 4, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(46, 24);
            linkLabel1.TabIndex = 0;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "全选";
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
            // 
            // FormCompare
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1328, 784);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormCompare";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "比价";
            FormClosing += FormCompare_FormClosing;
            Load += FormCompare_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private FlowLayoutPanel flowLayoutPanel1;
        private GroupBox groupBox2;
        private Panel panel1;
        private DataGridView dataGridView1;
        private Button button1;
        private LinkLabel linkLabel2;
        private LinkLabel linkLabel1;
        private DataGridViewTextBoxColumn Col_Code;
        private DataGridViewTextBoxColumn Col_Lang;
        private DataGridViewTextBoxColumn Col_Store;
        private DataGridViewTextBoxColumn Col_CurrencyCode;
        private DataGridViewTextBoxColumn Col_MSRP;
        private DataGridViewTextBoxColumn Col_ListPrice_1;
        private DataGridViewTextBoxColumn Col_ListPrice_2;
        private DataGridViewTextBoxColumn Col_CNY;
        private DataGridViewTextBoxColumn Col_CNYExchangeRate;
        private DataGridViewTextBoxColumn Col_WholesalePrice_1;
        private DataGridViewTextBoxColumn Col_WholesalePrice_2;
        private DataGridViewTextBoxColumn Col_Purchase;
    }
}