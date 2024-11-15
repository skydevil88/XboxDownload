using System.Data;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Management;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NetFwTypeLib;

namespace XboxDownload
{
    public partial class Form1 : Form
    {
        internal static bool bServiceFlag = false, bAutoStartup = false, bIPv6Support = false;
        internal readonly static string resourceDirectory = Path.Combine(Application.StartupPath, "Resource");
        internal static List<Market> lsMarket = new();
        internal static float dpiFactor = 1;
        internal static JsonSerializerOptions jsOptions = new() { PropertyNameCaseInsensitive = true };
        internal static DataTable dtHosts = new("Hosts"), dtDoHServer = new("DoH");
        private readonly DnsListen dnsListen;
        private readonly HttpListen httpListen;
        private readonly HttpsListen httpsListen;
        private readonly ToolTip toolTip1 = new()
        {
            AutoPopDelay = 30000,
            IsBalloon = true
        };

        public Form1()
        {
            InitializeComponent();

            Form1.dpiFactor = Environment.OSVersion.Version.Major >= 10 ? CreateGraphics().DpiX / 96f : Program.Utility.DpiX / 96f;
            if (Form1.dpiFactor > 1)
            {
                foreach (ColumnHeader col in lvLog.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvIpList.RowHeadersWidth = (int)(dgvIpList.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvIpList.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvHosts.RowHeadersWidth = (int)(dgvHosts.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvHosts.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                dgvDevice.RowHeadersWidth = (int)(dgvDevice.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dgvDevice.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
                foreach (ColumnHeader col in lvGame.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
            }

            ClassWeb.HttpClientFactory();
            dnsListen = new DnsListen(this);
            httpListen = new HttpListen(this);
            httpsListen = new HttpsListen(this);

            toolTip1.SetToolTip(this.labelDNS, "常用 DNS 服务器\n114.114.114.114 (114)\n180.76.76.76 (百度)\n223.5.5.5 (阿里)\n119.29.29.29 (腾讯)\n208.67.220.220 (OpenDns)\n8.8.8.8 (Google)\n168.126.63.1 (韩国)");
            toolTip1.SetToolTip(this.labelCom, "包括以下com游戏下载域名\nxvcf1.xboxlive.com\nxvcf2.xboxlive.com\nassets1.xboxlive.com\nassets2.xboxlive.com\nd1.xboxlive.com\nd2.xboxlive.com\ndlassets.xboxlive.com\ndlassets2.xboxlive.com\n\n以上域名不能使用 cn IP");
            toolTip1.SetToolTip(this.labelCn, "包括以下cn游戏下载域名\nassets1.xboxlive.cn\nassets2.xboxlive.cn\nd1.xboxlive.cn\nd2.xboxlive.cn");
            toolTip1.SetToolTip(this.labelCn2, "包括以下cn游戏下载域名\ndlassets.xboxlive.cn\ndlassets2.xboxlive.cn\n\n注：XboxOne部分老游戏下载域名，\nPC、主机新游戏都不再使用此域名。");
            toolTip1.SetToolTip(this.labelApp, "包括以下应用下载域名\ndl.delivery.mp.microsoft.com\ntlu.dl.delivery.mp.microsoft.com\n*.dl.delivery.mp.microsoft.com");
            toolTip1.SetToolTip(this.labelPS, "包括以下游戏下载域名\ngst.prod.dl.playstation.net\ngs2.ww.prod.dl.playstation.net\nzeus.dl.playstation.net\nares.dl.playstation.net");
            toolTip1.SetToolTip(this.labelNS, "包括以下游戏下载域名\natum.hac.lp1.d4c.nintendo.net\nbugyo.hac.lp1.eshop.nintendo.net\nctest-dl-lp1.cdn.nintendo.net\nctest-ul-lp1.cdn.nintendo.net");
            toolTip1.SetToolTip(this.labelEA, "包括以下游戏下载域名\norigin-a.akamaihd.net");
            toolTip1.SetToolTip(this.labelBattle, "包括以下游戏下载域名\nblzddist1-a.akamaihd.net\nus.cdn.blizzard.com\neu.cdn.blizzard.com\nkr.cdn.blizzard.com\nlevel3.blizzard.com\nblizzard.gcdn.cloudn.co.kr\n\n#网易国服(校园网可指定Akamai IPv6免流下载)\n*.necdn.leihuo.netease.com");
            toolTip1.SetToolTip(this.labelEpic, "包括以下游戏下载域名\nepicgames-download1-1251447533.file.myqcloud.com\nepicgames-download1.akamaized.net\ndownload.epicgames.com\nfastly-download.epicgames.com\ncloudflare.epicgamescdn.com\n\n建议优先使用国内CDN，速度不理想再选用 Akamai CDN");
            toolTip1.SetToolTip(this.labelUbi, "包括以下游戏下载域名\nuplaypc-s-ubisoft.cdn.ubionline.com.cn\nuplaypc-s-ubisoft.cdn.ubi.com\n\n注：XDefiant(不羁联盟)不支持使用国内CDN，\n可勾选\"自动优选 Akamai IP\"使用国外CDN。");
            toolTip1.SetToolTip(this.ckbDoH, "默认使用 阿里云DoH(加密DNS) 解析域名IP，\n防止上游DNS服务器被劫持污染。\nPC用户使用此功能，需要勾选“设置本机 DNS”\n\n注：网络正常可以不勾选。");
            toolTip1.SetToolTip(this.ckbSetDns, "开始监听将把电脑DNS设置为本机IP，停止监听后恢复默认设置，\nPC用户建议勾选，主机用户无需设置。\n\n注：如果退出Xbox下载助手后没网络，请点击旁边“修复”。");
            toolTip1.SetToolTip(this.ckbBetterAkamaiIP, "自动从 Akamai 优选 IP 列表中找出下载速度最快的节点\n支持 Xbox、PS、NS、EA、战网、EPIC、育碧、拳头游戏\n选中后临时忽略自定义IP（Xbox、PS不使用国内IP）\n同时还能解决Xbox安装停止，冷门游戏国内CDN没缓存下载慢等问题\n\n提示：\n更换IP后，Xbox、战网、育碧 拳头游戏 客户端需要暂停下载，然后重新恢复安装，\nEA app、Epic客户端请点击修复/重启，主机需要等待DNS缓存过期(100秒)。");

            tbDnsIP.Text = Properties.Settings.Default.DnsIP;
            tbComIP.Text = Properties.Settings.Default.ComIP;
            ckbGameLink.Checked = Properties.Settings.Default.GameLink;
            tbCnIP.Text = Properties.Settings.Default.CnIP;
            tbCnIP2.Text = Properties.Settings.Default.CnIP2;
            tbAppIP.Text = Properties.Settings.Default.AppIP;
            tbPSIP.Text = Properties.Settings.Default.PSIP;
            tbNSIP.Text = Properties.Settings.Default.NSIP;
            ckbNSBrowser.Checked = Properties.Settings.Default.NSBrowser;
            tbEAIP.Text = Properties.Settings.Default.EAIP;
            tbBattleIP.Text = Properties.Settings.Default.BattleIP;
            ckbBattleNetease.Checked = Properties.Settings.Default.BattleNetease;
            tbEpicIP.Text = Properties.Settings.Default.EpicIP;
            if (Properties.Settings.Default.EpicCDN) rbEpicCDN1.Checked = true;
            else rbEpicCDN2.Checked = true;
            tbUbiIP.Text = Properties.Settings.Default.UbiIP;
            ckbTruncation.Checked = Properties.Settings.Default.Truncation;
            ckbLocalUpload.Checked = Properties.Settings.Default.LocalUpload;
            if (string.IsNullOrEmpty(Properties.Settings.Default.LocalPath))
                Properties.Settings.Default.LocalPath = Path.Combine(Application.StartupPath, "Upload");
            tbLocalPath.Text = Properties.Settings.Default.LocalPath;
            cbListenIP.SelectedIndex = Properties.Settings.Default.ListenIP;
            ckbDnsService.Checked = Properties.Settings.Default.DnsService;
            ckbHttpService.Checked = Properties.Settings.Default.HttpService;
            ckbDoH.Checked = Properties.Settings.Default.DoH;
            ckbDisableIPv6DNS.Checked = Properties.Settings.Default.DisableIPv6DNS;
            ckbSetDns.Checked = Properties.Settings.Default.SetDns;
            ckbMicrosoftStore.Checked = Properties.Settings.Default.MicrosoftStore;
            ckbEAStore.Checked = Properties.Settings.Default.EAStore;
            ckbBattleStore.Checked = Properties.Settings.Default.BattleStore;
            ckbEpicStore.Checked = Properties.Settings.Default.EpicStore;
            ckbUbiStore.Checked = Properties.Settings.Default.UbiStore;
            ckbSniProxy.Checked = Properties.Settings.Default.SniProxy;
            ckbRecordLog.Checked = Properties.Settings.Default.RecordLog;
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;

            string dohserverFilePath = Path.Combine(resourceDirectory, "DohServer.json");
            if (File.Exists(dohserverFilePath))
            {
                JsonDocument? jsDoH = null;
                try
                {
                    jsDoH = JsonDocument.Parse(File.ReadAllText(dohserverFilePath));
                }
                catch { }
                if (jsDoH != null)
                {
                    foreach (JsonElement arr in jsDoH.RootElement.EnumerateArray())
                    {
                        string name = string.Empty, url = string.Empty, host = string.Empty;
                        if (arr.TryGetProperty("name", out JsonElement jeName)) name = jeName.ToString();
                        if (arr.TryGetProperty("url", out JsonElement jeUrl)) url = jeUrl.ToString();
                        if (arr.TryGetProperty("host", out JsonElement jeHost)) host = jeHost.ToString();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                        {
                            int originalRows = DnsListen.dohs.GetLength(0);
                            int originalColumns = DnsListen.dohs.GetLength(1);
                            int newRows = originalRows + 1;
                            int newColumns = originalColumns;
                            string[,] newArray = new string[newRows, newColumns];
                            for (int i = 0; i < originalRows; i++)
                            {
                                for (int j = 0; j < originalColumns; j++)
                                {
                                    newArray[i, j] = DnsListen.dohs[i, j];
                                }
                            }
                            newArray[originalRows, 0] = name;
                            newArray[originalRows, 1] = url;
                            newArray[originalRows, 2] = host;
                            DnsListen.dohs = newArray;
                        }
                    }
                }
            }

            int iDohServer = Properties.Settings.Default.DoHServer >= DnsListen.dohs.GetLongLength(0) ? 0 : Properties.Settings.Default.DoHServer;
            DnsListen.dohServer.Website = DnsListen.dohs[iDohServer, 1];
            if (!string.IsNullOrEmpty(DnsListen.dohs[iDohServer, 2])) DnsListen.dohServer.Headers = new() { { "Host", DnsListen.dohs[iDohServer, 2] } };

            rbEpicCDN1.CheckedChanged += RbCDN_CheckedChanged;
            rbEpicCDN2.CheckedChanged += RbCDN_CheckedChanged;
            ckbRecordLog.CheckedChanged += new EventHandler(CkbRecordLog_CheckedChanged);

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && !x.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (adapters.Length == 0) adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection ipCollection = adapterProperties.UnicastAddresses;
                foreach (UnicastIPAddressInformation ipadd in ipCollection)
                {
                    if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ComboboxItem item = new()
                        {
                            Text = ipadd.Address.ToString(),
                            Value = adapter
                        };
                        cbLocalIP.Items.Add(item);
                    }
                }
            }
            if (cbLocalIP.Items.Count >= 1)
            {
                int index = 0;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LocalIP))
                {
                    for (int i = 0; i < cbLocalIP.Items.Count; i++)
                    {
                        string ip = cbLocalIP.Items[i].ToString() ?? string.Empty;
                        if (Properties.Settings.Default.LocalIP == ip)
                        {
                            index = i;
                            break;
                        }
                        else if (Properties.Settings.Default.LocalIP.StartsWith(Regex.Replace(ip, @"\d+$", "")))
                        {
                            index = i;
                        }
                    }
                }
                cbLocalIP.SelectedIndex = index;
            }

            tbHosts1Akamai.Text = Properties.Resource.Akamai;
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (File.Exists(akamaiFilePath))
            {
                tbHosts2Akamai.Text = File.ReadAllText(akamaiFilePath).Trim() + "\r\n";
            }

            cbHosts.SelectedIndex = 0;
            cbSpeedTestTimeOut.SelectedIndex = 0;
            cbImportIP.SelectedIndex = 0;

            dtHosts.Columns.Add("Enable", typeof(Boolean));
            dtHosts.Columns.Add("HostName", typeof(String));
            dtHosts.Columns.Add("IP", typeof(String));
            dtHosts.Columns.Add("Remark", typeof(String));
            string hostsFilePath = Path.Combine(resourceDirectory, "Hosts.xml");
            if (File.Exists(hostsFilePath))
            {
                try
                {
                    dtHosts.ReadXml(hostsFilePath);
                }
                catch { }
                dtHosts.AcceptChanges();
            }
            dgvHosts.DataSource = dtHosts;

            dtDoHServer.Columns.Add("Enable", typeof(Boolean));
            dtDoHServer.Columns.Add("Host", typeof(String));
            dtDoHServer.Columns.Add("DoHServer", typeof(Int32));
            dtDoHServer.Columns.Add("Remark", typeof(String));
            string dohFilePath = Path.Combine(resourceDirectory, "DoH.xml");
            if (File.Exists(dohFilePath))
            {
                try
                {
                    dtDoHServer.ReadXml(dohFilePath);
                }
                catch { }
                int length = (int)DnsListen.dohs.GetLongLength(0);
                foreach (DataRow row in dtDoHServer.Rows)
                {
                    if (int.TryParse(row["DoHServer"].ToString(), out int index) && index >= length)
                        row["DoHServer"] = 0;
                }
                dtDoHServer.AcceptChanges();
            }
            DnsListen.SetDoHServer();

            Form1.lsMarket.AddRange((new List<Market>
            {
                new("Taiwan", "台湾", "TW", "zh-TW"),
                new("Hong Kong SAR", "香港", "HK", "zh-HK"),
                new("Singapore", "新加坡", "SG", "en-SG"),
                new("Korea", "韩国", "KR", "ko-KR"),
                new("Japan", "日本", "JP", "ja-JP"),
                new("United States","美国", "US", "en-US"),

                new("Argentina", "阿根廷", "AR", "es-AR"),
                new("United Arab Emirates", "阿联酋", "AE", "ar-AE"),
                new("Ireland", "爱尔兰", "IE", "en-IE"),
                new("Austria", "奥地利", "AT", "de-AT"),
                new("Austalia", "澳大利亚", "AU", "en-AU"),
                new("Brazil", "巴西", "BR", "pt-BR"),
                new("Belgium", "比利时", "BE", "nl-BE"),
                new("Poland", "波兰", "PL", "pl-PL"),
                new("Denmark", "丹麦", "DK", "da-DK"),
                new("Germany", "德国", "DE", "de-DE"),
                new("Russia", "俄罗斯", "RU", "ru-RU"),
                new("France", "法国", "FR", "fr-FR"),
                new("Finland", "芬兰", "FI", "fi-FI"),
                new("Colombia", "哥伦比亚", "CO", "es-CO"),
                //new("Korea", "韩国", "KR", "ko-KR"),
                new("Netherlands", "荷兰", "NL", "nl-NL"),
                new("Canada", "加拿大", "CA", "en-CA"),
                new("Czech Republic", "捷克共和国", "CZ", "cs-CZ"),
                //new("United States", "美国", "US", "en-US"),
                new("Mexico", "墨西哥", "MX", "es-MX"),
                new("South Africa", "南非", "ZA", "en-ZA"),
                new("Norway", "挪威", "NO", "nb-NO"),
                new("Portugal", "葡萄牙", "PT", "pt-PT"),
                //new("Japan", "日本", "JP", "ja-JP"),
                new("Sweden", "瑞典", "SE", "sv-SE"),
                new("Switzerland", "瑞士", "CH", "de-CH"),
                new("Saudi Arabia", "沙特阿拉伯", "SA", "ar-SA"),
                new("Slovakia", "斯洛伐克", "SK", "sk-SK"),
                //new("Taiwan", "台湾", "TW", "zh-TW"),
                new("Turkey", "土尔其", "TR", "tr-TR"),
                new("Spain", "西班牙", "ES", "es-ES"),
                new("Greece", "希腊", "GR", "el-GR"),
                //new("Hong Kong SAR", "香港", "HK", "zh-HK"),
                //new("Singapore", "新加坡", "SG", "en-SG"),
                new("New Zealand", "新西兰", "NZ", "en-NZ"),
                new("Hungary", "匈牙利", "HU", "hu-HU"),
                new("Israel", "以色列", "IL", "he-IL"),
                new("Italy", "意大利", "IT", "it-IT"),
                new("India", "印度", "IN", "en-IN"),
                new("United Kingdom", "英国", "GB", "en-GB"),
                new("Chile", "智利", "CL", "es-CL"),
                new("China", "中国", "CN", "zh-CN")
            }).ToArray());
            //Form1.lsMarket.Sort((x, y) => string.Compare(x.ename, y.ename));
            cbGameMarket.Items.AddRange(Form1.lsMarket.ToArray());
            cbGameMarket.SelectedIndex = 0;
            pbGame.Image = pbGame.InitialImage;

            if (Environment.OSVersion.Version.Major < 10)
            {
                linkAppxAdd.Enabled = false;
                gbAddAppxPackage.Visible = gbGamingServices.Visible = false;
            }
            string xboxGameFilePath = Path.Combine(resourceDirectory, "XboxGame.json");
            if (File.Exists(xboxGameFilePath))
            {
                string json = File.ReadAllText(xboxGameFilePath);
                XboxGameDownload.XboxGame? xboxGame = null;
                try
                {
                    xboxGame = JsonSerializer.Deserialize<XboxGameDownload.XboxGame>(json);
                }
                catch { }
                if (xboxGame != null && xboxGame.Serialize != null && !xboxGame.Serialize.IsEmpty)
                    XboxGameDownload.dicXboxGame = xboxGame.Serialize;
            }
            if (bAutoStartup)
            {
                ButStart_Click(null, null);
            }
        }

        private class ComboboxItem
        {
            public string? Text { get; set; }
            public object? Value { get; set; }
            public override string? ToString()
            {
                return Text;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DateTime.Compare(DateTime.Now, new DateTime(Properties.Settings.Default.NextUpdate)) >= 0)
            {
                tsmUpdate.Enabled = false;
                ThreadPool.QueueUserWorkItem(delegate { UpdateFile.Start(true, this); });
            }
            Task.Run(async () =>
            {
                bIPv6Support = await ClassWeb.TestIPv6();
                if (bIPv6Support) SaveLog("提示信息", "检测到正在使用IPv6联网，如果加速主机下载(Xbox、PS)，必需进入路由器后台关闭，PC用户忽略此信息。", "localhost", 0x0000FF);
            });
            if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 1803)
            {
                Task.Run(() =>
                {
                    string outputString = "";
                    try
                    {
                        using Process p = new();
                        p.StartInfo.FileName = "powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-DOConfig");
                        p.StandardInput.Close();
                        outputString = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                    }
                    catch { }
                    Match result = Regex.Match(outputString, @"DownBackLimitBps\s+:\s+(\d+)\r\n[a-zA-Z]+\s+:\s+[a-zA-Z]+\r\nDownloadForegroundLimitBps\s+:\s+(\d+)");
                    if (result.Success)
                    {
                        double DownBackLimitBps = double.Parse(result.Groups[1].Value);
                        double DownloadForegroundLimitBps = double.Parse(result.Groups[2].Value);
                        if (DownBackLimitBps > 0 || DownloadForegroundLimitBps > 0)
                        {
                            StringBuilder sb = new();
                            sb.Append("系统设置限速，");
                            if (DownBackLimitBps > 0) sb.Append("后台下载被限制" + Math.Round(DownBackLimitBps / 131072, 1, MidpointRounding.AwayFromZero) + "Mbps，");
                            if (DownloadForegroundLimitBps > 0) sb.Append("前台下载被限制" + Math.Round(DownloadForegroundLimitBps / 131072, 1, MidpointRounding.AwayFromZero) + "Mbps，");
                            sb.Append("请在Windows系统搜索“传递优化高级设置”解除限制。");
                            SaveLog("警告信息", sb.ToString(), "localhost", 0xFF0000);
                        }
                    }
                });
            }
        }

        private void TsmUpdate_Click(object sender, EventArgs e)
        {
            tsmUpdate.Enabled = false;
            ThreadPool.QueueUserWorkItem(delegate { UpdateFile.Start(false, this); });
        }

        private void TsmiStartup_Click(object sender, EventArgs e)
        {
            FormStartup dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void TsmProductManual_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(UpdateFile.project) { UseShellExecute = true });
        }

        private void TsmAbout_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/skydevil88/XboxDownload";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void TsmOpenSite_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            Process.Start(new ProcessStartInfo((string)tsmi.Tag) { UseShellExecute = true });
        }

        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TsmiShow_Click(sender, EventArgs.Empty);
            }
        }

        private void TsmiShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            OldUp = OldDown = 0;
            timerTraffic.Start();
        }

        private void TsmiExit_Click(object sender, EventArgs e)
        {
            bClose = true;
            this.Close();
        }

        bool bTips = true, bClose = false;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bClose) return;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            if (bTips && !bAutoStartup)
            {
                bTips = false;
                this.notifyIcon1.ShowBalloonTip(5, "Xbox下载助手", "最小化到系统托盘", ToolTipIcon.Info);
                timerTraffic.Stop();
            }
            e.Cancel = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon1.Visible = false;
            if (bServiceFlag) ButStart_Click(null, null);
            if (Form1.bAutoStartup) Application.Exit();
            this.Dispose();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Show();
            switch (tabControl1.SelectedTab.Name)
            {
                case "tabStore":
                    if (gbMicrosoftStore.Tag == null || (gbMicrosoftStore.Tag != null && DateTime.Compare(DateTime.Now, Convert.ToDateTime(gbMicrosoftStore.Tag).AddHours(12)) >= 0))
                    {
                        gbMicrosoftStore.Tag = DateTime.Now;
                        cbGameXGP1.Items.Clear();
                        cbGameXGP2.Items.Clear();
                        dicExchangeRate.Clear();
                    }
                    if (Environment.OSVersion.Version.Major >= 10)
                    {
                        if (cbGameXGP1.Items.Count == 0 || (cbGameXGP1.Items[0].ToString() ?? string.Empty).Contains("(加载失败)") || (cbGameXGP1.Items[^1].ToString() ?? string.Empty).Contains("(加载失败)"))
                        {
                            cbGameXGP1.Items.Clear();
                            cbGameXGP1.Items.Add(new Product("最受欢迎 Xbox Game Pass 游戏 (加载中)", "0"));
                            cbGameXGP1.SelectedIndex = 0;
                            ThreadPool.QueueUserWorkItem(delegate { XboxGamePass(1); });
                        }
                        if (cbGameXGP2.Items.Count == 0 || (cbGameXGP2.Items[0].ToString() ?? string.Empty).Contains("(加载失败)") || (cbGameXGP2.Items[^1].ToString() ?? string.Empty).Contains("(加载失败)"))
                        {
                            cbGameXGP2.Items.Clear();
                            cbGameXGP2.Items.Add(new Product("近期新增 Xbox Game Pass 游戏 (加载中)", "0"));
                            cbGameXGP2.SelectedIndex = 0;
                            ThreadPool.QueueUserWorkItem(delegate { XboxGamePass(2); });
                        }
                    }
                    else if (cbGameXGP1.Items.Count == 0)
                    {
                        cbGameXGP1.Items.Add(new Product("最受欢迎 Xbox Game Pass 游戏 (不支持)", "0"));
                        cbGameXGP1.SelectedIndex = 0;
                        cbGameXGP2.Items.Add(new Product("近期新增 Xbox Game Pass 游戏 (不支持)", "0"));
                        cbGameXGP2.SelectedIndex = 0;
                    }
                    break;
                case "tabTools":
                    if (cbAppxDrive.Items.Count == 0 && gbAddAppxPackage.Visible)
                    {
                        LinkAppxRefreshDrive_LinkClicked(null, null);
                    }
                    break;
            }
        }

        private void Dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        delegate void CallbackTextBox(TextBox tb, string str);
        public void SetTextBox(TextBox tb, string str)
        {
            if (tb.InvokeRequired)
            {
                CallbackTextBox d = new(SetTextBox);
                Invoke(d, new object[] { tb, str });
            }
            else tb.Text = str;
        }

        delegate void CallbackSaveLog(string status, string content, string ip, int argb);
        public void SaveLog(string status, string content, string ip, int argb = 0)
        {
            if (lvLog.InvokeRequired)
            {
                CallbackSaveLog d = new(SaveLog);
                Invoke(d, new object[] { status, content, ip, argb });
            }
            else
            {
                ListViewItem listViewItem = new(new string[] { status, content, ip, DateTime.Now.ToString("HH:mm:ss.fff") });
                if (argb >= 1) listViewItem.ForeColor = Color.FromArgb(argb);
                lvLog.Items.Insert(0, listViewItem);
            }
        }

        #region 选项卡-服务
        NetworkInterface? adapter = null;
        private long OldUp { get; set; }
        private long OldDown { get; set; }


        private void TimerTraffic_Tick(object sender, EventArgs e)
        {
            if (adapter != null)
            {
                long nowUp = adapter.GetIPStatistics().BytesSent;
                long nowDown = adapter.GetIPStatistics().BytesReceived;
                if (OldUp > 0 || OldDown > 0)
                {
                    long up = nowUp - OldUp;
                    long down = nowDown - OldDown;
                    labelTraffic.Text = String.Format("流量: ↑ {0} ↓ {1}", ClassMbr.ConvertBps(up * 8), ClassMbr.ConvertBps(down * 8));
                }
                OldUp = nowUp;
                OldDown = nowDown;
            }
        }

        private void CkbNSBrowser_CheckedChanged(object sender, EventArgs e)
        {
            linkNSHomepage.Enabled = ckbNSBrowser.Checked;
            if (ckbNSBrowser.Checked) ckbHttpService.Checked = true;
        }

        private void LinkNSHomepage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormNSBH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void RbCDN_CheckedChanged(object? sender, EventArgs? e)
        {
            if (sender == null) return;
            RadioButton control = (RadioButton)sender;
            if (!control.Checked) return;
            switch (control.Name)
            {
                case "rbEpicCDN1":
                    if (!Properties.Settings.Default.EpicCDN)
                        tbEpicIP.Clear();
                    else
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    break;
                case "rbEpicCDN2":
                    if (Properties.Settings.Default.EpicCDN)
                        tbEpicIP.Clear();
                    else
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    break;
            }
        }

        private void CkbBattleNetease_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbBattleNetease.Checked)
            {
                ckbDnsService.Checked = true;
                ckbSetDns.Checked = true;
            }
        }

        private void ButBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new()
            {
                SelectedPath = tbLocalPath.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tbLocalPath.Text = dlg.SelectedPath;
            }
        }

        private void LinkDoHServer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDoH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void CkbSetDns_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbSetDns.Checked)
            {
                ckbDnsService.Checked = true;
            }
            else
            {
                ckbBattleNetease.Checked = false;
            }
        }

        private void CkbGameLink_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbGameLink.Checked)
            {
                ckbHttpService.Checked = true;
            }
            else
            {
                ckbLocalUpload.Checked = false;
            }
        }

        private void CkbLocalUpload_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbLocalUpload.Checked)
            {
                ckbGameLink.Checked = true;
                ckbHttpService.Checked = true;
            }
        }

        private async void CkbBetterAkamaiIP_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbBetterAkamaiIP.Checked)
            {
                bool update = true;
                FileInfo fi = new(Path.Combine(resourceDirectory, "IP.AkamaiV2.txt"));
                if (fi.Exists && fi.Length >= 1) update = DateTime.Compare(DateTime.Now, fi.LastWriteTime.AddDays(7)) >= 0;
                if (update) await UpdateFile.DownloadIP(fi);
                List<string[]> lsIP = new();
                if (fi.Exists)
                {
                    using StreamReader sr = fi.OpenText();
                    string content = sr.ReadToEnd();
                    Match result = FormImportIP.rMatchIP.Match(content);
                    while (result.Success)
                    {
                        lsIP.Add(new string[] { result.Groups["IP"].Value, result.Groups["Location"].Value });
                        result = result.NextMatch();
                    }
                }
                if (lsIP.Count == 0)
                {
                    MessageBox.Show("Akamai 优选 IP 列表不存在，请在测速选项卡中导入。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                lsIP = lsIP.OrderBy(s => Guid.NewGuid()).Take(30).ToList();
                ckbBetterAkamaiIP.Enabled = false;
                string[] test = { "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt", "http://gst.prod.dl.playstation.net/networktest/get_192m", "http://ctest-dl-lp1.cdn.nintendo.net/30m" };
                Random ran = new();
                Uri uri = new(test[ran.Next(test.Length)]);
                StringBuilder sb = new();
                sb.AppendLine("GET " + uri.PathAndQuery + " HTTP/1.1");
                sb.AppendLine("Host: " + uri.Host);
                sb.AppendLine("User-Agent: XboxDownload" + (uri.Host.Contains("nintendo") ? "/Nintendo NX" : ""));
                sb.AppendLine("Range: bytes=0-10485759");
                sb.AppendLine();
                byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());
                CancellationTokenSource cts = new();
                Task[] tasks = new Task[lsIP.Count];
                string[] akamai = Array.Empty<string>();
                for (int i = 0; i <= tasks.Length - 1; i++)
                {
                    string[] _ip = lsIP[i];
                    tasks[i] = new Task(() =>
                    {
                        SocketPackage socketPackage = uri.Scheme == "http" ? ClassWeb.TcpRequest(uri, buffer, _ip[0], false, null, 30000, cts) : ClassWeb.TlsRequest(uri, buffer, _ip[0], false, null, 30000, cts);
                        if (akamai.Length == 0 && socketPackage.Buffer?.Length == 10485760) akamai = _ip;
                        else if (!cts.IsCancellationRequested) Task.Delay(30000, cts.Token);
                        socketPackage.Buffer = null;
                    });
                }
                Array.ForEach(tasks, x => x.Start());
                await Task.WhenAny(tasks);
                cts.Cancel();
                GC.Collect();
                if (!bServiceFlag) return;
                if (akamai.Length == 0)
                {
                    cts = new();
                    tasks = lsIP.Select(_ip => Task.Run(() =>
                    {
                        Uri uri = new(test[ran.Next(test.Length)]);
                        using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(uri.ToString().Replace(uri.Host, _ip[0]), "HEAD", null, null, new() { { "Host", uri.Host }, { "User-Agent", "XboxDownload" + (uri.Host.Contains("nintendo") ? "/Nintendo NX" : "") } }, 3000, null, cts.Token);
                        if (akamai.Length == 0 && response != null && response.IsSuccessStatusCode) akamai = _ip;
                        else if (!cts.IsCancellationRequested) Task.Delay(3000, cts.Token);
                    })).ToArray();
                    await Task.WhenAny(tasks);
                    cts.Cancel();
                    if (!bServiceFlag) return;
                    if (akamai.Length > 0)
                    {
                        SaveLog("提示信息", "优选 Akamai IP 测速超时，随机指定 -> " + akamai[0] + "，建议在测速选项卡中手动测速指定。", "localhost", 0xFF0000);
                    }
                    else
                    {
                        SaveLog("提示信息", "优选 Akamai IP 全部不能连接，请检查网络状况。", "localhost", 0xFF0000);
                        ckbBetterAkamaiIP.Enabled = true;
                        return;
                    }
                }
                else
                {
                    SaveLog("提示信息", "优选 Akamai IP -> " + akamai[0] + " (" + akamai[1] + ")", "localhost", 0x008000);
                }
                if (akamai.Length > 0)
                {
                    ckbBetterAkamaiIP.Tag = true;
                    DnsListen.SetAkamaiIP(akamai[0]);
                    UpdateHosts(true, akamai[0]);
                    DnsListen.UpdateHosts(akamai[0]);
                    if (ckbLocalUpload.Checked) Properties.Settings.Default.LocalUpload = false;
                    tbComIP.Text = tbCnIP.Text = tbCnIP2.Text = tbAppIP.Text = tbPSIP.Text = tbNSIP.Text = tbEAIP.Text = tbUbiIP.Text = tbBattleIP.Text = akamai[0];
                    if (!Properties.Settings.Default.EpicCDN) tbEpicIP.Text = akamai[0];
                }
                ckbBetterAkamaiIP.Enabled = true;
            }
            else if (bServiceFlag && Convert.ToBoolean(ckbBetterAkamaiIP.Tag))
            {
                ckbBetterAkamaiIP.Tag = null;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
                    tbComIP.Text = Properties.Settings.Default.ComIP;
                else if (DnsListen.dicService2V4.TryGetValue("xvcf2.xboxlive.com", out List<ResouceRecord>? lsComIp))
                    tbComIP.Text = lsComIp.Count >= 1 ? new IPAddress(lsComIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                    tbCnIP.Text = Properties.Settings.Default.CnIP;
                else if (DnsListen.dicService2V4.TryGetValue("assets2.xboxlive.cn", out List<ResouceRecord>? lsCnIp))
                    tbCnIP.Text = lsCnIp.Count >= 1 ? new IPAddress(lsCnIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP2))
                    tbCnIP2.Text = Properties.Settings.Default.CnIP2;
                else if (DnsListen.dicService2V4.TryGetValue("dlassets2.xboxlive.cn", out List<ResouceRecord>? lsCnIp2))
                    tbCnIP2.Text = lsCnIp2.Count >= 1 ? new IPAddress(lsCnIp2?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                    tbAppIP.Text = Properties.Settings.Default.AppIP;
                else if (DnsListen.dicService2V4.TryGetValue("2.tlu.dl.delivery.mp.microsoft.com", out List<ResouceRecord>? lsAppIp))
                    tbAppIP.Text = lsAppIp.Count >= 1 ? new IPAddress(lsAppIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.PSIP))
                    tbPSIP.Text = Properties.Settings.Default.PSIP;
                else if (DnsListen.dicService2V4.TryGetValue("gst.prod.dl.playstation.net", out List<ResouceRecord>? lsPSIp))
                    tbPSIP.Text = lsPSIp.Count >= 1 ? new IPAddress(lsPSIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.NSIP))
                    tbNSIP.Text = Properties.Settings.Default.NSIP;
                else if (DnsListen.dicService2V4.TryGetValue("atum.hac.lp1.d4c.nintendo.net", out List<ResouceRecord>? lsNSIp))
                    tbNSIP.Text = lsNSIp.Count >= 1 ? new IPAddress(lsNSIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                    tbEAIP.Text = Properties.Settings.Default.EAIP;
                else if (DnsListen.dicService2V4.TryGetValue("origin-a.akamaihd.net", out List<ResouceRecord>? lsEAIp))
                    tbEAIP.Text = lsEAIp.Count >= 1 ? new IPAddress(lsEAIp?[0].Datas!).ToString() : "";
                if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                    tbBattleIP.Text = Properties.Settings.Default.BattleIP;
                else if (DnsListen.dicService2V4.TryGetValue("blzddist1-a.akamaihd.net", out List<ResouceRecord>? lsBattleIp))
                    tbBattleIP.Text = lsBattleIp.Count >= 1 ? new IPAddress(lsBattleIp?[0].Datas!).ToString() : "";
                if (!Properties.Settings.Default.EpicCDN)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
                        tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                    else if (DnsListen.dicService2V4.TryGetValue("epicgames-download1.akamaized.net", out List<ResouceRecord>? lsEpicIp))
                        tbEpicIP.Text = lsEpicIp.Count >= 1 ? new IPAddress(lsEpicIp?[0].Datas!).ToString() : "";
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UbiIP))
                    tbUbiIP.Text = Properties.Settings.Default.UbiIP;
                else if (DnsListen.dicService2V4.TryGetValue("uplaypc-s-ubisoft.cdn.ubionline.com.cn", out List<ResouceRecord>? lsUbiIp))
                    tbUbiIP.Text = lsUbiIp.Count >= 1 ? new IPAddress(lsUbiIp?[0].Datas!).ToString() : "";
                DnsListen.SetAkamaiIP();
                UpdateHosts(true);
                DnsListen.UpdateHosts();
                if (ckbLocalUpload.Checked) Properties.Settings.Default.LocalUpload = true;
            }
            if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
        }

        public async void ButStart_Click(object? sender, EventArgs? e)
        {
            if (bServiceFlag)
            {
                butStart.Enabled = false;
                bServiceFlag = false;
                UpdateHosts(false);
                if (Properties.Settings.Default.SetDns) ClassDNS.SetDns();
                tbDnsIP.Text = Properties.Settings.Default.DnsIP;
                tbComIP.Text = Properties.Settings.Default.ComIP;
                tbCnIP.Text = Properties.Settings.Default.CnIP;
                tbCnIP2.Text = Properties.Settings.Default.CnIP2;
                tbAppIP.Text = Properties.Settings.Default.AppIP;
                tbPSIP.Text = Properties.Settings.Default.PSIP;
                tbNSIP.Text = Properties.Settings.Default.NSIP;
                tbEAIP.Text = Properties.Settings.Default.EAIP;
                tbBattleIP.Text = Properties.Settings.Default.BattleIP;
                tbEpicIP.Text = Properties.Settings.Default.EpicIP;
                tbUbiIP.Text = Properties.Settings.Default.UbiIP;
                pictureBox1.Image = Properties.Resource.Xbox1;
                linkTestDns.Enabled = linkRestartEABackgroundService.Enabled = linkRestartEpic.Enabled = false;

                foreach (Control control in this.groupBox1.Controls)
                {
                    if ((control is TextBox || control is CheckBox || control is Panel || control is Button || control is ComboBox) && control != butStart)
                        control.Enabled = true;
                }
                ckbBetterAkamaiIP.Checked = ckbBetterAkamaiIP.Enabled = false;
                ckbBetterAkamaiIP.Tag = null;
                linkRepairDNS.Enabled = linkSniProxy.Enabled = cbLocalIP.Enabled = true;
                linkSniProxy.Text = "设置";
                dnsListen.Close();
                httpListen.Close();
                httpsListen.Close();
                Program.SystemSleep.RestoreForCurrentThread();
                if (Properties.Settings.Default.SetDns)
                {
                    butStart.Text = "正在停止...";
                    await Task.Run(() =>
                    {
                        string[] hosts = { "www.xbox.com", "www.playstation.com", "www.nintendo.com" };
                        for (int i = 0; i < 15; i++)
                        {
                            IPHostEntry? hostEntry = null;
                            try
                            {
                                hostEntry = Dns.GetHostEntry(hosts[i % hosts.Length]);
                            }
                            catch { }
                            if (hostEntry == null)
                                Thread.Sleep(1000);
                            else
                                break;
                        }
                    });
                }
                butStart.Text = "开始监听";
            }
            else
            {
                string? dnsIP = null;
                if (!string.IsNullOrWhiteSpace(tbDnsIP.Text))
                {
                    if (IPAddress.TryParse(tbDnsIP.Text.Trim(), out IPAddress? ipAddress) && !IPAddress.IsLoopback(ipAddress))
                    {
                        dnsIP = tbDnsIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("DNS 服务器 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbDnsIP.Focus();
                        tbDnsIP.SelectAll();
                        return;
                    }
                }
                string? comIP = null;
                if (!string.IsNullOrWhiteSpace(tbComIP.Text))
                {
                    if (IPAddress.TryParse(tbComIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        comIP = tbComIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 com 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbComIP.Focus();
                        tbComIP.SelectAll();
                        return;
                    }
                }
                string? cnIP = null;
                if (!string.IsNullOrWhiteSpace(tbCnIP.Text))
                {
                    if (IPAddress.TryParse(tbCnIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        cnIP = tbCnIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 cn1 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbCnIP.Focus();
                        tbCnIP.SelectAll();
                        return;
                    }
                }
                string? cnIP2 = null;
                if (!string.IsNullOrWhiteSpace(tbCnIP2.Text))
                {
                    if (IPAddress.TryParse(tbCnIP2.Text.Trim(), out IPAddress? ipAddress))
                    {
                        cnIP = tbCnIP2.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 cn2 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbCnIP2.Focus();
                        tbCnIP2.SelectAll();
                        return;
                    }
                }
                string? appIP = null;
                if (!string.IsNullOrWhiteSpace(tbAppIP.Text))
                {
                    if (IPAddress.TryParse(tbAppIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        appIP = tbAppIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定应用下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbAppIP.Focus();
                        tbAppIP.SelectAll();
                        return;
                    }
                }
                string? psIP = null;
                if (!string.IsNullOrWhiteSpace(tbPSIP.Text))
                {
                    if (IPAddress.TryParse(tbPSIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        psIP = tbPSIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 PS 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbPSIP.Focus();
                        tbPSIP.SelectAll();
                        return;
                    }
                }
                string? nsIP = null;
                if (!string.IsNullOrWhiteSpace(tbNSIP.Text))
                {
                    if (IPAddress.TryParse(tbNSIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            nsIP = tbNSIP.Text = ipAddress.ToString();
                        }
                        else
                        {
                            MessageBox.Show("NS 主机不支持 IPv6", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            tbNSIP.Focus();
                            tbNSIP.SelectAll();
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("指定 NS 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbNSIP.Focus();
                        tbNSIP.SelectAll();
                        return;
                    }
                }
                string? eaIP = null;
                if (!string.IsNullOrWhiteSpace(tbEAIP.Text))
                {
                    if (IPAddress.TryParse(tbEAIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        eaIP = tbEAIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 EA 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbEAIP.Focus();
                        tbEAIP.SelectAll();
                        return;
                    }
                }
                string? battleIP = null;
                if (!string.IsNullOrWhiteSpace(tbBattleIP.Text))
                {
                    if (IPAddress.TryParse(tbBattleIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        battleIP = tbBattleIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 战网 域名IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbBattleIP.Focus();
                        tbBattleIP.SelectAll();
                        return;
                    }
                }
                string? epicIP = null;
                if (!string.IsNullOrWhiteSpace(tbEpicIP.Text))
                {
                    if (rbEpicCDN2.Checked)
                    {
                        if (IPAddress.TryParse(tbEpicIP.Text.Trim(), out IPAddress? ipAddress))
                        {
                            epicIP = tbEpicIP.Text = ipAddress.ToString();
                        }
                        else
                        {
                            MessageBox.Show("指定 Epic 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            tbEpicIP.Focus();
                            tbEpicIP.SelectAll();
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("腾讯云CDN会自动重定向分配下载服务器，无需指定IP。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        tbEpicIP.Focus();
                        tbEpicIP.SelectAll();
                        return;
                    }
                }
                string? ubiIP = null;
                if (!string.IsNullOrWhiteSpace(tbUbiIP.Text))
                {
                    if (IPAddress.TryParse(tbUbiIP.Text.Trim(), out IPAddress? ipAddress))
                    {
                        ubiIP = tbUbiIP.Text = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 育碧 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbUbiIP.Focus();
                        tbUbiIP.SelectAll();
                        return;
                    }
                }
                butStart.Enabled = false;

                Properties.Settings.Default.DnsIP = dnsIP;
                Properties.Settings.Default.ComIP = comIP;
                Properties.Settings.Default.CnIP = cnIP;
                Properties.Settings.Default.CnIP2 = cnIP2;
                Properties.Settings.Default.AppIP = appIP;
                Properties.Settings.Default.PSIP = psIP;
                Properties.Settings.Default.NSIP = nsIP;
                Properties.Settings.Default.NSBrowser = ckbNSBrowser.Checked;
                Properties.Settings.Default.EAIP = eaIP;
                Properties.Settings.Default.BattleIP = battleIP;
                Properties.Settings.Default.BattleNetease = ckbBattleNetease.Checked;
                Properties.Settings.Default.EpicIP = epicIP;
                Properties.Settings.Default.EpicCDN = rbEpicCDN1.Checked;
                Properties.Settings.Default.UbiIP = ubiIP;
                Properties.Settings.Default.GameLink = ckbGameLink.Checked;
                Properties.Settings.Default.Truncation = ckbTruncation.Checked;
                Properties.Settings.Default.LocalUpload = ckbLocalUpload.Checked;
                Properties.Settings.Default.LocalPath = tbLocalPath.Text;
                Properties.Settings.Default.ListenIP = cbListenIP.SelectedIndex;
                Properties.Settings.Default.DnsService = ckbDnsService.Checked;
                Properties.Settings.Default.HttpService = ckbHttpService.Checked;
                Properties.Settings.Default.DoH = ckbDoH.Checked;
                Properties.Settings.Default.DisableIPv6DNS = ckbDisableIPv6DNS.Checked;
                Properties.Settings.Default.SetDns = ckbSetDns.Checked;
                Properties.Settings.Default.MicrosoftStore = ckbMicrosoftStore.Checked;
                Properties.Settings.Default.EAStore = ckbEAStore.Checked;
                Properties.Settings.Default.BattleStore = ckbBattleStore.Checked;
                Properties.Settings.Default.EpicStore = ckbEpicStore.Checked;
                Properties.Settings.Default.UbiStore = ckbUbiStore.Checked;
                Properties.Settings.Default.SniProxy = ckbSniProxy.Checked;
                Properties.Settings.Default.Save();

                try
                {
                    Type? t1 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                    if (t1 != null)
                    {
                        if (Activator.CreateInstance(t1) is INetFwPolicy2 policy2)
                        {
                            bool bRuleAdd = true;
                            foreach (INetFwRule rule in policy2.Rules)
                            {
                                if (rule.Name == "XboxDownload" || rule.Name == "Xbox下载助手")
                                {
                                    if (bRuleAdd && rule.ApplicationName == Application.ExecutablePath && rule.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN && rule.Protocol == (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY && rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW && rule.Profiles == (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL && rule.Enabled)
                                        bRuleAdd = false;
                                    else
                                        policy2.Rules.Remove(rule.Name);
                                }
                                else if (String.Equals(rule.ApplicationName, Application.ExecutablePath, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    policy2.Rules.Remove(rule.Name);
                                }
                            }
                            if (bRuleAdd)
                            {
                                Type? t2 = Type.GetTypeFromProgID("HNetCfg.FwRule");
                                if (t2 != null)
                                {
                                    if (Activator.CreateInstance(t2) is INetFwRule rule)
                                    {
                                        rule.Name = "XboxDownload";
                                        rule.ApplicationName = Application.ExecutablePath;
                                        rule.Enabled = true;
                                        policy2.Rules.Add(rule);
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                string resultInfo = string.Empty;
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("netstat", @"-aon")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    p.Start();
                    resultInfo = p.StandardOutput.ReadToEnd();
                    p.Close();
                }
                Match result = Regex.Match(resultInfo, @"(?<protocol>TCP|UDP)\s+(?<ip>[^\s]+):(?<port>80|443|53)\s+[^\s]+\s+(?<status>[^\s]+\s+)?(?<pid>\d+)");
                if (result.Success)
                {
                    ConcurrentDictionary<Int32, Process?> dic = new();
                    StringBuilder sb = new();
                    while (result.Success)
                    {
                        string ip = result.Groups["ip"].Value;
                        if (Properties.Settings.Default.ListenIP == 0 && ip == Properties.Settings.Default.LocalIP || Properties.Settings.Default.ListenIP == 1)
                        {
                            string protocol = result.Groups["protocol"].Value;
                            if (protocol == "TCP" && result.Groups["status"].Value.Trim() == "LISTENING" || protocol == "UDP")
                            {
                                int port = Convert.ToInt32(result.Groups["port"].Value);
                                if (port == 53 && Properties.Settings.Default.DnsService || ((port == 80 || port == 443) && Properties.Settings.Default.HttpService))
                                {
                                    int pid = int.Parse(result.Groups["pid"].Value);
                                    if (!dic.ContainsKey(pid) && pid != 0)
                                    {
                                        sb.AppendLine(protocol + "\t" + ip + ":" + port);
                                        if (pid == 4)
                                        {
                                            dic.TryAdd(pid, null);
                                            sb.AppendLine("系统服务");
                                        }
                                        else
                                        {
                                            try
                                            {
                                                Process proc = Process.GetProcessById(pid);
                                                dic.TryAdd(pid, proc);
                                                string? filename = proc.MainModule?.FileName;
                                                sb.AppendLine(filename);
                                            }
                                            catch
                                            {
                                                sb.AppendLine("未知");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        result = result.NextMatch();
                    }
                    if (!dic.IsEmpty && MessageBox.Show("检测到以下端口被占用\n" + sb.ToString() + "\n是否尝试强制结束占用端口程序？", "端口占用", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        foreach (var item in dic)
                        {
                            if (item.Key == 4)
                            {
                                ServiceController[] services = ServiceController.GetServices();
                                foreach (ServiceController service in services)
                                {
                                    switch (service.ServiceName)
                                    {
                                        case "MsDepSvc":        //Web Deployment Agent Service (MsDepSvc)
                                        case "PeerDistSvc":     //BranchCache (PeerDistSvc)
                                        case "ReportServer":    //SQL Server Reporting Services (ReportServer)
                                        case "SyncShareSvc":    //Sync Share Service (SyncShareSvc)
                                        case "W3SVC":           //World Wide Web Publishing Service (W3SVC)
                                            if (service.Status == ServiceControllerStatus.Running)
                                            {
                                                service.Stop();
                                                service.WaitForStatus(ServiceControllerStatus.Stopped);
                                            }
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    item.Value?.Kill();
                                }
                                catch { }
                            }
                        }
                    }
                }
                bServiceFlag = true;
                pictureBox1.Image = Properties.Resource.Xbox2;
                butStart.Text = "停止监听";
                foreach (Control control in this.groupBox1.Controls)
                {
                    if (control is TextBox || control is CheckBox || control is Panel || control is Button || control is ComboBox)
                        control.Enabled = false;
                }
                ckbBetterAkamaiIP.Enabled = true;
                linkRepairDNS.Enabled = cbLocalIP.Enabled = false;
                if (Properties.Settings.Default.SniProxy)
                {
                    linkSniProxy.Text = "清理";
                    foreach (var proxy in HttpsListen.dicSniProxy.Values)
                    {
                        proxy.IPs = null;
                    }
                    _ = Task.Run(async () =>
                    {
                        bIPv6Support = await ClassWeb.TestIPv6();
                    });
                }
                else linkSniProxy.Enabled = false;
                UpdateHosts(true);
                DnsListen.UpdateHosts();
                if (Properties.Settings.Default.EAStore) linkRestartEABackgroundService.Enabled = true;
                if (Properties.Settings.Default.EpicStore) linkRestartEpic.Enabled = true;
                if (Properties.Settings.Default.DnsService)
                {
                    linkTestDns.Enabled = true;
                    new Thread(new ThreadStart(dnsListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                }
                if (Properties.Settings.Default.HttpService)
                {
                    new Thread(new ThreadStart(httpListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                    new Thread(new ThreadStart(httpsListen.Listen))
                    {
                        IsBackground = true
                    }.Start();
                }
                Program.SystemSleep.PreventForCurrentThread(false);
            }
            if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            butStart.Enabled = true;
        }

        private void UpdateHosts(bool add, string? akamai = null)
        {
            if (!(Properties.Settings.Default.MicrosoftStore || Properties.Settings.Default.EAStore || Properties.Settings.Default.BattleStore || Properties.Settings.Default.EpicStore || Properties.Settings.Default.UbiStore || Properties.Settings.Default.SniProxy)) return;

            StringBuilder sb = new();
            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                if (!(Properties.Settings.Default.SetDns && string.IsNullOrEmpty(Regex.Replace(sHosts, "#.*", "").Trim())))
                {
                    sHosts = Regex.Replace(sHosts, @"# Added by (XboxDownload|Xbox下载助手)\r\n(.*\r\n)*# End of (XboxDownload|Xbox下载助手)\r\n", "");
                    if (add)
                    {
                        if (string.IsNullOrEmpty(Properties.Settings.Default.ComIP)) tbComIP.Text = Properties.Settings.Default.LocalIP;
                        sb.AppendLine("# Added by XboxDownload");
                        if (Properties.Settings.Default.MicrosoftStore)
                        {
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                if (Properties.Settings.Default.GameLink)
                                {
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                else
                                {
                                    sb.AppendLine(akamai + " xvcf1.xboxlive.com");
                                    sb.AppendLine(akamai + " assets1.xboxlive.com");
                                    sb.AppendLine(akamai + " d1.xboxlive.com");
                                    sb.AppendLine(akamai + " dlassets.xboxlive.com");
                                    sb.AppendLine(akamai + " assets1.xboxlive.cn");
                                    sb.AppendLine(akamai + " d1.xboxlive.cn");
                                    sb.AppendLine(akamai + " dlassets.xboxlive.cn");
                                    sb.AppendLine(akamai + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                sb.AppendLine(akamai + " xvcf2.xboxlive.com");
                                sb.AppendLine(akamai + " assets2.xboxlive.com");
                                sb.AppendLine(akamai + " d2.xboxlive.com");
                                sb.AppendLine(akamai + " dlassets2.xboxlive.com");
                                sb.AppendLine(akamai + " assets2.xboxlive.cn");
                                sb.AppendLine(akamai + " d2.xboxlive.cn");
                                sb.AppendLine(akamai + " dlassets2.xboxlive.cn");
                                sb.AppendLine(akamai + " dl.delivery.mp.microsoft.com");
                                sb.AppendLine(akamai + " 2.tlu.dl.delivery.mp.microsoft.com");
                            }
                            else
                            {
                                string comIP = string.IsNullOrEmpty(Properties.Settings.Default.ComIP) ? Properties.Settings.Default.LocalIP : Properties.Settings.Default.ComIP;
                                if (Properties.Settings.Default.GameLink)
                                {
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(comIP + " xvcf2.xboxlive.com");
                                    sb.AppendLine(comIP + " assets2.xboxlive.com");
                                    sb.AppendLine(comIP + " d2.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets2.xboxlive.com");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " assets1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " d1.xboxlive.cn");
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " dlassets.xboxlive.cn");
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets2.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " 2.tlu.dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dlassets2.xboxlive.cn");
                                    }
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " tlu.dl.delivery.mp.microsoft.com");
                                }
                                else
                                {
                                    sb.AppendLine(comIP + " xvcf1.xboxlive.com");
                                    sb.AppendLine(comIP + " xvcf2.xboxlive.com");
                                    sb.AppendLine(comIP + " assets1.xboxlive.com");
                                    sb.AppendLine(comIP + " assets2.xboxlive.com");
                                    sb.AppendLine(comIP + " d1.xboxlive.com");
                                    sb.AppendLine(comIP + " d2.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets.xboxlive.com");
                                    sb.AppendLine(comIP + " dlassets2.xboxlive.com");
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets1.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " assets2.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d1.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP + " d2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP2))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.CnIP2 + " dlassets.xboxlive.cn");
                                        sb.AppendLine(Properties.Settings.Default.CnIP2 + " dlassets2.xboxlive.cn");
                                    }
                                    if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                                    {
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " tlu.dl.delivery.mp.microsoft.com");
                                        sb.AppendLine(Properties.Settings.Default.AppIP + " 2.tlu.dl.delivery.mp.microsoft.com");
                                    }
                                }
                            }
                            if (Properties.Settings.Default.HttpService)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " www.msftconnecttest.com");
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " packagespc.xboxlive.com");
                            }
                        }
                        if (Properties.Settings.Default.EAStore)
                        {
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                sb.AppendLine(akamai + " origin-a.akamaihd.net");
                            }
                            else if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                            {
                                sb.AppendLine(Properties.Settings.Default.EAIP + " origin-a.akamaihd.net");
                            }
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                        }
                        if (Properties.Settings.Default.BattleStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " us.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " eu.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " kr.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " level3.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " blizzard.gcdn.cloudn.co.kr");
                            sb.AppendLine("0.0.0.0 level3.ssl.blizzard.com");
                            if (!string.IsNullOrEmpty(akamai))
                            {
                                sb.AppendLine(akamai + " blzddist1-a.akamaihd.net");
                            }
                            else if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                            {
                                if (Regex.IsMatch(Properties.Settings.Default.BattleIP, @"^\d+\.\d+\.\d+\.\d+$"))
                                    sb.AppendLine(Properties.Settings.Default.BattleIP + " blzddist1-a.akamaihd.net");
                                else
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " blzddist1-a.akamaihd.net");
                            }
                        }
                        if (Properties.Settings.Default.EpicStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " fastly-download.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " cloudflare.epicgamescdn.com");
                            if (Properties.Settings.Default.EpicCDN)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " epicgames-download1.akamaized.net");
                                if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP)) sb.AppendLine(Properties.Settings.Default.EpicIP + " epicgames-download1-1251447533.file.myqcloud.com");
                            }
                            else
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " epicgames-download1-1251447533.file.myqcloud.com");
                                string ip = !string.IsNullOrEmpty(akamai) ? akamai : Properties.Settings.Default.EpicIP;
                                if (!string.IsNullOrEmpty(ip)) sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            }
                        }
                        if (Properties.Settings.Default.UbiStore)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " uplaypc-s-ubisoft.cdn.ubi.com");
                            if (!string.IsNullOrEmpty(Properties.Settings.Default.UbiIP))
                            {
                                if (Regex.IsMatch(Properties.Settings.Default.UbiIP, @"^\d+\.\d+\.\d+\.\d+$"))
                                    sb.AppendLine(Properties.Settings.Default.UbiIP + " uplaypc-s-ubisoft.cdn.ubionline.com.cn");
                                else
                                    sb.AppendLine(Properties.Settings.Default.LocalIP + " uplaypc-s-ubisoft.cdn.ubionline.com.cn");
                            }
                        }
                        if (Properties.Settings.Default.SniProxy)
                        {
                            foreach (string host in HttpsListen.dicSniProxy.Keys)
                            {
                                sb.AppendLine(Properties.Settings.Default.LocalIP + " " + host);
                            }
                        }
                        DataTable dt = Form1.dtHosts.Copy();
                        dt.RejectChanges();
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (!Convert.ToBoolean(dr["Enable"])) continue;
                            string? hostName = dr["HostName"].ToString()?.ToLower();
                            string? ip = dr["IP"].ToString()?.Trim();
                            if (!string.IsNullOrEmpty(hostName) && !hostName.StartsWith('*') && !string.IsNullOrEmpty(ip))
                            {
                                sb.AppendLine(ip + " " + hostName);
                            }
                        }
                        sb.AppendLine("# End of XboxDownload");
                        sHosts = sb.ToString() + sHosts;
                    }
                    FileSecurity fSecurity = fi.GetAccessControl();
                    fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        fi.Attributes = FileAttributes.Normal;
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        if (!string.IsNullOrEmpty(sHosts.Trim()))
                        {
                            using StreamWriter sw = new(fs);
                            sw.WriteLine(sHosts.Trim());
                        }
                    }
                    fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                }
            }
            catch (Exception ex)
            {
                if (add) MessageBox.Show("修改系统Hosts文件失败，错误信息：" + ex.Message + "\n\n三种解决方法：\n1、勾选“设置本机 DNS”。\n2、临时关闭安全软件或者添加白名单。\n3、手动删除\"" + Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\drivers\\etc\\hosts\"文件，点击开始监听会新建一个。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (Properties.Settings.Default.MicrosoftStore) ThreadPool.QueueUserWorkItem(delegate { RestartService("DoSvc"); });
        }

        private static void RestartService(string servicename)
        {
            Task.Run(() =>
            {
                ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == servicename).SingleOrDefault();
                if (service != null)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(30000);
                    try
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            });
        }

        private void LvLog_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && lvLog.SelectedItems.Count == 1)
            {
                cmsLog.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void LvLog_DoubleClick(object sender, EventArgs e)
        {
            if (lvLog.SelectedItems.Count == 1 && lvLog.SelectedItems[0].SubItems[0].Text.StartsWith("DNS"))
            {
                Match result = Regex.Match(lvLog.SelectedItems[0].SubItems[1].Text, @"(.+) -> ([^,']+)");
                if (result.Success)
                {
                    string host = result.Groups[1].Value;
                    string ip = result.Groups[2].Value;
                    if (Regex.IsMatch(ip, @"^((127\.0\.0\.1)|(10\.\d{1,3}\.\d{1,3}\.\d{1,3})|(172\.((1[6-9])|(2\d)|(3[01]))\.\d{1,3}\.\d{1,3})|(192\.168\.\d{1,3}\.\d{1,3}))$"))
                        return;
                    FormConnectTest dialog = new(host, ip);
                    dialog.ShowDialog();
                    dialog.Dispose();
                }
            }
        }

        private void TsmCopyLog_Click(object sender, EventArgs e)
        {
            string content = lvLog.SelectedItems[0].SubItems[1].Text;
            Clipboard.SetDataObject(content);
            if (Regex.IsMatch(content, @"^https?://(origin-a\.akamaihd\.net|ssl-lvlt\.cdn\.ea\.com|lvlt\.cdn\.ea\.com)"))
            {
                MessageBox.Show("离线包安装方法：下载完成后删除安装目录下的所有文件，把解压缩文件复制到安装目录，回到 EA app 或者 Origin 选择继续下载，等待游戏验证完成后即可。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsmExportLog_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new()
            {
                Title = "导出日志",
                Filter = "文本文件(*.txt)|*.txt",
                FileName = "导出日志"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new();
                for (int i = 0; i <= lvLog.Items.Count - 1; i++)
                {
                    sb.AppendLine(lvLog.Items[i].SubItems[0].Text + "\t" + lvLog.Items[i].SubItems[1].Text + "\t" + lvLog.Items[i].SubItems[2].Text + "\t" + lvLog.Items[i].SubItems[3].Text);
                }
                File.WriteAllText(dlg.FileName, sb.ToString());
            }
        }

        private void CbLocalIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LocalIP = cbLocalIP.Text;
            Properties.Settings.Default.Save();

            timerTraffic.Stop();
            adapter = (cbLocalIP.SelectedItem as ComboboxItem)?.Value as NetworkInterface;
            OldUp = OldDown = 0;
            timerTraffic.Start();
        }

        private void LinkTestDns_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDns dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void LabelTraffic_MouseEnter(object sender, EventArgs e)
        {
            if (adapter != null && adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) //Refresh Wireless adapter Speed
            {
                adapter = NetworkInterface.GetAllNetworkInterfaces().Where(s => s.Id == adapter!.Id).FirstOrDefault();
                OldUp = OldDown = 0;
            }
            if (adapter != null) toolTip1.SetToolTip(this.labelTraffic, "名称：" + adapter.Name + "\n描述：" + adapter.Description + "\n速度：" + ClassMbr.ConvertBps(adapter.Speed));
        }

        private void LinkRepairDNS_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show("非正常退出应用可能会造成DNS设置异常无法联网，\n此操作将把DNS设置改为自动获取，是否继续？", "修复 DNS", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    using (Process p = new())
                    {
                        p.StartInfo.FileName = @"powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-NetAdapter -Physical | Set-DnsClientServerAddress -ResetServerAddresses");
                        p.StandardInput.WriteLine("exit");
                    }
                    MessageBox.Show("修复 DNS 成功，如有其它问题可以在测速选项卡中点击“清除系统Hosts文件”。", "Success", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("修复 DNS 失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LinkRestartEABackgroundService_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? path = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                var rk = key.OpenSubKey(@"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop");
                if (rk != null)
                {
                    path = rk.GetValue("LauncherAppPath", null)?.ToString();
                    rk.Close();
                }
            }
            if (path != null && File.Exists(path))
            {
                if (MessageBox.Show("EA app 还没开始下载或者停止下载超过一分钟，可以不用修复。\n\n点击 “是” 将会立即更新 IP 并且重启 EA app，是否继续？", "修复 EA app", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
                    Process? processes = Process.GetProcesses().Where(s => s.ProcessName == "EADesktop").FirstOrDefault();
                    if (processes != null)
                    {
                        try
                        {
                            processes.Kill();
                        }
                        catch { }
                    }
                    ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "EABackgroundService").FirstOrDefault();
                    if (service != null)
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show("没有找到 EA app。", "修复 EA app", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LinkRestartEpic_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? path = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                var rk = key.OpenSubKey(@"SOFTWARE\WOW6432Node\Epic Games\EOS\InstallHelper");
                if (rk != null)
                {
                    path = rk.GetValue("Path", null)?.ToString();
                    rk.Close();
                }
                if (path != null)
                {
                    Match result = Regex.Match(path, @"(^.+\\Epic Games\\)");
                    if (result.Success) path = result.Groups[1].Value + "Launcher\\Portal\\Binaries\\Win32\\EpicGamesLauncher.exe";
                }
            }
            if (path != null && File.Exists(path))
            {
                if (MessageBox.Show("已要下载列表中的游戏需要重启客户端才能保证使用新 IP。\n\n点击 “是” 将会立即重启 Epic 客户端，是否继续？", "重启Epic客户端", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
                    Process? processes = Process.GetProcesses().Where(s => s.ProcessName == "EpicGamesLauncher").FirstOrDefault();
                    if (processes != null)
                    {
                        try
                        {
                            processes.Kill();
                        }
                        catch { }
                    }
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show("没有找到Epic客户端。", "重启Epic客户端", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LinkSniProxy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (bServiceFlag)
            {
                Task.Run(async () =>
                {
                    bIPv6Support = await ClassWeb.TestIPv6();
                });
                foreach (var proxy in HttpsListen.dicSniProxy.Values)
                {
                    proxy.IPs = null;
                }
                SaveLog("提示信息", "已清理本地代理服务DNS缓存。", "localhost", 0x008000);
            }
            else
            {
                FormSniProxy dialog = new();
                dialog.ShowDialog();
                dialog.Dispose();
            }
        }

        private void CkbRecordLog_CheckedChanged(object? sender, EventArgs? e)
        {
            Properties.Settings.Default.RecordLog = ckbRecordLog.Checked;
            Properties.Settings.Default.Save();
        }

        private void LinkClearLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lvLog.Items.Clear();
        }
        #endregion

        #region 选项卡-测速
        private void DgvIpList_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1) return;
            if (e.Button == MouseButtons.Left && dgvIpList.Columns[dgvIpList.CurrentCell.ColumnIndex].Name == "Col_Speed" && dgvIpList.Rows[e.RowIndex].Tag != null)
            {
                var msg = dgvIpList.Rows[e.RowIndex].Tag;
                if (msg != null)
                    MessageBox.Show(msg.ToString(), "Request Headers", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }

        private void DgvIpList_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.Button != MouseButtons.Right) return;
            string? host = dgvIpList.Tag.ToString();
            dgvIpList.ClearSelection();
            DataGridViewRow dgvr = dgvIpList.Rows[e.RowIndex];
            dgvr.Selected = true;
            tsmUseIP.Visible = tsmExportRule.Visible = true;
            foreach (var item in this.tsmUseIP.DropDownItems)
            {
                if (item.GetType() == typeof(ToolStripMenuItem))
                {
                    if (item is not ToolStripMenuItem tsmi || tsmi.Name == "tsmUseIPHosts")
                        continue;
                    tsmi.Visible = false;
                }
            }
            tssUseIP1.Visible = false;
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    tsmUseIPCn.Visible = true;
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    tsmUseIPCn2.Visible = true;
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    tsmUseIPApp.Visible = true;
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    tsmUseIPPS.Visible = true;
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    tssUseIP1.Visible = true;
                    tsmUseIPCom.Visible = true;
                    tsmUseIPXbox.Visible = true;
                    tsmUseIPApp.Visible = true;
                    tsmUseIPPS.Visible = true;
                    if (host != "AkamaiV6")
                        tsmUseIPNS.Visible = true;
                    tsmUseIPEa.Visible = true;
                    tsmUseAkamai.Visible = true;
                    tsmUseIPBattle.Visible = true;
                    tsmUseIPEpic.Visible = true;
                    tsmUseIPUbi.Visible = true;
                    break;
                case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                    tsmUseIPUbi.Visible = true;
                    break;
                default:
                    break;
            }
            tsmSpeedTest.Visible = true;
            tsmSpeedTest.Enabled = ctsSpeedTest is null;
            tsmSpeedTestLog.Enabled = dgvr.Tag is not null;
            cmsIP.Show(MousePosition.X, MousePosition.Y);
        }

        private void TsmUseIP_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? ip = dgvr.Cells["Col_IPAddress"].Value?.ToString();
            if (ip == null) return;
            if (sender is not ToolStripMenuItem tsmi) return;
            if (bServiceFlag && tsmi.Name != "tsmUseAkamai")
            {
                MessageBox.Show("请先停止监听后再设置。", "使用指定IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            switch (tsmi.Name)
            {
                case "tsmUseIPCom":
                    tabControl1.SelectedTab = tabService;
                    tbComIP.Text = ip;
                    tbComIP.Focus();
                    break;
                case "tsmUseIPCn":
                    tabControl1.SelectedTab = tabService;
                    tbCnIP.Text = ip;
                    tbCnIP.Focus();
                    break;
                case "tsmUseIPCn2":
                    tabControl1.SelectedTab = tabService;
                    tbCnIP2.Text = ip;
                    tbCnIP2.Focus();
                    break;
                case "tsmUseIPXbox":
                    tabControl1.SelectedTab = tabService;
                    tbComIP.Text = tbCnIP.Text = tbCnIP2.Text = ip;
                    tbCnIP2.Focus();
                    break;
                case "tsmUseIPApp":
                    tabControl1.SelectedTab = tabService;
                    tbAppIP.Text = ip;
                    tbAppIP.Focus();
                    break;
                case "tsmUseIPPS":
                    tabControl1.SelectedTab = tabService;
                    tbPSIP.Text = ip;
                    tbPSIP.Focus();
                    break;
                case "tsmUseIPNS":
                    tabControl1.SelectedTab = tabService;
                    tbNSIP.Text = ip;
                    tbNSIP.Focus();
                    break;
                case "tsmUseIPEa":
                    tabControl1.SelectedTab = tabService;
                    tbEAIP.Text = ip;
                    tbEAIP.Focus();
                    break;
                case "tsmUseIPBattle":
                    tabControl1.SelectedTab = tabService;
                    tbBattleIP.Text = ip;
                    tbBattleIP.Focus();
                    break;
                case "tsmUseIPEpic":
                    tabControl1.SelectedTab = tabService;
                    rbEpicCDN2.Checked = true;
                    tbEpicIP.Text = ip;
                    tbEpicIP.Focus();
                    break;
                case "tsmUseIPUbi":
                    tabControl1.SelectedTab = tabService;
                    tbUbiIP.Text = ip;
                    tbUbiIP.Focus();
                    break;
                case "tsmUseAkamai":
                    tabControl1.SelectedTab = tabCDN;
                    string ips = string.Join(", ", tbCdnAkamai.Text.Replace("，", ",").Split(',').Select(a => a.Trim()).Where(a => !a.Equals(ip)).ToArray());
                    tbCdnAkamai.Text = string.IsNullOrEmpty(ips) ? ip : ip + ", " + ips;
                    tbCdnAkamai.Focus();
                    tbCdnAkamai.SelectionStart = 0;
                    tbCdnAkamai.SelectionLength = ip.Length;
                    tbCdnAkamai.ScrollToCaret();
                    break;
            }
        }

        private async void CbImportIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbImportIP.SelectedIndex == 0) return;

            dgvIpList.Rows.Clear();
            flpTestUrl.Controls.Clear();
            tbDlUrl.Clear();
            cbImportIP.Enabled = false;

            string display = string.Empty, host = string.Empty;
            switch (cbImportIP.SelectedIndex)
            {
                case 1:
                    display = host = "assets1.xboxlive.cn";
                    break;
                case 2:
                    display = host = "dlassets.xboxlive.cn";
                    break;
                case 3:
                    display = host = "tlu.dl.delivery.mp.microsoft.com";
                    break;
                case 4:
                    display = host = "gst.prod.dl.playstation.net";
                    break;
                case 5:
                    display = host = "Akamai";
                    break;
                case 6:
                    display = "Akamai 优选 IP";
                    host = "AkamaiV2";
                    break;
                case 7:
                    display = "Akamai IPv6";
                    host = "AkamaiV6";
                    break;
                case 8:
                    display = host = "uplaypc-s-ubisoft.cdn.ubionline.com.cn";
                    break;
            }
            dgvIpList.Tag = host;
            gbIPList.Text = "IP 列表 (" + display + ")";

            bool update = true;
            FileInfo fi = new(Path.Combine(resourceDirectory, "IP." + host + ".txt"));
            if (fi.Exists && fi.Length >= 1) update = DateTime.Compare(DateTime.Now, fi.LastWriteTime.AddHours(24)) >= 0;
            if (update)
            {
                await UpdateFile.DownloadIP(fi);
            }
            string content = string.Empty;
            if (fi.Exists)
            {
                using StreamReader sr = fi.OpenText();
                content = sr.ReadToEnd();
            }
            List<DataGridViewRow> list = new();
            Match result = FormImportIP.rMatchIP.Match(content);
            if (result.Success)
            {
                while (result.Success)
                {
                    string ip = result.Groups["IP"].Value;
                    string location = result.Groups["Location"].Value.Trim();

                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    if (location.Contains("电信"))
                        dgvr.Cells[0].Value = ckbChinaTelecom.Checked;
                    if (location.Contains("联通"))
                        dgvr.Cells[0].Value = ckbChinaUnicom.Checked;
                    if (location.Contains("移动"))
                        dgvr.Cells[0].Value = ckbChinaMobile.Checked;
                    if (location.Contains("香港") || location.Contains("澳门"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("台湾"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("日本"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("韩国"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("新加坡"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "电信|联通|移动|香港|澳门|台湾|日本|韩国|新加坡"))
                        dgvr.Cells[0].Value = ckbOther.Checked;
                    dgvr.Cells[1].Value = ip;
                    dgvr.Cells[2].Value = location;
                    list.Add(dgvr);
                    result = result.NextMatch();
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                }
            }
            cbImportIP.Enabled = true;
        }

        private void AddTestUrl(string host)
        {
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://assets1.xboxlive.cn/Z/routing/extraextralarge.txt",
                            Text = "Xbox测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        string[,] games = new string[,]
                        {
                            {"光环: 无限(XS)", "0698b936-d300-4451-b9a0-0be0514bbbe5_xs", "/1/c6d465e7-df25-4b5c-987d-ad8dc643c24e/0698b936-d300-4451-b9a0-0be0514bbbe5/1.4036.37830.0.138cc93c-6e75-47aa-891f-54e9f29a54a1/Microsoft.254428597CFE2_1.4036.37830.0_neutral__8wekyb3d8bbwe_xs.xvc" },
                            {"极限竞速: 地平线5(PC)", "3d263e92-93cd-4f9b-90c7-5438150cecbf", "/8/51e320a7-a17e-4545-9f79-057ecd50b052/3d263e92-93cd-4f9b-90c7-5438150cecbf/3.653.463.0.6c56cb81-2e55-4e84-b45a-0efe148ec1a1/Microsoft.624F8B84B80_3.653.463.0_x64__8wekyb3d8bbwe.msixvc" },
                            {"战争机器5(PC)", "1e66a3e7-2f7b-461c-9f46-3ee0aec64b8c", "/8/82e2c767-56a2-4cff-9adf-bc901fd81e1a/1e66a3e7-2f7b-461c-9f46-3ee0aec64b8c/1.1.967.0.4e71a28b-d845-42e5-86bf-36afdd5eb82f/Microsoft.HalifaxBaseGame_1.1.967.0_x64__8wekyb3d8bbwe.msixvc"}
                        };
                        for (int i = 0; i <= games.GetLength(0) - 1; i++)
                        {
                            string? url = null;
                            if (XboxGameDownload.dicXboxGame.TryGetValue(games[i, 1], out XboxGameDownload.Products? XboxGame))
                            {
                                if (XboxGame.Url != null && XboxGame.Version > new Version(Regex.Match(games[i, 2], @"(\d+\.\d+\.\d+\.\d+)").Value))
                                {
                                    url = XboxGame.Url;
                                    string hosts = Regex.Match(url, @"(?<=://)[a-zA-Z\.0-9]+(?=\/)").Value;
                                    url = hosts switch
                                    {
                                        "xvcf1.xboxlive.com" => url.Replace("xvcf1.xboxlive.com", "assets1.xboxlive.cn"),
                                        "xvcf2.xboxlive.com" => url.Replace("xvcf2.xboxlive.com", "assets2.xboxlive.cn"),
                                        _ => url.Replace(".xboxlive.com", ".xboxlive.cn"),
                                    };
                                }
                            }
                            if (string.IsNullOrEmpty(url)) url = "http://assets1.xboxlive.cn" + games[i, 2];
                            LinkLabel lb = new()
                            {
                                Tag = url,
                                Text = games[i, 0],
                                AutoSize = true,
                                Parent = this.flpTestUrl,
                                LinkColor = Color.Green
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        }
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "游戏下载主域名",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                    }
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1b5a4a08-06f0-49d6-b25f-d7322c11f3c8/372e2966-b158-4488-8bc8-15ef23db1379/1.5.0.1018.88cd7a5d-f56a-40c7-afd8-85cd4940b891/ACUEU771E1BF7_1.5.0.1018_x64__b6krnev7r9sf8",
                            Text = "刺客信条: 大革命",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1d6640d3-3441-42bd-bffd-953d7d09ff5c/26213de4-885d-4eaa-a433-ed5157116507/1.2.1.0.89417ea8-51b5-408c-9283-60c181763a39/Microsoft.Max_1.2.1.0_neutral__ph1m9x8skttmg",
                            Text = "麦克斯：兄弟魔咒",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/77d0d59a-34b7-4482-a1c7-c0abbed17de2/db7a9163-9c5e-43a8-b8bf-fe0208149792/1.0.0.3.65565c9c-8a1e-438a-b714-2d9965f0485b/ChildOfLight_1.0.0.3_x64__b6krnev7r9sf8",
                            Text = "光之子",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/1c4b6e60-b2e3-420c-a8a8-540fb14c9286/57f7a51d-e6c2-42b2-967b-6f075e1923a7/1.0.0.5.acd29c4f-6d78-41c8-a705-90de47b8273b/SHPUPWW446612E0_1.0.0.5_x64__zjr0dfhgjwvde",
                            Text = "型可塑",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "XboxOne部分老游戏下载域名",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        ToolTip toolTip1 = new()
                        {
                            AutoPopDelay = 30000,
                            IsBalloon = true
                        };
                        toolTip1.SetToolTip(lbTip, "PC、主机新游戏都不再使用此域名。\n下载慢可以勾选“自动优选 Akamai IP”，使用国外CDN服务器。");
                    }
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "986a47b3-0085-4c0c-b3b3-3b806f969b00|MsixBundle|9MV0B5HZVK9Z",
                            Text = "Xbox app(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "64293252-5926-453c-9494-2d4021f1c78d|MsixBundle|9WZDNCRFJBMP",
                            Text = "微软商店(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "e0229546-200d-4c66-a693-df9bf799635f|EAppxBundle|9PNQKHFLD2WQ",
                            Text = "极限竞速: 地平线4(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "4828c82e-7fe6-4d95-9572-20bbe9721c86|EAppx|9NBLGGH4PBBM",
                            Text = "战争机器4(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        Label lbTip = new()
                        {
                            ForeColor = Color.Red,
                            Text = "应用和部分PC游戏",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        ToolTip toolTip1 = new()
                        {
                            AutoPopDelay = 30000,
                            IsBalloon = true
                        };
                        toolTip1.SetToolTip(lbTip, "Xbox app 提示 “此游戏不支持安装到特定文件夹。它将与其他 Windows 应用一起安装。”，\n以上游戏都是使用 tlu.dl.delivery.mp.microsoft.com 应用域名下载。");
                    }
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/networktest/get_192m",
                            Text = "PSN测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/gst/prod/00/PPSA04478_00/app/pkg/26/f_f2e4ff2bc3be11cb844dfe2a7ff8df357d7930152fb5984294a794823ec7472b/EP1464-PPSA04478_00-XXXXXXXXXXXXXXXX_0.pkg",
                            Text = "糖豆人(PS5)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://gs2.ww.prod.dl.playstation.net/gs2/appkgo/prod/CUSA03962_00/4/f_526a2fab32d369a8ca6298b59686bf823fa9edfe95acb85bc140c27f810842ce/f/UP0102-CUSA03962_00-BH70000000000001_0.pkg",
                            Text = "生化危机7(PS4)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://zeus.dl.playstation.net/cdn/UP1004/NPUB31154_00/eISFknCNDxqSsVVywSenkJdhzOIfZjrqKHcuGBHEGvUxQJksdPvRNYbIyWcxFsvH.pkg",
                            Text = "侠盗猎车手5(PS3)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb5 = new()
                        {
                            Tag = "http://ares.dl.playstation.net/cdn/JP0102/PCSG00350_00/fMBmIgPfrBTVSZCRQFevSzxaPyzFWOuorSKrvdIjDIJwmaGLjpTmRgzLLTJfASFYZMqEpwSknlWocYelXNHMkzXvpbbvtCSymAwWF.pkg",
                            Text = "怪物猎人: 边境G(PSV)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb5.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                    }
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt",
                            Text = "Xbox测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/networktest/get_192m",
                            Text = "PSN测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://ctest-dl-lp1.cdn.nintendo.net/30m",
                            Text = "Switch测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://origin-a.akamaihd.net/Origin-Client-Download/origin/live/OriginThinSetup.exe",
                            Text = "Origin(EA)",
                            AutoSize = true,
                            Parent = this.flpTestUrl,
                            LinkColor = Color.Green
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        if (host == "Akamai" || host == "AkamaiV2")
                        {
                            LinkLabel lb = new()
                            {
                                Name = "UploadBetterAkamaiIp",
                                Text = "上传更新优选IP",
                                AutoSize = true,
                                Parent = this.flpTestUrl,
                                LinkColor = Color.Red,
                                Enabled = false
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.Link_UploadBetterAkamaiIp);
                        }
                        else if (host == "AkamaiV6")
                        {
                            LinkLabel lb = new()
                            {
                                Tag = "https://www.test-ipv6.com/",
                                Text = "IPv6 连接测试",
                                AutoSize = true,
                                Parent = this.flpTestUrl,
                                LinkColor = Color.Red
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.Link_LinkClicked);
                        }
                    }
                    break;
            }
        }

        private void GetAppUrl(string wuCategoryId, string extension, CancellationToken? cts = null)
        {
            SetTextBox(tbDlUrl, "正在获取下载链接，请稍候...");
            string? url = null;
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload", cts);
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                XboxPackage.App? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Code != null && json.Code == "200")
                {
                    url = json.Data?.Where(x => (x.Name ?? string.Empty).ToLower().EndsWith("." + extension)).Select(x => x.Url).FirstOrDefault();
                }
            }
            this.Invoke(new Action(() =>
            {
                tbDlUrl.Text = url;
            }));
        }

        private void LinkTestUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is not LinkLabel link) return;
            string? url = link.Tag.ToString();
            if (url == null) return;
            if (Regex.IsMatch(url, @"^https?://"))
            {
                tbDlUrl.Text = url;
            }
            else if (Regex.IsMatch(url, @"^\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\|"))
            {
                string[] product = url.Split('|');
                string wuCategoryId = product[0];
                string extension = product[1].ToLower();
                ThreadPool.QueueUserWorkItem(delegate { GetAppUrl(wuCategoryId, extension); });
            }
        }

        private void CkbLocation_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            string network = cb.Text;
            bool isChecked = cb.Checked;
            foreach (DataGridViewRow dgvr in dgvIpList.Rows)
            {
                string? location = dgvr.Cells["Col_Location"].Value.ToString();
                if (location == null) continue;
                switch (network)
                {
                    case "电信":
                        if (location.Contains("电信"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "联通":
                        if (location.Contains("联通"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "移动":
                        if (location.Contains("移动"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "香港":
                        if (location.Contains("香港") || location.Contains("澳门"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "台湾":
                        if (location.Contains("台湾"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "日本":
                        if (location.Contains("日本"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "韩国":
                        if (location.Contains("韩国"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    case "新加坡":
                        if (location.Contains("新加坡"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                    default:
                        if (!Regex.IsMatch(location, "电信|联通|移动|香港|澳门|台湾|日本|韩国|新加坡"))
                            dgvr.Cells["Col_Check"].Value = isChecked;
                        break;
                }
            }
        }

        private void LinkFindIpArea_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (dgvIpList.Rows.Count == 0)
            {
                MessageBox.Show("请先导入IP。", "IP列表为空", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            FormIpLocation dialog = new();
            dialog.ShowDialog();
            string key = dialog.key;
            dialog.Dispose();
            if (!string.IsNullOrEmpty(key))
            {
                key = key.Replace("\\", "\\\\")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)")
                    .Replace("[", "\\[")
                    .Replace("]", "\\]")
                    .Replace("{", "\\{")
                    .Replace("}", "\\}")
                    .Replace(".", "\\.")
                    .Replace("+", "\\+")
                    .Replace("*", "\\*")
                    .Replace("?", "\\?")
                    .Replace("^", "\\^")
                    .Replace("$", "\\$")
                    .Replace("|", "\\|");
                key = ".*?" + Regex.Replace(key, @"\s+", ".*?") + ".*?";
                Regex reg = new(@key);
                int rowIndex = 0;
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Location"].Value == null) continue;
                    string? location = dgvr.Cells["Col_Location"].Value.ToString();
                    if (location != null && reg.IsMatch(location))
                    {
                        dgvr.Cells["Col_Check"].Value = true;
                        dgvIpList.Rows.Remove(dgvr);
                        dgvIpList.Rows.Insert(rowIndex, dgvr);
                        rowIndex++;
                    }
                }
                if (rowIndex >= 1) dgvIpList.Rows[0].Cells[0].Selected = true;
            }
        }

        private void LinkExportIP_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (dgvIpList.Rows.Count == 0) return;
            string? host = dgvIpList.Tag.ToString();
            if (host == "AkamaiV2") host = "Akamai";
            SaveFileDialog dlg = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Title = "导出数据",
                Filter = "文本文件(*.txt)|*.txt",
                FileName = "导出IP(" + host + ")"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new();
                sb.AppendLine(host);
                sb.AppendLine("");
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Speed"].Value != null && !string.IsNullOrEmpty(dgvr.Cells["Col_Speed"].Value.ToString()))
                        sb.AppendLine(dgvr.Cells["Col_IPAddress"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")\t" + dgvr.Cells["Col_TTL"].Value + "|" + dgvr.Cells["Col_RoundtripTime"].Value + "|" + dgvr.Cells["Col_Speed"].Value);
                    else
                        sb.AppendLine(dgvr.Cells["Col_IPAddress"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")");
                }
                File.WriteAllText(dlg.FileName, sb.ToString());
            }
        }

        private void LinkImportIPManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormImportIP dialog = new();
            dialog.ShowDialog();
            string host = dialog.host;
            DataTable dt = dialog.dt;
            dialog.Dispose();
            if (dt != null && dt.Rows.Count >= 1)
            {
                cbImportIP.SelectedIndex = 0;
                dgvIpList.Rows.Clear();
                flpTestUrl.Controls.Clear();
                tbDlUrl.Clear();
                dgvIpList.Tag = host;
                gbIPList.Text = "IP 列表 (" + host + ")";
                List<DataGridViewRow> list = new();
                foreach (DataRow dr in dt.Select("", "Location, IpLong"))
                {
                    string location = dr["Location"].ToString() ?? string.Empty;
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    if (location.Contains("电信"))
                        dgvr.Cells[0].Value = ckbChinaTelecom.Checked;
                    if (location.Contains("联通"))
                        dgvr.Cells[0].Value = ckbChinaUnicom.Checked;
                    if (location.Contains("移动"))
                        dgvr.Cells[0].Value = ckbChinaMobile.Checked;
                    if (location.Contains("香港") || location.Contains("澳门"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("台湾"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("日本"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("韩国"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("新加坡"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "电信|联通|移动|香港|澳门|台湾|日本|韩国|新加坡"))
                        dgvr.Cells[0].Value = ckbOther.Checked;
                    dgvr.Cells[1].Value = dr["IP"];
                    dgvr.Cells[2].Value = location;
                    list.Add(dgvr);
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                }
            }
        }

        private void TsmUseIPHosts_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? host = dgvIpList.Tag.ToString();
            string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(ip)) return;

            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                StringBuilder sb = new();
                string msg = string.Empty;
                switch (host)
                {
                    case "assets1.xboxlive.cn":
                    case "assets2.xboxlive.cn":
                    case "d1.xboxlive.cn":
                    case "d2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+(assets1|assets2|d1|d2)\.xboxlive\.cn\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " assets1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " assets2.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d2.xboxlive.cn # XboxDownload");
                        msg = "\nXbox、PC商店游戏下载可能会使用com域名，只写入cn域名加速不一定有效。";
                        break;
                    case "dlassets.xboxlive.cn":
                    case "dlassets2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+(dlassets2?)\.xboxlive\.cn\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " dlassets.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn # XboxDownload");
                        msg = "\nXbox、PC商店游戏下载可能会使用com域名，只写入cn域名加速不一定有效。";
                        break;
                    case "dl.delivery.mp.microsoft.com":
                    case "tlu.dl.delivery.mp.microsoft.com":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+((tlu\.)?dl\.delivery\.mp\.microsoft\.com)\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com # XboxDownload");
                        break;
                    case "gst.prod.dl.playstation.net":
                    case "gs2.ww.prod.dl.playstation.net":
                    case "zeus.dl.playstation.net":
                    case "ares.dl.playstation.net":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+\.dl\.playstation\.net\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " zeus.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " ares.dl.playstation.net # XboxDownload");
                        break;
                    case "Akamai":
                    case "AkamaiV2":
                    case "AkamaiV6":
                    case "atum.hac.lp1.d4c.nintendo.net":
                    case "origin-a.akamaihd.net":
                    case "blzddist1-a.akamaihd.net":
                    case "epicgames-download1.akamaized.net":
                    case "uplaypc-s-ubisoft.cdn.ubi.com":
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+(\.xboxlive\.com|\.delivery\.mp\.microsoft\.com|\.dl\.playstation\.net|\.nintendo\.net|\.cdn\.ea\.com|\.akamaihd\.net|\.akamaized\.net|\.ubi\.net)\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " xvcf1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " xvcf2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " assets1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " assets2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " d1.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " d2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dlassets.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dlassets2.xboxlive.com # XboxDownload");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " zeus.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " ares.dl.playstation.net # XboxDownload");
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " bugyo.hac.lp1.eshop.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net # XboxDownload");
                            sb.AppendLine(ip + " origin-a.akamaihd.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com # XboxDownload");
                            sb.AppendLine(ip + " blzddist1-a.akamaihd.net # XboxDownload");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net # XboxDownload");
                            sb.AppendLine(ip + " uplaypc-s-ubisoft.cdn.ubi.com # XboxDownload");
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\n战网、Epic、育碧 需要使用监听方式跳转。";
                        }
                        else
                        {
                            sb.AppendLine(ip + " origin-a.akamaihd.net # XboxDownload");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com # XboxDownload");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net # XboxDownload");
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\nNS主机、战网客户端、育碧客户端 不支持使用 IPv6。";
                        }
                        break;
                    case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sHosts = Regex.Replace(sHosts, @"[^\s]+\s+[^\s]+\.cdn\.ubionline\.com\.cn\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                            sb.AppendLine(ip + " " + host + " # XboxDownload");
                        }
                        else
                        {
                            MessageBox.Show("育碧客户端不支持使用IPv6，请使用监听方式。", "客户端不支持", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        break;
                    default:
                        sHosts = Regex.Replace(sHosts, @"[^\s]+\s+" + host + @"\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " " + host + " # XboxDownload");
                        break;
                }
                sHosts = sHosts.Trim() + "\r\n" + sb.ToString();
                FileSecurity fSecurity = fi.GetAccessControl();
                fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    fi.Attributes = FileAttributes.Normal;
                using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    if (!string.IsNullOrEmpty(sHosts.Trim()))
                    {
                        using StreamWriter sw = new(fs);
                        sw.WriteLine(sHosts.Trim());
                    }
                }
                fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                MessageBox.Show("系统Hosts文件写入成功，以下规则已写入系统Hosts文件\n\n" + sb.ToString() + msg, "写入系统Hosts文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入系统Hosts文件失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TsmExportRule_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            string? host = dgvIpList.Tag.ToString();
            string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(ip)) return;

            StringBuilder sb = new();
            if (sender is not ToolStripMenuItem tsmi) return;
            string msg = string.Empty;
            switch (host)
            {
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/assets1.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/assets2.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/d1.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/d2.xboxlive.cn/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " assets1.xboxlive.cn");
                        sb.AppendLine(ip + " assets2.xboxlive.cn");
                        sb.AppendLine(ip + " d1.xboxlive.cn");
                        sb.AppendLine(ip + " d2.xboxlive.cn");
                    }
                    msg = "\nXbox、PC商店游戏下载可能会使用com域名，只写入cn域名加速不一定有效。";
                    break;
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/dlassets.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/dlassets2.xboxlive.cn/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " dlassets.xboxlive.cn");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn");
                    }
                    msg = "\nXbox、PC商店游戏下载可能会使用com域名，只写入cn域名加速不一定有效。";
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/tlu.dl.delivery.mp.microsoft.com/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com");
                    }
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/gst.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/gs2.ww.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/zeus.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/ares.dl.playstation.net/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net");
                        sb.AppendLine(ip + " zeus.dl.playstation.net");
                        sb.AppendLine(ip + " ares.dl.playstation.net");
                    }
                    break;
                case "Akamai":
                case "AkamaiV2":
                case "AkamaiV6":
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "epicgames-download1.akamaized.net":
                case "uplaypc-s-ubisoft.cdn.ubi.com":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("# Xbox 国际域名");
                        sb.AppendLine("address=/xvcf1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/xvcf2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/assets1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/assets2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/d1.xboxlive.com/" + ip);
                        sb.AppendLine("address=/d2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dlassets.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dlassets2.xboxlive.com/" + ip);
                        sb.AppendLine("address=/dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/tlu.dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine();
                        sb.AppendLine("# PlayStation");
                        sb.AppendLine("address=/gst.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/gs2.ww.prod.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/zeus.dl.playstation.net/" + ip);
                        sb.AppendLine("address=/ares.dl.playstation.net/" + ip);
                        sb.AppendLine();
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine("# Nintendo Switch");
                            sb.AppendLine("address=/atum.hac.lp1.d4c.nintendo.net/" + ip);
                            sb.AppendLine("address=/bugyo.hac.lp1.eshop.nintendo.net/" + ip);
                            sb.AppendLine("address=/ctest-ul-lp1.cdn.nintendo.net/" + ip);
                            sb.AppendLine("address=/ctest-dl-lp1.cdn.nintendo.net/" + ip);
                            sb.AppendLine("address=/atum-eda.hac.lp1.d4c.nintendo.net/0.0.0.0");
                            sb.AppendLine();
                            sb.AppendLine("# EA、战网、Epic、育碧");
                            sb.AppendLine("address=/origin-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/ssl-lvlt.cdn.ea.com/0.0.0.0");
                            sb.AppendLine("address=/blzddist1-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/epicgames-download1.akamaized.net/" + ip);
                            sb.AppendLine("address=/uplaypc-s-ubisoft.cdn.ubi.com/" + ip);
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\n战网、Epic、育碧 需要使用监听方式跳转。";
                        }
                        else
                        {
                            sb.AppendLine("# EA、Epic");
                            sb.AppendLine("address=/origin-a.akamaihd.net/" + ip);
                            sb.AppendLine("address=/ssl-lvlt.cdn.ea.com/0.0.0.0");
                            sb.AppendLine("address=/epicgames-download1.akamaized.net/" + ip);
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\nNS主机、战网客户端、育碧客户端 不支持使用 IPv6。";
                        }
                    }
                    else
                    {
                        sb.AppendLine("# Xbox 国际域名");
                        sb.AppendLine(ip + " xvcf1.xboxlive.com");
                        sb.AppendLine(ip + " xvcf2.xboxlive.com");
                        sb.AppendLine(ip + " assets1.xboxlive.com");
                        sb.AppendLine(ip + " assets2.xboxlive.com");
                        sb.AppendLine(ip + " d1.xboxlive.com");
                        sb.AppendLine(ip + " d2.xboxlive.com");
                        sb.AppendLine(ip + " dlassets.xboxlive.com");
                        sb.AppendLine(ip + " dlassets2.xboxlive.com");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com");
                        sb.AppendLine();
                        sb.AppendLine("# PlayStation");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net");
                        sb.AppendLine(ip + " zeus.dl.playstation.net");
                        sb.AppendLine(ip + " ares.dl.playstation.net");
                        sb.AppendLine();
                        if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                        {
                            sb.AppendLine("# Nintendo Switch");
                            sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net");
                            sb.AppendLine(ip + " bugyo.hac.lp1.eshop.nintendo.net");
                            sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net");
                            sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net");
                            sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net");
                            sb.AppendLine();
                            sb.AppendLine("# EA、战网、Epic、育碧");
                            sb.AppendLine(ip + " origin-a.akamaihd.net");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                            sb.AppendLine(ip + " blzddist1-a.akamaihd.net");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            sb.AppendLine(ip + " uplaypc-s-ubisoft.cdn.ubi.com");
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\n战网、Epic、育碧 需要使用监听方式跳转。";
                        }
                        else
                        {
                            sb.AppendLine("# EA、战网、Epic、育碧");
                            sb.AppendLine(ip + " origin-a.akamaihd.net");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                            sb.AppendLine(ip + " epicgames-download1.akamaized.net");
                            msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\nNS主机、战网客户端、育碧客户端 不支持使用 IPv6。";
                        }
                    }
                    break;
                case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                    if (Regex.IsMatch(ip, @"\d+\.\d+\.\d+\.\d+"))
                    {
                        sb.AppendLine("# 育碧");
                        if (tsmi.Name == "tsmDNSmasp")
                            sb.AppendLine("address=/" + host + "/" + ip);
                        else
                            sb.AppendLine(ip + " " + host);
                    }
                    else
                    {
                        MessageBox.Show("育碧客户端不支持使用IPv6，请使用监听方式。", "客户端不支持", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    break;
                default:
                    if (tsmi.Name == "tsmDNSmasp")
                        sb.AppendLine("address=/" + host + "/" + ip);
                    else
                        sb.AppendLine(ip + " " + host);
                    break;
            }
            Clipboard.SetDataObject(sb.ToString());
            MessageBox.Show("以下规则已复制到剪贴板\n\n" + sb.ToString() + msg, "导出规则", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TsmSpeedTest_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            List<DataGridViewRow> ls = new()
            {
                dgvIpList.SelectedRows[0]
            };
            dgvIpList.ClearSelection();
            if (string.IsNullOrEmpty(tbDlUrl.Text) && flpTestUrl.Controls.Count >= 1)
            {
                LinkLabel? link = flpTestUrl.Controls[0] as LinkLabel;
                tbDlUrl.Text = link?.Tag.ToString();
            }
            foreach (Control control in this.panelSpeedTest.Controls)
            {
                if (control is TextBox || control is CheckBox || control is Button || control is ComboBox || control is LinkLabel || control is FlowLayoutPanel)
                    control.Enabled = false;
            }
            Col_IPAddress.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
            ThreadPool.QueueUserWorkItem(delegate { SpeedTest(ls); });
        }

        private void TsmSpeedTestLog_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            if (dgvr.Tag != null) MessageBox.Show(dgvr.Tag.ToString(), "Request Headers", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private async void Link_UploadBetterAkamaiIp(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel linkLabel = (LinkLabel)sender;
            string text = Regex.Replace(linkLabel.Text, @"\(.+\)", "").Trim();
            linkLabel.Text = text;
            JsonArray ja = new();
            foreach (DataGridViewRow dgvr in dgvIpList.Rows)
            {
                if (dgvr.Cells["Col_Speed"].Value == null) continue;
                if (double.TryParse(dgvr.Cells["Col_Speed"].Value.ToString(), out double speed) && speed >= 10)
                {
                    string? _ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    string? _location = dgvr.Cells["Col_Location"].Value.ToString();
                    if (!string.IsNullOrEmpty(_ip) && !string.IsNullOrEmpty(_location))
                    {
                        ja.Add(new JsonObject()
                        {
                            ["ip"] = _ip,
                            ["location"] = _location,
                            ["speed"] = speed
                        });
                    }
                }
            }
            if (ja.Count >= 1 && MessageBox.Show("此功能针对中国大陆地区用户使用，非中国大陆地区、开通国际精品网、专线 或者使用 加速器、代理软件 测速的用户请不要上传，谢谢合作！\n\n以下 IP （下载速度超过10MB/s）将会上传到 “Akamai 优选 IP” 列表，是否继续？\n" + string.Join("\n", ja.Select(a => a!["ip"] + "\t" + a!["location"] + "\t" + a!["speed"]).ToArray()), "上传更新 Akamai 优选 IP", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                linkLabel.Text = text + " (检查位置)";
                bool bCheckLocation = false;
                using (HttpResponseMessage? response1 = await ClassWeb.HttpResponseMessageAsync("https://qifu-api.baidubce.com/ip/local/geo/v1/district", "GET", null, null, null, 6000))
                {
                    if (response1 != null && response1.IsSuccessStatusCode)
                    {
                        JsonDocument? jsonDocument = null;
                        try
                        {
                            jsonDocument = JsonDocument.Parse(response1.Content.ReadAsStringAsync().Result);
                        }
                        catch { }
                        if (jsonDocument != null)
                        {
                            JsonElement root = jsonDocument.RootElement;
                            if (root.TryGetProperty("data", out JsonElement je))
                            {
                                string country = je.TryGetProperty("country", out JsonElement jeCountry) ? jeCountry.ToString().Trim() : "";
                                string prov = je.TryGetProperty("prov", out JsonElement jeProv) ? jeProv.ToString().Trim() : "";
                                bCheckLocation = country == "中国" && !Regex.IsMatch(prov, @"香港|澳门|台湾");
                            }
                        }
                    }
                }
                if (bCheckLocation)
                {
                    linkLabel.Text = text + " (正在上传)";
                    using HttpResponseMessage? response2 = await ClassWeb.HttpResponseMessageAsync(UpdateFile.website + "/Akamai/Better", "POST", ja.ToString(), "application/json", null, 6000, "XboxDownload");
                    if (response2 != null && response2.IsSuccessStatusCode)
                    {
                        string ipFilepath = Path.Combine(resourceDirectory, "IP.AkamaiV2.txt");
                        if (File.Exists(ipFilepath)) File.SetLastWriteTime(ipFilepath, DateTime.Now.AddDays(-7));
                        linkLabel.Text = text + " (上传成功)";
                    }
                    else
                        linkLabel.Text = text + " (上传失败)";
                }
                else
                {
                    linkLabel.Text = text + " (非中国大陆地区)";
                }
            }
        }

        private void LinkHostsClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string sHosts;
                FileInfo fi = new(Environment.SystemDirectory + "\\drivers\\etc\\hosts");
                using (FileStream fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    using StreamReader sr = new(fs);
                    sHosts = sr.ReadToEnd();
                }
                StringBuilder sb1 = new(), sb2 = new();
                string header = string.Empty;
                Match result = Regex.Match(sHosts, @"# Added by (XboxDownload|Xbox下载助手)\r\n(.*\r\n)*# End of (XboxDownload|Xbox下载助手)\r\n");
                if (result.Success)
                {
                    header = result.Groups[0].Value;
                    sHosts = sHosts.Replace(header, "");
                    if (!bServiceFlag)
                    {
                        sb2.Append(header);
                        header = string.Empty;
                    }
                }
                foreach (string str in sHosts.Split('\n'))
                {
                    string tmp = str.Trim();
                    if (tmp.StartsWith('#') || string.IsNullOrEmpty(tmp))
                        sb1.AppendLine(tmp);
                    else
                        sb2.AppendLine(tmp);
                }
                if (sb2.Length == 0)
                {
                    MessageBox.Show("Hosts文件没有写入任何规则，无需清除。", "清除系统Hosts文件", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                else if (MessageBox.Show("是否确认清除以下写入规则？\n\n" + sb2.ToString(), "清除系统Hosts文件", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    FileSecurity fSecurity = fi.GetAccessControl();
                    fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        fi.Attributes = FileAttributes.Normal;
                    using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        string hosts = string.Empty;
                        if (bServiceFlag && !Properties.Settings.Default.SetDns) hosts = (header + sb1.ToString()).Trim();
                        else hosts = sb1.ToString().Trim();
                        using StreamWriter sw = new(fs);
                        if (hosts.Length > 0)
                            sw.WriteLine(hosts);
                        else
                            sw.Write(hosts);
                    }
                    fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fSecurity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("清除系统Hosts文件失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkHostsEdit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sHostsPath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
            if (File.Exists(sHostsPath))
                Process.Start("notepad.exe", sHostsPath);
            else
                MessageBox.Show("Hosts 文件不存在", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void ButSpeedTest_Click(object sender, EventArgs? e)
        {
            if (ctsSpeedTest == null)
            {
                if (dgvIpList.Rows.Count == 0)
                {
                    MessageBox.Show("请先导入IP。", "IP列表为空", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                List<DataGridViewRow> ls = new();
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (Convert.ToBoolean(dgvr.Cells["Col_Check"].Value))
                    {
                        ls.Add(dgvr);
                    }
                }
                if (ls.Count == 0)
                {
                    MessageBox.Show("请勾选需要测试IP。", "选择测试IP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int rowIndex = 0;
                foreach (DataGridViewRow dgvr in ls.ToArray())
                {
                    dgvIpList.Rows.Remove(dgvr);
                    dgvIpList.Rows.Insert(rowIndex, dgvr);
                    rowIndex++;
                }
                dgvIpList.Rows[0].Cells[0].Selected = true;

                butSpeedTest.Text = "停止测速";
                foreach (Control control in this.panelSpeedTest.Controls)
                {
                    switch (control.Name)
                    {
                        case "linkHostsClear":
                        case "linkHostsEdit":
                        case "butSpeedTest":
                            break;
                        default:
                            control.Enabled = false;
                            break;
                    }
                }
                Col_IPAddress.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
                Col_Check.ReadOnly = true;
                var timeout = cbSpeedTestTimeOut.SelectedIndex switch
                {
                    1 => 45000,
                    2 => 60000,
                    _ => 30000,
                };
                Thread thread = new(new ThreadStart(() =>
                {
                    SpeedTest(ls, timeout);
                }))
                {
                    IsBackground = true
                };
                thread.Start();
            }
            else
            {
                butSpeedTest.Enabled = false;
                ctsSpeedTest.Cancel();
            }
            dgvIpList.ClearSelection();
        }

        CancellationTokenSource? ctsSpeedTest = null;
        private void SpeedTest(List<DataGridViewRow> ls, int timeout = 30000)
        {
            ctsSpeedTest = new CancellationTokenSource();
            string url = tbDlUrl.Text.Trim();
            if (!Regex.IsMatch(tbDlUrl.Text, @"^https?://") && flpTestUrl.Controls.Count >= 1)
            {
                if (flpTestUrl.Controls[0] is LinkLabel link)
                {
                    url = link.Tag.ToString() ?? string.Empty;
                    if (Regex.IsMatch(url, @"^https?://"))
                    {
                        SetTextBox(tbDlUrl, url);
                    }
                    else if (Regex.IsMatch(url, @"^\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\|"))
                    {
                        string[] product = url.Split('|');
                        string wuCategoryId = product[0];
                        string extension = product[1].ToLower();
                        ls[0].Cells["Col_Speed"].Value = "获取下载链接";
                        GetAppUrl(wuCategoryId, extension, ctsSpeedTest.Token);
                        if (ctsSpeedTest.IsCancellationRequested)
                        {
                            ls[0].Cells["Col_Speed"].Value = null;
                        }
                        url = tbDlUrl.Text;
                        if (!Regex.IsMatch(url, @"^https?://"))
                        {
                            url = flpTestUrl.Controls[2].Tag.ToString() ?? string.Empty;
                            SetTextBox(tbDlUrl, url);
                        }
                    }
                }
            }
            Uri? uri = null;
            if (Regex.IsMatch(url, @"^https?://"))
            {
                try
                {
                    uri = new Uri(url);
                }
                catch { }
            }
            string? _tag = dgvIpList.Tag.ToString();
            if (uri != null)
            {
                int range = Regex.IsMatch(gbIPList.Text, @"Akamai") ? 31457250 : 52428799;  //国外IP测试下载30M，国内IP测试下载50M
                //if (Form1.debug) range = 1048575;     //1M

                string userAgent = uri.Host.EndsWith(".nintendo.net") ? "XboxDownload (Nintendo NX)" : "XboxDownload";
                Stopwatch sw = new();
                StringBuilder sb = new();
                sb.AppendLine("GET " + uri.PathAndQuery + " HTTP/1.1");
                sb.AppendLine("Host: " + uri.Host);
                sb.AppendLine("User-Agent: " + userAgent);
                sb.AppendLine("Range: bytes=0-" + range);
                sb.AppendLine();
                byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());
                using Ping p1 = new();
                foreach (DataGridViewRow dgvr in ls)
                {
                    if (ctsSpeedTest.IsCancellationRequested) break;
                    string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    if (string.IsNullOrEmpty(ip)) continue;
                    dgvr.Cells["Col_302"].Value = false;
                    dgvr.Cells["Col_TTL"].Value = null;
                    dgvr.Cells["Col_RoundtripTime"].Value = null;
                    dgvr.Cells["Col_Speed"].Value = "正在测试";
                    dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                    dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                    dgvr.Tag = null;
                    try
                    {
                        PingReply reply = p1.Send(ip);
                        if (reply.Status == IPStatus.Success)
                        {
                            dgvr.Cells["Col_TTL"].Value = reply.Options?.Ttl;
                            dgvr.Cells["Col_RoundtripTime"].Value = reply.RoundtripTime;
                        }
                    }
                    catch { }
                    sw.Restart();
                    SocketPackage socketPackage = uri.Scheme == "https" ? ClassWeb.TlsRequest(uri, buffer, ip, false, null, timeout, ctsSpeedTest) : ClassWeb.TcpRequest(uri, buffer, ip, false, null, timeout, ctsSpeedTest);
                    sw.Stop();
                    if (socketPackage.Headers.StartsWith("HTTP/1.1 302"))
                    {
                        dgvr.Cells["Col_302"].Value = true;
                        Match result = Regex.Match(socketPackage.Headers, @"Location: (.+)");
                        if (result.Success)
                        {
                            Uri uri2 = new(uri, result.Groups[1].Value);
                            dgvr.Tag = socketPackage.Headers + "===============临时性重定向(302)===============\n" + uri2.OriginalString + "\n\n";
                            sb.Clear();
                            sb.AppendLine("GET " + uri2.PathAndQuery + " HTTP/1.1");
                            sb.AppendLine("Host: " + uri2.Host);
                            sb.AppendLine("User-Agent: " + userAgent);
                            sb.AppendLine("Range: bytes=0-" + range);
                            sb.AppendLine();
                            byte[] buffer2 = Encoding.ASCII.GetBytes(sb.ToString());
                            sw.Restart();
                            socketPackage = uri2.Scheme == "https" ? ClassWeb.TlsRequest(uri2, buffer2, null, false, null, timeout, ctsSpeedTest) : ClassWeb.TcpRequest(uri2, buffer2, null, false, null, timeout, ctsSpeedTest);
                            sw.Stop();
                        }
                    }
                    dgvr.Tag += string.IsNullOrEmpty(socketPackage.Err) ? socketPackage.Headers : socketPackage.Err;
                    if (socketPackage.Headers.StartsWith("HTTP/1.1 206") && socketPackage.Buffer != null)
                    {
                        double speed = Math.Round((double)(socketPackage.Buffer.Length) / sw.ElapsedMilliseconds * 1000 / 1024 / 1024, 2, MidpointRounding.AwayFromZero);
                        dgvr.Cells["Col_Speed"].Value = speed;
                        dgvr.Tag += "下载：" + ClassMbr.ConvertBytes((ulong)socketPackage.Buffer.Length) + "，耗时：" + sw.ElapsedMilliseconds.ToString("N0") + " 毫秒，平均速度：" + speed + " MB/s";
                    }
                    else
                    {
                        dgvr.Cells["Col_Speed"].Value = (double)0;
                        dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Red;
                    }
                    socketPackage.Buffer = null;
                }
            }
            else
            {
                using Ping p1 = new();
                foreach (DataGridViewRow dgvr in ls)
                {
                    if (ctsSpeedTest.IsCancellationRequested) break;
                    string? ip = dgvr.Cells["Col_IPAddress"].Value.ToString();
                    if (string.IsNullOrEmpty(ip)) continue;
                    dgvr.Cells["Col_302"].Value = false;
                    dgvr.Cells["Col_TTL"].Value = null;
                    dgvr.Cells["Col_RoundtripTime"].Value = null;
                    dgvr.Cells["Col_Speed"].Value = "正在测试";
                    dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                    dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                    dgvr.Tag = null;
                    try
                    {
                        PingReply reply = p1.Send(ip);
                        if (reply.Status == IPStatus.Success)
                        {
                            dgvr.Cells["Col_TTL"].Value = reply.Options?.Ttl;
                            dgvr.Cells["Col_RoundtripTime"].Value = reply.RoundtripTime;
                        }
                    }
                    catch { }
                    dgvr.Cells["Col_Speed"].Value = null;
                }
            }
            GC.Collect();
            ctsSpeedTest = null;

            bool bUploadBetterAkamaiIpEnable = false;
            LinkLabel? linkUploadBetterAkamaiIp = null;
            if (_tag == "Akamai" || _tag == "AkamaiV2")
            {
                linkUploadBetterAkamaiIp = this.Controls.Find("UploadBetterAkamaiIp", true)[0] as LinkLabel;
                foreach (DataGridViewRow dgvr in dgvIpList.Rows)
                {
                    if (dgvr.Cells["Col_Speed"].Value == null) continue;
                    if (double.TryParse(dgvr.Cells["Col_Speed"].Value.ToString(), out double speed) && speed >= 10)
                    {
                        bUploadBetterAkamaiIpEnable = true;
                        break;
                    }
                }
            }

            this.Invoke(new Action(() =>
            {
                butSpeedTest.Text = "开始测速";
                foreach (Control control in this.panelSpeedTest.Controls)
                {
                    control.Enabled = true;
                }
                Col_IPAddress.SortMode = Col_Location.SortMode = Col_Speed.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = DataGridViewColumnSortMode.Automatic;
                Col_Check.ReadOnly = false;
                butSpeedTest.Enabled = true;
                if (linkUploadBetterAkamaiIp != null) linkUploadBetterAkamaiIp.Enabled = bUploadBetterAkamaiIpEnable;
            }));
        }
        #endregion

        #region 选项卡-域名
        private void DgvHosts_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["Col_Enable"].Value = true;
        }

        private void DgvHosts_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            dgvHosts.Rows[e.RowIndex].ErrorText = "";
            if (dgvHosts.Rows[e.RowIndex].IsNewRow) return;
            switch (dgvHosts.Columns[e.ColumnIndex].Name)
            {
                case "Col_IP":
                    if (!string.IsNullOrWhiteSpace(e.FormattedValue.ToString()))
                    {
                        if (!(IPAddress.TryParse(e.FormattedValue.ToString()?.Trim(), out _)))
                        {
                            e.Cancel = true;
                            dgvHosts.Rows[e.RowIndex].ErrorText = "不是有效IP地址";
                        }
                    }
                    break;
            }
        }

        private void DgvHosts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            switch (dgvHosts.Columns[e.ColumnIndex].Name)
            {
                case "Col_HostName":
                    dgvHosts.CurrentCell.Value = Regex.Replace((dgvHosts.CurrentCell.FormattedValue.ToString() ?? string.Empty).Trim().ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2");
                    break;
                case "Col_IP":
                    dgvHosts.CurrentCell.Value = dgvHosts.CurrentCell.FormattedValue.ToString()?.Trim();
                    break;
            }
        }

        private void DgvHosts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            DataGridViewRow dgvr = dgvHosts.Rows[e.RowIndex];
            string? hostName = dgvr.Cells["Col_HostName"].Value?.ToString();
            string? ip = dgvr.Cells["Col_IP"].Value?.ToString();
            if (!string.IsNullOrEmpty(hostName) && !string.IsNullOrEmpty(ip) && DnsListen.reHosts.IsMatch(hostName))
            {
                FormConnectTest dialog = new(hostName, ip);
                dialog.ShowDialog();
                dialog.Dispose();
            }
        }

        private void CbHosts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbHosts.SelectedIndex <= 0) return;
            if (cbHosts.Text == "Xbox360主机本地上传")
            {
                string[] hostNames = new string[] { "download.xbox.com", "download.xbox.com.edgesuite.net", "xbox-ecn102.vo.msecnd.net" };
                foreach (string hostName in hostNames)
                {
                    DataRow[] rows = dtHosts.Select("HostName='" + hostName + "'");
                    if (rows.Length >= 1)
                    {
                        rows[0]["Enable"] = true;
                        rows[0]["IP"] = Properties.Settings.Default.LocalIP;
                        rows[0]["Remark"] = "Xbox360主机下载域名";
                    }
                    else
                    {
                        DataRow dr = dtHosts.NewRow();
                        dr["Enable"] = true;
                        dr["HostName"] = hostName;
                        dr["IP"] = Properties.Settings.Default.LocalIP;
                        dr["Remark"] = "Xbox360主机下载域名";
                        dtHosts.Rows.Add(dr);
                    }
                    dgvHosts.ClearSelection();
                }
                DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == hostNames[0]).Select(r => r).FirstOrDefault();
                if (dgvr != null) dgvr.Cells["Col_IP"].Selected = true;
            }
        }

        private void ButHostSave_Click(object sender, EventArgs e)
        {
            dtHosts.AcceptChanges();
            string hostFilepath = Path.Combine(resourceDirectory, "Hosts.xml");
            if (dtHosts.Rows.Count >= 1)
            {
                if (!Directory.Exists(resourceDirectory)) Directory.CreateDirectory(resourceDirectory);
                dtHosts.WriteXml(hostFilepath);
            }
            else if (File.Exists(hostFilepath))
            {
                File.Delete(hostFilepath);
            }
            dgvHosts.ClearSelection();
            if (bServiceFlag)
            {
                DnsListen.UpdateHosts();
                if (ckbBetterAkamaiIP.Checked) ckbBetterAkamaiIP.Checked = false;
                else UpdateHosts(true);
                if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            }
        }

        private void ButHostReset_Click(object sender, EventArgs e)
        {
            dtHosts.RejectChanges();
            dgvHosts.ClearSelection();
        }

        private void LinkHostsAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormHost dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            string host = dialog.host, ip = dialog.ip;
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(ip))
            {
                DataRow[] rows = dtHosts.Select("HostName='" + host + "'");
                DataRow dr;
                if (rows.Length >= 1)
                {
                    dr = rows[0];
                }
                else
                {
                    dr = dtHosts.NewRow();
                    dr["Enable"] = true;
                    dr["HostName"] = host;
                    dtHosts.Rows.Add(dr);
                }
                dr["IP"] = ip.ToString();
                DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == host).Select(r => r).FirstOrDefault();
                if (dgvr != null) dgvr.Cells["Col_IP"].Selected = true;
            }
        }

        private void LinkHostsImport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormImportHosts dialog = new();
            dialog.ShowDialog();
            string hosts = dialog.hosts;
            dialog.Dispose();
            if (string.IsNullOrEmpty(hosts)) return;
            Regex regex = new(@"^(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})\s+(?<hostname>[^\s]+)(?<remark>.*)|^address=/(?<hostname>[^/]+)/(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})(?<remark>.*)$");
            string[] array = hosts.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in array)
            {
                Match result = regex.Match(str.Trim());
                if (result.Success)
                {
                    string hostname = result.Groups["hostname"].Value.Trim().ToLower();
                    string remark = result.Groups["remark"].Value.Trim();
                    if (remark.StartsWith('#'))
                        remark = remark[1..].Trim();
                    if (IPAddress.TryParse(result.Groups["ip"].Value, out IPAddress? ip) && DnsListen.reHosts.IsMatch(hostname))
                    {
                        DataRow[] rows = dtHosts.Select("HostName='" + hostname + "'");
                        DataRow dr;
                        if (rows.Length >= 1)
                        {
                            dr = rows[0];
                        }
                        else
                        {
                            dr = dtHosts.NewRow();
                            dr["Enable"] = true;
                            dr["HostName"] = hostname;
                            dtHosts.Rows.Add(dr);
                        }
                        dr["IP"] = ip.ToString();
                        if (!string.IsNullOrEmpty(remark)) dr["Remark"] = remark;
                    }
                }
            }
        }

        private void LinkHostClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = dgvHosts.Rows.Count - 2; i >= 0; i--)
            {
                dgvHosts.Rows.RemoveAt(i);
            }
        }
        #endregion

        #region 选项卡-CDN
        private void LinkCdnSpeedTest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (ctsSpeedTest != null)
            {
                MessageBox.Show("测速任务进行中，请稍候再试。", "测速", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl1.SelectedTab = tabSpeedTest;
                return;
            }
            List<string> lsIpTmp = new();
            foreach (string str in tbCdnAkamai.Text.Replace("，", ",").Split(','))
            {
                if (IPAddress.TryParse(str.Trim(), out IPAddress? address))
                {
                    string ip = address.ToString();
                    if (!lsIpTmp.Contains(ip))
                    {
                        lsIpTmp.Add(ip);
                    }
                }
            }
            if (lsIpTmp.Count >= 1)
            {
                string host = "Akamai";
                cbImportIP.SelectedIndex = 0;
                dgvIpList.Rows.Clear();
                flpTestUrl.Controls.Clear();
                tbDlUrl.Clear();
                dgvIpList.Tag = host;
                List<DataGridViewRow> list = new();
                gbIPList.Text = "IP 列表 (" + host + ")";
                foreach (string ip in lsIpTmp)
                {
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    dgvr.Cells[0].Value = true;
                    dgvr.Cells[1].Value = ip;
                    list.Add(dgvr);
                }
                if (list.Count >= 1)
                {
                    dgvIpList.Rows.AddRange(list.ToArray());
                    dgvIpList.ClearSelection();
                    AddTestUrl(host);
                    ButSpeedTest_Click(sender, null);
                    tabControl1.SelectedTab = tabSpeedTest;
                }
            }
            else
            {
                foreach (var item in cbImportIP.Items)
                {
                    string? str = item.ToString();
                    if (str != null && str.Contains("Akamai"))
                    {
                        cbImportIP.SelectedItem = item;
                        break;
                    }
                }
                tabControl1.SelectedTab = tabSpeedTest;
            }
        }

        private void ButCdnSave_Click(object sender, EventArgs e)
        {
            List<string> lsIpV4 = new(), lsIpV6 = new();
            foreach (string str in tbCdnAkamai.Text.Replace("，", ",").Split(','))
            {
                if (IPAddress.TryParse(str.Trim(), out IPAddress? ipAddress))
                {
                    string ip = ipAddress.ToString();
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork && !lsIpV4.Contains(ip))
                    {
                        lsIpV4.Add(ip);
                    }
                    else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 && !lsIpV6.Contains(ip))
                    {
                        lsIpV6.Add(ip);
                    }
                }
            }
            List<string> lsIp = lsIpV6.Union(lsIpV4).ToList<string>();
            tbCdnAkamai.Text = string.Join(", ", lsIp.ToArray());
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (string.IsNullOrWhiteSpace(tbHosts2Akamai.Text))
            {
                if (File.Exists(akamaiFilePath)) File.Delete(akamaiFilePath);
            }
            else
            {
                if (!Directory.Exists(resourceDirectory)) Directory.CreateDirectory(resourceDirectory);
                File.WriteAllText(akamaiFilePath, tbHosts2Akamai.Text.Trim() + "\r\n");
            }
            Properties.Settings.Default.IpsAkamai = tbCdnAkamai.Text;
            Properties.Settings.Default.Save();
            if (bServiceFlag)
            {
                DnsListen.UpdateHosts();
                if (ckbBetterAkamaiIP.Checked) ckbBetterAkamaiIP.Checked = false;
                if (Properties.Settings.Default.SetDns) DnsListen.FlushDns();
            }
        }

        private void ButCdnReset_Click(object sender, EventArgs e)
        {
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;
            string akamaiFilePath = Path.Combine(resourceDirectory, "Akamai.txt");
            if (File.Exists(akamaiFilePath))
            {
                tbHosts2Akamai.Text = File.ReadAllText(akamaiFilePath).Trim() + "\r\n";
            }
            else tbHosts2Akamai.Clear();
        }
        #endregion

        #region 选项卡-硬盘
        private void ButScan_Click(object sender, EventArgs e)
        {
            dgvDevice.Rows.Clear();
            butEnablePc.Enabled = butEnableXbox.Enabled = false;
            List<DataGridViewRow> list = new();

            ManagementClass mc = new("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (var mo in moc)
            {
                string? sDeviceID = mo.Properties["DeviceID"].Value.ToString();
                if (string.IsNullOrEmpty(sDeviceID)) continue;
                string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
                if (string.Equals(mbr[..892], ClassMbr.MBR))
                {
                    string mode = mbr[1020..];
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvDevice);
                    dgvr.Resizable = DataGridViewTriState.False;
                    dgvr.Tag = mode;
                    dgvr.Cells[0].Value = sDeviceID;
                    dgvr.Cells[1].Value = mo.Properties["Model"].Value;
                    dgvr.Cells[2].Value = mo.Properties["InterfaceType"].Value;
                    dgvr.Cells[3].Value = ClassMbr.ConvertBytes(Convert.ToUInt64(mo.Properties["Size"].Value));
                    if (mode == "99CC")
                        dgvr.Cells[4].Value = "Xbox 模式";
                    else if (mode == "55AA")
                        dgvr.Cells[4].Value = "PC 模式";
                    list.Add(dgvr);
                }
            }
            if (list.Count >= 1)
            {
                dgvDevice.Rows.AddRange(list.ToArray());
                dgvDevice.ClearSelection();
            }
        }

        private void DgvDevice_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;
            string? mode = dgvDevice.Rows[e.RowIndex].Tag?.ToString();
            if (mode == "99CC")
            {
                butEnablePc.Enabled = true;
                butEnableXbox.Enabled = false;
            }
            else if (mode == "55AA")
            {
                butEnablePc.Enabled = false;
                butEnableXbox.Enabled = true;
            }
        }

        private void ButEnablePc_Click(object sender, EventArgs e)
        {
            if (dgvDevice.SelectedRows.Count != 1) return;
            if (Environment.OSVersion.Version.Major < 10)
            {
                MessageBox.Show("低于Win10操作系统转换后会蓝屏，请升级操作系统。", "操作系统版本过低", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string? sDeviceID = dgvDevice.SelectedRows[0].Cells["Col_DeviceID"].Value.ToString();
            if (sDeviceID == null) return;
            string? mode = dgvDevice.SelectedRows[0].Tag?.ToString();
            string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
            if (mode == "99CC" && mbr[..892] == ClassMbr.MBR && mbr[1020..] == mode)
            {
                string newMBR = string.Concat(mbr.AsSpan(0, 1020), "55AA");
                if (ClassMbr.WriteMBR(sDeviceID, ClassMbr.HexToByte(newMBR)))
                {
                    dgvDevice.SelectedRows[0].Tag = "55AA";
                    dgvDevice.SelectedRows[0].Cells["Col_Mode"].Value = "PC 模式";
                    dgvDevice.ClearSelection();
                    butEnablePc.Enabled = false;
                    using (Process p = new())
                    {
                        p.StartInfo.FileName = "diskpart.exe";
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.Start();
                        p.StandardInput.WriteLine("rescan");
                        p.StandardInput.WriteLine("exit");
                        p.Close();
                    }
                    MessageBox.Show("成功转换PC模式。", "转换PC模式", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
        }

        private void ButEnableXbox_Click(object sender, EventArgs e)
        {
            if (dgvDevice.SelectedRows.Count != 1) return;
            string? sDeviceID = dgvDevice.SelectedRows[0].Cells["Col_DeviceID"].Value.ToString();
            if (sDeviceID == null) return;
            string? mode = dgvDevice.SelectedRows[0].Tag?.ToString();
            string mbr = ClassMbr.ByteToHex(ClassMbr.ReadMBR(sDeviceID));
            if (mode == "55AA" && mbr[..892] == ClassMbr.MBR && mbr[1020..] == mode)
            {
                string newMBR = string.Concat(mbr.AsSpan(0, 1020), "99CC");
                if (ClassMbr.WriteMBR(sDeviceID, ClassMbr.HexToByte(newMBR)))
                {
                    dgvDevice.SelectedRows[0].Tag = "99CC";
                    dgvDevice.SelectedRows[0].Cells["Col_Mode"].Value = "Xbox 模式";
                    dgvDevice.ClearSelection();
                    butEnableXbox.Enabled = false;
                    MessageBox.Show("成功转换Xbox模式。", "转换Xbox模式", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
        }

        private async void ButAnalyze_Click(object sender, EventArgs e)
        {
            string url = tbDownloadUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            if (!Regex.IsMatch(url, @"^https?://"))
            {
                if (!url.StartsWith('/')) url = "/" + url;
                url = "http://assets1.xboxlive.cn" + url;
                tbDownloadUrl.Text = url;
            }
            tbFilePath.Text = string.Empty;
            tbContentId.Text = tbProductID.Text = tbBuildID.Text = tbFileTimeCreated.Text = tbDriveSize.Text = tbPackageVersion.Text = string.Empty;
            butAnalyze.Enabled = butOpenFile.Enabled = linkCopyContentID.Enabled = linkRename.Enabled = linkProductID.Visible = false;
            Dictionary<string, string> headers = new() { { "Range", "bytes=0-4095" } };
            using HttpResponseMessage? response = await ClassWeb.HttpResponseMessageAsync(url, "GET", null, null, headers);
            if (response != null && response.IsSuccessStatusCode)
            {
                byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                XvcParse(buffer);
            }
            else
            {
                string msg = response != null ? "下载失败，错误信息：" + response.ReasonPhrase : "下载失败";
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            butAnalyze.Enabled = butOpenFile.Enabled = true;
        }

        private void ButOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Title = "Open an Xbox Package"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            string sFilePath = ofd.FileName;
            tbDownloadUrl.Text = "";
            tbFilePath.Text = sFilePath;
            tbContentId.Text = tbProductID.Text = tbBuildID.Text = tbFileTimeCreated.Text = tbDriveSize.Text = tbPackageVersion.Text = string.Empty;
            butAnalyze.Enabled = butOpenFile.Enabled = linkCopyContentID.Enabled = linkRename.Enabled = linkProductID.Visible = false;
            FileStream? fs = null;
            try
            {
                fs = new FileStream(sFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (fs != null)
            {
                int len = fs.Length >= 49152 ? 49152 : (int)fs.Length;
                byte[] bFileBuffer = new byte[len];
                fs.Read(bFileBuffer, 0, len);
                fs.Close();
                XvcParse(bFileBuffer);
            }
            butAnalyze.Enabled = butOpenFile.Enabled = true;
        }

        private void XvcParse(byte[] bFileBuffer)
        {
            if (bFileBuffer != null && bFileBuffer.Length >= 4096)
            {
                using MemoryStream ms = new(bFileBuffer);
                BinaryReader? br = null;
                try
                {
                    br = new BinaryReader(ms);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (br != null)
                {
                    br.BaseStream.Position = 0x200;
                    if (Encoding.Default.GetString(br.ReadBytes(0x8)) == "msft-xvd")
                    {
                        br.BaseStream.Position = 0x210;
                        tbFileTimeCreated.Text = DateTime.FromFileTime(BitConverter.ToInt64(br.ReadBytes(0x8), 0)).ToString();

                        br.BaseStream.Position = 0x218;
                        tbDriveSize.Text = ClassMbr.ConvertBytes(BitConverter.ToUInt64(br.ReadBytes(0x8), 0)).ToString();

                        br.BaseStream.Position = 0x220;
                        tbContentId.Text = (new Guid(br.ReadBytes(0x10))).ToString();

                        br.BaseStream.Position = 0x39C;
                        Byte[] bProductID = br.ReadBytes(0x10);
                        tbProductID.Text = (new Guid(bProductID)).ToString();
                        string productid = Encoding.Default.GetString(bProductID, 0, 7) + Encoding.Default.GetString(bProductID, 9, 5);
                        if (Regex.IsMatch(productid, @"^[a-zA-Z0-9]{12}$"))
                        {
                            linkProductID.Text = productid;
                            linkProductID.Visible = true;
                        }

                        br.BaseStream.Position = 0x3AC;
                        tbBuildID.Text = (new Guid(br.ReadBytes(0x10))).ToString();

                        br.BaseStream.Position = 0x3BC;
                        ushort PackageVersion1 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3BE;
                        ushort PackageVersion2 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3C0;
                        ushort PackageVersion3 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        br.BaseStream.Position = 0x3C2;
                        ushort PackageVersion4 = BitConverter.ToUInt16(br.ReadBytes(0x2), 0);
                        tbPackageVersion.Text = PackageVersion4 + "." + PackageVersion3 + "." + PackageVersion2 + "." + PackageVersion1;
                        linkCopyContentID.Enabled = true;
                        if (!string.IsNullOrEmpty(tbFilePath.Text))
                        {
                            string filename = Path.GetFileName(tbFilePath.Text).ToLowerInvariant();
                            if (filename != tbContentId.Text.ToLowerInvariant() && !filename.EndsWith(".msixvc")) linkRename.Enabled = true;
                        }
                    }
                    else
                    {
                        MessageBox.Show("不是有效文件", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    br.Close();
                }
            }
            else
            {
                MessageBox.Show("不是有效文件", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkCopyContentID_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sContentID = tbContentId.Text;
            if (!string.IsNullOrEmpty(sContentID))
            {
                Clipboard.SetDataObject(sContentID.ToUpper());
            }
        }

        private void LinkRename_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show(string.Format("是否确认重命名本地文件？\n\n修改前文件名：{0}\n修改后文件名：{1}", Path.GetFileName(tbFilePath.Text), tbContentId.Text.ToUpperInvariant()), "重命名本地文件", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                FileInfo fi = new(tbFilePath.Text);
                try
                {
                    fi.MoveTo(Path.GetDirectoryName(tbFilePath.Text) + "\\" + tbContentId.Text.ToUpperInvariant());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("重命名本地文件失败，错误信息：" + ex.Message, "重命名本地文件", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                linkRename.Enabled = false;
                tbContentId.Focus();
            }
        }

        private void LinkProductID_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + linkProductID.Text;
            if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            tabControl1.SelectedTab = tabStore;
        }
        #endregion

        #region 选项卡-商店
        private void TbGameUrl_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (butGame.Enabled)
                {
                    ButGame_Click(sender, EventArgs.Empty);
                    e.Handled = true;
                }
            }
        }

        private void ButGame_Click(object sender, EventArgs e)
        {
            string url = tbGameUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            Market market = (Market)cbGameMarket.SelectedItem;
            string language = market.language;
            string pat =
                    @"^https?://www\.xbox\.com(/[^/]*)?/games/store/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com(/[^/]*)?/p/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com/store/productId/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://apps\.microsoft\.com(/store)?/detail(/[^/]+)?/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"productid=(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^(?<productId>[a-zA-Z0-9]{12})$";
            Match result = Regex.Match(url, pat, RegexOptions.IgnoreCase);
            if (result.Success)
            {
                pbGame.Image = pbGame.InitialImage;
                tbGameTitle.Clear();
                tbGameDeveloperName.Clear();
                tbGameCategory.Clear();
                tbGameOriginalReleaseDate.Clear();
                cbGameBundled.Items.Clear();
                tbGamePrice.Clear();
                tbGameDescription.Clear();
                tbGameLanguages.Clear();
                lvGame.Items.Clear();
                butGame.Enabled = false;
                linkCompare.Enabled = false;
                linkGameWebsite.Enabled = false;
                this.cbGameBundled.SelectedIndexChanged -= new EventHandler(this.CbGameBundled_SelectedIndexChanged);
                string productId = result.Groups["productId"].Value.ToUpperInvariant();
                url = "https://www.microsoft.com/" + language + "/p/_/" + productId;
                linkGameWebsite.Links[0].LinkData = url;
                ThreadPool.QueueUserWorkItem(delegate { XboxStore(market, productId); });
            }
            else
            {
                MessageBox.Show("无效 URL/ProductId", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TbGameSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Down || e.KeyValue == (int)Keys.Up)
            {
                lvGameSearch.Focus();
                if (lvGameSearch.Items.Count >= 1 && lvGameSearch.SelectedItems.Count == 0)
                {
                    lvGameSearch.Items[0].Selected = true;
                }
            }
        }

        private void TbGameSearch_Leave(object sender, EventArgs e)
        {
            if (lvGameSearch.Focused == false)
            {
                lvGameSearch.Visible = false;
            }
        }

        private void TbGameSearch_Enter(object sender, EventArgs e)
        {
            if (lvGameSearch.Items.Count >= 1)
            {
                lvGameSearch.Visible = true;
            }
        }

        string query = string.Empty;
        private void TbGameSearch_TextChanged(object sender, EventArgs e)
        {
            string query = tbGameSearch.Text.Trim();
            if (this.query == query) return;
            lvGameSearch.Items.Clear();
            lvGameSearch.Visible = false;
            this.query = query;
            if (string.IsNullOrEmpty(query)) return;
            ThreadPool.QueueUserWorkItem(delegate { GameSearch(query); });
        }

        private void LvGameSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Enter)
            {
                ListViewItem item = lvGameSearch.SelectedItems[0];
                string productId = item.SubItems[1].Text;
                lvGameSearch.Visible = false;
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + productId;
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void LvGameSearch_DoubleClick(object sender, EventArgs e)
        {
            if (lvGameSearch.SelectedItems.Count >= 1)
            {
                ListViewItem item = lvGameSearch.SelectedItems[0];
                string productId = item.SubItems[1].Text;
                lvGameSearch.Visible = false;
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + productId;
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void LvGameSearch_Leave(object sender, EventArgs e)
        {
            if (tbGameSearch.Focused == false)
            {
                lvGameSearch.Visible = false;
            }
        }

        private void GameSearch(string query)
        {
            Thread.Sleep(300);
            if (this.query != query) return;
            string language = ClassWeb.language;
            if (language == "zh-CN") language = "zh-TW";
            string url = "https://www.microsoft.com/msstoreapiprod/api/autosuggest?market=" + language + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Microsoft-Terms,Iris-Products,xSearch-Products&filter=+ClientType:StoreWeb&counts=5,1,5&query=" + ClassWeb.UrlEncode(query);
            string html = ClassWeb.HttpResponseContent(url);
            if (this.query != query) return;
            List<ListViewItem> ls = new();
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                ClassGame.Search? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<ClassGame.Search>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.ResultSets != null && json.ResultSets.Count >= 1)
                {
                    foreach (var resultSets in json.ResultSets)
                    {
                        if (resultSets.Suggests == null) continue;
                        foreach (var suggest in resultSets.Suggests)
                        {
                            if (suggest.Metas == null) continue;
                            var BigCatalogId = Array.FindAll(suggest.Metas.ToArray(), a => a.Key == "BigCatalogId");
                            if (BigCatalogId.Length == 1)
                            {
                                string? title = suggest.Title;
                                string? productId = BigCatalogId[0].Value;
                                if (title != null && productId != null)
                                {
                                    ListViewItem item = new(new string[] { title, productId });
                                    ls.Add(item);
                                    if (imageList1.Images.ContainsKey(productId))
                                    {
                                        item.ImageKey = productId;
                                    }
                                    else if (!string.IsNullOrEmpty(suggest.ImageUrl))
                                    {
                                        string imgUrl = suggest.ImageUrl.StartsWith("//") ? "http:" + suggest.ImageUrl : suggest.ImageUrl;
                                        imgUrl = Regex.Replace(imgUrl, @"\?.+", "") + "?w=25&h=25";
                                        Task.Run(() =>
                                        {
                                            using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(imgUrl);
                                            if (response != null && response.IsSuccessStatusCode)
                                            {
                                                using Stream stream = response.Content.ReadAsStreamAsync().Result;
                                                Image img = Image.FromStream(stream);
                                                imageList1.Images.Add(productId, img);
                                                this.Invoke(new Action(() => { item.ImageKey = productId; }));
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.Invoke(new Action(() =>
            {
                lvGameSearch.Items.Clear();
                if (ls.Count >= 1)
                {
                    int size = (int)(25 * Form1.dpiFactor);
                    imageList1.ImageSize = new Size(size, size);
                    lvGameSearch.Height = ls.Count * (size + 2);
                    lvGameSearch.Visible = true;
                    lvGameSearch.Items.AddRange(ls.ToArray());
                }
                else
                {
                    lvGameSearch.Visible = false;
                }
            }));
        }

        private void XboxGamePass(int sort)
        {
            ComboBox cb;
            string siglId1 = string.Empty, siglId2 = string.Empty, text1 = string.Empty, text2 = string.Empty;
            if (sort == 1)
            {
                cb = cbGameXGP1;
                siglId1 = "eab7757c-ff70-45af-bfa6-79d3cfb2bf81";
                siglId2 = "a884932a-f02b-40c8-a903-a008c23b1df1";
                text1 = "最受欢迎 Xbox Game Pass 主机游戏 ({0})";
                text2 = "最受欢迎 Xbox Game Pass 电脑游戏 ({0})";
            }
            else
            {
                cb = cbGameXGP2;
                siglId1 = "f13cf6b4-57e6-4459-89df-6aec18cf0538";
                siglId2 = "163cdff5-442e-4957-97f5-1050a3546511";
                text1 = "近期新增 Xbox Game Pass 主机游戏 ({0})";
                text2 = "近期新增 Xbox Game Pass 电脑游戏 ({0})";
            }
            List<Product> lsProduct1 = new();
            List<Product> lsProduct2 = new();
            Task[] tasks = new Task[2];
            tasks[0] = new Task(() =>
            {
                lsProduct1 = GetXGPGames(siglId1, text1);
            });
            tasks[1] = new Task(() =>
            {
                lsProduct2 = GetXGPGames(siglId2, text2);
            });
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            List<Product> lsProduct = lsProduct1.Union(lsProduct2).ToList<Product>();
            if (lsProduct.Count >= 1)
            {
                this.Invoke(new Action(() =>
                {
                    cb.Items.Clear();
                    cb.Items.AddRange(lsProduct.ToArray());
                    cb.SelectedIndex = 0;
                }));
            }
        }

        private static List<Product> GetXGPGames(string siglId, string text)
        {
            List<Product> lsProduct = new();
            List<string> lsBundledId = new();
            string url = "https://catalog.gamepass.com/sigls/v2?id=" + siglId + "&language=en-US&market=US";
            string html = ClassWeb.HttpResponseContent(url);
            Match result = Regex.Match(html, @"\{""id"":""(?<ProductId>[a-zA-Z0-9]{12})""\}");
            while (result.Success)
            {
                lsBundledId.Add(result.Groups["ProductId"].Value.ToLowerInvariant());
                result = result.NextMatch();
            }
            if (lsBundledId.Count >= 1)
            {
                url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + string.Join(",", lsBundledId.ToArray()) + "&market=US&languages=zh-Hans,zh-Hant&MS-CV=DGU1mcuYo0WMMp+F.1";
                html = ClassWeb.HttpResponseContent(url);
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    ClassGame.Game? json = null;
                    try
                    {
                        json = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                    if (json != null && json.Products != null && json.Products.Count >= 1)
                    {
                        lsProduct.Add(new Product(string.Format(text, json.Products.Count), "0"));
                        foreach (var product in json.Products)
                        {
                            string? title = product.LocalizedProperties?[0].ProductTitle;
                            string? productId = product.ProductId;
                            if (title != null && productId != null)
                                lsProduct.Add(new Product("  " + title, productId));
                        }
                    }
                }
            }
            if (lsProduct.Count == 0)
                lsProduct.Add(new Product(string.Format(text, "加载失败"), "0"));
            return lsProduct;
        }

        private void CbGameXGP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not ComboBox cb || cb.SelectedIndex <= 0) return;
            Product product = (Product)cb.SelectedItem;
            if (product.id == "0") return;
            tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + product.id;
            if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
        }

        private void LinkGameChinese_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormChinese dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            if (!string.IsNullOrEmpty(dialog.productid))
            {
                tbGameUrl.Text = "https://www.microsoft.com/store/productid/" + dialog.productid.ToUpperInvariant();
                foreach (var item in cbGameMarket.Items)
                {
                    Market market = (Market)item;
                    if (market.language == "zh-CN")
                    {
                        cbGameMarket.SelectedItem = item;
                        break;
                    }
                }
                if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
            }
        }

        private void LinkGameWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? url = e.Link.LinkData.ToString();
            if (url != null) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void CbGameBundled_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbGameBundled.SelectedIndex < 0) return;
            tbGameTitle.Clear();
            tbGameDeveloperName.Clear();
            tbGameCategory.Clear();
            tbGameOriginalReleaseDate.Clear();
            tbGamePrice.Clear();
            tbGameDescription.Clear();
            tbGameLanguages.Clear();
            lvGame.Items.Clear();
            linkCompare.Enabled = false;
            linkGameWebsite.Enabled = false;

            var market = (Market)cbGameBundled.Tag;
            var json = (ClassGame.Game)gbGameInfo.Tag;
            int index = cbGameBundled.SelectedIndex;
            string language = market.language;
            switch (language)
            {
                case "zh-TW":
                case "zh-HK":
                    language += ",zh-Hans";
                    break;
                case "en-SG":
                case "zh-SG":
                    language = "zh-Hans," + language;
                    break;
                case "zh-CN":
                    language += ",zh-Hans";
                    break;
            }
            StoreParse(market, json, index, language);
        }

        private void XboxStore(Market market, string productId)
        {
            cbGameBundled.Tag = market;
            string language = market.language;
            switch (language)
            {
                case "zh-TW":
                case "zh-HK":
                    language += ",zh-Hans";
                    break;
                case "en-SG":
                case "zh-SG":
                    language = "zh-Hans," + language;
                    break;
                case "zh-CN":
                    language += ",zh-Hans";
                    break;
            }
            string url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + productId + "&market=" + market.code + "&languages=" + language + ",neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
            string html = ClassWeb.HttpResponseContent(url);
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                ClassGame.Game? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Products != null && json.Products.Count >= 1)
                {
                    StoreParse(market, json, 0, language);
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("无效 URL/ProductId", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        butGame.Enabled = true;
                    }));
                }
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("无法连接服务器，请稍候再试。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    butGame.Enabled = true;
                }));
            }
        }


        internal static ConcurrentDictionary<String, Double> dicExchangeRate = new();

        private void StoreParse(Market market, ClassGame.Game json, int index, string language)
        {
            string title = string.Empty, developerName = string.Empty, publisherName = string.Empty, description = string.Empty;
            var product = json.Products[index];
            List<string> bundledId = new();
            List<ListViewItem> lsDownloadUrl = new();
            var localizedPropertie = product.LocalizedProperties;
            if (localizedPropertie != null && localizedPropertie.Count >= 1)
            {
                title = localizedPropertie[0].ProductTitle;
                developerName = localizedPropertie[0].DeveloperName;
                publisherName = localizedPropertie[0].PublisherName;
                description = localizedPropertie[0].ProductDescription;
                string? imageUri = localizedPropertie[0].Images.Where(x => x.ImagePurpose == "BoxArt").Select(x => x.Uri).FirstOrDefault() ?? localizedPropertie[0].Images.Where(x => x.Width == x.Height).OrderByDescending(x => x.Width).Select(x => x.Uri).FirstOrDefault();
                if (!string.IsNullOrEmpty(imageUri))
                {
                    if (imageUri.StartsWith("//")) imageUri = "https:" + imageUri;
                    try
                    {
                        pbGame.LoadAsync(imageUri + "?w=170&h=170");
                    }
                    catch { }
                }
            }

            string originalReleaseDate = string.Empty;
            var marketProperties = product.MarketProperties;
            if (marketProperties != null && marketProperties.Count >= 1)
            {
                originalReleaseDate = marketProperties[0].OriginalReleaseDate.ToLocalTime().ToString("d");
            }

            string category = string.Empty;
            var properties = product.Properties;
            if (properties != null)
            {
                category = properties.Category;
            }

            string gameLanguages = string.Empty;
            if (product.DisplaySkuAvailabilities != null)
            {
                foreach (var displaySkuAvailabilitie in product.DisplaySkuAvailabilities)
                {
                    if (displaySkuAvailabilitie.Sku.SkuType == "full")
                    {
                        string wuCategoryId = string.Empty;
                        if (displaySkuAvailabilitie.Sku.Properties.Packages != null)
                        {
                            foreach (var packages in displaySkuAvailabilitie.Sku.Properties.Packages)
                            {
                                List<ClassGame.PlatformDependencies> platformDependencie = packages.PlatformDependencies;
                                List<ClassGame.PackageDownloadUris> packageDownloadUris = packages.PackageDownloadUris;
                                if (platformDependencie != null && packages.PlatformDependencies.Count >= 1)
                                {
                                    wuCategoryId = packages.FulfillmentData.WuCategoryId.ToLower();
                                    string url = packageDownloadUris != null && packageDownloadUris.Count >= 1 ? packageDownloadUris[0].Uri : string.Empty;
                                    if (url == "https://productingestionbin1.blob.core.windows.net") url = string.Empty;
                                    string contentId = packages.ContentId.ToLower();
                                    switch (platformDependencie[0].PlatformName)
                                    {
                                        case "Windows.Xbox":
                                            switch (packages.PackageRank)
                                            {
                                                case 50000:
                                                    {
                                                        string key = contentId + "_x";
                                                        ListViewItem item = new(new string[] { "Xbox One", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 0;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                item.SubItems[3].Tag = XboxGame.Url;
                                                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                    find = true;
                                                                else
                                                                {
                                                                    item.ForeColor = Color.Red;
                                                                    item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 0, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case 51000:
                                                    {
                                                        string key = contentId + "_xs";
                                                        ListViewItem item = new(new string[] { "Xbox Series X|S", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 1;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                item.SubItems[3].Tag = XboxGame.Url;
                                                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                    find = true;
                                                                else
                                                                {
                                                                    item.ForeColor = Color.Red;
                                                                    item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 1, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    {
                                                        string version = Regex.Match(packages.PackageFullName, @"(\d+\.\d+\.\d+\.\d+)").Value;
                                                        string filename = packages.PackageFullName + "." + packages.PackageFormat;
                                                        string key = filename.Replace(version, "").ToLower();
                                                        ListViewItem? item = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App" && x.SubItems[2].Tag.ToString() == key).FirstOrDefault();
                                                        if (item == null)
                                                        {
                                                            item = new ListViewItem(new string[] { "Xbox One", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                            {
                                                                Tag = "App"
                                                            };
                                                            item.SubItems[0].Tag = 0;
                                                            item.SubItems[1].Tag = version;
                                                            item.SubItems[2].Tag = key;
                                                            item.SubItems[3].Tag = filename;
                                                            lsDownloadUrl.Add(item);
                                                        }
                                                        else
                                                        {
                                                            string? tag = item.SubItems[1].Tag.ToString();
                                                            if (tag != null && new Version(version) > new Version(tag))
                                                            {
                                                                item.SubItems[2].Text = ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes);
                                                                item.SubItems[1].Tag = version;
                                                                item.SubItems[3].Tag = filename;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                            break;
                                        case "Windows.Desktop":
                                        case "Windows.Universal":
                                            switch (packages.PackageFormat.ToLower())
                                            {
                                                case "msixvc":
                                                    {
                                                        string key = contentId;
                                                        ListViewItem item = new(new string[] { "Windows PC", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                        {
                                                            Tag = "Game"
                                                        };
                                                        item.SubItems[0].Tag = 2;
                                                        item.SubItems[1].Tag = product.ProductId;
                                                        item.SubItems[2].Tag = key;
                                                        lsDownloadUrl.Add(item);
                                                        if (string.IsNullOrEmpty(url))
                                                        {
                                                            bool find = false;
                                                            if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                            {
                                                                if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                                                                {
                                                                    find = true;
                                                                    item.SubItems[3].Tag = XboxGame.Url;
                                                                    item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                                                                }
                                                            }
                                                            if (!find)
                                                            {
                                                                ThreadPool.QueueUserWorkItem(delegate { GetGamePackage(item, contentId, key, 2, packages); });
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case "appx":
                                                case "appxbundle":
                                                case "eappx":
                                                case "eappxbundle":
                                                case "msix":
                                                case "msixbundle":
                                                    {
                                                        string version = Regex.Match(packages.PackageFullName, @"(\d+\.\d+\.\d+\.\d+)").Value;
                                                        string filename = packages.PackageFullName + "." + packages.PackageFormat;
                                                        string key = filename.Replace(version, "").ToLower();
                                                        ListViewItem? item = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App" && x.SubItems[2].Tag.ToString() == key).FirstOrDefault();
                                                        if (item == null)
                                                        {
                                                            item = new ListViewItem(new string[] { "Windows PC", market.cname, ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes), Path.GetFileName(url) })
                                                            {
                                                                Tag = "App"
                                                            };
                                                            item.SubItems[0].Tag = 2;
                                                            item.SubItems[1].Tag = version;
                                                            item.SubItems[2].Tag = key;
                                                            item.SubItems[3].Tag = filename;
                                                            lsDownloadUrl.Add(item);
                                                        }
                                                        else
                                                        {
                                                            string? tag = item.SubItems[1].Tag.ToString();
                                                            if (tag != null && new Version(version) > new Version(tag))
                                                            {
                                                                item.SubItems[2].Text = ClassMbr.ConvertBytes(packages.MaxDownloadSizeInBytes);
                                                                item.SubItems[1].Tag = version;
                                                                item.SubItems[3].Tag = filename;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                            break;
                                    }
                                    if (packages.Languages != null) gameLanguages = string.Join(", ", packages.Languages);
                                }
                            }
                            List<ListViewItem> lsAppItems = lsDownloadUrl.ToArray().Where(x => x.Tag.ToString() == "App").ToList();
                            if (lsAppItems.Count >= 1)
                            {
                                lvGame.Tag = wuCategoryId;
                                bool find = true;
                                foreach (var item in lsAppItems)
                                {
                                    string? filename = item.SubItems[3].Tag.ToString();
                                    if (filename != null && dicAppPackage.TryGetValue(filename.ToLower(), out AppPackage? appPackage) && (DateTime.Now - appPackage.Date).TotalSeconds <= 300)
                                    {
                                        string expire = string.Empty;
                                        Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                                        if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                                        item.SubItems[3].Text = filename + expire;
                                    }
                                    else
                                    {
                                        item.SubItems[3].Text = "正在获取下载链接，请稍候...";
                                        find = false;
                                    }
                                }
                                if (!find) ThreadPool.QueueUserWorkItem(delegate { GetAppPackage(wuCategoryId, lsAppItems); });
                            }
                        }
                        if (displaySkuAvailabilitie.Sku.Properties.BundledSkus != null && displaySkuAvailabilitie.Sku.Properties.BundledSkus.Count >= 1)
                        {
                            foreach (var BundledSkus in displaySkuAvailabilitie.Sku.Properties.BundledSkus)
                            {
                                bundledId.Add(BundledSkus.BigId);
                            }
                        }
                        break;
                    }
                }
            }

            List<Product> lsProduct = new();
            if (bundledId.Count >= 1 && json.Products.Count == 1)
            {
                string url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + string.Join(",", bundledId.ToArray()) + "&market=" + market.code + "&languages=" + language + ",neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
                string html = ClassWeb.HttpResponseContent(url);
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    ClassGame.Game? json2 = null;
                    try
                    {
                        json2 = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                    if (json2 != null && json2.Products != null && json2.Products.Count >= 1)
                    {
                        json.Products.AddRange(json2.Products);
                        lsProduct.Add(new Product("此捆绑包内容(" + json2.Products.Count + ")", ""));
                        foreach (var product2 in json2.Products)
                        {
                            lsProduct.Add(new Product(product2.LocalizedProperties[0].ProductTitle, product2.ProductId));
                        }
                    }
                }
            }

            if (index == 0) gbGameInfo.Tag = json;
            var DisplaySkuAvailabilities = product.DisplaySkuAvailabilities;
            if (DisplaySkuAvailabilities != null)
            {
                string CurrencyCode = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.CurrencyCode.ToUpperInvariant();
                double MSRP = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.MSRP;
                double ListPrice_1 = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
                double ListPrice_2 = DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
                double WholesalePrice_1 = DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.WholesalePrice;
                double WholesalePrice_2 = DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.WholesalePrice : 0;
                if (ListPrice_1 > MSRP) MSRP = ListPrice_1;
                if (!string.IsNullOrEmpty(CurrencyCode) && MSRP > 0 && CurrencyCode != "CNY" && !dicExchangeRate.ContainsKey(CurrencyCode))
                {
                    ClassGame.ExchangeRate(CurrencyCode);
                }
                double ExchangeRate = dicExchangeRate.ContainsKey(CurrencyCode) ? dicExchangeRate[CurrencyCode] : 0;

                StringBuilder sbPrice = new();
                if (MSRP > 0)
                {
                    sbPrice.Append(string.Format("币种: {0}, 建议零售价: {1}", CurrencyCode, String.Format("{0:N}", MSRP)));
                    if (ExchangeRate > 0)
                    {
                        sbPrice.Append(string.Format("({0})", String.Format("{0:N}", MSRP * ExchangeRate)));
                    }
                    if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                    {
                        sbPrice.Append(string.Format(", 普通折扣{0}%: {1}", Math.Round(ListPrice_1 / MSRP * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", ListPrice_1)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", ListPrice_1 * ExchangeRate)));
                        }
                    }
                    if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                    {
                        string member = (DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags.Length >= 1 && DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags[0] == "LegacyDiscountEAAccess") ? "EA Play" : "会员";
                        sbPrice.Append(string.Format(", {0}折扣{1}%: {2}", member, Math.Round(ListPrice_2 / MSRP * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", ListPrice_2)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", ListPrice_2 * ExchangeRate)));
                        }
                    }
                    if (WholesalePrice_1 > 0)
                    {
                        sbPrice.Append(string.Format(", 批发价: {0}", String.Format("{0:N}", WholesalePrice_1)));
                        if (ExchangeRate > 0)
                        {
                            sbPrice.Append(string.Format("({0})", String.Format("{0:N}", WholesalePrice_1 * ExchangeRate)));
                        }
                        if (WholesalePrice_2 > 0 && WholesalePrice_2 < WholesalePrice_1)
                        {
                            sbPrice.Append(string.Format(", 批发价折扣{0}%: {1}", Math.Round(WholesalePrice_2 / WholesalePrice_1 * 100, 0, MidpointRounding.AwayFromZero), String.Format("{0:N}", WholesalePrice_2)));
                            if (ExchangeRate > 0)
                            {
                                sbPrice.Append(string.Format("({0})", String.Format("{0:N}", WholesalePrice_2 * ExchangeRate)));
                            }
                        }
                    }
                    if (ExchangeRate > 0)
                    {
                        sbPrice.Append(string.Format(", CNY汇率: {0}", ExchangeRate));
                    }
                }

                this.Invoke(new Action(() =>
                {
                    tbGameTitle.Text = title;
                    tbGameDeveloperName.Text = publisherName.Trim() + " / " + developerName.Trim();
                    tbGameCategory.Text = category;
                    tbGameOriginalReleaseDate.Text = originalReleaseDate;
                    if (lsProduct.Count >= 1)
                    {
                        cbGameBundled.Items.AddRange(lsProduct.ToArray());
                        cbGameBundled.SelectedIndex = 0;
                        this.cbGameBundled.SelectedIndexChanged += new EventHandler(this.CbGameBundled_SelectedIndexChanged);
                    }
                    tbGameDescription.Text = description;
                    tbGameLanguages.Text = gameLanguages;
                    if (MSRP > 0)
                    {
                        tbGamePrice.Text = sbPrice.ToString();
                        linkCompare.Enabled = true;
                    }
                    if (lsDownloadUrl.Count >= 1)
                    {
                        lsDownloadUrl.Sort((x, y) => string.Compare(x.SubItems[0].Tag.ToString(), y.SubItems[0].Tag.ToString()));
                        lvGame.Items.AddRange(lsDownloadUrl.ToArray());
                    }
                    butGame.Enabled = true;
                    linkGameWebsite.Enabled = true;
                }));
            }
        }

        readonly ConcurrentDictionary<string, DateTime> dicGetGamePackage = new();

        private void GetGamePackage(ListViewItem item, string contentId, string key, int platform, ClassGame.Packages packages)
        {
            XboxPackage.Game? json = null;
            if (!dicGetGamePackage.ContainsKey(key) || DateTime.Compare(dicGetGamePackage[key], DateTime.Now) < 0)
            {
                string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetGamePackage?contentId=" + contentId + "&platform=" + platform, "GET", null, null, null, 30000, "XboxDownload");
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.Game>(html, Form1.jsOptions);
                    }
                    catch { }
                }
            }
            bool succeed = false;
            if (json != null && json.Code == "200")
            {
                DateTime limit = DateTime.Now.AddMinutes(3);
                dicGetGamePackage.AddOrUpdate(key, limit, (oldkey, oldvalue) => limit);
                if (json.Data != null)
                {
                    Version version = new(Regex.Match(json.Data.Url, @"(\d+\.\d+\.\d+\.\d+)").Value);
                    bool update = false;
                    if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                    {
                        if (version > XboxGame.Version)
                        {
                            XboxGame.Version = version;
                            XboxGame.FileSize = json.Data.Size;
                            XboxGame.Url = json.Data.Url;
                            update = true;
                        }
                    }
                    else
                    {
                        XboxGame = new XboxGameDownload.Products
                        {
                            Version = version,
                            FileSize = json.Data.Size,
                            Url = json.Data.Url
                        };
                        XboxGameDownload.dicXboxGame.TryAdd(key, XboxGame);
                        update = true;
                    }
                    if (update) XboxGameDownload.SaveXboxGame();
                    this.Invoke(new Action(() =>
                    {
                        if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                        {
                            succeed = true;
                            item.ForeColor = Color.Empty;
                        }
                        else
                        {
                            if (platform != 2)
                            {
                                item.ForeColor = Color.Red;
                            }
                        }
                        item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                        item.SubItems[3].Tag = XboxGame.Url;
                        item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                    }));
                }
            }
            if (!succeed && (platform == 0 || platform == 2))
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Authorization))
                {
                    string hosts = "packagespc.xboxlive.com", url = String.Empty;
                    ulong filesize = 0;
                    string? ip = ClassDNS.DoH(hosts);
                    if (string.IsNullOrEmpty(ip)) return;
                    using (HttpResponseMessage? response = ClassWeb.HttpResponseMessage("https://" + ip + "/GetBasePackage/" + contentId, "GET", null, null, new() { { "Host", hosts }, { "Authorization", Properties.Settings.Default.Authorization } }))
                    {
                        if (response != null)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string html = response.Content.ReadAsStringAsync().Result;
                                if (Regex.IsMatch(html, @"^{.+}$"))
                                {
                                    XboxGameDownload.PackageFiles? packageFiles = null;
                                    try
                                    {
                                        var json2 = JsonSerializer.Deserialize<XboxGameDownload.Game>(html, Form1.jsOptions);
                                        if (json2 != null && json2.PackageFound)
                                        {
                                            packageFiles = json2.PackageFiles.Where(x => Regex.IsMatch(x.RelativeUrl, @"(\.msixvc|\.xvc)$", RegexOptions.IgnoreCase)).FirstOrDefault() ?? json2.PackageFiles.Where(x => !Regex.IsMatch(x.RelativeUrl, @"(\.xsp|\.phf)$", RegexOptions.IgnoreCase)).FirstOrDefault();
                                        }
                                    }
                                    catch { }
                                    if (packageFiles != null)
                                    {
                                        Match result = Regex.Match(packageFiles.RelativeUrl, @"(?<version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}");
                                        if (result.Success)
                                        {
                                            url = packageFiles.CdnRootPaths[0].Replace(".xboxlive.cn", ".xboxlive.com") + packageFiles.RelativeUrl;
                                            Version version = new(result.Groups["version"].Value);
                                            XboxGameDownload.Products XboxGame = new()
                                            {
                                                Version = version,
                                                FileSize = packageFiles.FileSize,
                                                Url = url
                                            };
                                            filesize = packageFiles.FileSize;
                                            XboxGameDownload.dicXboxGame.AddOrUpdate(key, XboxGame, (oldkey, oldvalue) => XboxGame);
                                            packages.MaxDownloadSizeInBytes = filesize;
                                            packages.PackageDownloadUris[0].Uri = url;
                                            XboxGameDownload.SaveXboxGame();
                                        }
                                    }
                                }
                            }
                            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Properties.Settings.Default.Authorization = null;
                                Properties.Settings.Default.Save();
                                if (platform == 2) url = "授权已失效，请使用监听方式打开Xbox app，随便找一个游戏点击安装（无需实际安装），等待日志显示下载链接即可更新授权。";
                            }
                        }
                    }
                    this.Invoke(new Action(() =>
                    {
                        if (filesize > 0)
                        {
                            item.ForeColor = Color.Empty;
                            item.SubItems[2].Text = ClassMbr.ConvertBytes(filesize);
                        }
                        if (!string.IsNullOrEmpty(url))
                        {
                            item.SubItems[3].Tag = url;
                            item.SubItems[3].Text = Path.GetFileName(url);
                        }
                    }));
                    if (Regex.IsMatch(url, @"^https?://")) _ = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/AddGameUrl?url=" + ClassWeb.UrlEncode(url), "PUT", null, null, null, 30000, "XboxDownload");
                }
                else if (platform == 2)
                {
                    this.Invoke(new Action(() =>
                    {
                        item.SubItems[3].Text = "授权已失效，请使用监听方式打开Xbox app，随便找一个游戏点击安装（无需实际安装），等待日志显示下载链接即可更新授权。";
                    }));
                }
            }
        }

        readonly ConcurrentDictionary<string, AppPackage> dicAppPackage = new();
        class AppPackage
        {
            public string Url { get; set; } = "";
            public DateTime Date { get; set; }
        }

        private void GetAppPackage(string wuCategoryId, List<ListViewItem> lsAppItems)
        {
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
            XboxPackage.App? json = null;
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
            }
            if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
            {
                foreach (var item in json.Data)
                {
                    if (!string.IsNullOrEmpty(item.Url))
                    {
                        AppPackage appPackage = new()
                        {
                            Url = item.Url,
                            Date = DateTime.Now
                        };
                        dicAppPackage.AddOrUpdate(item.Name.ToLower(), appPackage, (oldkey, oldvalue) => appPackage);
                    }
                }
            }
            this.Invoke(new Action(() =>
            {
                foreach (var item in lsAppItems)
                {
                    string filename = String.Empty;
                    string? tag = item.SubItems[3].Tag?.ToString();
                    if (tag != null && dicAppPackage.TryGetValue(tag.ToLower(), out _))
                    {
                        filename = tag;
                    }
                    else if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
                    {
                        var key = item.SubItems[2].Tag.ToString()?.ToLower();
                        if (key != null)
                        {
                            var data = json.Data.Where(x => Regex.Replace(x.Name, @"\d+\.\d+\.\d+\.\d+", "").ToLower() == key).FirstOrDefault();
                            if (data != null)
                            {
                                item.SubItems[3].Tag = data.Name;
                                item.SubItems[2].Text = ClassMbr.ConvertBytes(data.Size);
                                filename = data.Name;
                            }
                        }
                    }
                    string expire = string.Empty;
                    if (dicAppPackage.TryGetValue(filename.ToLower(), out AppPackage? appPackage))
                    {
                        Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                        if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                    }
                    item.SubItems[3].Text = filename + expire;
                }
            }));
        }

        private void LinkCompare_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int index = cbGameBundled.SelectedIndex == -1 ? 0 : cbGameBundled.SelectedIndex;
            FormCompare dialog = new(gbGameInfo.Tag, index);
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is not LinkLabel link) return;
            string? url = link.Tag.ToString();
            if (string.IsNullOrEmpty(url)) return;
            switch (link.Name)
            {
                case "linkWebPage":
                    if (gbGameInfo.Tag != null)
                    {
                        url += "?" + ((ClassGame.Game)gbGameInfo.Tag).Products[0].ProductId;
                    }
                    break;
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void LinkAppxAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tabControl1.SelectedTab = tabTools;
            tbAppxFilePath.Focus();
        }

        private void LvGame_MouseClick(object sender, MouseEventArgs e)
        {
            if (lvGame.SelectedItems.Count == 1)
            {
                ListViewItem item = lvGame.SelectedItems[0];
                string text = item.SubItems[3].Text;
                if (e.Button == MouseButtons.Left)
                {
                    if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]")) return;
                    if (item.Tag.ToString() == "Game")
                    {
                        if (Regex.IsMatch(item.SubItems[3].Text, @"^https?://"))
                            item.SubItems[3].Text = Path.GetFileName(item.SubItems[3].Text);

                        else
                            item.SubItems[3].Text = item.SubItems[3].Tag.ToString();
                    }
                    else
                    {
                        if (Regex.IsMatch(item.SubItems[3].Text, @"^https?://"))
                        {
                            string expire = string.Empty;
                            if (!string.IsNullOrEmpty(item.SubItems[1].Text) && dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                            {
                                Match result = Regex.Match(appPackage.Url, @"P1=(\d+)");
                                if (result.Success) expire = " (Expire: " + DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime() + ")";
                            }
                            item.SubItems[3].Text = item.SubItems[3].Tag.ToString() + expire;
                        }
                        else if (dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                        {
                            item.SubItems[3].Text = appPackage.Url;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(text) && !Regex.IsMatch(text, @"请稍候"))
                {
                    if (!Regex.IsMatch(text, @"授权"))
                    {
                        bool isGame = item.Tag.ToString() == "Game";
                        tsmCopyUrl1.Visible = true;
                        tsmCopyUrl2.Visible = tsmCopyUrl3.Visible = isGame;
                        tsmCopyUrl3.Enabled = isGame && item.SubItems[3].Tag != null && Regex.IsMatch(item.SubItems[3].Tag.ToString() ?? string.Empty, @"http://[^\.]+\.xboxlive\.com/(\d{1,2}|Z)/");
                        tsmAllUrl.Visible = !isGame && lvGame.Tag != null && item.SubItems[0].Text == "Windows PC";
                        tsmAuthorization.Visible = false;
                    }
                    else
                    {
                        tsmCopyUrl1.Visible = tsmCopyUrl2.Visible = tsmCopyUrl3.Visible = tsmAllUrl.Visible = false;
                        tsmAuthorization.Visible = true;
                    }
                    cmsCopyUrl.Show(MousePosition.X, MousePosition.Y);
                }
            }
        }

        private void TsmCopyUrl_Click(object sender, EventArgs e)
        {
            string url = string.Empty;
            ListViewItem item = lvGame.SelectedItems[0];
            if (item.Tag.ToString() == "Game")
            {
                url = item.SubItems[3].Tag.ToString() ?? string.Empty;
            }
            else
            {
                if (dicAppPackage.TryGetValue((item.SubItems[3].Tag.ToString() ?? string.Empty).ToLower(), out AppPackage? appPackage))
                {
                    url = appPackage.Url;
                }
            }
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            if (tsmi.Name == "tsmCopyUrl2")
            {
                string hosts = Regex.Match(url, @"(?<=://)[a-zA-Z\.0-9]+(?=\/)").Value;
                url = hosts switch
                {
                    "xvcf1.xboxlive.com" => url.Replace("xvcf1.xboxlive.com", "assets1.xboxlive.cn"),
                    "xvcf2.xboxlive.com" => url.Replace("xvcf2.xboxlive.com", "assets2.xboxlive.cn"),
                    _ => url.Replace(".xboxlive.com", ".xboxlive.cn"),
                };
            }
            else if (tsmi.Name == "tsmCopyUrl3")
            {
                Match result = Regex.Match(url, @"http://[^\.]+\.xboxlive\.com/(\d{1,2}|Z)/(.+)");
                if (result.Success) url = "http://xbasset" + result.Groups[1].Value.Replace("Z", "0") + ".blob.core.windows.net/" + result.Groups[2].Value;
            }
            Clipboard.SetDataObject(url);
            if (lvGame.SelectedItems[0].ForeColor == Color.Red)
            {
                MessageBox.Show("已有新的版本，此下载链接已过时。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsmAllUrl_Click(object sender, EventArgs e)
        {
            string? wuCategoryId = lvGame.Tag.ToString();
            if (string.IsNullOrEmpty(wuCategoryId)) return;
            lvGame.Tag = null;
            ListViewItem last = new(new string[] { "", "", "", "请稍候..." });
            lvGame.Items.Add(last);
            List<String> filter = new();
            foreach (ListViewItem item in lvGame.Items)
            {
                string? filename = item.SubItems[3].Tag?.ToString();
                if (string.IsNullOrEmpty(filename)) continue;
                filter.Add(filename.ToLower());
            }
            Task.Factory.StartNew(() =>
            {
                string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage2?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
                XboxPackage.App? json = null;
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                    }
                    catch { }
                }
                List<ListViewItem> list = new();
                if (json != null && json.Code != null && json.Code == "200" && json.Data != null)
                {
                    json.Data.Sort((x, y) => string.Compare(x.Name, y.Name));
                    foreach (var item in json.Data)
                    {
                        if (!string.IsNullOrEmpty(item.Url))
                        {
                            AppPackage appPackage = new()
                            {
                                Url = item.Url,
                                Date = DateTime.Now
                            };
                            dicAppPackage.AddOrUpdate(item.Name.ToLower(), appPackage, (oldkey, oldvalue) => appPackage);
                        }
                        if (!filter.Contains(item.Name.ToLower()))
                        {
                            ListViewItem lvi = new(new string[] { "", "", ClassMbr.ConvertBytes(item.Size), item.Name })
                            {
                                Tag = "App"
                            };
                            lvi.SubItems[3].Tag = item.Name;
                            list.Add(lvi);
                        }
                    }
                }
                this.Invoke(new Action(() =>
                {
                    if (last.Index != -1)
                    {
                        lvGame.Items.RemoveAt(lvGame.Items.Count - 1);
                        if (list.Count > 0) lvGame.Items.AddRange(list.ToArray());
                    }
                }));
            });
        }

        private void TsmAuthorization_Click(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                MessageBox.Show("只支持Win10或以上版本操作系统。", "操作系统版本过低", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (bServiceFlag && Properties.Settings.Default.HttpService && Properties.Settings.Default.MicrosoftStore)
            {
                ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
                Process.Start(new ProcessStartInfo("msxbox://game/?productId=" + (tsmi.Tag ?? lvGame.SelectedItems[0].SubItems[1].Tag)) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("请先启动监听方式并且加勾选 启用HTTP(S)服务、加速微软商店 选项。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
        #endregion

        #region 选项卡-工具
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x219)
            {
                switch (m.WParam.ToInt32())
                {
                    case 0x8000: //U盘插入
                    case 0x8004: //U盘卸载
                        LinkRefreshDrive_LinkClicked(null, null);
                        break;
                    default:
                        break;
                }
            }
            base.WndProc(ref m);
        }

        private void CbDrive_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbDrive.Items.Count >= 1)
            {
                string driverName = cbDrive.Text;
                DriveInfo driveInfo = new(driverName);
                if (driveInfo.DriveType == DriveType.Removable)
                {
                    if (driveInfo.IsReady && driveInfo.DriveFormat == "NTFS")
                    {
                        List<string> listStatus = new();
                        if (File.Exists(driverName + "$ConsoleGen8Lock"))
                            listStatus.Add(rbXboxOne.Text + " 回国");
                        else if (File.Exists(driverName + "$ConsoleGen8"))
                            listStatus.Add(rbXboxOne.Text + " 出国");
                        if (File.Exists(driverName + "$ConsoleGen9Lock"))
                            listStatus.Add(rbXboxSeries.Text + " 回国");
                        else if (File.Exists(driverName + "$ConsoleGen9"))
                            listStatus.Add(rbXboxSeries.Text + " 出国");
                        if (listStatus.Count >= 1)
                            labelStatusDrive.Text = "当前U盘状态：" + string.Join(", ", listStatus.ToArray());
                        else
                            labelStatusDrive.Text = "当前U盘状态：未转换";
                    }
                    else
                    {
                        labelStatusDrive.Text = "当前U盘状态：不是NTFS格式";
                    }
                }
            }
        }

        private void LinkRefreshDrive_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs? e)
        {
            cbDrive.Items.Clear();
            DriveInfo[] driverList = Array.FindAll(DriveInfo.GetDrives(), a => a.DriveType == DriveType.Removable);
            if (driverList.Length >= 1)
            {
                cbDrive.Items.AddRange(driverList);
                cbDrive.SelectedIndex = 0;
            }
            else
            {
                labelStatusDrive.Text = "当前U盘状态：没有找到U盘";
            }
        }

        private void LinkUsbDevice_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormUsbDevice dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            LinkRefreshDrive_LinkClicked(sender, e);
        }

        private void ButConsoleRegionUnlock_Click(object sender, EventArgs e)
        {
            ConsoleRegion(true);
        }

        private void ButConsoleRegionLock_Click(object sender, EventArgs e)
        {
            ConsoleRegion(false);
        }

        private void ConsoleRegion(bool unlock)
        {
            if (cbDrive.Items.Count == 0)
            {
                MessageBox.Show("请插入U盘。", "没有找到U盘", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            labelStatusDrive.Text = "当前U盘状态：制作中，请稍候...";
            string driverName = cbDrive.Text;
            DriveInfo driveInfo = new(driverName);
            if (driveInfo.DriveType == DriveType.Removable)
            {
                if (!driveInfo.IsReady || driveInfo.DriveFormat != "NTFS")
                {
                    string show, caption, cmd;
                    if (driveInfo.IsReady && driveInfo.DriveFormat == "FAT32")
                    {
                        show = "当前U盘格式 " + driveInfo.DriveFormat + "，是否把U盘转换为 NTFS 格式？\n\n注意，如果U盘有重要数据请先备份!\n\n当前U盘位置： " + driverName + "，容量：" + ClassMbr.ConvertBytes(Convert.ToUInt64(driveInfo.TotalSize)) + "\n取消转换请按\"否(N)\"";
                        caption = "转换U盘格式";
                        cmd = "convert " + Regex.Replace(driverName, @"\\$", "") + " /fs:ntfs /x";
                    }
                    else
                    {
                        show = "当前U盘格式 " + (driveInfo.IsReady ? driveInfo.DriveFormat : "RAW") + "，是否把U盘格式化为 NTFS？\n\n警告，格式化将删除U盘中的所有文件!\n警告，格式化将删除U盘中的所有文件!\n警告，格式化将删除U盘中的所有文件!\n\n当前U盘位置： " + driverName + "，容量：" + (driveInfo.IsReady ? ClassMbr.ConvertBytes(Convert.ToUInt64(driveInfo.TotalSize)) : "未知") + "\n取消格式化请按\"否(N)\"";
                        caption = "格式化U盘";
                        cmd = "format " + Regex.Replace(driverName, @"\\$", "") + " /fs:ntfs /q";
                    }
                    if (MessageBox.Show(show, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        string outputString;
                        using Process p = new();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();

                        p.StandardInput.WriteLine(cmd);
                        p.StandardInput.WriteLine("exit");

                        p.StandardInput.Close();
                        outputString = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                    }
                }
                if (driveInfo.IsReady && driveInfo.DriveFormat == "NTFS")
                {
                    string[] files = { "$ConsoleGen8", "$ConsoleGen8Lock", "$ConsoleGen9", "$ConsoleGen9Lock" };
                    foreach (string file in files)
                    {
                        if (File.Exists(driverName + "\\" + file))
                        {
                            File.Delete(driverName + "\\" + file);
                        }
                    }
                    if (rbXboxOne.Checked)
                    {
                        using (File.Create(driverName + (unlock ? "$ConsoleGen8" : "$ConsoleGen8Lock"))) { }
                    }
                    else if (rbXboxSeries.Checked)
                    {
                        using (File.Create(driverName + (unlock ? "$ConsoleGen9" : "$ConsoleGen9Lock"))) { }
                    }
                    if (Regex.IsMatch(driveInfo.VolumeLabel, @"[^\x00-\xFF]")) //卷标含有非英文字符
                    {
                        driveInfo.VolumeLabel = "";
                    }
                }
                else
                {
                    MessageBox.Show("U盘不是NTFS格式，请重新格式化NTFS格式后再转换。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                CbDrive_SelectedIndexChanged(null, null);
            }
            else
            {
                labelStatusDrive.Text = "当前U盘状态：" + driverName + " 设备不存在";
            }
        }

        private void CbAppxDrive_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (cbAppxDrive.Tag == null) return;
            DataTable dt = (DataTable)cbAppxDrive.Tag;
            string drive = cbAppxDrive.Text, gamesPath;
            string? storePath;
            bool error = false;
            DataRow? dr = dt.Rows.Find(drive);
            if (dr != null)
            {
                storePath = dr["StorePath"].ToString();
                if (Convert.ToBoolean(dr["IsOffline"]))
                {
                    error = true;
                    storePath += " (离线)";
                }
            }
            else
            {
                error = true;
                storePath = "(未知错误)";
            }
            if (File.Exists(drive + "\\.GamingRoot"))
            {
                try
                {
                    using FileStream fs = new(drive + "\\.GamingRoot", FileMode.Open, FileAccess.Read, FileShare.Read);
                    using BinaryReader br = new(fs);
                    if (ClassMbr.ByteToHex(br.ReadBytes(0x8)) == "5247425801000000")
                    {
                        gamesPath = drive + Encoding.GetEncoding("UTF-16").GetString(br.ReadBytes((int)fs.Length - 0x8)).Trim('\0');
                        if (!Directory.Exists(gamesPath))
                        {
                            error = true;
                            gamesPath += " (文件夹不存在)";
                        }
                    }
                    else
                    {
                        error = true;
                        gamesPath = drive + " (文件夹未知)";
                    }
                }
                catch (Exception ex)
                {
                    error = true;
                    gamesPath = drive + " (" + ex.Message + ")";
                }
            }
            else
            {
                error = true;
                gamesPath = drive + " (文件夹未知)";
            }
            linkFixAppxDrive.Visible = error;
            labelInstallationLocation.ForeColor = error ? Color.Red : Color.Green;
            labelInstallationLocation.Text = $"应用安装目录：{storePath}\r\n游戏安装目录：{gamesPath}";
        }

        private void LinkAppxRefreshDrive_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs? e)
        {
            cbAppxDrive.Items.Clear();
            cbAppxDrive.Tag = null;
            DriveInfo[] driverList = Array.FindAll(DriveInfo.GetDrives(), a => a.DriveType == DriveType.Fixed && a.IsReady && a.DriveFormat == "NTFS");
            if (driverList.Length >= 1)
            {
                cbAppxDrive.Items.AddRange(driverList);
                cbAppxDrive.SelectedIndex = 0;
            }
            ThreadPool.QueueUserWorkItem(delegate { GetAppxVolume(); });
        }

        private void LinkFixAppxDrive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service == null || service.Status != ServiceControllerStatus.Running)
            {
                MessageBox.Show("没有检测到游戏服务(Gaming Services)，请先启动游戏服务再执行此操作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string drive = cbAppxDrive.Text, path;
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (drive == Directory.GetDirectoryRoot(dir))
                path = dir + "\\WindowsApps";
            else
                path = drive + "WindowsApps";
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("Add-AppxVolume -Path \"" + path + "\"");
                p.StandardInput.WriteLine("Mount-AppxVolume -Volume \"" + path + "\"");
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("调用PowerShell失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            bool fixGamingRoot = false;
            if (!File.Exists(drive + "\\.GamingRoot"))
            {
                fixGamingRoot = true;
            }
            else
            {
                using FileStream fs = new(drive + "\\.GamingRoot", FileMode.Open, FileAccess.Read, FileShare.Read);
                using BinaryReader br = new(fs);
                if (ClassMbr.ByteToHex(br.ReadBytes(0x8)) == "5247425801000000")
                {
                    string gamesPath = drive + Encoding.GetEncoding("UTF-16").GetString(br.ReadBytes((int)fs.Length - 0x8)).Trim('\0');
                    if (!Directory.Exists(gamesPath))
                    {
                        fixGamingRoot = true;
                    }
                }
            }
            if (fixGamingRoot)
            {
                if (service != null)
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                    try
                    {
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        }
                        if (service.Status != ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        }
                    }
                    catch { }
                }
            }
            MessageBox.Show("安装位置修复已完成。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ThreadPool.QueueUserWorkItem(delegate { GetAppxVolume(); });
        }

        private void GetAppxVolume()
        {
            string outputString = "";
            try
            {
                using Process p = new();
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("Get-AppxVolume");
                p.StandardInput.Close();
                outputString = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch { }
            DataTable dt = new();
            DataColumn dcDirectoryRoot = dt.Columns.Add("DirectoryRoot", typeof(String));
            dt.Columns.Add("StorePath", typeof(String));
            dt.Columns.Add("IsOffline", typeof(Boolean));
            dt.PrimaryKey = new DataColumn[] { dcDirectoryRoot };
            Match result = Regex.Match(outputString, @"(?<Name>\\\\\?\\Volume\{\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\})\s+(?<PackageStorePath>.+)\s+(?<IsOffline>True|False)\s+(?<IsSystemVolume>True|False)");
            while (result.Success)
            {
                string storePath = result.Groups["PackageStorePath"].Value.Trim();
                string directoryRoot = Directory.GetDirectoryRoot(storePath);
                bool isOffline = result.Groups["IsOffline"].Value == "True";
                DataRow? dr = dt.Rows.Find(directoryRoot);
                if (dr == null)
                {
                    dr = dt.NewRow();
                    dr["DirectoryRoot"] = directoryRoot;
                    dr["StorePath"] = storePath;
                    dr["IsOffline"] = isOffline;
                    dt.Rows.Add(dr);
                }
                result = result.NextMatch();
            }
            cbAppxDrive.Tag = dt;
            this.Invoke(new Action(() =>
            {
                CbAppxDrive_SelectedIndexChanged(null, null);
            }));
        }

        private void ButAppxOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Title = "Open an Xbox Package"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            tbAppxFilePath.Text = ofd.FileName;
        }

        private void ButAppxInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbAppxFilePath.Text)) return;
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service == null || service.Status != ServiceControllerStatus.Running)
            {
                MessageBox.Show("没有检测到游戏服务(Gaming Services)，请先启动游戏服务再执行此操作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (linkFixAppxDrive.Visible)
            {
                if (MessageBox.Show("安装目录好像有问题，是否要继续安装？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            string filepath = tbAppxFilePath.Text;
            tbAppxFilePath.Clear();
            string cmd;
            if (Path.GetFileName(filepath) == "AppxManifest.xml")
            {
                /*
                绕过微软商店应用许可部署应用
                使用说明：
                1、开启开发者选项 系统->设置->隐私和安全性->开发者选项
                2、把下载回来的 .appx 或者 .appxbundle 文件解压到工件目录
                3、选择 AppxManifest.xml 点击安装
                上述方法仅适用于非 eappx/eappxbundle 安装包。
                微软已经表示，这是一个预期的功能，将来可能不会打补丁。
                */
                string appSignature = Path.GetDirectoryName(filepath) + "\\AppxSignature.p7x";
                if (File.Exists(appSignature))
                {
                    File.Move(appSignature, appSignature + ".bak");
                }
                cmd = "-noexit \"Add-AppxPackage -Register '" + filepath + "'\necho 部署脚本执行完毕，如果没有其它需要可以直接关闭此窗口\"";
            }
            else
            {
                cmd = "-noexit \"Add-AppxPackage -Path '" + filepath + "' -Volume '" + cbAppxDrive.Text + "'\necho 部署脚本执行完毕，如果没有其它需要可以直接关闭此窗口。\"";
            }
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.Arguments = cmd;
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("安装微软商店应用软件失败，错误信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LinkRestartGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = false;
            ThreadPool.QueueUserWorkItem(delegate { ReStartGamingServices(); });
        }

        private void ReStartGamingServices()
        {
            bool bTimeOut = false, bDone = false;
            ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
            if (service != null)
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                try
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    }
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        if (service.Status == ServiceControllerStatus.Running) bDone = true;
                        else bTimeOut = true;
                    }
                    else bTimeOut = true;
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("重启游戏服务出错\n错误信息：" + ex.Message, "重启游戏服务出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = true;
                    }));
                    return;
                }
            }
            this.Invoke(new Action(() =>
            {
                if (bTimeOut)
                    MessageBox.Show("重启游戏服务超时，请选择重装游戏服务。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (bDone)
                    MessageBox.Show("重启游戏服务已完成。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("找不到游戏服务，可能没有安装。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                linkRestartGamingServices.Enabled = linkReInstallGamingServices.Enabled = true;
            }));
        }

        private void LinkReInstallGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                MessageBox.Show("只支持Win10或以上版本操作系统。", "操作系统版本过低", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (MessageBox.Show("请确认是否要重装游戏服务？", "重装游戏服务", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = false;
                linkReInstallGamingServices.Text = "获取游戏服务应用下载链接";
                ThreadPool.QueueUserWorkItem(delegate { ReInstallGamingServices(); });
            }
        }

        private void LinkAppGamingServices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tbGameUrl.Text = "9MWPM2CQNLHN";
            ButGame_Click(sender, EventArgs.Empty);
            tabControl1.SelectedTab = tabStore;
        }

        private void ReInstallGamingServices()
        {
            XboxPackage.Data? data = null;
            string html = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/GetAppPackage?WuCategoryId=f2ea4abe-4e1e-48ff-9022-a8a758303181", "GET", null, null, null, 30000, "XboxDownload");
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                XboxPackage.App? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, Form1.jsOptions);
                }
                catch { }
                if (json != null && json.Code != null && json.Code == "200")
                {
                    data = json.Data.Where(x => x.Name.ToLower().EndsWith(".appxbundle")).FirstOrDefault();
                }
            }
            if (data != null)
            {
                this.Invoke(new Action(() =>
                {
                    linkReInstallGamingServices.Text = "下载游戏服务应用安装包";
                }));
                string filePath = Path.GetTempPath() + data.Name;
                if (File.Exists(filePath))
                    File.Delete(filePath);
                using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(data.Url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                    try
                    {
                        using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        fs.Write(buffer, 0, buffer.Length);
                        fs.Flush();
                        fs.Close();
                    }
                    catch { }
                }
                else
                {
                    string msg = response != null ? "下载失败，错误信息：" + response.ReasonPhrase : "下载失败";
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                if (File.Exists(filePath))
                {
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "重装游戏服务";
                    }));
                    try
                    {
                        using Process p = new();
                        p.StartInfo.FileName = @"powershell.exe";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.StandardInput.WriteLine("Get-AppxPackage Microsoft.GamingServices | Remove-AppxPackage -AllUsers");
                        p.StandardInput.WriteLine("Add-AppxPackage \"" + filePath + "\"");
                        p.StandardInput.WriteLine("exit");
                        p.WaitForExit();
                    }
                    catch { }
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "清理临时文件";
                        MessageBox.Show("重装游戏服务已完成。", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));

                    DateTime t1 = DateTime.Now.AddSeconds(30);
                    bool completed = false;
                    while (!completed)
                    {
                        ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "GamingServices").SingleOrDefault();
                        if (service != null && service.Status == ServiceControllerStatus.Running)
                            completed = true;
                        else if (DateTime.Compare(t1, DateTime.Now) <= 0)
                            break;
                        else
                            Thread.Sleep(100);
                    }
                    File.Delete(filePath);
                    this.Invoke(new Action(() =>
                    {
                        linkReInstallGamingServices.Text = "一键重装游戏服务";
                        linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = true;
                    }));
                    if (!completed)
                    {
                        try
                        {
                            Process.Start("ms-windows-store://pdp/?productid=9mwpm2cqnlhn");
                        }
                        catch { }
                    }
                    return;
                }
            }
            try
            {
                using Process p = new();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("get-appxpackage Microsoft.GamingServices | remove-AppxPackage -allusers");
                p.StandardInput.WriteLine("start ms-windows-store://pdp/?productid=9mwpm2cqnlhn");
                p.StandardInput.WriteLine("exit");
                p.WaitForExit();
            }
            catch { }
            this.Invoke(new Action(() =>
            {
                linkReInstallGamingServices.Text = "一键重装游戏服务";
                linkReInstallGamingServices.Enabled = linkRestartGamingServices.Enabled = true;
            }));
        }
        #endregion

        private void tbGameUrl_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string? ost = "ms-windows-store://pdp/?productid=" + linkProductID.Text;
            if (ost != null) Process.Start(new ProcessStartInfo(ost) { UseShellExecute = true });

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
    }
}