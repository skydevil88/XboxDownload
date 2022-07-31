using System;
using System.Data;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormImportIP : Form
    {
        public String host = string.Empty;
        public DataTable dt = null;
        public FormImportIP()
        {
            InitializeComponent();

            dt = new DataTable();
            dt.Columns.Add("IP", typeof(string));
            dt.Columns.Add("IpFilter", typeof(string));
            dt.Columns.Add("ASN", typeof(string));
            dt.Columns.Add("IpLong", typeof(ulong));
            dt.PrimaryKey = new DataColumn[] { dt.Columns["IpFilter"] };

            comboBox1.SelectedIndex = 0;
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = ((LinkLabel)sender).Text;
            System.Diagnostics.Process.Start(url);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string content = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            string[] array = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length >= 1)
            {
                if (String.Equals(array[0].Trim(), "Akamai", StringComparison.CurrentCultureIgnoreCase))
                {
                    this.host = "Akamai";
                }
                else
                {
                    foreach (string str in array)
                    {
                        string tmp = str.Trim();
                        if (Regex.IsMatch(tmp, @"^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$"))
                        {
                            this.host = tmp.ToLowerInvariant();
                            switch (this.host)
                            {
                                case "atum.hac.lp1.d4c.nintendo.net":
                                    this.host = "Akamai";
                                    break;
                                default:
                                    if (Regex.IsMatch(this.host, @"\.akamaihd\.net$"))
                                    {
                                        this.host = "Akamai";
                                    }
                                    break;
                            }
                            break;
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(this.host))
            {
                MessageBox.Show("提交内容不符合条件。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Match result = Regex.Match(content, @"(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*\((?<ASN>[^\)]+)\)|(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})(?<ASN>.+)\d+ms|^\s*(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*$", RegexOptions.Multiline);
            while (result.Success)
            {
                string ip = result.Groups["IP"].Value;
                UInt64 ipLong = IpToLong(ip);
                if (ipLong == 0) return;
                string IpFilter = Regex.Replace(ip, @"\d{0,3}$", "");
                DataRow dr = dt.Rows.Find(IpFilter);
                if (dr == null)
                {
                    dr = dt.NewRow();
                    dr["IP"] = ip;
                    dr["IpFilter"] = IpFilter;
                    dr["ASN"] = Regex.Replace(result.Groups["ASN"].Value.Trim(), @" ([-a-zA-Z0-9]+\.)+[a-zA-Z0-9]{2,}", "");
                    dr["IpLong"] = ipLong;
                    dt.Rows.Add(dr);
                }
                result = result.NextMatch();
            }
            this.Close();
        }

        private void LinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string hosts = Regex.Replace(comboBox1.Text, @"\s.+$", "").Trim();
            Clipboard.SetDataObject(hosts);
            MessageBox.Show("域名(" + hosts + ")已复制到剪贴板", "复制域名", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LinkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Application.StartupPath,
                Filter = "文本文件(*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(openFileDialog.FileName);
                textBox1.Text = sr.ReadToEnd();
                sr.Close();
            }
        }

        public ulong IpToLong(string ip)
        {
            ulong IntIp = 0;
            if (IPAddress.TryParse(ip, out IPAddress ipaddress))
            {
                string[] ips = ipaddress.ToString().Split('.');
                IntIp = ulong.Parse(ips[0]) << 0x18 | ulong.Parse(ips[1]) << 0x10 | ulong.Parse(ips[2]) << 0x8 | ulong.Parse(ips[3]);
            }
            return IntIp;
        }
    }
}
