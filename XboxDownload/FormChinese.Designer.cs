namespace XboxDownload
{
    partial class FormChinese
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
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            dgvGames = new DataGridView();
            Col_Name = new DataGridViewTextBoxColumn();
            Col_Note = new DataGridViewTextBoxColumn();
            Col_ProductId = new DataGridViewTextBoxColumn();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvGames).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Location = new Point(0, 784);
            tableLayoutPanel1.Margin = new Padding(4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(568, 60);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(123, 18);
            label1.Name = "label1";
            label1.Size = new Size(442, 24);
            label1.TabIndex = 0;
            label1.Text = "非国行主机安装国服游戏，可参考主机安装教程方法二";
            // 
            // dgvGames
            // 
            dgvGames.AllowUserToAddRows = false;
            dgvGames.AllowUserToDeleteRows = false;
            dgvGames.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvGames.Columns.AddRange(new DataGridViewColumn[] { Col_Name, Col_Note, Col_ProductId });
            dgvGames.Dock = DockStyle.Fill;
            dgvGames.Location = new Point(0, 0);
            dgvGames.Margin = new Padding(4);
            dgvGames.MultiSelect = false;
            dgvGames.Name = "dgvGames";
            dgvGames.ReadOnly = true;
            dgvGames.RowHeadersWidth = 40;
            dgvGames.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGames.Size = new Size(568, 784);
            dgvGames.TabIndex = 2;
            dgvGames.CellDoubleClick += DgvGames_CellDoubleClick;
            dgvGames.RowPostPaint += DgvGames_RowPostPaint;
            // 
            // Col_Name
            // 
            Col_Name.Frozen = true;
            Col_Name.HeaderText = "名称 (双击选择)";
            Col_Name.MinimumWidth = 8;
            Col_Name.Name = "Col_Name";
            Col_Name.ReadOnly = true;
            Col_Name.SortMode = DataGridViewColumnSortMode.NotSortable;
            Col_Name.Width = 175;
            // 
            // Col_Note
            // 
            Col_Note.Frozen = true;
            Col_Note.HeaderText = "备注";
            Col_Note.MinimumWidth = 8;
            Col_Note.Name = "Col_Note";
            Col_Note.ReadOnly = true;
            Col_Note.SortMode = DataGridViewColumnSortMode.NotSortable;
            Col_Note.Width = 140;
            // 
            // Col_ProductId
            // 
            Col_ProductId.HeaderText = "ProductId";
            Col_ProductId.MinimumWidth = 8;
            Col_ProductId.Name = "Col_ProductId";
            Col_ProductId.ReadOnly = true;
            Col_ProductId.SortMode = DataGridViewColumnSortMode.NotSortable;
            Col_ProductId.Visible = false;
            Col_ProductId.Width = 90;
            // 
            // FormChinese
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(568, 844);
            Controls.Add(dgvGames);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormChinese";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "国服独占中文游戏列表(主机)";
            Load += FormChinese_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvGames).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private DataGridView dgvGames;
        private DataGridViewTextBoxColumn Col_Name;
        private DataGridViewTextBoxColumn Col_Note;
        private DataGridViewTextBoxColumn Col_ProductId;
        private Label label1;
    }
}