using System.Text;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormProxy : Form
    {
        public FormProxy()
        {
            InitializeComponent();

            if (File.Exists(Form1.resourcePath + "\\Proxy.txt"))
            {
                textBox1.Text = File.ReadAllText(Form1.resourcePath + "\\Proxy.txt").Trim() + "\r\n";
            }

            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                cbDoh.Items.Add(DnsListen.dohs[i, 0]);
            }
            cbDoh.SelectedIndex = Properties.Settings.Default.DoHProxy >= DnsListen.dohs.GetLongLength(0) ? 0 : Properties.Settings.Default.DoHProxy;
        }

        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string[] hosts1 = Array.Empty<string>();
            switch (checkedListBox1.Items[e.Index].ToString())
            {
                case "Steam 商店社区":
                    hosts1 = new string[] { "store.steampowered.com", "api.steampowered.com", "login.steampowered.com", "help.steampowered.com", "checkout.steampowered.com", "steamcommunity.com" };
                    break;
            }

            StringBuilder sb = new();
            foreach (string host in Regex.Split(textBox1.Text.Trim(), @"\n"))
            {
                string _host = host.Trim();
                if (string.IsNullOrEmpty(_host)) continue;
                if (!Array.Exists(hosts1, element => element.Equals(_host)))
                {
                    sb.AppendLine(host);
                }
            }
            string hosts2 = sb.ToString();
            if (e.NewValue == CheckState.Checked)
            {
                string hosts = string.Join("\r\n", hosts1) + "\r\n";
                textBox1.Text = hosts + hosts2;
                textBox1.Focus();
                textBox1.Select(0, hosts.Length - 2);
            }
            else textBox1.Text = hosts2;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            List<string> lsHosts = new();
            foreach (string host in Regex.Split(textBox1.Text.Trim(), @"\n"))
            {
                string _host = Regex.Replace(host.Trim().ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2");
                if (!string.IsNullOrEmpty(_host) && !lsHosts.Contains(_host))
                {
                    lsHosts.Add(_host);
                }
            }
            string hosts = string.Join("\r\n", lsHosts.ToArray());
            if (!string.IsNullOrEmpty(hosts))
            {
                if (!Directory.Exists(Form1.resourcePath)) Directory.CreateDirectory(Form1.resourcePath);
                File.WriteAllText(Form1.resourcePath + "\\" + "Proxy.txt", hosts);
            }
            else if (File.Exists(Form1.resourcePath + "\\Proxy.txt"))
            {
                File.Delete(Form1.resourcePath + "\\Proxy.txt");
            }
            HttpsListen.CreateCertificate();
            Properties.Settings.Default.DoHProxy = cbDoh.SelectedIndex;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
