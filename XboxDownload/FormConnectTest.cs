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

        private async void ButTest_Click(object? sender, EventArgs? e)
        {
            string hostName = tbHostName.Text.Trim();
            if (string.IsNullOrEmpty(hostName))
            {
                tbHostName.Focus();
                MessageBox.Show("域名不能空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (!int.TryParse(tbPort.Text, out int port) || port < 1 || port > 65535)
            {
                tbPort.Focus();
                MessageBox.Show("无效端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (!IPAddress.TryParse(tbIP.Text.Trim(), out IPAddress? ip))
            {
                tbIP.Focus();
                MessageBox.Show("无效IP", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            butTest.Enabled = false;
            tbMessage.Clear();
            Uri uri = new("https://" + hostName + ":" + port);
            await ConnectTest(uri, ip);
        }

        private async Task ConnectTest(Uri uri, IPAddress ip)
        {
            bool verified = false;
            string location = string.Empty, errMsg = string.Empty;
            Task[] tasks = new Task[2];
            tasks[0] = new Task(() =>
            {
                verified = ClassWeb.ConnectTest(uri, ip, out errMsg);
            });
            tasks[1] = new Task(() =>
            {
                location = "\r\n\r\n//IP地址: " + ip + " " + ClassDNS.QueryLocation(ip.ToString());
            });
            Array.ForEach(tasks, x => x.Start());
            await Task.WhenAll(tasks);
            if (verified)
            {
                tbMessage.ForeColor = Color.Green;
                tbMessage.Text = "OK" + location;
            }
            else
            {
                tbMessage.ForeColor = Color.Red;
                tbMessage.Text = errMsg + location;
            }
            butTest.Enabled = true;
        }
    }
}
