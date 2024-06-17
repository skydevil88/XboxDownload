
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormDoH : Form
    {
        private readonly string[] hosts = { "www.xbox.com", "www.playstation.com", "www.nintendo.com" };

        public FormDoH()
        {
            InitializeComponent();

            if (Form1.dpixRatio > 1)
            {
                dataGridView1.RowHeadersWidth = (int)(dataGridView1.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
                dataGridView2.RowHeadersWidth = (int)(dataGridView2.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dataGridView2.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
            }

            int w = (int)(135 * Form1.dpixRatio);
            foreach (string host in hosts)
            {
                DataGridViewTextBoxColumn col = new()
                {
                    Name = host,
                    Width = w,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                col.DefaultCellStyle.Format = "N0";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns.Add(col);
            }

            List<DataGridViewRow> listDgvr = new();
            List<KeyValuePair<int, string>> listKvp = new();
            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                cbDoh.Items.Add(DnsListen.dohs[i, 0]);
                DataGridViewRow dgvr = new();
                dgvr.CreateCells(dataGridView1);
                dgvr.Resizable = DataGridViewTriState.False;
                string name = DnsListen.dohs[i, 0];
                dgvr.Cells[0].Value = true;
                dgvr.Cells[1].Value = name;
                dgvr.Cells[0].ToolTipText = null;
                dgvr.Cells[1].ToolTipText = null;
                for (int j = 0; j <= hosts.Length - 1; j++)
                {
                    dgvr.Cells[j + 2].ToolTipText = null;
                }
                listDgvr.Add(dgvr);
                listKvp.Add(new KeyValuePair<int, string>(i, name));
            }
            cbDoh.SelectedIndex = Properties.Settings.Default.DoHServer >= DnsListen.dohs.GetLongLength(0) ? 0 : Properties.Settings.Default.DoHServer;
            if (listDgvr.Count >= 1) dataGridView1.Rows.AddRange(listDgvr.ToArray());
            Col_DoHServer.ValueMember = "key";
            Col_DoHServer.DisplayMember = "value";
            Col_DoHServer.DataSource = listKvp;
            dataGridView2.DataSource = Form1.dtDoH;
        }

        private void FormDoH_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
            dataGridView2.ClearSelection();
        }

        private void Dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void ButSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DoHServer = cbDoh.SelectedIndex;
            Properties.Settings.Default.Save();
            DnsListen.dohServer = DnsListen.dohs[cbDoh.SelectedIndex, 1];
            string dohHost = DnsListen.dohs[cbDoh.SelectedIndex, 2];
            if (!string.IsNullOrEmpty(dohHost))
            {
                DnsListen.dohHeaders = new Dictionary<string, string>
                {
                    { "Host", dohHost }
                };
            }
            else DnsListen.dohHeaders = null;
            this.Close();
        }

        private void CbCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dgvr in dataGridView1.Rows)
            {
                if (dgvr.IsNewRow) break;
                dgvr.Cells[0].Value = cbCheckAll.Checked;
            }
        }

        private async void ButTest_Click(object sender, EventArgs e)
        {
            butTest.Enabled = false;
            dataGridView1.ClearSelection();

            await Task.Run(() =>
            {
                Task[] tasks = new Task[dataGridView1.Rows.Count];
                for (int i = 0; i <= tasks.Length - 1; i++)
                {
                    int tmp = i;
                    tasks[tmp] = new Task(() =>
                    {
                        DataGridViewRow dgvr = dataGridView1.Rows[tmp];
                        if (Convert.ToBoolean(dgvr.Cells[0].Value))
                        {
                            dgvr.Cells[2].Value = dgvr.Cells[3].Value = dgvr.Cells[4].Value = null;
                            dgvr.Cells[2].Style.ForeColor = dgvr.Cells[3].Style.ForeColor = dgvr.Cells[4].Style.ForeColor = Color.Empty;
                            string dohServer = DnsListen.dohs[tmp, 1];
                            string dohHost = DnsListen.dohs[tmp, 2];
                            Dictionary<string, string>? dohHeaders = null;
                            if (!string.IsNullOrEmpty(dohHost))
                            {
                                dohHeaders = new Dictionary<string, string>
                                {
                                    { "Host", dohHost }
                                };
                            }

                            _ = ClassDNS.DoH("www.baidu.com", dohServer, dohHeaders, "A", 3000);
                            Stopwatch sw = new();
                            for (int x = 0; x <= hosts.Length - 1; x++)
                            {
                                string host = hosts[x];
                                sw.Restart();
                                string? ip = ClassDNS.DoH(host, dohServer, dohHeaders, "A", 3000);
                                sw.Stop();
                                if (!string.IsNullOrEmpty(ip))
                                {
                                    dgvr.Cells[x + 2].ToolTipText = "IP: " + ip;
                                    dgvr.Cells[x + 2].Value = (int)sw.ElapsedMilliseconds;
                                }
                                else
                                {
                                    dgvr.Cells[x + 2].ToolTipText = null;
                                    dgvr.Cells[x + 2].Value = "error";
                                    dgvr.Cells[x + 2].Style.ForeColor = Color.Red;
                                }
                            }
                        }
                    });
                }
                Array.ForEach(tasks, x => x.Start());
                Task.WaitAll(tasks);
            });
            butTest.Enabled = true;
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/skydevil88/XboxDownload/discussions/96") { UseShellExecute = true });
        }

        private void DataGridView2_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            int i = dataGridView2.Rows.Count - 2;
            dataGridView2.Rows[i].Cells["Col_Enable"].Value = true;
            dataGridView2.Rows[i].Cells["Col_DoHServer"].Value = 0;
        }

        private void ButDoHSave_Click(object sender, EventArgs e)
        {
            Form1.dtDoH.AcceptChanges();
            if (Form1.dtDoH.Rows.Count >= 1)
            {
                if (!Directory.Exists(Form1.resourcePath)) Directory.CreateDirectory(Form1.resourcePath);
                Form1.dtDoH.WriteXml(Form1.resourcePath + "\\DoH.xml");
            }
            else if (File.Exists(Form1.resourcePath + "\\DoH.xml"))
            {
                File.Delete(Form1.resourcePath + "\\DoH.xml");
            }
            DnsListen.UseDoH();
            this.Close();
        }

        private void ButDohReset_Click(object sender, EventArgs e)
        {
            Form1.dtDoH.RejectChanges();
            dataGridView2.ClearSelection();
        }

        private void DataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            switch (dataGridView2.Columns[e.ColumnIndex].Name)
            {
                case "Col_Host":
                    dataGridView2.CurrentCell.Value = Regex.Replace((dataGridView2.CurrentCell.FormattedValue.ToString() ?? string.Empty).Trim().ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2");
                    break;
            }
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/skydevil88/XboxDownload/discussions/96#discussioncomment-9784721") { UseShellExecute = true });
        }
    }
}
