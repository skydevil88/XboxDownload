using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormUsbDevice : Form
    {
        public FormUsbDevice()
        {
            InitializeComponent();

            if (Form1.dpiFactor > 1)
            {
                dgvDevice.RowHeadersWidth = (int)(dgvDevice.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvDevice.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
            }
        }

        private void FormUsbDevice_Load(object sender, EventArgs e)
        {
            LinkRefresh_LinkClicked(sender, null);
        }

        private void DgvDevice_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            rbGPT.Enabled = rbMBR.Enabled = button2.Enabled = true;
        }

        private void LinkRefresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs? e)
        {
            dgvDevice.Rows.Clear();
            rbGPT.Enabled = rbMBR.Enabled = button2.Enabled = false;
            List<DataGridViewRow> list = new();

            ManagementClass mc = new("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (var (mo, sDeviceID, sInterfaceType, sMediaType) in from ManagementObject mo in moc
                                                                        let sDeviceID = mo.Properties["DeviceID"].Value?.ToString()
                                                                        let sInterfaceType = mo.Properties["InterfaceType"].Value?.ToString()
                                                                        let sMediaType = mo.Properties["MediaType"].Value?.ToString()
                                                                        select (mo, sDeviceID, sInterfaceType, sMediaType))
            {
                if (string.IsNullOrEmpty(sDeviceID) || sInterfaceType != "USB" || sMediaType != "Removable Media") continue;
                int index = Convert.ToInt32(mo.Properties["Index"].Value);
                int partitions = Convert.ToInt32(mo.Properties["Partitions"].Value);
                var lstDisk = (from ManagementObject diskPartition in mo.GetRelated("Win32_DiskPartition")
                               from ManagementBaseObject disk in diskPartition.GetRelated("Win32_LogicalDisk")
                               select disk.Properties["Name"].Value.ToString()).ToList();
                string outputString = "";
                try
                {
                    using Process p = new();
                    p.StartInfo.FileName = "DiskPart.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.StandardInput.WriteLine("list disk");
                    p.StandardInput.Close();
                    outputString = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                catch { }

                Match result = Regex.Match(outputString, @"\s" + index + ".{43}(?<Gpt>.)");
                string type = result.Success ? result.Groups["Gpt"].Value == "*" ? "GPT" : "MBR" : "未知";
                DataGridViewRow dgvr = new();
                dgvr.CreateCells(dgvDevice);
                dgvr.Resizable = DataGridViewTriState.False;
                dgvr.Tag = index;
                dgvr.Cells[0].Value = sDeviceID;
                dgvr.Cells[1].Value = mo.Properties["Model"].Value;
                dgvr.Cells[2].Value = mo.Properties["InterfaceType"].Value;
                dgvr.Cells[3].Value = ClassMbr.ConvertBytes(Convert.ToUInt64(mo.Properties["Size"].Value));
                dgvr.Cells[4].Value = type;
                dgvr.Cells[4].Style.ForeColor = type == "MBR" ? Color.Green : Color.Red;
                dgvr.Cells[5].Value = partitions;
                dgvr.Cells[5].Style.ForeColor = partitions == 1 ? Color.Green : Color.Red;
                dgvr.Cells[6].Value = string.Join(',', lstDisk.ToArray());
                list.Add(dgvr);
            }

            if (list.Count >= 1)
            {
                dgvDevice.Rows.AddRange(list.ToArray());
                dgvDevice.ClearSelection();
            }
            rbMBR.Checked = true;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (dgvDevice.SelectedRows.Count != 1) return;
            if (MessageBox.Show("确认重新分区U盘？\n\n警告，此操作将删除U盘中的所有分区和文件!\n警告，此操作将删除U盘中的所有分区和文件!\n警告，此操作将删除U盘中的所有分区和文件!", "重新分区", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                int index = Convert.ToInt32(dgvDevice.SelectedRows[0].Tag);
                bool mbr = rbMBR.Checked;
                linkRefresh.Enabled = rbGPT.Enabled = rbMBR.Enabled = button2.Enabled = false;
                try
                {
                    using Process p = new();
                    p.StartInfo.FileName = "DiskPart.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.StandardInput.WriteLine("list disk");
                    p.StandardInput.WriteLine("select disk " + index);
                    p.StandardInput.WriteLine("clean");
                    p.StandardInput.WriteLine("online disk");
                    p.StandardInput.WriteLine("attributes disk clear readonly");
                    p.StandardInput.WriteLine("convert " + (mbr ? "mbr" : "gpt"));
                    p.StandardInput.WriteLine("create partition primary");
                    p.StandardInput.WriteLine("select partition 1");
                    p.StandardInput.WriteLine("format fs=ntfs quick");
                    if (string.IsNullOrEmpty(dgvDevice.SelectedRows[0].Cells[6].Value.ToString()))
                    {
                        p.StandardInput.WriteLine("assign");
                    }
                    p.StandardInput.Close();
                    p.WaitForExit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("重新分区失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                MessageBox.Show("已完成分区。", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                linkRefresh.Enabled = rbMBR.Checked = true;
                LinkRefresh_LinkClicked(sender, null);
            }
        }
    }
}
