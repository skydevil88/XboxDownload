using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormDns : Form
    {
        public FormDns()
        {
            InitializeComponent();
        }

        Thread thread = null;
        private void ButTest_Click(object sender, EventArgs e)
        {
            string domainName = cbDomainName.Text.Trim();
            if (!string.IsNullOrEmpty(domainName))
            {
                butTest.Enabled = false;
                textBox1.Text = ">nslookup " + domainName + " " + Properties.Settings.Default.LocalIP + "\r\n";
                thread = new Thread(new ThreadStart(() =>
                {
                    Test(domainName);
                }))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }

        private void CbDomainName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cbDomainName.Text = Regex.Replace(cbDomainName.Text.Trim(), @"^(https?://)?([^/|:]+).*$", "$2");
        }

        private void FormDns_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (thread != null && thread.IsAlive) thread.Abort();
        }

        private void Test(string domainName)
        {
            string resultInfo = string.Empty;
            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo("nslookup", domainName + " " + Properties.Settings.Default.LocalIP)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true
                };
                p.Start();
                resultInfo = p.StandardOutput.ReadToEnd();
                p.Close();
            }
            SetMsg(resultInfo);
            MatchCollection mc = Regex.Matches(resultInfo, @":\s*(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
            if (mc.Count == 2)
            {
                string ip = mc[1].Groups["ip"].Value;
                SetMsg("\n//IP地址: " + ip + " " + ClassDNS.QueryLocation(ip));
            }
            else if (Regex.IsMatch(resultInfo, @"timeout", RegexOptions.IgnoreCase))
            {
                SetMsg("*** 请求超时");
            }
            else
            {
                SetMsg("*** 找不到 " + domainName + ": Non-existent domain");
            }
            SetButEnable(true);
        }

        delegate void CallbackButEnable(bool enabled);
        private void SetButEnable(bool enabled)
        {
            if (butTest.InvokeRequired)
            {
                CallbackButEnable d = new CallbackButEnable(SetButEnable);
                this.Invoke(d, new object[] { enabled });
            }
            else
            {
                butTest.Enabled = enabled;
            }
        }

        delegate void CallbackMsg(string str);
        private void SetMsg(string str)
        {
            if (textBox1.InvokeRequired)
            {
                CallbackMsg d = new CallbackMsg(SetMsg);
                Invoke(d, new object[] { str });
            }
            else textBox1.AppendText(str);
        }
    }
}
