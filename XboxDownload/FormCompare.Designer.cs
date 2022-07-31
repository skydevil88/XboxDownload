
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Col_Code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_Lang = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_Store = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_CurrencyCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_MSRP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_ListPrice_1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_ListPrice_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_CNY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_CNYExchangeRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_WholesalePrice_1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_WholesalePrice_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Col_Purchase = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.flowLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1328, 170);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "选择商店 (0)";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 24);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1322, 143);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Col_Code,
            this.Col_Lang,
            this.Col_Store,
            this.Col_CurrencyCode,
            this.Col_MSRP,
            this.Col_ListPrice_1,
            this.Col_ListPrice_2,
            this.Col_CNY,
            this.Col_CNYExchangeRate,
            this.Col_WholesalePrice_1,
            this.Col_WholesalePrice_2,
            this.Col_Purchase});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(3, 24);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 35;
            this.dataGridView1.RowTemplate.Height = 30;
            this.dataGridView1.Size = new System.Drawing.Size(1322, 532);
            this.dataGridView1.TabIndex = 5;
            this.dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView1_CellDoubleClick);
            this.dataGridView1.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.DataGridView1_RowPostPaint);
            // 
            // Col_Code
            // 
            this.Col_Code.HeaderText = "Code";
            this.Col_Code.MinimumWidth = 8;
            this.Col_Code.Name = "Col_Code";
            this.Col_Code.ReadOnly = true;
            this.Col_Code.Visible = false;
            this.Col_Code.Width = 150;
            // 
            // Col_Lang
            // 
            this.Col_Lang.HeaderText = "Lang";
            this.Col_Lang.MinimumWidth = 8;
            this.Col_Lang.Name = "Col_Lang";
            this.Col_Lang.ReadOnly = true;
            this.Col_Lang.Visible = false;
            this.Col_Lang.Width = 150;
            // 
            // Col_Store
            // 
            this.Col_Store.HeaderText = "商店";
            this.Col_Store.MinimumWidth = 8;
            this.Col_Store.Name = "Col_Store";
            this.Col_Store.ReadOnly = true;
            this.Col_Store.Width = 110;
            // 
            // Col_CurrencyCode
            // 
            this.Col_CurrencyCode.HeaderText = "币种";
            this.Col_CurrencyCode.MinimumWidth = 8;
            this.Col_CurrencyCode.Name = "Col_CurrencyCode";
            this.Col_CurrencyCode.ReadOnly = true;
            this.Col_CurrencyCode.Width = 55;
            // 
            // Col_MSRP
            // 
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle8.Format = "N2";
            dataGridViewCellStyle8.NullValue = null;
            this.Col_MSRP.DefaultCellStyle = dataGridViewCellStyle8;
            this.Col_MSRP.HeaderText = "建议零售价";
            this.Col_MSRP.MinimumWidth = 8;
            this.Col_MSRP.Name = "Col_MSRP";
            this.Col_MSRP.ReadOnly = true;
            this.Col_MSRP.Width = 98;
            // 
            // Col_ListPrice_1
            // 
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle9.Format = "N2";
            this.Col_ListPrice_1.DefaultCellStyle = dataGridViewCellStyle9;
            this.Col_ListPrice_1.HeaderText = "普通折扣";
            this.Col_ListPrice_1.MinimumWidth = 8;
            this.Col_ListPrice_1.Name = "Col_ListPrice_1";
            this.Col_ListPrice_1.ReadOnly = true;
            this.Col_ListPrice_1.Width = 98;
            // 
            // Col_ListPrice_2
            // 
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle10.Format = "N2";
            this.Col_ListPrice_2.DefaultCellStyle = dataGridViewCellStyle10;
            this.Col_ListPrice_2.HeaderText = "会员折扣";
            this.Col_ListPrice_2.MinimumWidth = 8;
            this.Col_ListPrice_2.Name = "Col_ListPrice_2";
            this.Col_ListPrice_2.ReadOnly = true;
            this.Col_ListPrice_2.Width = 98;
            // 
            // Col_CNY
            // 
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.Color.Red;
            dataGridViewCellStyle11.Format = "N2";
            this.Col_CNY.DefaultCellStyle = dataGridViewCellStyle11;
            this.Col_CNY.HeaderText = "CNY售价";
            this.Col_CNY.MinimumWidth = 8;
            this.Col_CNY.Name = "Col_CNY";
            this.Col_CNY.ReadOnly = true;
            this.Col_CNY.Width = 98;
            // 
            // Col_CNYExchangeRate
            // 
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Col_CNYExchangeRate.DefaultCellStyle = dataGridViewCellStyle12;
            this.Col_CNYExchangeRate.HeaderText = "CNY汇率";
            this.Col_CNYExchangeRate.MinimumWidth = 8;
            this.Col_CNYExchangeRate.Name = "Col_CNYExchangeRate";
            this.Col_CNYExchangeRate.ReadOnly = true;
            this.Col_CNYExchangeRate.Width = 98;
            // 
            // Col_WholesalePrice_1
            // 
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle13.Format = "N2";
            this.Col_WholesalePrice_1.DefaultCellStyle = dataGridViewCellStyle13;
            this.Col_WholesalePrice_1.HeaderText = "批发价";
            this.Col_WholesalePrice_1.MinimumWidth = 8;
            this.Col_WholesalePrice_1.Name = "Col_WholesalePrice_1";
            this.Col_WholesalePrice_1.ReadOnly = true;
            this.Col_WholesalePrice_1.Width = 98;
            // 
            // Col_WholesalePrice_2
            // 
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle14.Format = "N2";
            this.Col_WholesalePrice_2.DefaultCellStyle = dataGridViewCellStyle14;
            this.Col_WholesalePrice_2.HeaderText = "批发价折扣";
            this.Col_WholesalePrice_2.MinimumWidth = 8;
            this.Col_WholesalePrice_2.Name = "Col_WholesalePrice_2";
            this.Col_WholesalePrice_2.ReadOnly = true;
            this.Col_WholesalePrice_2.Width = 98;
            // 
            // Col_Purchase
            // 
            this.Col_Purchase.HeaderText = "官网";
            this.Col_Purchase.MinimumWidth = 8;
            this.Col_Purchase.Name = "Col_Purchase";
            this.Col_Purchase.ReadOnly = true;
            this.Col_Purchase.Width = 65;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer1.Size = new System.Drawing.Size(1328, 784);
            this.splitContainer1.SplitterDistance = 170;
            this.splitContainer1.TabIndex = 6;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dataGridView1);
            this.groupBox2.Controls.Add(this.panel1);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1328, 610);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "信息";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.linkLabel2);
            this.panel1.Controls.Add(this.linkLabel1);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(3, 556);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1322, 51);
            this.panel1.TabIndex = 6;
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(58, 16);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(80, 18);
            this.linkLabel2.TabIndex = 3;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "取消选择";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel2_LinkClicked);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(9, 16);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(44, 18);
            this.linkLabel1.TabIndex = 1;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "全选";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1_LinkClicked);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(603, 9);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 34);
            this.button1.TabIndex = 0;
            this.button1.Text = "查询";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // FormCompare
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1328, 784);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCompare";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "比价";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormCompare_FormClosing);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_Code;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_Lang;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_Store;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_CurrencyCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_MSRP;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_ListPrice_1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_ListPrice_2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_CNY;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_CNYExchangeRate;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_WholesalePrice_1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_WholesalePrice_2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Col_Purchase;
    }
}