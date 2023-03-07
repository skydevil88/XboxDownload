using System.Net;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormConnectTest : Form
    {
        public FormConnectTest(string hostName, string ip)
        {
            InitializeComponent();
            tbHostName.Text = hostName.Trim();
            tbIP.Text = Regex.Replace(ip, ",.+", "").Trim();

            if (!string.IsNullOrEmpty(tbHostName.Text) && !string.IsNullOrEmpty(tbIP.Text))
            {
                ButTest_Click(null, null);
            }
        }

        private void ButTest_Click(object? sender, EventArgs? e)
        {
            string hostName = tbHostName.Text.Trim();
            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("域名不能空", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!int.TryParse(tbPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("无效端口", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!IPAddress.TryParse(tbIP.Text.Trim(), out IPAddress? ip))
            {
                MessageBox.Show("无效IP", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            butTest.Enabled = false;
            tbMessage.Clear();
            Uri uri = new("https://" + hostName + ":" + port);
            Task.Run(() =>
            {
                bool verified = ClassWeb.VerifySslCertificate(uri, ip, out string errMsg);

                if (verified)
                {
                    SetMsg("OK");
                }
                else
                {
                    SetMsg(errMsg);
                }
                SetButEnable(true);
            });
        }

        delegate void CallbackButEnable(bool enabled);
        private void SetButEnable(bool enabled)
        {
            if (this.IsDisposed) return;
            if (butTest.InvokeRequired)
            {
                CallbackButEnable d = new(SetButEnable);
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
            if (this.IsDisposed) return;
            if (tbMessage.InvokeRequired)
            {
                CallbackMsg d = new(SetMsg);
                Invoke(d, new object[] { str });
            }
            else tbMessage.AppendText(str);
        }
    }
}
