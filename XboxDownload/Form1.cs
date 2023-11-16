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
using System.Text.RegularExpressions;
using System.Web;
using NetFwTypeLib;

namespace XboxDownload
{
    public partial class Form1 : Form
    {
        internal static Boolean bServiceFlag = false, bAutoStartup = false;
        internal readonly static String resourcePath = Application.StartupPath + "Resource";
        internal const string gameData = "XboxGame.json";
        internal static List<Market> lsMarket = new();
        internal static float dpixRatio = 1;
        private readonly DataTable dtHosts = new("Hosts");
        private readonly DnsListen dnsListen;
        private readonly HttpListen httpListen;
        private readonly HttpsListen httpsListen;

        public Form1()
        {
            InitializeComponent();

            Form1.dpixRatio = Environment.OSVersion.Version.Major >= 10 ? CreateGraphics().DpiX / 96 : Program.Utility.DpiX / 96;
            if (Form1.dpixRatio > 1)
            {
                foreach (ColumnHeader col in lvLog.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
                dgvIpList.RowHeadersWidth = (int)(dgvIpList.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dgvIpList.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
                dgvHosts.RowHeadersWidth = (int)(dgvHosts.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dgvHosts.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
                dgvDevice.RowHeadersWidth = (int)(dgvDevice.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dgvDevice.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
                foreach (ColumnHeader col in lvGame.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
            }

            ClassWeb.HttpClientFactory();
            dnsListen = new DnsListen(this);
            httpListen = new HttpListen(this);
            httpsListen = new HttpsListen(this);

            ToolTip toolTip1 = new()
            {
                AutoPopDelay = 30000,
                IsBalloon = true
            };
            toolTip1.SetToolTip(this.labelDNS, "常用 DNS 服务器\n114.114.114.114 (114)\n180.76.76.76 (百度)\n223.5.5.5 (阿里)\n119.29.29.29 (腾讯)\n208.67.220.220 (OpenDns)\n8.8.8.8 (Google)\n168.126.63.1 (韩国)");
            toolTip1.SetToolTip(this.labelCom, "包括以下com游戏下载域名\nxvcf1.xboxlive.com\nxvcf2.xboxlive.com\nassets1.xboxlive.com\nassets2.xboxlive.com\nd1.xboxlive.com\nd2.xboxlive.com\ndlassets.xboxlive.com\ndlassets2.xboxlive.com\n\n以上域名不能使用 cn IP");
            toolTip1.SetToolTip(this.labelCn, "包括以下cn游戏下载域名\nassets1.xboxlive.cn\nassets2.xboxlive.cn\nd1.xboxlive.cn\nd2.xboxlive.cn");
            toolTip1.SetToolTip(this.labelApp, "包括以下应用下载域名\ndl.delivery.mp.microsoft.com\ntlu.dl.delivery.mp.microsoft.com\ndlassets.xboxlive.cn\ndlassets2.xboxlive.cn");
            toolTip1.SetToolTip(this.labelPS, "包括以下游戏下载域名\ngst.prod.dl.playstation.net\ngs2.ww.prod.dl.playstation.net\nzeus.dl.playstation.net\nares.dl.playstation.net");
            toolTip1.SetToolTip(this.labelNS, "包括以下游戏下载域名\natum.hac.lp1.d4c.nintendo.net\nbugyo.hac.lp1.eshop.nintendo.net\nctest-dl-lp1.cdn.nintendo.net\nctest-ul-lp1.cdn.nintendo.net");
            toolTip1.SetToolTip(this.labelEA, "包括以下游戏下载域名\norigin-a.akamaihd.net");
            toolTip1.SetToolTip(this.labelBattle, "包括以下游戏下载域名\nblzddist1-a.akamaihd.net\nblzddist2-a.akamaihd.net\nblzddist3-a.akamaihd.net");
            toolTip1.SetToolTip(this.labelEpic, "包括以下游戏下载域名\nepicgames-download1-1251447533.file.myqcloud.com");
            toolTip1.SetToolTip(this.ckbDoH, "使用 阿里云DoH(加密DNS) 解析域名IP，\n防止上游DNS服务器被劫持污染。\nXbox各种联网问题可以勾选此选项。\n需要在PC使用可以勾选“设置本机 DNS”。");
            toolTip1.SetToolTip(this.ckbSetDns, "开始监听将把电脑DNS设置为本机IP并禁用IPv6，停止监听后改回自动获取，\n本功能需要配合“启用 DNS 服务”使用，主机玩家无需设置。\n注：如果退出下载助手后没网络，请手动把电脑DNS改回自动获取。");

            tbDnsIP.Text = Properties.Settings.Default.DnsIP;
            tbComIP.Text = Properties.Settings.Default.ComIP;
            tbCnIP.Text = Properties.Settings.Default.CnIP;
            tbAppIP.Text = Properties.Settings.Default.AppIP;
            tbPSIP.Text = Properties.Settings.Default.PSIP;
            tbNSIP.Text = Properties.Settings.Default.NSIP;
            ckbNSBrowser.Checked = Properties.Settings.Default.NSBrowser;
            tbEAIP.Text = Properties.Settings.Default.EAIP;
            ckbEACDN.Checked = Properties.Settings.Default.EACDN;
            tbBattleIP.Text = Properties.Settings.Default.BattleIP;
            ckbBattleCDN.Checked = Properties.Settings.Default.BattleCDN;
            tbEpicIP.Text = Properties.Settings.Default.EpicIP;
            ckbEpicCDN.Checked = Properties.Settings.Default.EpicCDN;
            ckbGameLink.Checked = Properties.Settings.Default.GameLink;
            ckbTruncation.Checked = Properties.Settings.Default.Truncation;
            ckbLocalUpload.Checked = Properties.Settings.Default.LocalUpload;
            if (string.IsNullOrEmpty(Properties.Settings.Default.LocalPath))
                Properties.Settings.Default.LocalPath = Application.StartupPath + "Upload";
            tbLocalPath.Text = Properties.Settings.Default.LocalPath;
            cbListenIP.SelectedIndex = Properties.Settings.Default.ListenIP;
            ckbDnsService.Checked = Properties.Settings.Default.DnsService;
            ckbHttpService.Checked = Properties.Settings.Default.HttpService;
            ckbDoH.Checked = Properties.Settings.Default.DoH;
            ckbSetDns.Checked = Properties.Settings.Default.SetDns;
            ckbMicrosoftStore.Checked = Properties.Settings.Default.MicrosoftStore;
            ckbEAStore.Checked = Properties.Settings.Default.EAStore;
            ckbBattleStore.Checked = Properties.Settings.Default.BattleStore;
            ckbEpicStore.Checked = Properties.Settings.Default.EpicStore;
            ckbRecordLog.Checked = Properties.Settings.Default.RecordLog;
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;
            ckbEnableCdnIP.Checked = Properties.Settings.Default.EnableCdnIP;

            ckbGameLink.CheckedChanged += new EventHandler(CkbGameLink_CheckedChanged);
            ckbLocalUpload.CheckedChanged += new EventHandler(CkbLocalUpload_CheckedChanged);
            ckbSetDns.CheckedChanged += new EventHandler(CkbSetDns_CheckedChanged);
            ckbRecordLog.CheckedChanged += new EventHandler(CkbRecordLog_CheckedChanged);

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && !x.Description.Contains("Virtual")).ToArray();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                UnicastIPAddressInformationCollection ipCollection = adapterProperties.UnicastAddresses;
                foreach (UnicastIPAddressInformation ipadd in ipCollection)
                {
                    if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                        cbLocalIP.Items.Add(ipadd.Address.ToString());
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
            if (File.Exists(resourcePath + "\\Akamai.txt"))
            {
                string json = File.ReadAllText(resourcePath + "\\Akamai.txt");
                tbHosts2Akamai.Text = json.Trim() + "\r\n";
            }
            SetCdn();

            cbHosts.SelectedIndex = 0;
            cbSpeedTestTimeOut.SelectedIndex = 0;
            cbDohDNS.SelectedIndex = 0;
            cbImportIP.SelectedIndex = 0;

            dtHosts.Columns.Add("Enable", typeof(Boolean));
            dtHosts.Columns.Add("HostName", typeof(String));
            dtHosts.Columns.Add("IPv4", typeof(String));
            dtHosts.Columns.Add("Remark", typeof(String));
            if (File.Exists(resourcePath + "\\Hosts.xml"))
            {
                try
                {
                    dtHosts.ReadXml(resourcePath + "\\Hosts.xml");
                }
                catch { }
                dtHosts.AcceptChanges();
            }
            dgvHosts.DataSource = dtHosts;

            Form1.lsMarket.AddRange((new List<Market>
            {
                new Market("Taiwan", "台湾", "TW", "zh-TW"),
                new Market("Hong Kong SAR", "香港", "HK", "zh-HK"),
                new Market("Singapore", "新加坡", "SG", "en-SG"),
                new Market("Korea", "韩国", "KR", "ko-KR"),
                new Market("Japan", "日本", "JP", "ja-JP"),
                new Market("United States","美国", "US", "en-US"),

                new Market("Argentina", "阿根廷", "AR", "es-AR"),
                new Market("United Arab Emirates", "阿联酋", "AE", "ar-AE"),
                new Market("Ireland", "爱尔兰", "IE", "en-IE"),
                new Market("Austria", "奥地利", "AT", "de-AT"),
                new Market("Austalia", "澳大利亚", "AU", "en-AU"),
                new Market("Brazil", "巴西", "BR", "pt-BR"),
                new Market("Belgium", "比利时", "BE", "nl-BE"),
                new Market("Poland", "波兰", "PL", "pl-PL"),
                new Market("Denmark", "丹麦", "DK", "da-DK"),
                new Market("Germany", "德国", "DE", "de-DE"),
                new Market("Russia", "俄罗斯", "RU", "ru-RU"),
                new Market("France", "法国", "FR", "fr-FR"),
                new Market("Finland", "芬兰", "FI", "fi-FI"),
                new Market("Colombia", "哥伦比亚", "CO", "es-CO"),
                //new Market("Korea", "韩国", "KR", "ko-KR"),
                new Market("Netherlands", "荷兰", "NL", "nl-NL"),
                new Market("Canada", "加拿大", "CA", "en-CA"),
                new Market("Czech Republic", "捷克共和国", "CZ", "cs-CZ"),
                //new Market("United States", "美国", "US", "en-US"),
                new Market("Mexico", "墨西哥", "MX", "es-MX"),
                new Market("South Africa", "南非", "ZA", "en-ZA"),
                new Market("Norway", "挪威", "NO", "nb-NO"),
                new Market("Portugal", "葡萄牙", "PT", "pt-PT"),
                //new Market("Japan", "日本", "JP", "ja-JP"),
                new Market("Sweden", "瑞典", "SE", "sv-SE"),
                new Market("Switzerland", "瑞士", "CH", "de-CH"),
                new Market("Saudi Arabia", "沙特阿拉伯", "SA", "ar-SA"),
                new Market("Slovakia", "斯洛伐克", "SK", "sk-SK"),
                //new Market("Taiwan", "台湾", "TW", "zh-TW"),
                new Market("Turkey", "土尔其", "TR", "tr-TR"),
                new Market("Spain", "西班牙", "ES", "es-ES"),
                new Market("Greece", "希腊", "GR", "el-GR"),
                //new Market("Hong Kong SAR", "香港", "HK", "zh-HK"),
                //new Market("Singapore", "新加坡", "SG", "en-SG"),
                new Market("New Zealand", "新西兰", "NZ", "en-NZ"),
                new Market("Hungary", "匈牙利", "HU", "hu-HU"),
                new Market("Israel", "以色列", "IL", "he-IL"),
                new Market("Italy", "意大利", "IT", "it-IT"),
                new Market("India", "印度", "IN", "en-IN"),
                new Market("United Kingdom", "英国", "GB", "en-GB"),
                new Market("Chile", "智利", "CL", "es-CL"),
                new Market("China", "中国", "CN", "zh-CN")
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

            if (File.Exists(resourcePath + "\\XboxGame.json"))
            {
                string json = File.ReadAllText(resourcePath + "\\XboxGame.json");
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

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DateTime.Compare(DateTime.Now, new DateTime(Properties.Settings.Default.NextUpdate)) >= 0)
            {
                tsmUpdate.Enabled = false;
                ThreadPool.QueueUserWorkItem(delegate { UpdateFile.Start(true, this); });
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
            Process.Start(new ProcessStartInfo("https://github.com/skydevil88/XboxDownload") { UseShellExecute = true });
        }

        private void TsmAbout_Click(object sender, EventArgs e)
        {
            FormAbout dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
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
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

        private void TsmiShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
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
                        GetEACdn();
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
        private void CkbNSBrowser_CheckedChanged(object sender, EventArgs e)
        {
            linkNSHomepage.Enabled = ckbNSBrowser.Checked;
        }

        private void LinkNSHomepage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormNSBH dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
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

        private void CkbDnsService_CheckedChanged(object sender, EventArgs e)
        {
            if (!ckbDnsService.Checked)
                ckbSetDns.Checked = false;
        }

        private void CkbGameLink_CheckedChanged(object? sender, EventArgs? e)
        {
            if (!ckbGameLink.Checked)
            {
                ckbLocalUpload.Checked = false;
            }
        }

        private void CkbLocalUpload_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbLocalUpload.Checked)
            {
                ckbGameLink.Checked = true;
            }
        }

        private void CkbSetDns_CheckedChanged(object? sender, EventArgs? e)
        {
            if (ckbSetDns.Checked)
            {
                ckbDnsService.Checked = true;
            }
        }

        private void CkbXboxStopped_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbXboxStopped.Checked)
            {
                string akamai = "223.119.50.144";
                dnsListen.ComIP = dnsListen.CnIP = dnsListen.AppIP = IPAddress.Parse(akamai).GetAddressBytes();
                UpdateHosts(true, akamai);
                if (Properties.Settings.Default.MicrosoftStore) RestartService("DoSvc");
                MessageBox.Show("Xbox安装停止通常是CDN缓存有坏块，勾选此选项将会临时把下载IP全部改为Akamai CDN，从国外下载损坏数据（关闭代理软件），下载几分钟后请手动取消此勾选。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (IPAddress.TryParse(tbComIP.Text, out IPAddress? comIP))
                    dnsListen.ComIP = comIP.GetAddressBytes();
                else
                    dnsListen.ComIP = null;
                if (IPAddress.TryParse(tbCnIP.Text, out IPAddress? cnIP))
                    dnsListen.CnIP = cnIP.GetAddressBytes();
                else
                    dnsListen.CnIP = null;
                if (IPAddress.TryParse(tbAppIP.Text, out IPAddress? appIP))
                    dnsListen.AppIP = appIP.GetAddressBytes();
                else
                    dnsListen.AppIP = null;
                UpdateHosts(true);
                if (Properties.Settings.Default.MicrosoftStore) RestartService("DoSvc");
            }
        }

        public void ButStart_Click(object? sender, EventArgs? e)
        {
            if (bServiceFlag)
            {
                butStart.Enabled = false;
                bServiceFlag = false;
                UpdateHosts(false);
                if (Properties.Settings.Default.SetDns) ClassDNS.SetDns(null);
                if (string.IsNullOrEmpty(Properties.Settings.Default.DnsIP)) tbDnsIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.ComIP)) tbComIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.CnIP)) tbCnIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.AppIP)) tbAppIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.PSIP)) tbPSIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.NSIP)) tbNSIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.EAIP)) tbEAIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.BattleIP)) tbBattleIP.Clear();
                if (string.IsNullOrEmpty(Properties.Settings.Default.EpicIP)) tbEpicIP.Clear();
                pictureBox1.Image = Properties.Resource.Xbox1;
                linkTestDns.Enabled = false;
                butStart.Text = "开始监听";
                foreach (Control control in this.groupBox1.Controls)
                {
                    if ((control is TextBox || control is CheckBox || control is Button || control is ComboBox) && control != butStart)
                        control.Enabled = true;
                }
                ckbXboxStopped.Checked = false;
                ckbXboxStopped.Enabled = false;
                cbLocalIP.Enabled = true;
                dnsListen.Close();
                httpListen.Close();
                httpsListen.Close();
                Program.SystemSleep.RestoreForCurrentThread();
                if (Properties.Settings.Default.MicrosoftStore) RestartService("DoSvc");
            }
            else
            {
                string? dnsIP = null;
                if (!string.IsNullOrEmpty(tbDnsIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbDnsIP.Text, out IPAddress? ipAddress) && !IPAddress.IsLoopback(ipAddress))
                    {
                        dnsIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("DNS 服务器 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbDnsIP.Focus();
                        return;
                    }
                }
                string? comIP = null;
                if (!string.IsNullOrEmpty(tbComIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbComIP.Text, out IPAddress? ipAddress))
                    {
                        comIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 com 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbComIP.Focus();
                        return;
                    }
                }
                string? cnIP = null;
                if (!string.IsNullOrEmpty(tbCnIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbCnIP.Text, out IPAddress? ipAddress))
                    {
                        cnIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 cn 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbCnIP.Focus();
                        return;
                    }
                }
                string? appIP = null;
                if (!string.IsNullOrEmpty(tbAppIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbAppIP.Text, out IPAddress? ipAddress))
                    {
                        appIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定应用下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbAppIP.Focus();
                        return;
                    }
                }
                string? psIP = null;
                if (!string.IsNullOrEmpty(tbPSIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbPSIP.Text, out IPAddress? ipAddress))
                    {
                        psIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 PS 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbPSIP.Focus();
                        return;
                    }
                }
                string? nsIP = null;
                if (!string.IsNullOrEmpty(tbNSIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbNSIP.Text, out IPAddress? ipAddress))
                    {
                        nsIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 NS 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbNSIP.Focus();
                        return;
                    }
                }
                string? eaIP = null;
                if (!string.IsNullOrEmpty(tbEAIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbEAIP.Text, out IPAddress? ipAddress))
                    {
                        eaIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 EA 下载域名 IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbEAIP.Focus();
                        return;
                    }
                }
                string? battleIP = null;
                if (!string.IsNullOrEmpty(tbBattleIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbBattleIP.Text, out IPAddress? ipAddress))
                    {
                        battleIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 战网国际服域名IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbBattleIP.Focus();
                        return;
                    }
                }
                string? epicIP = null;
                if (!string.IsNullOrEmpty(tbEpicIP.Text.Trim()))
                {
                    if (IPAddress.TryParse(tbEpicIP.Text, out IPAddress? ipAddress))
                    {
                        epicIP = ipAddress.ToString();
                    }
                    else
                    {
                        MessageBox.Show("指定 Epic 下载域名IP 不正确", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        tbEpicIP.Focus();
                        return;
                    }
                }
                butStart.Enabled = false;

                Properties.Settings.Default.DnsIP = dnsIP;
                Properties.Settings.Default.ComIP = comIP;
                Properties.Settings.Default.CnIP = cnIP;
                Properties.Settings.Default.AppIP = appIP;
                Properties.Settings.Default.PSIP = psIP;
                Properties.Settings.Default.NSIP = nsIP;
                Properties.Settings.Default.NSBrowser = ckbNSBrowser.Checked;
                Properties.Settings.Default.EAIP = eaIP;
                Properties.Settings.Default.EACDN = ckbEACDN.Checked;
                Properties.Settings.Default.BattleIP = battleIP;
                Properties.Settings.Default.BattleCDN = ckbBattleCDN.Checked;
                Properties.Settings.Default.EpicIP = epicIP;
                Properties.Settings.Default.EpicCDN = ckbEpicCDN.Checked;
                Properties.Settings.Default.GameLink = ckbGameLink.Checked;
                Properties.Settings.Default.Truncation = ckbTruncation.Checked;
                Properties.Settings.Default.LocalUpload = ckbLocalUpload.Checked;
                Properties.Settings.Default.LocalPath = tbLocalPath.Text;
                Properties.Settings.Default.ListenIP = cbListenIP.SelectedIndex;
                Properties.Settings.Default.DnsService = ckbDnsService.Checked;
                Properties.Settings.Default.HttpService = ckbHttpService.Checked;
                Properties.Settings.Default.DoH = ckbDoH.Checked;
                Properties.Settings.Default.SetDns = ckbSetDns.Checked;
                Properties.Settings.Default.MicrosoftStore = ckbMicrosoftStore.Checked;
                Properties.Settings.Default.EAStore = ckbEAStore.Checked;
                Properties.Settings.Default.BattleStore = ckbBattleStore.Checked;
                Properties.Settings.Default.EpicStore = ckbEpicStore.Checked;
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
                    if (!dic.IsEmpty && MessageBox.Show("检测到以下端口被占用\n" + sb.ToString() + "\n是否尝试强制结束占用端口程序？", "启用服务失败", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
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
                    if (control is TextBox || control is CheckBox || control is Button || control is ComboBox)
                        control.Enabled = false;
                }
                ckbXboxStopped.Enabled = true;
                cbLocalIP.Enabled = false;
                Task.Run(() =>
                {
                    using HttpResponseMessage? response = ClassWeb.HttpResponseMessage("https://ipv6.lookup.test-ipv6.com/", "HEAD");
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        SaveLog("提示信息", "检测到使用IPv6联网，如果用在Xbox主机下载加速，必需关闭。", "localhost", 0x0000FF);
                    }
                });
                UpdateHosts(true);
                if (Properties.Settings.Default.MicrosoftStore) RestartService("DoSvc");
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
            butStart.Enabled = true;
        }

        private void UpdateHosts(bool add, string? xboxIp = null)
        {
            if (add)
            {
                DnsListen.dicHosts1.Clear();
                DnsListen.dicHosts2.Clear();
                DataTable dt = dtHosts.Clone();
                if (File.Exists(resourcePath + "\\Hosts.xml"))
                {
                    try
                    {
                        dt.ReadXml(resourcePath + "\\Hosts.xml");
                    }
                    catch { }
                }
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr.RowState == DataRowState.Deleted) continue;
                    if (Convert.ToBoolean(dr["Enable"]))
                    {
                        string? hostName = dr["HostName"].ToString()?.Trim().ToLower();
                        if (!string.IsNullOrEmpty(hostName) && IPAddress.TryParse(dr["IPv4"].ToString()?.Trim(), out IPAddress? ip))
                        {
                            if (hostName.StartsWith("*."))
                            {
                                hostName = Regex.Replace(hostName, @"^\*\.", "");
                                Regex re = new("\\." + hostName.Replace(".", "\\.") + "$");
                                if (!DnsListen.dicHosts2.ContainsKey(re) && DnsListen.reHosts.IsMatch(hostName))
                                {
                                    List<ResouceRecord> lsIp = new()
                                    {
                                        new ResouceRecord
                                        {
                                            Datas = ip.GetAddressBytes(),
                                            TTL = 100,
                                            QueryClass = 1,
                                            QueryType = QueryType.A
                                        }
                                    };
                                    DnsListen.dicHosts2.TryAdd(re, lsIp);
                                }
                            }
                            else if (!DnsListen.dicHosts1.ContainsKey(hostName) && DnsListen.reHosts.IsMatch(hostName))
                            {
                                List<ResouceRecord> lsIp = new()
                                {
                                    new ResouceRecord
                                    {
                                        Datas = ip.GetAddressBytes(),
                                        TTL = 100,
                                        QueryClass = 1,
                                        QueryType = QueryType.A
                                    }
                                };
                                DnsListen.dicHosts1.TryAdd(hostName, lsIp);
                            }
                        }
                    }
                }
            }

            if (!(Properties.Settings.Default.MicrosoftStore || Properties.Settings.Default.EAStore || Properties.Settings.Default.BattleStore || Properties.Settings.Default.EpicStore)) return;

            StringBuilder sb = new();
            string sHostsPath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
            try
            {
                FileInfo fi = new(sHostsPath);
                if (!fi.Exists)
                {
                    StreamWriter sw = fi.CreateText();
                    sw.Close();
                    fi.Refresh();
                }
                FileSecurity fSecurity = fi.GetAccessControl();
                fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    fi.Attributes = FileAttributes.Normal;
                string sHosts = string.Empty;
                using (StreamReader sw = new(sHostsPath))
                {
                    sHosts = sw.ReadToEnd();
                }
                sHosts = Regex.Replace(sHosts, @"# Added by (XboxDownload|Xbox下载助手)\r\n(.*\r\n)+# End of (XboxDownload|Xbox下载助手)\r\n", "");
                if (add)
                {
                    string comIP = string.IsNullOrEmpty(Properties.Settings.Default.ComIP) ? Properties.Settings.Default.LocalIP : Properties.Settings.Default.ComIP;
                    if (!Properties.Settings.Default.DnsService && Properties.Settings.Default.HttpService && Properties.Settings.Default.MicrosoftStore && string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
                        tbComIP.Text = comIP;
                    sb.AppendLine("# Added by XboxDownload");
                    if (Properties.Settings.Default.MicrosoftStore)
                    {
                        string cnIP, appIP;
                        if (string.IsNullOrEmpty(xboxIp))
                        {
                            cnIP = Properties.Settings.Default.CnIP;
                            appIP = Properties.Settings.Default.AppIP;
                        }
                        else
                        {
                            comIP = cnIP = appIP = xboxIp;
                        }
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
                            if (!string.IsNullOrEmpty(cnIP))
                            {
                                sb.AppendLine(cnIP + " assets2.xboxlive.cn");
                                sb.AppendLine(cnIP + " d2.xboxlive.cn");
                            }
                            if (!string.IsNullOrEmpty(appIP))
                            {
                                sb.AppendLine(appIP + " dlassets2.xboxlive.cn");
                            }
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
                            if (!string.IsNullOrEmpty(cnIP))
                            {
                                sb.AppendLine(cnIP + " assets1.xboxlive.cn");
                                sb.AppendLine(cnIP + " assets2.xboxlive.cn");
                                sb.AppendLine(cnIP + " d1.xboxlive.cn");
                                sb.AppendLine(cnIP + " d2.xboxlive.cn");
                            }
                            if (!string.IsNullOrEmpty(appIP))
                            {
                                sb.AppendLine(appIP + " dlassets.xboxlive.cn");
                                sb.AppendLine(appIP + " dlassets2.xboxlive.cn");
                            }
                        }
                        if (!string.IsNullOrEmpty(appIP))
                        {
                            sb.AppendLine(appIP + " dl.delivery.mp.microsoft.com");
                            sb.AppendLine(appIP + " tlu.dl.delivery.mp.microsoft.com");
                        }
                        if (Properties.Settings.Default.HttpService)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " www.msftconnecttest.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " packagespc.xboxlive.com");
                        }
                    }
                    if (Properties.Settings.Default.EAStore)
                    {
                        if (Properties.Settings.Default.EACDN)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " api1.origin.com");
                            sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                        }
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                        {
                            sb.AppendLine(Properties.Settings.Default.EAIP + " origin-a.akamaihd.net");
                        }
                    }
                    if (Properties.Settings.Default.BattleStore)
                    {
                        if (Properties.Settings.Default.BattleCDN)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " us.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " eu.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " kr.cdn.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " level3.blizzard.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " blizzard.gcdn.cloudn.co.kr");
                        }
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                        {
                            sb.AppendLine(Properties.Settings.Default.BattleIP + " blzddist1-a.akamaihd.net");
                            sb.AppendLine(Properties.Settings.Default.BattleIP + " blzddist2-a.akamaihd.net");
                            sb.AppendLine(Properties.Settings.Default.BattleIP + " blzddist3-a.akamaihd.net");
                        }
                    }
                    if (Properties.Settings.Default.EpicStore)
                    {
                        if (Properties.Settings.Default.EpicCDN)
                        {
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " epicgames-download1.akamaized.net");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download2.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download3.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " download4.epicgames.com");
                            sb.AppendLine(Properties.Settings.Default.LocalIP + " fastly-download.epicgames.com");
                        }
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
                        {
                            sb.AppendLine(Properties.Settings.Default.EpicIP + " epicgames -download1-1251447533.file.myqcloud.com");
                        }
                    }
                    foreach (var item in DnsListen.dicHosts1)
                    {
                        if (item.Key == Environment.MachineName)
                            continue;
                        byte[]? b = item.Value[0].Datas;
                        if (b != null) sb.AppendLine(new IPAddress(b) + " " + item.Key);
                    }
                    sb.AppendLine("# End of XboxDownload");
                    sHosts = sb.ToString() + sHosts;
                }
                using (StreamWriter sw = new(sHostsPath, false))
                {
                    sw.Write(sHosts.Trim() + "\r\n");
                }
                fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
            }
            catch (Exception ex)
            {
                if (add) MessageBox.Show("修改系统Hosts文件失败，错误信息：" + ex.Message + "\n\n解决方法：手动删除\"" + Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\drivers\\etc\\hosts\"文件，点击开始监听会新建一个。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                tsmConnectTest.Visible = lvLog.SelectedItems[0].SubItems[0].Text == "DNS 查询" && Regex.IsMatch(lvLog.SelectedItems[0].SubItems[1].Text, @" -> \d+.\d+.\d+.\d+");
                cmsLog.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void LvLog_DoubleClick(object sender, EventArgs e)
        {
            if (lvLog.SelectedItems.Count == 1)
            {
                string[] strArray = Regex.Split(lvLog.SelectedItems[0].SubItems[1].Text, " -> ");
                if (strArray.Length != 2) return;
                FormConnectTest dialog = new(strArray[0], strArray[1]);
                dialog.ShowDialog();
                dialog.Dispose();
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

        private void TsmConnectTest_Click(object sender, EventArgs e)
        {
            string[] strArray = Regex.Split(lvLog.SelectedItems[0].SubItems[1].Text, " -> ");
            if (strArray.Length != 2) return;
            FormConnectTest dialog = new(strArray[0], strArray[1]);
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void CbLocalIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LocalIP = cbLocalIP.Text;
            Properties.Settings.Default.Save();
        }

        private void LinkTestDns_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormDns dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
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
            string host = string.Empty;
            Match result = Regex.Match(gbIPList.Text, @"\((?<host>[^\)]+)\)");
            if (result.Success) host = result.Groups["host"].Value;
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
                case "xvcf1.xboxlive.com":
                case "xvcf2.xboxlive.com":
                case "assets1.xboxlive.com":
                case "assets2.xboxlive.com":
                case "d1.xboxlive.com":
                case "d2.xboxlive.com":
                case "dlassets.xboxlive.com":
                case "dlassets2.xboxlive.com":
                    tsmUseIPCom.Visible = true;
                    tsmUseIPApp.Visible = true;
                    break;
                case "assets1.xboxlive.cn":
                case "assets2.xboxlive.cn":
                case "d1.xboxlive.cn":
                case "d2.xboxlive.cn":
                    tsmUseIPCn.Visible = true;
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    tsmUseIPApp.Visible = true;
                    break;
                case "gst.prod.dl.playstation.net":
                case "gs2.ww.prod.dl.playstation.net":
                case "zeus.dl.playstation.net":
                case "ares.dl.playstation.net":
                    tsmUseIPPS.Visible = true;
                    break;
                case "Akamai":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "atum.hac.lp1.d4c.nintendo.net":
                    tssUseIP1.Visible = true;
                    tsmUseIPCom.Visible = true;
                    tsmUseIPXbox.Visible = true;
                    tsmUseIPApp.Visible = true;
                    tsmUseIPNS.Visible = true;
                    tsmUseIPEa.Visible = true;
                    tsmUseAkamai.Visible = true;
                    tsmUseIPBattle.Visible = true;
                    break;
                case "epicgames-download1-1251447533.file.myqcloud.com":
                    tsmUseIPEpic.Visible = true;
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
            string? ip = dgvr.Cells["Col_IP"].Value?.ToString();
            if (ip == null) return;
            if (sender is not ToolStripMenuItem tsmi) return;
            if (bServiceFlag && tsmi.Name != "tsmUseAkamai")
            {
                MessageBox.Show("请先停止监听后再设置。", "使用指定IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            switch (tsmi.Name)
            {
                case "tsmUseIPCn":
                    tabControl1.SelectedTab = tabService;
                    tbCnIP.Text = ip;
                    tbCnIP.Focus();
                    break;
                case "tsmUseIPCom":
                    tabControl1.SelectedTab = tabService;
                    tbComIP.Text = ip;
                    tbComIP.Focus();
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
                    tbEpicIP.Text = ip;
                    tbEpicIP.Focus();
                    break;
                case "tsmUseAkamai":
                    tabControl1.SelectedTab = tabCND;
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

            string host = string.Empty;
            switch (cbImportIP.SelectedIndex)
            {
                case 1:
                    host = "assets1.xboxlive.cn";
                    break;
                case 2:
                    host = "tlu.dl.delivery.mp.microsoft.com";
                    break;
                case 3:
                    host = "gst.prod.dl.playstation.net";
                    break;
                case 4:
                    host = "Akamai";
                    break;
            }
            dgvIpList.Tag = host;
            gbIPList.Text = "IP 列表 (" + host + ")";

            bool update = true;
            FileInfo fi = new(resourcePath + "\\IP." + host + ".txt");
            if (fi.Exists && fi.Length >= 1) update = DateTime.Compare(DateTime.Now, fi.LastWriteTime.AddHours(24)) >= 0;
            if (update)
            {
                await UpdateFile.Download(fi);
            }
            string content = string.Empty;
            if (fi.Exists)
            {
                using StreamReader sr = fi.OpenText();
                content = sr.ReadToEnd();
            }

            List<DataGridViewRow> list = new();
            Match result = Regex.Match(content, @"(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*\((?<Location>[^\)]+)\)|(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})(?<Location>.+)\dms|^\s*(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*$", RegexOptions.Multiline);
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
                    if (location.Contains("香港"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("台湾"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("日本"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("韩国"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("新加坡"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "电信|联通|移动|香港|台湾|日本|韩国|新加坡"))
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
                            Parent = this.flpTestUrl
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        string[,] games = new string[,]
                        {
                            {"光环:无限(XS)", "0698b936-d300-4451-b9a0-0be0514bbbe5", "/12/ba393baf-afe2-4f1f-926b-8ebdc88afdbc/0698b936-d300-4451-b9a0-0be0514bbbe5/1.3932.60255.0.2713b0ec-ed32-42c9-bed7-b25f550f6ce6/Microsoft.254428597CFE2_1.3932.60255.0_neutral__8wekyb3d8bbwe_xs.xvc" },
                            {"极限竞速:地平线5(PC)", "3d263e92-93cd-4f9b-90c7-5438150cecbf", "/5/5cb6d65f-eca7-44a7-95f7-80eb28da2236/3d263e92-93cd-4f9b-90c7-5438150cecbf/3.619.349.0.cd216d63-6229-4ce4-8ba5-f7cbaee352eb/Microsoft.624F8B84B80_3.619.349.0_x64__8wekyb3d8bbwe.msixvc" },
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
                                Parent = this.flpTestUrl
                            };
                            lb.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        }
                        _ = new Label()
                        {
                            ForeColor = Color.Green,
                            Text = "主下载域名(PC主机共用)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                    }
                    break;
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "986a47b3-0085-4c0c-b3b3-3b806f969b00|MsixBundle|9MV0B5HZVK9Z",
                            Text = "Xbox(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "64293252-5926-453c-9494-2d4021f1c78d|MsixBundle|9WZDNCRFJBMP",
                            Text = "微软商店(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "e0229546-200d-4c66-a693-df9bf799635f|EAppxBundle|9PNQKHFLD2WQ",
                            Text = "地平线4(PC)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://dlassets.xboxlive.cn/public/content/77d0d59a-34b7-4482-a1c7-c0abbed17de2/db7a9163-9c5e-43a8-b8bf-fe0208149792/1.0.0.3.65565c9c-8a1e-438a-b714-2d9965f0485b/ChildOfLight_1.0.0.3_x64__b6krnev7r9sf8",
                            Text = "光之子(XboxOne)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        _ = new Label()
                        {
                            ForeColor = Color.Green,
                            Text = "应用和部分游戏使用此域名下载",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
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
                            Parent = this.flpTestUrl
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://gst.prod.dl.playstation.net/gst/prod/00/PPSA04478_00/app/pkg/26/f_f2e4ff2bc3be11cb844dfe2a7ff8df357d7930152fb5984294a794823ec7472b/EP1464-PPSA04478_00-XXXXXXXXXXXXXXXX_0.pkg",
                            Text = "糖豆人(PS5)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://gs2.ww.prod.dl.playstation.net/gs2/appkgo/prod/CUSA03962_00/4/f_526a2fab32d369a8ca6298b59686bf823fa9edfe95acb85bc140c27f810842ce/f/UP0102-CUSA03962_00-BH70000000000001_0.pkg",
                            Text = "生化危机7(PS4)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://zeus.dl.playstation.net/cdn/UP1004/NPUB31154_00/eISFknCNDxqSsVVywSenkJdhzOIfZjrqKHcuGBHEGvUxQJksdPvRNYbIyWcxFsvH.pkg",
                            Text = "侠盗猎车手5(PS3)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb5 = new()
                        {
                            Tag = "http://ares.dl.playstation.net/cdn/JP0102/PCSG00350_00/fMBmIgPfrBTVSZCRQFevSzxaPyzFWOuorSKrvdIjDIJwmaGLjpTmRgzLLTJfASFYZMqEpwSknlWocYelXNHMkzXvpbbvtCSymAwWF.pkg",
                            Text = "怪物猎人:边境G(PSV)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb5.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                    }
                    break;
                case "Akamai":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
                case "atum.hac.lp1.d4c.nintendo.net":
                    {
                        LinkLabel lb1 = new()
                        {
                            Tag = "http://ctest-dl-lp1.cdn.nintendo.net/30m",
                            Text = "Switch测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb2 = new()
                        {
                            Tag = "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt",
                            Text = "Xbox测速文件",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb2.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb3 = new()
                        {
                            Tag = "http://origin-a.akamaihd.net/Origin-Client-Download/origin/live/OriginThinSetup.exe",
                            Text = "Origin(EA)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb3.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                        LinkLabel lb4 = new()
                        {
                            Tag = "http://blzddist1-a.akamaihd.net/tpr/odin/data/e9/07/e9079f76b9939f279dd2cb04f3b28143",
                            Text = "Call of Duty: Warzone(战网)",
                            AutoSize = true,
                            Parent = this.flpTestUrl
                        };
                        lb4.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LinkTestUrl_LinkClicked);
                    }
                    break;
            }
        }

        private void GetAppUrl(string wuCategoryId, string extension, CancellationToken? cts = null)
        {
            SetTextBox(tbDlUrl, "正在获取下载链接，请稍候...");
            string? url = null;
            for (int i = 0; i <= 2; i++)
            {
                string html = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload", cts);
                if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
                {
                    XboxPackage.App? json = null;
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.App>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { }
                    if (json != null && json.Code != null && json.Code == "200")
                    {
                        url = json.Data?.Where(x => (x.Name ?? string.Empty).ToLower().EndsWith("." + extension)).Select(x => x.Url).FirstOrDefault();
                    }
                }
                if (!string.IsNullOrEmpty(url)) break;
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
                        if (location.Contains("香港"))
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
                        if (!Regex.IsMatch(location, "电信|联通|移动|香港|台湾|日本|韩国|新加坡"))
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
                        sb.AppendLine(dgvr.Cells["Col_IP"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")\t" + dgvr.Cells["Col_TTL"].Value + "|" + dgvr.Cells["Col_RoundtripTime"].Value + "|" + dgvr.Cells["Col_Speed"].Value);
                    else
                        sb.AppendLine(dgvr.Cells["Col_IP"].Value + "\t(" + dgvr.Cells["Col_Location"].Value + ")");
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
                    string? location = dr["Location"].ToString();
                    if (location == null) continue;
                    DataGridViewRow dgvr = new();
                    dgvr.CreateCells(dgvIpList);
                    dgvr.Resizable = DataGridViewTriState.False;
                    if (location.Contains("电信"))
                        dgvr.Cells[0].Value = ckbChinaTelecom.Checked;
                    if (location.Contains("联通"))
                        dgvr.Cells[0].Value = ckbChinaUnicom.Checked;
                    if (location.Contains("移动"))
                        dgvr.Cells[0].Value = ckbChinaMobile.Checked;
                    if (location.Contains("香港"))
                        dgvr.Cells[0].Value = ckbHK.Checked;
                    if (location.Contains("台湾"))
                        dgvr.Cells[0].Value = ckbTW.Checked;
                    if (location.Contains("日本"))
                        dgvr.Cells[0].Value = ckbJapan.Checked;
                    if (location.Contains("韩国"))
                        dgvr.Cells[0].Value = ckbKorea.Checked;
                    if (location.Contains("新加坡"))
                        dgvr.Cells[0].Value = ckbSG.Checked;
                    if (!Regex.IsMatch(location, "电信|联通|移动|香港|台湾|日本|韩国|新加坡"))
                        dgvr.Cells[0].Value = ckbOther.Checked;
                    dgvr.Cells[1].Value = dr["IP"];
                    dgvr.Cells[2].Value = dr["Location"];
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
            string? ip = dgvr.Cells["Col_IP"].Value.ToString();

            string sHostsPath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
            try
            {
                FileInfo fi = new(sHostsPath);
                if (!fi.Exists)
                {
                    StreamWriter sw = fi.CreateText();
                    sw.Close();
                    fi.Refresh();
                }
                FileSecurity fSecurity = fi.GetAccessControl();
                fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    fi.Attributes = FileAttributes.Normal;
                string sHosts = string.Empty;
                using (StreamReader sw = new(sHostsPath))
                {
                    sHosts = sw.ReadToEnd();
                }
                StringBuilder sb = new();
                string msg = string.Empty;
                switch (host)
                {
                    case "assets1.xboxlive.cn":
                    case "assets2.xboxlive.cn":
                    case "d1.xboxlive.cn":
                    case "d2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+(assets1|assets2|d1|d2)\.xboxlive\.cn\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " assets1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " assets2.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d1.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " d2.xboxlive.cn # XboxDownload");
                        msg = "\nXbox、PC商店游戏下载可能会使用com域名，只写入cn域名加速不一定有效。";
                        RestartService("DoSvc");
                        break;
                    case "dl.delivery.mp.microsoft.com":
                    case "tlu.dl.delivery.mp.microsoft.com":
                    case "dlassets.xboxlive.cn":
                    case "dlassets2.xboxlive.cn":
                        sHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+[^\s]+\.delivery\.mp\.microsoft\.com\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com # XboxDownload");
                        sb.AppendLine(ip + " dlassets.xboxlive.cn # XboxDownload");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn # XboxDownload");
                        RestartService("DoSvc");
                        break;
                    case "gst.prod.dl.playstation.net":
                    case "gs2.ww.prod.dl.playstation.net":
                    case "zeus.dl.playstation.net":
                    case "ares.dl.playstation.net":
                        sHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+[^\s]+\.dl\.playstation\.net\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " gst.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " gs2.ww.prod.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " zeus.dl.playstation.net # XboxDownload");
                        sb.AppendLine(ip + " ares.dl.playstation.net # XboxDownload");
                        break;
                    case "Akamai":
                    case "atum.hac.lp1.d4c.nintendo.net":
                    case "origin-a.akamaihd.net":
                    case "blzddist1-a.akamaihd.net":
                        sHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+[^\s]+(\.xboxlive\.com|\.delivery\.mp\.microsoft\.com|\.nintendo\.net|\.cdn\.ea\.com|\.akamaihd\.net)\s+# (XboxDownload|Xbox下载助手)\r\n", "");
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
                        sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net # XboxDownload");
                        sb.AppendLine(ip + " bugyo.hac.lp1.eshop.nintendo.net # XboxDownload");
                        sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net # XboxDownload");
                        sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net # XboxDownload");
                        sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net # XboxDownload");
                        sb.AppendLine(ip + " origin-a.akamaihd.net # XboxDownload");
                        sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com # XboxDownload");
                        sb.AppendLine(ip + " blzddist1-a.akamaihd.net # XboxDownload");
                        msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\n暴雪战网只能用监听方式加速。";
                        break;
                    default:
                        sHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+" + host + @"\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                        sb.AppendLine(ip + " " + host + " # XboxDownload");
                        break;
                }
                using (StreamWriter sw = new(sHostsPath, false))
                {
                    sw.Write(sHosts.Trim() + "\r\n" + sb.ToString());
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
            string? ip = dgvr.Cells["Col_IP"].Value.ToString();
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
                case "dl.delivery.mp.microsoft.com":
                case "tlu.dl.delivery.mp.microsoft.com":
                case "dlassets.xboxlive.cn":
                case "dlassets2.xboxlive.cn":
                    if (tsmi.Name == "tsmDNSmasp")
                    {
                        sb.AppendLine("address=/dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/tlu.dl.delivery.mp.microsoft.com/" + ip);
                        sb.AppendLine("address=/dlassets.xboxlive.cn/" + ip);
                        sb.AppendLine("address=/dlassets2.xboxlive.cn/" + ip);
                    }
                    else
                    {
                        sb.AppendLine(ip + " dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " tlu.dl.delivery.mp.microsoft.com");
                        sb.AppendLine(ip + " dlassets.xboxlive.cn");
                        sb.AppendLine(ip + " dlassets2.xboxlive.cn");
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
                case "atum.hac.lp1.d4c.nintendo.net":
                case "origin-a.akamaihd.net":
                case "blzddist1-a.akamaihd.net":
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
                        sb.AppendLine("# Nintendo Switch");
                        sb.AppendLine("address=/atum.hac.lp1.d4c.nintendo.net/" + ip);
                        sb.AppendLine("address=/bugyo.hac.lp1.eshop.nintendo.net/" + ip);
                        sb.AppendLine("address=/ctest-ul-lp1.cdn.nintendo.net/" + ip);
                        sb.AppendLine("address=/ctest-dl-lp1.cdn.nintendo.net/" + ip);
                        sb.AppendLine("address=/atum-eda.hac.lp1.d4c.nintendo.net/0.0.0.0");
                        sb.AppendLine();
                        sb.AppendLine("# EA、战网国际服");
                        sb.AppendLine("address=/origin-a.akamaihd.net/" + ip);
                        sb.AppendLine("address=/ssl-lvlt.cdn.ea.com/0.0.0.0");
                        sb.AppendLine("address=/blzddist1-a.akamaihd.net/" + ip);
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
                        sb.AppendLine("# Nintendo Switch");
                        sb.AppendLine(ip + " atum.hac.lp1.d4c.nintendo.net");
                        sb.AppendLine(ip + " bugyo.hac.lp1.eshop.nintendo.net");
                        sb.AppendLine(ip + " ctest-ul-lp1.cdn.nintendo.net");
                        sb.AppendLine(ip + " ctest-dl-lp1.cdn.nintendo.net");
                        sb.AppendLine("0.0.0.0 atum-eda.hac.lp1.d4c.nintendo.net");
                        sb.AppendLine();
                        sb.AppendLine("# EA、战网国际服");
                        sb.AppendLine(ip + " origin-a.akamaihd.net");
                        sb.AppendLine("0.0.0.0 ssl-lvlt.cdn.ea.com");
                        sb.AppendLine(ip + " blzddist1-a.akamaihd.net");
                    }
                    msg = "\nOrigin 的用户可以在“工具 -> EA Origin 切换CDN服务器”中指定使用 Akamai。\n\n暴雪战网只能用监听方式加速。";
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
            Col_IP.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
            ThreadPool.QueueUserWorkItem(delegate { SpeedTest(ls); });
        }

        private void TsmSpeedTestLog_Click(object sender, EventArgs e)
        {
            if (dgvIpList.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvIpList.SelectedRows[0];
            if (dgvr.Tag != null) MessageBox.Show(dgvr.Tag.ToString(), "Request Headers", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void LinkHostsClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sHostsPath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
            try
            {
                FileInfo fi = new(sHostsPath);
                if (!fi.Exists)
                {
                    StreamWriter sw = fi.CreateText();
                    sw.Close();
                    fi.Refresh();
                }
                FileSecurity fSecurity = fi.GetAccessControl();
                fSecurity.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    fi.Attributes = FileAttributes.Normal;
                string sHosts = string.Empty;
                using (StreamReader sw = new(sHostsPath))
                {
                    sHosts = sw.ReadToEnd();
                }
                string newHosts = Regex.Replace(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+.+\s+# (XboxDownload|Xbox下载助手)\r\n", "");
                if (String.Equals(sHosts, newHosts))
                {
                    MessageBox.Show("Hosts文件没有写入任何规则，无需清除。\n\n注：只清除Xbox下载助手写入的规则。", "清除系统Hosts文件", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
                else
                {
                    StringBuilder sb = new();
                    Match result = Regex.Match(sHosts, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\s+.+\s+# (XboxDownload|Xbox下载助手)\r\n");
                    while (result.Success)
                    {
                        sb.Append(result.Groups[0].Value);
                        result = result.NextMatch();
                    }
                    if (MessageBox.Show("是否确认清除以下写入规则？\n\n" + sb.ToString(), "清除系统Hosts文件", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        using StreamWriter sw = new(sHostsPath, false);
                        sw.Write(newHosts.Trim() + "\r\n");
                    }
                }
                fSecurity.RemoveAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                fi.SetAccessControl(fSecurity);
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
                    if ((control is TextBox || control is CheckBox || control is Button || control is ComboBox || control is LinkLabel || control is FlowLayoutPanel) && control != butSpeedTest)
                        control.Enabled = false;
                }
                Col_IP.SortMode = Col_Location.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = Col_Speed.SortMode = DataGridViewColumnSortMode.NotSortable;
                Col_Check.ReadOnly = true;
                var timeout = cbSpeedTestTimeOut.SelectedIndex switch
                {
                    1 => 45000,
                    2 => 60000,
                    _ => 30000,
                };
                Thread threadS = new(new ThreadStart(() =>
                {
                    SpeedTest(ls, timeout);
                }))
                {
                    IsBackground = true
                };
                threadS.Start();
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
            if (uri != null)
            {
                int range = 104857599;                  //100M
                //if (Form1.debug) range = 1048575;     //1M

                string userAgent = uri.Host.EndsWith(".nintendo.net") ? "XboxDownload/1.0 (Nintendo NX)" : "XboxDownload/1.0";
                Stopwatch sw = new();
                if (uri.Host.EndsWith(".dl.playstation.net"))
                {
                    foreach (DataGridViewRow dgvr in ls)
                    {
                        if (ctsSpeedTest.IsCancellationRequested) break;
                        string? ip = dgvr.Cells["Col_IP"].Value.ToString();
                        if (string.IsNullOrEmpty(ip)) continue;
                        dgvr.Cells["Col_302"].Value = false;
                        dgvr.Cells["Col_TTL"].Value = null;
                        dgvr.Cells["Col_RoundtripTime"].Value = null;
                        dgvr.Cells["Col_Speed"].Value = "正在测试";
                        dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                        dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                        dgvr.Tag = null;

                        using (Ping p1 = new())
                        {
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
                        }

                        Uri uri2 = new(uri.Scheme + "://" + ip + ":" + uri.Port + "/" + uri.Host + uri.PathAndQuery);
                        StringBuilder sb = new();
                        sb.AppendLine("GET " + uri2.PathAndQuery + " HTTP/1.1");
                        sb.AppendLine("Host: " + uri2.Host);
                        sb.AppendLine("User-Agent: " + userAgent);
                        sb.AppendLine("Range: bytes=0-" + range);
                        sb.AppendLine();
                        byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());

                        sw.Restart();
                        SocketPackage socketPackage = uri.Scheme == "https" ? ClassWeb.TlsRequest(uri2, buffer, null, false, null, timeout, ctsSpeedTest) : ClassWeb.TcpRequest(uri2, buffer, null, false, null, timeout, ctsSpeedTest);
                        sw.Stop();
                        dgvr.Tag = string.IsNullOrEmpty(socketPackage.Err) ? socketPackage.Headers : socketPackage.Err;
                        if (socketPackage.Headers.StartsWith("HTTP/1.1 206"))
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
                    }
                }
                else
                {
                    StringBuilder sb = new();
                    sb.AppendLine("GET " + uri.PathAndQuery + " HTTP/1.1");
                    sb.AppendLine("Host: " + uri.Host);
                    sb.AppendLine("User-Agent: " + userAgent);
                    sb.AppendLine("Range: bytes=0-" + range);
                    sb.AppendLine();
                    byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());

                    foreach (DataGridViewRow dgvr in ls)
                    {
                        if (ctsSpeedTest.IsCancellationRequested) break;
                        string? ip = dgvr.Cells["Col_IP"].Value.ToString();
                        if (string.IsNullOrEmpty(ip)) continue;
                        dgvr.Cells["Col_302"].Value = false;
                        dgvr.Cells["Col_TTL"].Value = null;
                        dgvr.Cells["Col_RoundtripTime"].Value = null;
                        dgvr.Cells["Col_Speed"].Value = "正在测试";
                        dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                        dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                        dgvr.Tag = null;

                        using (Ping p1 = new())
                        {
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
                        }
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
                                dgvr.Tag = socketPackage.Headers + "===============临时性重定向(跳转)===============\n" + uri2.OriginalString + "\n\n";
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
                        if (socketPackage.Headers.StartsWith("HTTP/1.1 206"))
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
                    }
                }
            }
            else
            {
                foreach (DataGridViewRow dgvr in ls)
                {
                    if (ctsSpeedTest.IsCancellationRequested) break;
                    string? ip = dgvr.Cells["Col_IP"].Value.ToString();
                    if (string.IsNullOrEmpty(ip)) continue;
                    dgvr.Cells["Col_302"].Value = false;
                    dgvr.Cells["Col_TTL"].Value = null;
                    dgvr.Cells["Col_RoundtripTime"].Value = null;
                    dgvr.Cells["Col_Speed"].Value = "正在测试";
                    dgvr.Cells["Col_RoundtripTime"].Style.ForeColor = Color.Empty;
                    dgvr.Cells["Col_Speed"].Style.ForeColor = Color.Empty;
                    dgvr.Tag = null;

                    using (Ping p1 = new())
                    {
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
                    }
                    dgvr.Cells["Col_Speed"].Value = null;
                }
            }
            GC.Collect();
            ctsSpeedTest = null;
            this.Invoke(new Action(() =>
            {
                butSpeedTest.Text = "开始测速";
                foreach (Control control in this.panelSpeedTest.Controls)
                {
                    if (control is TextBox || control is CheckBox || control is Button || control is ComboBox || control is LinkLabel || control is FlowLayoutPanel)
                        control.Enabled = true;
                }
                Col_IP.SortMode = Col_Location.SortMode = Col_Speed.SortMode = Col_TTL.SortMode = Col_RoundtripTime.SortMode = DataGridViewColumnSortMode.Automatic;
                Col_Check.ReadOnly = false;
                butSpeedTest.Enabled = true;
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
                case "Col_HostName":
                    string? hostName = e.FormattedValue.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(hostName))
                    {
                        if (hostName.StartsWith("*."))
                        {
                            if (!DnsListen.reHosts.IsMatch(Regex.Replace(hostName, @"^\*\.", "")))
                            {
                                e.Cancel = true;
                                dgvHosts.Rows[e.RowIndex].ErrorText = "域名格式不正确";
                            }
                        }
                        else if (!DnsListen.reHosts.IsMatch(Regex.Replace((e.FormattedValue.ToString() ?? string.Empty).Trim().ToLower(), @"^(https?://)?([^/|:|\s]+).*$", "$2")))
                        {
                            e.Cancel = true;
                            dgvHosts.Rows[e.RowIndex].ErrorText = "域名格式不正确";
                        }
                    }
                    break;
                case "Col_IPv4":
                    if (!string.IsNullOrWhiteSpace(e.FormattedValue.ToString()))
                    {
                        if (!IPAddress.TryParse(e.FormattedValue.ToString()?.Trim(), out _))
                        {
                            e.Cancel = true;
                            dgvHosts.Rows[e.RowIndex].ErrorText = "不是有效IPv4地址";
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
                case "Col_IPv4":
                    dgvHosts.CurrentCell.Value = dgvHosts.CurrentCell.FormattedValue.ToString()?.Trim();
                    break;
            }
        }

        private void DgvHosts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            DataGridViewRow dgvr = dgvHosts.Rows[e.RowIndex];
            string? hostName = dgvr.Cells["Col_HostName"].Value?.ToString();
            string? ip = dgvr.Cells["Col_IPv4"].Value?.ToString();
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
            Match result = Regex.Match(cbHosts.Text, @"(?<remark>[^\(]+)(\((?<hostname>.+)\))?");
            if (result.Success)
            {
                string remark = result.Groups["remark"].Value;
                if (remark == "Xbox360主机本地上传")
                {
                    string[] hostNames = new string[] { "download.xbox.com", "download.xbox.com.edgesuite.net", "xbox-ecn102.vo.msecnd.net" };
                    foreach (string hostName in hostNames)
                    {
                        DataRow[] rows = dtHosts.Select("HostName='" + hostName + "'");
                        if (rows.Length >= 1)
                        {
                            rows[0]["Enable"] = true;
                            rows[0]["IPv4"] = Properties.Settings.Default.LocalIP;
                            rows[0]["Remark"] = "Xbox360主机下载域名";
                        }
                        else
                        {
                            DataRow dr = dtHosts.NewRow();
                            dr["Enable"] = true;
                            dr["HostName"] = hostName;
                            dr["IPv4"] = Properties.Settings.Default.LocalIP;
                            dr["Remark"] = "Xbox360主机下载域名";
                            dtHosts.Rows.Add(dr);
                        }
                        dgvHosts.ClearSelection();
                    }
                    DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == hostNames[0]).Select(r => r).FirstOrDefault();
                    if (dgvr != null) dgvr.Cells["Col_IPv4"].Selected = true;
                }
                else
                {
                    string dnsServer = cbDohDNS.SelectedIndex switch
                    {
                        1 => "1.12.12.12",//doh.pub
                        2 => "180.163.249.75",//doh.360.cn
                        3 => "8.8.8.8",
                        _ => "223.5.5.5",//dns.alidns.com
                    };
                    string hostname = result.Groups["hostname"].Value.ToLower();
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
                    dr["IPv4"] = null;
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        dr["IPv4"] = ClassDNS.DoH(hostname, dnsServer);
                    });
                    dr["Remark"] = remark;
                    dgvHosts.ClearSelection();
                    DataGridViewRow? dgvr = dgvHosts.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Col_HostName"].Value.ToString() == hostname).Select(r => r).FirstOrDefault();
                    if (dgvr != null) dgvr.Cells["Col_IPv4"].Selected = true;
                }
            }
        }

        private void ButHostSave_Click(object sender, EventArgs e)
        {
            dtHosts.AcceptChanges();
            if (dtHosts.Rows.Count >= 1)
            {
                if (!Directory.Exists(resourcePath)) Directory.CreateDirectory(resourcePath);
                dtHosts.WriteXml(resourcePath + "\\Hosts.xml");
            }
            else if (File.Exists(resourcePath + "\\Hosts.xml"))
            {
                File.Delete(resourcePath + "\\Hosts.xml");
            }
            dgvHosts.ClearSelection();
            if (bServiceFlag) UpdateHosts(true);
        }

        private void ButHostReset_Click(object sender, EventArgs e)
        {
            dtHosts.RejectChanges();
            dgvHosts.ClearSelection();
        }

        private void LinkHostsConnectTest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormConnectTest dialog = new(string.Empty, string.Empty);
            dialog.ShowDialog();
            dialog.Dispose();
        }

        private void LinkHostsImport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormImportHosts dialog = new();
            dialog.ShowDialog();
            string hosts = dialog.hosts;
            dialog.Dispose();
            if (string.IsNullOrEmpty(hosts)) return;
            Regex regex = new(@"^(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+(?<hostname>[^\s+]+)(?<remark>.*)|^address=/(?<hostname>[^/+]+)/(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?<remark>.*)$");
            string[] array = hosts.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in array)
            {
                Match result = regex.Match(str.Trim());
                if (result.Success)
                {
                    string hostname = result.Groups["hostname"].Value.Trim().ToLower();
                    string remark = result.Groups["remark"].Value.Trim();
                    if (remark.StartsWith("#"))
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
                        dr["IPv4"] = ip.ToString();
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

        private void LinkCdnExportRule_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string ip = string.Empty;
            foreach (string str in tbCdnAkamai.Text.Split(','))
            {
                if (IPAddress.TryParse(str.Trim(), out IPAddress? address))
                {
                    ip = address.ToString();
                    break;
                }
            }
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("请先添加优选IP。", "导出规则", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbCdnAkamai.Focus();
                return;
            }
            StringBuilder sb = new();
            List<string> lsHostsTmp = new();
            foreach (string str in Regex.Replace(tbHosts1Akamai.Text, @"\#[^\r\n]+", "").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                string hosts = str.Trim().ToLower();
                if (hosts.StartsWith("*."))
                {
                    hosts = Regex.Replace(hosts, @"^\*\.", "");
                    if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                    {
                        lsHostsTmp.Add(hosts);
                        sb.AppendLine("'." + hosts + "': " + ip);
                    }
                }
                else if (hosts.StartsWith("*"))
                {
                    hosts = Regex.Replace(hosts, @"^\*", "");
                    if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                    {
                        lsHostsTmp.Add(hosts);
                        sb.AppendLine("'" + hosts + "': " + ip);
                        sb.AppendLine("'." + hosts + "': " + ip);
                    }
                }
                else if (DnsListen.reHosts.IsMatch(hosts))
                {
                    sb.AppendLine("'" + hosts + "': " + ip);
                }
            }
            foreach (string str in Regex.Replace(tbHosts2Akamai.Text, @"\#[^\r\n]+", "").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(str))
                    continue;
                string hosts = str.Trim().ToLower();
                if (hosts.StartsWith("*."))
                {
                    hosts = Regex.Replace(hosts, @"^\*\.", "");
                    if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                    {
                        lsHostsTmp.Add(hosts);
                        sb.AppendLine("'*." + hosts + "': " + ip);
                    }
                }
                else if (DnsListen.reHosts.IsMatch(hosts))
                {
                    sb.AppendLine("'" + hosts + "': " + ip);
                }
            }
            Clipboard.SetDataObject(sb.ToString() + "\r\n#- IP-CIDR," + ip + "/32,DIRECT #请把此条直连规则添加到规则设置中的自定义规则，并且删除开头#号\r\n");
            MessageBox.Show("规则已复制到剪贴板，支持在 OpenWrt 中的 OpenClash 使用。\n\n使用设置：\n1.打开 OpenClash 覆写设置->DNS 设置->Hosts，把规则粘贴到“自定义 Hosts”\n2.在规则设置中添加一条自定义规则（优先匹配），\n把IP “" + ip + "” 设置为直连。\n“- IP-CIDR," + ip + "/32,DIRECT”", "导出规则", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButCdnSave_Click(object sender, EventArgs e)
        {
            if (sender != null)
            {
                if (string.IsNullOrWhiteSpace(tbHosts2Akamai.Text))
                {
                    if (File.Exists(resourcePath + "\\" + "Akamai.txt")) File.Delete(resourcePath + "\\" + "Akamai.txt");
                }
                else
                {
                    if (!Directory.Exists(resourcePath)) Directory.CreateDirectory(resourcePath);
                    File.WriteAllText(resourcePath + "\\" + "Akamai.txt", tbHosts2Akamai.Text.Trim() + "\r\n");
                }
                Properties.Settings.Default.IpsAkamai = tbCdnAkamai.Text;
                Properties.Settings.Default.EnableCdnIP = ckbEnableCdnIP.Checked;
                Properties.Settings.Default.Save();
            }
            SetCdn();
        }

        private void ButCdnReset_Click(object sender, EventArgs e)
        {
            tbCdnAkamai.Text = Properties.Settings.Default.IpsAkamai;
            if (File.Exists(resourcePath + "\\Akamai.txt"))
            {
                using StreamReader sr = new(resourcePath + "\\Akamai.txt");
                tbHosts2Akamai.Text = sr.ReadToEnd().Trim() + "\r\n";
            }
            else tbHosts2Akamai.Clear();
            ckbEnableCdnIP.Checked = Properties.Settings.Default.EnableCdnIP;
        }

        private void SetCdn()
        {
            DnsListen.dicCdn1.Clear();
            DnsListen.dicCdn2.Clear();
            if (Properties.Settings.Default.EnableCdnIP)
            {
                string hosts2 = tbHosts2Akamai.Text.Trim();
                List<string> lsIpTmp = new();
                List<ResouceRecord> lsIp = new();
                foreach (string str in tbCdnAkamai.Text.Replace("，", ",").Split(','))
                {
                    if (IPAddress.TryParse(str.Trim(), out IPAddress? address))
                    {
                        string ip = address.ToString();
                        if (!lsIpTmp.Contains(ip))
                        {
                            lsIpTmp.Add(ip);
                            lsIp.Add(new ResouceRecord
                            {
                                Datas = address.GetAddressBytes(),
                                TTL = 100,
                                QueryClass = 1,
                                QueryType = QueryType.A
                            });
                        }
                    }
                }
                tbCdnAkamai.Text = string.Join(", ", lsIpTmp.ToArray());
                if (lsIp.Count >= 1)
                {
                    List<string> lsHostsTmp = new();
                    foreach (string str in Regex.Replace(tbHosts1Akamai.Text, @"\#[^\r\n]+", "").Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(str))
                            continue;

                        string hosts = str.Trim().ToLower();
                        if (hosts.StartsWith("*."))
                        {
                            hosts = Regex.Replace(hosts, @"^\*\.", "");
                            if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                            {
                                lsHostsTmp.Add(hosts);
                                DnsListen.dicCdn2.TryAdd(new Regex("\\." + hosts.Replace(".", "\\.") + "$"), lsIp);
                            }
                        }
                        else if (DnsListen.reHosts.IsMatch(hosts))
                        {
                            DnsListen.dicCdn1.TryAdd(hosts, lsIp);
                        }
                    }
                    foreach (string str in Regex.Replace(hosts2, @"\#[^\r\n]+", "").Split('\n'))
                    {
                        string hosts = str.Trim().ToLower();
                        if (hosts.StartsWith("*."))
                        {
                            hosts = Regex.Replace(hosts, @"^\*\.", "");
                            if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                            {
                                lsHostsTmp.Add(hosts);
                                DnsListen.dicCdn2.TryAdd(new Regex("\\." + hosts.Replace(".", "\\.") + "$"), lsIp);
                            }
                        }
                        else if (hosts.StartsWith("*"))
                        {
                            hosts = Regex.Replace(hosts, @"^\*", "");
                            if (DnsListen.reHosts.IsMatch(hosts) && !lsHostsTmp.Contains(hosts))
                            {
                                lsHostsTmp.Add(hosts);
                                DnsListen.dicCdn1.TryAdd(hosts, lsIp);
                                DnsListen.dicCdn2.TryAdd(new Regex("\\." + hosts.Replace(".", "\\.") + "$"), lsIp);
                            }
                        }
                        else if (DnsListen.reHosts.IsMatch(hosts))
                        {
                            DnsListen.dicCdn1.TryAdd(hosts, lsIp);
                        }
                    }
                }
            }
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
                if (!url.StartsWith("/")) url = "/" + url;
                url = "http://assets1.xboxlive.cn" + url;
                tbDownloadUrl.Text = url;
            }
            tbFilePath.Text = string.Empty;
            tbContentId.Text = tbProductID.Text = tbBuildID.Text = tbFileTimeCreated.Text = tbDriveSize.Text = tbPackageVersion.Text = string.Empty;
            butAnalyze.Enabled = butOpenFile.Enabled = linkCopyContentID.Enabled = linkRename.Enabled = linkProductID.Visible = false;
            Dictionary<string, string> headers = new() { { "Range", "0-4095" } };
            using HttpResponseMessage? response = await Task.Run(() => ClassWeb.HttpResponseMessage(url, "GET", null, null, headers));
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
            tbGameUrl.Text = "https://www.microsoft.com/store/productId/" + linkProductID.Text;
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
            if (Regex.IsMatch(url, "^https?://marketplace.xbox.com/"))
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
                url = Regex.Replace(url, @"(/[a-zA-Z]{2}-[a-zA-Z]{2})?/Product/", "/" + language + "/Product/");
                linkGameWebsite.Links[0].LinkData = url;
                tbGameUrl.Text = url;
                ThreadPool.QueueUserWorkItem(delegate { Xbox360Marketplace(url, language); });
            }
            else
            {
                string pat =
                    @"^https?://www\.xbox\.com(/[^/]*)?/games/store/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com(/[^/]*)?/p/[^/]+/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://www\.microsoft\.com/store/productId/(?<productId>[a-zA-Z0-9]{12})|" +
                    @"^https?://apps\.microsoft\.com/store/detail(/[^/]+)?/(?<productId>[a-zA-Z0-9]{12})|" +
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
                tbGameUrl.Text = "https://www.microsoft.com/store/productId/" + productId;
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
                tbGameUrl.Text = "https://www.microsoft.com/store/productId/" + productId;
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
            string url = "https://www.microsoft.com/msstoreapiprod/api/autosuggest?market=" + language + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Microsoft-Terms,Iris-Products,DCatAll-Products&filter=+ClientType:StoreWeb&counts=5,1,5&query=" + ClassWeb.UrlEncode(query);
            string html = ClassWeb.HttpResponseContent(url, "GET", null, null, null, 30000, "Nothing");
            if (this.query != query) return;
            List<ListViewItem> ls = new();
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                ClassGame.Search? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<ClassGame.Search>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    int size = (int)(25 * Form1.dpixRatio);
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
                        json = JsonSerializer.Deserialize<ClassGame.Game>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            tbGameUrl.Text = "https://www.microsoft.com/store/productId/" + product.id;
            if (butGame.Enabled) ButGame_Click(sender, EventArgs.Empty);
        }

        private void LinkGameChinese_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormChinese dialog = new();
            dialog.ShowDialog();
            dialog.Dispose();
            if (!string.IsNullOrEmpty(dialog.productid))
            {
                tbGameUrl.Text = "https://www.microsoft.com/store/productId/" + dialog.productid.ToUpperInvariant();
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
                    json = JsonSerializer.Deserialize<ClassGame.Game>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                                        item.SubItems[3].Text = filename;
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
                        json2 = JsonSerializer.Deserialize<ClassGame.Game>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                string html = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/GetGamePackage?contentId=" + contentId + "&platform=" + platform, "GET", null, null, null, 30000, "XboxDownload");
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.Game>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    if (update)
                    {
                        XboxGameDownload.SaveXboxGame();
                    }
                    this.Invoke(new Action(() =>
                    {
                        if (XboxGame.FileSize == packages.MaxDownloadSizeInBytes)
                        {
                            succeed = true;
                            item.ForeColor = Color.Empty;
                            item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                        }
                        else
                        {
                            if (platform != 2)
                            {
                                item.ForeColor = Color.Red;
                                item.SubItems[2].Text = ClassMbr.ConvertBytes(XboxGame.FileSize);
                                item.SubItems[3].Text = Path.GetFileName(XboxGame.Url);
                            }
                        }
                    }));
                }
            }
            if (!succeed && platform == 2)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Authorization))
                {
                    string hosts = "packagespc.xboxlive.com", url = String.Empty;
                    ulong filesize = 0;
                    string? ip = ClassDNS.DoH(hosts);
                    if (string.IsNullOrEmpty(ip)) return;
                    using HttpResponseMessage? response = ClassWeb.HttpResponseMessage("https://" + ip + "/GetBasePackage/" + contentId, "GET", null, null, new() { { "Host", hosts }, { "Authorization", Properties.Settings.Default.Authorization } });
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
                                    var json2 = JsonSerializer.Deserialize<XboxGameDownload.Game>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (json2 != null && json2.PackageFound)
                                    {
                                        contentId = json2.ContentId;
                                        packageFiles = json2.PackageFiles.Where(x => x.RelativeUrl.ToLower().EndsWith(".msixvc")).FirstOrDefault();
                                    }
                                }
                                catch { }
                                if (packageFiles != null)
                                {
                                    url = packageFiles.CdnRootPaths[0] + packageFiles.RelativeUrl;
                                    Version version;
                                    Match result = Regex.Match(url, @"(?<version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}");
                                    if (result.Success)
                                        version = new Version(result.Groups["version"].Value);
                                    else
                                        version = new Version();
                                    XboxGameDownload.Products XboxGame = new()
                                    {
                                        Version = version,
                                        FileSize = packageFiles.FileSize,
                                        Url = url
                                    };
                                    filesize = packageFiles.FileSize;
                                    XboxGameDownload.dicXboxGame.AddOrUpdate(contentId.ToLower(), XboxGame, (oldkey, oldvalue) => XboxGame);
                                    packages.MaxDownloadSizeInBytes = filesize;
                                    packages.PackageDownloadUris[0].Uri = url;
                                    XboxGameDownload.SaveXboxGame();
                                }
                            }
                        }
                        else if ((int)response.StatusCode == 401)
                        {
                            Properties.Settings.Default.Authorization = null;
                            Properties.Settings.Default.Save();
                            url = "授权已失效，请使用监听方式打开Xbox app，随便找一个游戏点击安装（无需实际安装），等待日志显示下载地址即可更新授权。";
                        }
                    }
                    this.Invoke(new Action(() =>
                    {
                        if (filesize > 0) item.SubItems[2].Text = ClassMbr.ConvertBytes(filesize);
                        item.SubItems[3].Text = Path.GetFileName(url);
                    }));
                    if (Regex.IsMatch(url, @"^https?://")) _ = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/AddGameUrl?url=" + ClassWeb.UrlEncode(url), "PUT", null, null, null, 30000, "XboxDownload");
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        item.SubItems[3].Text = "授权已失效，请使用监听方式打开Xbox app，随便找一个游戏点击安装（无需实际安装），等待日志显示下载地址即可更新授权。";
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
            string html = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
            XboxPackage.App? json = null;
            if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
            {
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    item.SubItems[3].Text = filename;
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
                        {
                            item.SubItems[3].Text = Path.GetFileName(item.SubItems[3].Text);
                        }
                        else if (XboxGameDownload.dicXboxGame.TryGetValue(item.SubItems[2].Tag.ToString() ?? string.Empty, out XboxGameDownload.Products? XboxGame))
                        {
                            item.SubItems[3].Text = XboxGame.Url;
                        }
                    }
                    else
                    {
                        if (Regex.IsMatch(item.SubItems[3].Text, @"^https?://"))
                        {
                            item.SubItems[3].Text = item.SubItems[3].Tag.ToString();
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
                        tsmCopyUrl1.Visible = tsmCopyUrl2.Visible = true;
                        tsmCopyUrl2.Enabled = isGame;
                        tsmAllUrl.Visible = !isGame && lvGame.Tag != null && item.SubItems[0].Text == "Windows PC";
                        tsmAuthorization.Visible = false;
                    }
                    else
                    {
                        tsmCopyUrl1.Visible = tsmCopyUrl2.Visible = tsmAllUrl.Visible = false;
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
                if (XboxGameDownload.dicXboxGame.TryGetValue(item.SubItems[2].Tag.ToString() ?? string.Empty, out XboxGameDownload.Products? XboxGame))
                {
                    url = XboxGame.Url;
                }
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
                string html = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/GetAppPackage2?WuCategoryId=" + wuCategoryId, "GET", null, null, null, 30000, "XboxDownload");
                XboxPackage.App? json = null;
                if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                {
                    try
                    {
                        json = JsonSerializer.Deserialize<XboxPackage.App>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        private void Xbox360Marketplace(string url, string language)
        {
            cbGameBundled.Tag = null;
            string title = string.Empty, publisherName = string.Empty, developerName = string.Empty, category = string.Empty, originalReleaseDate = string.Empty, description = string.Empty;
            string html = ClassWeb.HttpResponseContent(url);
            Match result = Regex.Match(html, @"<title>(?<title>.+)</title>");
            if (result.Success) title = HttpUtility.HtmlDecode(result.Groups["title"].Value);
            lock (ClassWeb.docLock)
            {
                ClassWeb.SetHtmlDocument(html, false);
                if (ClassWeb.doc != null && ClassWeb.doc.Body != null)
                {
                    HtmlElement hec = ClassWeb.doc.GetElementById("ProductPublishing");
                    if (hec != null)
                    {
                        if (hec.Children.Count == 4)
                        {
                            result = Regex.Match(hec.OuterHtml, @"<LI><LABEL>[^<]+</LABEL>(?<releasedate>[^<]+)\r\n<LI><LABEL>[^<]+</LABEL>(?<developer>[^<]+)\r\n<LI><LABEL>[^<]+</LABEL>(?<publisher>[^<]+)\r\n<LI><LABEL>[^<]+</LABEL>(?<genres>[^<]+)");
                            if (result.Success)
                            {
                                if (DateTime.TryParse(result.Groups["releasedate"].Value, System.Globalization.CultureInfo.CreateSpecificCulture(language), System.Globalization.DateTimeStyles.None, out DateTime dt))
                                    originalReleaseDate = dt.ToShortDateString();
                                publisherName = HttpUtility.HtmlDecode(result.Groups["publisher"].Value).Trim();
                                developerName = HttpUtility.HtmlDecode(result.Groups["developer"].Value).Trim();
                                category = HttpUtility.HtmlDecode(result.Groups["genres"].Value).Trim();
                            }
                        }
                        else if (hec.Children.Count == 3)
                        {
                            result = Regex.Match(hec.OuterHtml, @"<LABEL>[^<]+</LABEL>(?<releasedate>[^<]+)\r\n<LI><LABEL>[^<]+</LABEL>(?<publisher>[^<]+)\r\n<LI><LABEL>[^<]+</LABEL>(?<genres>[^<]+)");
                            if (result.Success)
                            {
                                if (DateTime.TryParse(result.Groups["releasedate"].Value, System.Globalization.CultureInfo.CreateSpecificCulture(language), System.Globalization.DateTimeStyles.None, out DateTime dt))
                                {
                                    originalReleaseDate = dt.ToShortDateString();
                                    publisherName = HttpUtility.HtmlDecode(result.Groups["publisher"].Value).Trim();
                                    category = HttpUtility.HtmlDecode(result.Groups["genres"].Value).Trim();
                                }
                                else
                                {
                                    publisherName = HttpUtility.HtmlDecode(result.Groups["releasedate"].Value).Trim();
                                    category = HttpUtility.HtmlDecode(result.Groups["genres"].Value).Trim();
                                }
                            }
                        }
                    }
                    hec = ClassWeb.doc.GetElementById("overview1");
                    if (hec != null)
                    {
                        description = hec.InnerText.Trim();
                        result = Regex.Match(hec.OuterHtml, @"<IMG[^>]+src=""(?<boxart>[^""]+)"">");
                        if (result.Success)
                        {
                            try
                            {
                                pbGame.LoadAsync(result.Groups["boxart"].Value);
                            }
                            catch { }
                        }
                    }
                }
                ClassWeb.ObjectDisposed();
            }
            this.Invoke(new Action(() =>
            {
                tbGameTitle.Text = title;
                tbGameDeveloperName.Text = publisherName + " / " + developerName;
                tbGameCategory.Text = category;
                tbGameOriginalReleaseDate.Text = originalReleaseDate;
                tbGameDescription.Text = description;
                butGame.Enabled = true;
                linkGameWebsite.Enabled = true;
            }));
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
            string html = ClassWeb.HttpResponseContent(UpdateFile.homePage + "/Game/GetAppPackage?WuCategoryId=f2ea4abe-4e1e-48ff-9022-a8a758303181", "GET", null, null, null, 30000, "XboxDownload");
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                XboxPackage.App? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<XboxPackage.App>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        private void GetEACdn()
        {
            string? eaCoreIni = null;
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                var rk = key.OpenSubKey(@"SOFTWARE\WOW6432Node\Origin") ?? key.OpenSubKey(@"SOFTWARE\Origin");
                if (rk != null)
                {
                    string? originPath = rk.GetValue("OriginPath", null)?.ToString();
                    if (originPath != null && File.Exists(originPath))
                    {
                        linkEaOriginRepair.Links[0].LinkData = originPath;
                        eaCoreIni = Path.GetDirectoryName(originPath) + "\\EACore.ini";
                    }
                    rk.Close();
                }
            }
            if (string.IsNullOrEmpty(eaCoreIni))
            {
                labelStatusEACdn.Text = "当前使用CDN：没有安装 Origin";
                return;
            }
            gpEACdn.Tag = eaCoreIni;
            string CdnOverride = Program.FilesIniRead("Feature", "CdnOverride", eaCoreIni).Trim();
            switch (CdnOverride.ToLower())
            {
                case "akamai":
                    rbEACdn1.Checked = true;
                    labelStatusEACdn.Text = "当前使用CDN：Akamai";
                    break;
                case "level3":
                    rbEACdn2.Checked = true;
                    labelStatusEACdn.Text = "当前使用CDN：Level3";
                    break;
                default:
                    rbEACdn0.Checked = true;
                    labelStatusEACdn.Text = "当前使用CDN：自动";
                    break;
            }
        }

        private void ButEACdn_Click(object sender, EventArgs e)
        {
            if (gpEACdn.Tag == null) GetEACdn();
            string? eaCoreIni = gpEACdn.Tag?.ToString();
            if (string.IsNullOrEmpty(eaCoreIni))
            {
                MessageBox.Show("没有安装 EA Origin", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string status;
            if (rbEACdn1.Checked)
            {
                Program.FilesIniWrite("Feature", "CdnOverride", "akamai", eaCoreIni);
                status = "当前使用CDN：Akamai";
            }
            else if (rbEACdn2.Checked)
            {
                Program.FilesIniWrite("Feature", "CdnOverride", "level3", eaCoreIni);
                status = "当前使用CDN：Level3";
            }
            else
            {
                Program.FilesIniWrite("Feature", null, null, eaCoreIni);
                status = "当前使用CDN：自动";
            }
            labelStatusEACdn.Text = status;
        }

        private void LinkEaOriginRepair_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (gpEACdn.Tag == null) GetEACdn();
            if (gpEACdn.Tag == null)
            {
                MessageBox.Show("没有安装 EA Origin", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            if (MessageBox.Show("此操作将删除Origin缓存文件和登录信息，执行下一步之前请先退出Origin，是否继续？", "修复 EA Origin", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Process[] processes = Process.GetProcessesByName("Origin");
                if (processes.Length == 0)
                {
                    DirectoryInfo di1 = new(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Origin");
                    DirectoryInfo di2 = new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Local\\Origin");
                    try
                    {
                        if (di1.Exists) di1.Delete(true);
                        if (di2.Exists) di2.Delete(true);
                    }
                    catch { }
                    string? path = e.Link.LinkData?.ToString();
                    if (path != null) Process.Start(path);
                }
                else
                {
                    MessageBox.Show("请先退出 Origin。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
        }

        private void LinkEaOriginNoUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (gpEACdn.Tag == null) GetEACdn();
            string? eaCoreIni = gpEACdn.Tag?.ToString();
            if (string.IsNullOrEmpty(eaCoreIni))
            {
                MessageBox.Show("没有安装 EA Origin", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            linkEaOriginNoUpdate.Enabled = false;
            Task.Run(() =>
            {
                Process[] processes = Process.GetProcessesByName("Origin");
                if (processes.Length == 1)
                {
                    try
                    {
                        processes[0].Kill();
                    }
                    catch { }
                }
                ServiceController? service = ServiceController.GetServices().Where(s => s.ServiceName == "Origin Web Helper Service").SingleOrDefault();
                if (service != null)
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                }
                Program.FilesIniWrite("Bootstrap", "EnableUpdating", "false", eaCoreIni);
                using (var key = Microsoft.Win32.Registry.LocalMachine)
                {
                    var rk = key.CreateSubKey(@"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop");
                    rk.SetValue("InstallSuccessful", true);
                }
                Thread.Sleep(3000);
                string dllPath = Path.GetDirectoryName(gpEACdn.Tag?.ToString()) + "\\version.dll";
                try
                {
                    using FileStream fs = new(dllPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    fs.Write(Properties.Resource.version, 0, Properties.Resource.version.Length);
                    fs.Flush();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        linkEaOriginNoUpdate.Enabled = true;
                        MessageBox.Show("复制文件出错，信息：" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }
                this.Invoke(new Action(() =>
                {
                    linkEaOriginNoUpdate.Enabled = true;
                    MessageBox.Show("已经成功禁止 Origin 强制更新到 EA App。\n\n注：如果不生效请检查 " + dllPath + " 是否被杀毒软件删除。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            });
        }
        #endregion
    }
}