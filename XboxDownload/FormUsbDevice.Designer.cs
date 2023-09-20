namespace XboxDownload
{
    partial class FormUsbDevice
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
            dgvDevice = new DataGridView();
            Column1 = new DataGridViewTextBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            Column5 = new DataGridViewTextBoxColumn();
            Column6 = new DataGridViewTextBoxColumn();
            Column7 = new DataGridViewTextBoxColumn();
            button1 = new Button();
            button2 = new Button();
            label1 = new Label();
            rbGPT = new RadioButton();
            rbMBR = new RadioButton();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvDevice).BeginInit();
            SuspendLayout();
            // 
            // dgvDevice
            // 
            dgvDevice.AllowUserToAddRows = false;
            dgvDevice.AllowUserToDeleteRows = false;
            dgvDevice.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDevice.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6, Column7 });
            dgvDevice.Location = new Point(8, 8);
            dgvDevice.Margin = new Padding(2);
            dgvDevice.MultiSelect = false;
            dgvDevice.Name = "dgvDevice";
            dgvDevice.ReadOnly = true;
            dgvDevice.RowHeadersWidth = 40;
            dgvDevice.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDevice.ShowCellToolTips = false;
            dgvDevice.Size = new Size(816, 161);
            dgvDevice.TabIndex = 2;
            dgvDevice.CellClick += DgvDevice_CellClick;
            // 
            // Column1
            // 
            Column1.HeaderText = "设备编号";
            Column1.MinimumWidth = 8;
            Column1.Name = "Column1";
            Column1.ReadOnly = true;
            Column1.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column1.Width = 140;
            // 
            // Column2
            // 
            Column2.HeaderText = "型号";
            Column2.MinimumWidth = 8;
            Column2.Name = "Column2";
            Column2.ReadOnly = true;
            Column2.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column2.Width = 280;
            // 
            // Column3
            // 
            Column3.HeaderText = "接口";
            Column3.MinimumWidth = 8;
            Column3.Name = "Column3";
            Column3.ReadOnly = true;
            Column3.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column3.Width = 70;
            // 
            // Column4
            // 
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleRight;
            Column4.DefaultCellStyle = dataGridViewCellStyle1;
            Column4.HeaderText = "容量";
            Column4.MinimumWidth = 8;
            Column4.Name = "Column4";
            Column4.ReadOnly = true;
            Column4.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column4.Width = 70;
            // 
            // Column5
            // 
            Column5.HeaderText = "分区表";
            Column5.MinimumWidth = 8;
            Column5.Name = "Column5";
            Column5.ReadOnly = true;
            Column5.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column5.Width = 65;
            // 
            // Column6
            // 
            Column6.HeaderText = "分区数";
            Column6.MinimumWidth = 8;
            Column6.Name = "Column6";
            Column6.ReadOnly = true;
            Column6.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column6.Width = 65;
            // 
            // Column7
            // 
            Column7.HeaderText = "卷标";
            Column7.MinimumWidth = 8;
            Column7.Name = "Column7";
            Column7.ReadOnly = true;
            Column7.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column7.Width = 65;
            // 
            // button1
            // 
            button1.ForeColor = Color.Green;
            button1.Location = new Point(8, 174);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(71, 24);
            button1.TabIndex = 3;
            button1.Text = "刷新";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // button2
            // 
            button2.Enabled = false;
            button2.ForeColor = Color.Red;
            button2.Location = new Point(382, 173);
            button2.Margin = new Padding(2);
            button2.Name = "button2";
            button2.Size = new Size(71, 24);
            button2.TabIndex = 4;
            button2.Text = "重新分区";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Button2_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = SystemColors.GrayText;
            label1.Location = new Point(506, 177);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(335, 17);
            label1.TabIndex = 5;
            label1.Text = "如果Xbox不能识别U盘，请尝试重新分区，再不行重置主机。";
            // 
            // rbGPT
            // 
            rbGPT.AutoSize = true;
            rbGPT.Enabled = false;
            rbGPT.Location = new Point(271, 176);
            rbGPT.Margin = new Padding(2);
            rbGPT.Name = "rbGPT";
            rbGPT.Size = new Size(49, 21);
            rbGPT.TabIndex = 6;
            rbGPT.TabStop = true;
            rbGPT.Text = "GPT";
            rbGPT.UseVisualStyleBackColor = true;
            // 
            // rbMBR
            // 
            rbMBR.AutoSize = true;
            rbMBR.Checked = true;
            rbMBR.Enabled = false;
            rbMBR.ForeColor = Color.Green;
            rbMBR.Location = new Point(324, 176);
            rbMBR.Margin = new Padding(2);
            rbMBR.Name = "rbMBR";
            rbMBR.Size = new Size(54, 21);
            rbMBR.TabIndex = 7;
            rbMBR.TabStop = true;
            rbMBR.Text = "MBR";
            rbMBR.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(175, 177);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(92, 17);
            label2.TabIndex = 8;
            label2.Text = "选择分区表类型";
            // 
            // FormUsbDevice
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(833, 201);
            Controls.Add(label2);
            Controls.Add(rbMBR);
            Controls.Add(rbGPT);
            Controls.Add(label1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(dgvDevice);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormUsbDevice";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "USB Driver (MBR磁盘存在2TB分区限制，超出后的容量无法被使用)";
            Load += FormUsbDevice_Load;
            ((System.ComponentModel.ISupportInitialize)dgvDevice).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvDevice;
        private Button button1;
        private Button button2;
        private Label label1;
        private RadioButton rbGPT;
        private RadioButton rbMBR;
        private Label label2;
        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private DataGridViewTextBoxColumn Column6;
        private DataGridViewTextBoxColumn Column7;
    }
}