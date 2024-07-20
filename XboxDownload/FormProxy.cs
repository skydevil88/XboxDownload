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
                            if (jeHosts.ValueKind != JsonValueKind.Array) continue;
                            string? hosts = string.Join(", ", jeHosts.EnumerateArray().Select(x => x.GetString()?.Trim()));
                            if (string.IsNullOrEmpty(hosts)) continue;
                            string? fakeHost = item[1]?.ToString()?.Trim();
                            string? ip = item[2]?.ToString()?.Trim();
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

            int total = 0;
            List<int> ls = new();
            foreach (string part in Properties.Settings.Default.SinProxys.Split(','))
            {
                ls.Add(int.Parse(part));
            }
            for (int i = 0; i <= DnsListen.dohs.GetLongLength(0) - 1; i++)
            {
                checkedListBox2.Items.Add(DnsListen.dohs[i, 0]);
                if (ls.Contains(i))
                {
                    checkedListBox2.SetItemCheckState(i, CheckState.Checked);
                    total++;
                }
            }
            groupBox3.Text = Regex.Replace(groupBox3.Text, @"\d+", total.ToString());
        }

        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string[] hosts1 = Array.Empty<string>();
            switch (checkedListBox1.Items[e.Index].ToString())
            {
                case "Steam 商店社区":
                    hosts1 = new string[] { "store.steampowered.com", "api.steampowered.com", "login.steampowered.com", "help.steampowered.com", "checkout.steampowered.com", "steamcommunity.com" };
                    break;
                case "GitHub":
                    hosts1 = new string[] { "*github.com", "*githubusercontent.com", "*github.io", "*github.blog", "*githubstatus.com", "*githubassets.com" };
                    break;
                case "Pixiv":
                    hosts1 = new string[] { "*pixiv.net | 210.140.92.187", "*.pximg.net | 210.140.92.141" };
                    break;
            }

            StringBuilder sb = new();
            foreach (string host in textBox1.Text.Trim().ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrEmpty(host)) continue;
                if (!Array.Exists(hosts1, element => element.Equals(host)))
                {
                    sb.AppendLine(host);
                }
            }
            string hosts2 = sb.ToString();
            if (e.NewValue == CheckState.Checked)
            {
                string hosts = string.Join(Environment.NewLine, hosts1) + Environment.NewLine;
                textBox1.Text = hosts + hosts2;
                textBox1.Focus();
                textBox1.Select(0, hosts.Length - 2);
            }
            else textBox1.Text = hosts2;
        }

        private void CheckedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            groupBox3.Text = Regex.Replace(groupBox3.Text, @"\d+", checkedListBox2.CheckedIndices.Count.ToString());
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string ipPattern = @"^((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|" +                                                                     //匹配包含8组1到4个十六进制数字、由冒号分隔的IPv6地址。
                               @"(([0-9A-Fa-f]{1,4}:){1,7}:)|" +                                                                                        //匹配以零压缩开头的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,6}:[0-9A-Fa-f]{1,4})|" +                                                                        //匹配带有一组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,5}(:[0-9A-Fa-f]{1,4}){1,2})|" +                                                                 //匹配带有两组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,4}(:[0-9A-Fa-f]{1,4}){1,3})|" +                                                                 //匹配带有三组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,3}(:[0-9A-Fa-f]{1,4}){1,4})|" +                                                                 //匹配带有四组零的地址
                               @"(([0-9A-Fa-f]{1,4}:){1,2}(:[0-9A-Fa-f]{1,4}){1,5})|" +                                                                 //匹配带有五组零的地址
                               @"([0-9A-Fa-f]{1,4}:((:[0-9A-Fa-f]{1,4}){1,6}))|" +                                                                      //匹配带有六组零的地址
                               @"(:((:[0-9A-Fa-f]{1,4}){1,7}|:))|" +                                                                                    //匹配以零开头并包含七组零或一个冒号的地址
                               @"(::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))|" +  //匹配包含嵌入IPv4地址的IPv6地址，带有可选的前导零和可选的ffff组
                               @"(([0-9A-Fa-f]{1,4}:){1,4}:((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))|" +     //匹配包含最多四组后跟嵌入IPv4地址的IPv6地址
                               @"(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])(\.(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])){3})$";                      //匹配IPv4地址
            Regex reIP = new(ipPattern);

            List<List<object>> lsSniProxy = new();
            foreach (string str in textBox1.Text.Trim().ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string[] proxy = str.Split('|');
                if (proxy.Length == 0) continue;
                ArrayList arrHost = new();
                string sni = string.Empty;
                IPAddress? ip = null;
                if (proxy.Length >= 1)
                {
                    foreach (string host in proxy[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        string _host = Regex.Replace(host.ToLower().Trim(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                        if (!string.IsNullOrEmpty(_host))
                        {
                            arrHost.Add(_host);
                        }
                    }
                }
                if (proxy.Length == 2)
                {
                    proxy[1] = proxy[1].Trim();
                    if (!(reIP.IsMatch(proxy[1]) && IPAddress.TryParse(proxy[1], out ip)))
                        sni = Regex.Replace(proxy[1].ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                }
                else if (proxy.Length >= 3)
                {
                    sni = Regex.Replace(proxy[1].ToLower().Trim(), @"^(https?://)?([^/|:|\s]+).*$", "$2").Trim();
                    _ = IPAddress.TryParse(proxy[2].Trim(), out ip);
                }
                if (arrHost.Count >= 1) lsSniProxy.Add(new List<object> { arrHost, sni, ip != null ? ip.ToString() : string.Empty });
            }
            if (lsSniProxy.Count >= 1)
            {
                if (!Directory.Exists(Form1.resourcePath)) Directory.CreateDirectory(Form1.resourcePath);
                File.WriteAllText(Form1.resourcePath + "\\SniProxy.json", JsonSerializer.Serialize(lsSniProxy, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (File.Exists(Form1.resourcePath + "\\SniProxy.json"))
            {
                File.Delete(Form1.resourcePath + "\\SniProxy.json");
            }

            List<int> ls = new();
            for (int i = 0; i <= checkedListBox2.Items.Count - 1; i++)
            {
                if (checkedListBox2.GetItemChecked(i))
                    ls.Add(i);
            }
            if (ls.Count == 0) ls.Add(3);
            Properties.Settings.Default.SinProxys = string.Join(',', ls.ToArray());
            Properties.Settings.Default.Save();
            HttpsListen.CreateCertificate();
            this.Close();
        }
    }
}
