using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormProxy : Form
    {
        public FormProxy()
        {
            InitializeComponent();

            if (File.Exists(Form1.resourcePath + "\\SniProxy.json"))
            {
                List<List<object>>? SniProxy = null;
                try
                {
                    SniProxy = JsonSerializer.Deserialize<List<List<object>>>(File.ReadAllText(Form1.resourcePath + "\\SniProxy.json"));
                }
                catch { }
                if (SniProxy != null)
                {
                    StringBuilder sb = new();
                    foreach (var item in SniProxy)
                    {
                        if (item.Count == 3)
                        {
                            JsonElement jeHosts = (JsonElement)item[0];
                            string? hosts = jeHosts.ValueKind == JsonValueKind.Array ? string.Join(", ", jeHosts.EnumerateArray().Select(item => item.GetString()?.Trim())) : string.Empty;
                            string? fakeHost = item[1]?.ToString()?.Trim();
                            string? ip = item[2]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(hosts)) continue;
                            if (string.IsNullOrEmpty(fakeHost) && string.IsNullOrEmpty(ip))
                                sb.AppendLine(hosts);
                            else if (!string.IsNullOrEmpty(fakeHost) && !string.IsNullOrEmpty(ip))
                                sb.AppendLine(hosts + " | " + fakeHost + " | " + ip);
                            else
                                sb.AppendLine(hosts + " | " + fakeHost + ip);
                        }
                    }
                    textBox1.Text = sb.ToString();
                }
            }

            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                cbDoh.Items.Add(DnsListen.dohs[i, 0]);
            }
            cbDoh.SelectedIndex = Properties.Settings.Default.DoHProxy >= DnsListen.dohs.GetLongLength(0) ? 3 : Properties.Settings.Default.DoHProxy;
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
            List<List<object>> lsSniProxy = new();
            foreach (string proxy in Regex.Split(textBox1.Text.Trim(), @"\n"))
            {
                ArrayList arrHost = new();
                string fakeHost = string.Empty, ip = string.Empty;
                string[] _proxy = proxy.Trim().Split('|');
                if (_proxy.Length >= 1)
                {
                    foreach (string host in Regex.Split(_proxy[0], @","))
                    {
                        string _host = Regex.Replace(host.ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                        if (!string.IsNullOrEmpty(_host))
                        {
                            arrHost.Add(_host);
                        }
                    }
                }
                if (_proxy.Length == 2)
                {
                    fakeHost = Regex.Replace(_proxy[1].ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                    if (IPAddress.TryParse(fakeHost, out IPAddress? address))
                    {
                        fakeHost = string.Empty;
                        ip = address.ToString();
                    }
                }
                else if (_proxy.Length >= 3)
                {
                    fakeHost = Regex.Replace(_proxy[1].ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                    if (IPAddress.TryParse(_proxy[2].Trim(), out IPAddress? address))
                        ip = address.ToString();
                }
                if (arrHost.Count >= 1) lsSniProxy.Add(new List<object> { arrHost, fakeHost, ip });
            }
            if (lsSniProxy.Count >= 1)
            {
                if (!Directory.Exists(Form1.resourcePath)) Directory.CreateDirectory(Form1.resourcePath);
                File.WriteAllText(Form1.resourcePath + "\\SniProxy.json", JsonSerializer.Serialize(lsSniProxy, new JsonSerializerOptions { WriteIndented = true, }));
            }
            else if (File.Exists(Form1.resourcePath + "\\SniProxy.json"))
            {
                File.Delete(Form1.resourcePath + "\\SniProxy.json");
            }
            HttpsListen.CreateCertificate();
            Properties.Settings.Default.DoHProxy = cbDoh.SelectedIndex;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
