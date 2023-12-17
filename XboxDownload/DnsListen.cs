using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.NetworkInformation;
using System.Data;

namespace XboxDownload
{
    internal class DnsListen
    {
        Socket? socket = null;
        private readonly Form1 parentForm;
        private readonly string dohServer = "https://223.5.5.5";
        private readonly Regex reDoHBlacklist = new("google|youtube|facebook|twitter");
        public static Regex reHosts = new(@"^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$");
        public static ConcurrentDictionary<String, List<ResouceRecord>> dicService = new(), dicHosts1 = new();
        public static ConcurrentDictionary<Regex, List<ResouceRecord>> dicHosts2 = new();
        public static ConcurrentDictionary<String, Dns> dicDns = new();
        
        public class Dns
        {
            public string IPv4 { get; set; } = "";

            public string IPv6 { get; set; } = "";
        }

        public DnsListen(Form1 parentForm)
        {
            this.parentForm = parentForm;
        }

        public void Listen()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && !x.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (Properties.Settings.Default.SetDns)
            {
                dicDns.Clear();
                using var key = Microsoft.Win32.Registry.LocalMachine;
                foreach (NetworkInterface adapter in adapters)
                {
                    var dns = new Dns();
                    var rk1 = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + adapter.Id);
                    if (rk1 != null)
                    {
                        string? ip = rk1.GetValue("NameServer", null) as string;
                        if (string.IsNullOrEmpty(ip) || ip == Properties.Settings.Default.LocalIP) ip = "";
                        dns.IPv4 = ip;
                        rk1.Close();
                    }
                    var rk2 = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\" + adapter.Id);
                    if (rk2 != null)
                    {
                        string? ip = rk2.GetValue("NameServer", null) as string;
                        if (string.IsNullOrEmpty(ip) || ip == "::") ip = "";
                        dns.IPv6 = ip;
                        rk2.Close();
                    }
                    dicDns.TryAdd(adapter.Id, dns);
                }
            }
            int port = 53;
            IPEndPoint? iPEndPoint = null;
            if (string.IsNullOrEmpty(Properties.Settings.Default.DnsIP))
            {
                foreach (NetworkInterface adapter in adapters)
                {
                    IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                    foreach (IPAddress dns in adapterProperties.DnsAddresses)
                    {
                        if (dns.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (dns.ToString() == Properties.Settings.Default.LocalIP || IPAddress.IsLoopback(dns))
                                continue;
                            iPEndPoint = new IPEndPoint(dns, port);
                            break;
                        }
                    }
                    if (iPEndPoint != null) break;
                }
                iPEndPoint ??= new IPEndPoint(IPAddress.Parse("114.114.114.114"), port);
                if (Form1.bServiceFlag)
                    parentForm.SetTextBox(parentForm.tbDnsIP, iPEndPoint.Address.ToString());
            }
            else
            {
                iPEndPoint = new IPEndPoint(IPAddress.Parse(Properties.Settings.Default.DnsIP), port);
            }

            IPEndPoint ipe = new(Properties.Settings.Default.ListenIP == 0 ? IPAddress.Parse(Properties.Settings.Default.LocalIP) : IPAddress.Any, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.Bind(ipe);
            }
            catch (SocketException ex)
            {
                parentForm.Invoke(new Action(() =>
                {
                    parentForm.pictureBox1.Image = Properties.Resource.Xbox3;
                    MessageBox.Show($"启用DNS服务失败!\n错误信息: {ex.Message}\n\n解决方法：1、停用占用 {port} 端口的服务。2、监听IP选择(Any)", "启用DNS服务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }

            Byte[] localIP = IPAddress.Parse(Properties.Settings.Default.LocalIP).GetAddressBytes();
            Byte[]? comIP = null, cnIP = null, appIP = null, psIP = null, nsIP = null, eaIP = null, battleIP = null, epicIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
            {
                comIP = IPAddress.Parse(Properties.Settings.Default.ComIP).GetAddressBytes();
            }
            else
            {
                if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbComIP, Properties.Settings.Default.LocalIP);
                comIP = localIP;
            }
            Task[] tasks = new Task[7];
            tasks[0] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
                {
                    cnIP = IPAddress.Parse(Properties.Settings.Default.CnIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("assets1.xboxlive.cn") : ClassDNS.HostToIP("assets2.xboxlive.cn", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbCnIP, ip);
                        cnIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[1] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
                {
                    appIP = IPAddress.Parse(Properties.Settings.Default.AppIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("dl.delivery.mp.microsoft.com") : ClassDNS.HostToIP("dl.delivery.mp.microsoft.com", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbAppIP, ip);
                        appIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[2] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.PSIP))
                {
                    psIP = IPAddress.Parse(Properties.Settings.Default.PSIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("gst.prod.dl.playstation.net") : ClassDNS.HostToIP("gst.prod.dl.playstation.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbPSIP, ip);
                        psIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[3] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.NSIP))
                {
                    nsIP = IPAddress.Parse(Properties.Settings.Default.NSIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("atum.hac.lp1.d4c.nintendo.net") : ClassDNS.HostToIP("atum.hac.lp1.d4c.nintendo.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbNSIP, ip);
                        nsIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[4] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
                {
                    eaIP = IPAddress.Parse(Properties.Settings.Default.EAIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("origin-a.akamaihd.net") : ClassDNS.HostToIP("origin-a.akamaihd.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbEAIP, ip);
                        eaIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[5] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
                {
                    battleIP = IPAddress.Parse(Properties.Settings.Default.BattleIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("blzddist1-a.akamaihd.net") : ClassDNS.HostToIP("blzddist1-a.akamaihd.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbBattleIP, ip);
                        battleIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[6] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
                {
                    epicIP = IPAddress.Parse(Properties.Settings.Default.EpicIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("epicgames-download1-1251447533.file.myqcloud.com") : ClassDNS.HostToIP("epicgames-download1-1251447533.file.myqcloud.com", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbEpicIP, ip);
                        epicIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (!Form1.bServiceFlag) return;

            dicService.Clear();
            List<ResouceRecord> lsLocalIP = new() { new ResouceRecord { Datas = localIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
            if (Properties.Settings.Default.GameLink)
            {
                dicService.TryAdd("xvcf1.xboxlive.com", lsLocalIP);
                dicService.TryAdd("assets1.xboxlive.com", lsLocalIP);
                dicService.TryAdd("d1.xboxlive.com", lsLocalIP);
                dicService.TryAdd("dlassets.xboxlive.com", lsLocalIP);
                dicService.TryAdd("assets1.xboxlive.cn", lsLocalIP);
                dicService.TryAdd("d1.xboxlive.cn", lsLocalIP);
                dicService.TryAdd("dlassets.xboxlive.cn", lsLocalIP);
                if (comIP != null)
                {
                    List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("xvcf2.xboxlive.com", lsComIP);
                    dicService.TryAdd("assets2.xboxlive.com", lsComIP);
                    dicService.TryAdd("d2.xboxlive.com", lsComIP);
                    dicService.TryAdd("dlassets2.xboxlive.com", lsComIP);
                }
                if (cnIP != null)
                {
                    List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("assets2.xboxlive.cn", lsCnIP);
                    dicService.TryAdd("d2.xboxlive.cn", lsCnIP);
                }
                if (appIP != null)
                {
                    List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                    dicService.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsLocalIP);
                    dicService.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                    dicService.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                }
            }
            else
            {
                if (comIP != null)
                {
                    List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("xvcf1.xboxlive.com", lsComIP);
                    dicService.TryAdd("xvcf2.xboxlive.com", lsComIP);
                    dicService.TryAdd("assets1.xboxlive.com", lsComIP);
                    dicService.TryAdd("assets2.xboxlive.com", lsComIP);
                    dicService.TryAdd("d1.xboxlive.com", lsComIP);
                    dicService.TryAdd("d2.xboxlive.com", lsComIP);
                    dicService.TryAdd("dlassets.xboxlive.com", lsComIP);
                    dicService.TryAdd("dlassets2.xboxlive.com", lsComIP);
                }
                if (cnIP != null)
                {
                    List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("assets1.xboxlive.cn", lsCnIP);
                    dicService.TryAdd("assets2.xboxlive.cn", lsCnIP);
                    dicService.TryAdd("d1.xboxlive.cn", lsCnIP);
                    dicService.TryAdd("d2.xboxlive.cn", lsCnIP);
                }
                if (appIP != null)
                {
                    List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    dicService.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                    dicService.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                    dicService.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                    dicService.TryAdd("dlassets.xboxlive.cn", lsAppIP);
                    dicService.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                }
            }
            if (psIP != null)
            {
                List<ResouceRecord> lsPsIP = new() { new ResouceRecord { Datas = psIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                dicService.TryAdd("gst.prod.dl.playstation.net", lsPsIP);
                dicService.TryAdd("gs2.ww.prod.dl.playstation.net", lsPsIP);
                dicService.TryAdd("zeus.dl.playstation.net", lsPsIP);
                dicService.TryAdd("ares.dl.playstation.net", lsPsIP);
            }
            if (nsIP != null) 
            {
                List<ResouceRecord> lsNsIP = new() { new ResouceRecord { Datas = nsIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                dicService.TryAdd("atum.hac.lp1.d4c.nintendo.net", lsNsIP);
                dicService.TryAdd("bugyo.hac.lp1.eshop.nintendo.net", lsNsIP);
                dicService.TryAdd("ctest-dl-lp1.cdn.nintendo.net", lsNsIP);
                dicService.TryAdd("ctest-ul-lp1.cdn.nintendo.net", lsNsIP);
            }
            dicService.TryAdd("atum-eda.hac.lp1.d4c.nintendo.net", new List<ResouceRecord>());
            if (eaIP != null)
            {
                List<ResouceRecord> lsEaIP = new() { new ResouceRecord { Datas = eaIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                dicService.TryAdd("origin-a.akamaihd.net", lsEaIP);
            }
            dicService.TryAdd("ssl-lvlt.cdn.ea.com", new List<ResouceRecord>());
            if (battleIP != null)
            {
                List<ResouceRecord> lsBattleIP = new() { new ResouceRecord { Datas = battleIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                dicService.TryAdd("blzddist1-a.akamaihd.net", lsBattleIP);
                dicService.TryAdd("blzddist2-a.akamaihd.net", lsBattleIP);
                dicService.TryAdd("blzddist3-a.akamaihd.net", lsBattleIP);
            }
            if (epicIP != null)
            {
                List<ResouceRecord> lsEpicIP = new() { new ResouceRecord { Datas = epicIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                dicService.TryAdd("epicgames-download1-1251447533.file.myqcloud.com", lsEpicIP);
            }
            if (Properties.Settings.Default.HttpService)
            {
                dicService.TryAdd("www.msftconnecttest.com", lsLocalIP);
                dicService.TryAdd("ctest.cdn.nintendo.net", lsLocalIP);
            }
            if (Properties.Settings.Default.SetDns) ClassDNS.SetDns(Properties.Settings.Default.LocalIP);
            while (Form1.bServiceFlag)
            {
                try
                {
                    var client = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                    var buff = new byte[512];
                    int read = socket.ReceiveFrom(buff, ref client);
                    _ = Task.Factory.StartNew(() =>
                    {
                        var dns = new DNS(buff, read);
                        if (dns.QR == 0 && dns.Opcode == 0 && dns.Querys.Count == 1)
                        {
                            string queryName = (dns.Querys[0].QueryName ?? string.Empty).ToLower();
                            switch (dns.Querys[0].QueryType)
                            {
                                case QueryType.A:
                                    if (dicService.TryGetValue(queryName, out List<ResouceRecord>? lsServiceIp))
                                    {
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResouceRecords = lsServiceIp;
                                        socket?.SendTo(dns.ToBytes(), client);
                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsServiceIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x008000);
                                        return;
                                    }
                                    if (dicHosts1.TryGetValue(queryName, out List<ResouceRecord>? lsHostsIp))
                                    {
                                        if (lsHostsIp.Count >= 2) lsHostsIp = lsHostsIp.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResouceRecords = lsHostsIp;
                                        socket?.SendTo(dns.ToBytes(), client);
                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsHostsIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                        return;
                                    }
                                    var lsHostsIp2 = dicHosts2.Where(kvp => kvp.Key.IsMatch(queryName)).Select(x => x.Value).FirstOrDefault();
                                    if(lsHostsIp2 != null)
                                    {
                                        dicHosts1.TryAdd(queryName, lsHostsIp2);
                                        if (lsHostsIp2.Count >= 2) lsHostsIp2 = lsHostsIp2.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResouceRecords = lsHostsIp2;
                                        socket?.SendTo(dns.ToBytes(), client);
                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsHostsIp2.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                        return;
                                    }
                                    if (Properties.Settings.Default.DoH && !reDoHBlacklist.IsMatch(queryName))
                                    {
                                        string html = ClassWeb.HttpResponseContent(this.dohServer + "/resolve?name=" + ClassWeb.UrlEncode(queryName) + "&type=A", "GET", null, null, null, 6000);
                                        if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
                                        {
                                            ClassDNS.Api? json = null;
                                            try
                                            {
                                                json = JsonSerializer.Deserialize<ClassDNS.Api>(html, Form1.jsOptions);
                                            }
                                            catch { }
                                            if (json != null && json.Answer != null)
                                            {
                                                if (json.Status == 0)
                                                {
                                                    dns.QR = 1;
                                                    dns.RA = 1;
                                                    dns.RD = 1;
                                                    dns.ResouceRecords = new List<ResouceRecord>();
                                                    foreach (var answer in json.Answer)
                                                    {
                                                        if (answer.Type == 1 && IPAddress.TryParse(answer.Data, out IPAddress? ipAddress))
                                                        {
                                                            dns.ResouceRecords.Add(new ResouceRecord
                                                            {
                                                                Datas = ipAddress.GetAddressBytes(),
                                                                TTL = answer.TTL,
                                                                QueryClass = 1,
                                                                QueryType = QueryType.A
                                                            });
                                                        }
                                                    }
                                                    socket?.SendTo(dns.ToBytes(), client);
                                                    var arrIp = json.Answer.Where(x => x.Type == 1).Select(x => x.Data);
                                                    if (arrIp != null)
                                                    {
                                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", arrIp.ToArray()), ((IPEndPoint)client).Address.ToString());
                                                    }
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName, ((IPEndPoint)client).Address.ToString());
                                    break;
                                case QueryType.AAAA:
                                    dns.QR = 1;
                                    dns.RA = 1;
                                    dns.RD = 1;
                                    dns.ResouceRecords = new List<ResouceRecord>();
                                    socket?.SendTo(dns.ToBytes(), client);
                                    return;
                            }
                        }
                        try
                        {
                            var proxy = new UdpClient();
                            proxy.Client.ReceiveTimeout = 6000;
                            proxy.Connect(iPEndPoint);
                            proxy.Send(buff, read);
                            var bytes = proxy.Receive(ref iPEndPoint);
                            socket?.SendTo(bytes, client);
                        }
                        catch (Exception ex)
                        {
                            if (Form1.bServiceFlag && Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", ex.Message, ((IPEndPoint)client).Address.ToString());
                        }
                    });
                }
                catch { }
            }
        }

        readonly ConcurrentDictionary<String, List<ResouceRecord>> dicService2 = new();
        public void SetXboxDownloadIP(string? ip)
        {
            string[] hosts = { "xvcf1.xboxlive.com", "xvcf2.xboxlive.com", "assets1.xboxlive.com", "assets2.xboxlive.com", "d1.xboxlive.com", "d2.xboxlive.com", "dlassets.xboxlive.com", "dlassets2.xboxlive.com", "assets1.xboxlive.cn", "assets2.xboxlive.cn", "d1.xboxlive.cn", "d2.xboxlive.cn", "dlassets.xboxlive.cn", "dlassets2.xboxlive.cn", "dl.delivery.mp.microsoft.com", "tlu.dl.delivery.mp.microsoft.com", "2.tlu.dl.delivery.mp.microsoft.com" };
            if (string.IsNullOrEmpty(ip))
            {
                foreach (string host in hosts)
                {
                    if (dicService2.TryGetValue(host, out List<ResouceRecord>? vlaue))
                    {
                        dicService.AddOrUpdate(host, vlaue, (oldkey, oldvalue) => vlaue);
                    }
                    else
                    {
                        dicService.TryRemove(host, out _);
                    }
                }
            }
            else
            {
                dicService2.Clear();
                List<ResouceRecord> lsIP = new() { new ResouceRecord { Datas = IPAddress.Parse(ip).GetAddressBytes(), TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                foreach (string host in hosts)
                {
                    if (dicService.TryGetValue(host, out List<ResouceRecord>? vlaue))
                    {
                        dicService2.TryAdd(host, vlaue);
                    }
                    dicService.AddOrUpdate(host, lsIP, (oldkey, oldvalue) => lsIP);
                }
            }
        }

        public void Close()
        {
            socket?.Close();
            socket?.Dispose();
            socket = null;
        }

        public static void UpdateHosts()
        {
            dicHosts1.Clear();
            dicHosts2.Clear();
            DataTable dt = Form1.dtHosts.Copy();
            dt.RejectChanges();
            foreach (DataRow dr in dt.Rows)
            {
                if (!Convert.ToBoolean(dr["Enable"])) continue;
                string? hostName = dr["HostName"].ToString()?.Trim().ToLower();
                if (!string.IsNullOrEmpty(hostName) && IPAddress.TryParse(dr["IPv4"].ToString()?.Trim(), out IPAddress? ip))
                {
                    if (hostName.StartsWith("*."))
                    {
                        hostName = Regex.Replace(hostName, @"^\*\.", "");
                        Regex re = new("\\." + hostName.Replace(".", "\\.") + "$");
                        if (!dicHosts2.ContainsKey(re) && reHosts.IsMatch(hostName))
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
                            dicHosts2.TryAdd(re, lsIp);
                        }
                    }
                    else if (!dicHosts1.ContainsKey(hostName) && reHosts.IsMatch(hostName))
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
                        dicHosts1.TryAdd(hostName, lsIp);
                    }
                }
            }

            if (Properties.Settings.Default.EnableCdnIP)
            {
                List<string> lsIpTmp = new();
                List<ResouceRecord> lsIp = new();
                foreach (string str in Properties.Settings.Default.IpsAkamai.Replace("，", ",").Split(','))
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
                if (lsIp.Count >= 1)
                {
                    foreach (string str in Properties.Resource.Akamai.Split('\n'))
                    {
                        string host = Regex.Replace(str, @"\#.+", "").Trim().ToLower();
                        if (string.IsNullOrEmpty(host)) continue;
                        if (host.StartsWith("*."))
                        {
                            host = Regex.Replace(host, @"^\*\.", "");
                            if (reHosts.IsMatch(host))
                            {
                                dicHosts2.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp);
                            }
                        }
                        else if (reHosts.IsMatch(host))
                        {
                            dicHosts1.TryAdd(host, lsIp);
                        }
                    }
                    if (File.Exists(Form1.resourcePath + "\\Akamai.txt"))
                    {
                        foreach (string str in File.ReadAllText(Form1.resourcePath + "\\Akamai.txt").Split('\n'))
                        {
                            string host = Regex.Replace(str, @"\#.+", "").Trim().ToLower();
                            if (string.IsNullOrEmpty(host)) continue;
                            if (host.StartsWith("*."))
                            {
                                host = Regex.Replace(host, @"^\*\.", "");
                                if (reHosts.IsMatch(host))
                                {
                                    dicHosts2.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp);
                                }
                            }
                            else if (host.StartsWith("*"))
                            {
                                host = Regex.Replace(host, @"^\*", "");
                                if (reHosts.IsMatch(host))
                                {
                                    dicHosts1.TryAdd(host, lsIp);
                                    dicHosts2.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp);
                                }
                            }
                            else if (reHosts.IsMatch(host))
                            {
                                dicHosts1.TryAdd(host, lsIp);
                            }
                        }
                    }
                }
            }
        }
    }

    public enum QueryType
    {
        A = 1,
        NS = 2,
        MD = 3,
        MF = 4,
        CNAME = 5,
        SOA = 6,
        MB = 7,
        MG = 8,
        MR = 9,
        WKS = 11,
        PTR = 12,
        HINFO = 13,
        MINFO = 14,
        MX = 15,
        TXT = 16,
        AAAA = 28,
        HTTPS = 65,
        AXFR = 252,
        ANY = 255
    }

    public class Query
    {
        public string? QueryName { get; set; }
        public QueryType QueryType { get; set; }
        public Int16 QueryClass { get; set; }

        public Query()
        {
        }

        public Query(Func<int, byte[]> read)
        {
            var name = new StringBuilder();
            var length = read(1)[0];
            while (length != 0)
            {
                for (var i = 0; i < length; i++)
                {
                    name.Append((char)read(1)[0]);
                }
                length = read(1)[0];
                if (length != 0)
                    name.Append('.');
            }
            QueryName = name.ToString();

            QueryType = (QueryType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(read(2), 0));
            QueryClass = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(read(2), 0));
        }

        public virtual byte[] ToBytes()
        {
            var list = new List<byte>();

            if (QueryName != null)
            {
                var a = QueryName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < a.Length; i++)
                {
                    list.Add((byte)a[i].Length);
                    for (var j = 0; j < a[i].Length; j++)
                        list.Add((byte)a[i][j]);
                }
                list.Add(0);
            }

            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)QueryType)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(QueryClass)));

            return list.ToArray();
        }
    }

    public class ResouceRecord : Query
    {
        public Int16 Point { get; set; }
        public Int32 TTL { get; set; }
        public byte[]? Datas { get; set; }

        public ResouceRecord() : base()
        {
            var bytes = new byte[] { 0xc0, 0x0c };
            Point = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
        }

        public ResouceRecord(Func<int, byte[]> read) : base()
        {
            TTL = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(read(4), 0));
            var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(read(2), 0));
            Datas = read(length);
        }
        public override byte[] ToBytes()
        {
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Point)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)QueryType)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(QueryClass)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(TTL)));
            if (Datas != null)
            {
                list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)Datas.Length)));
                list.AddRange(Datas);
            }

            return list.ToArray();
        }
    }

    public class DNS
    {
        public Int16 标志 { get; set; }
        public int QR { get; set; }     //0表示查询报文 1表示响应报文
        public int Opcode { get; set; } //0表示标准查询,1表示反向查询,2表示服务器状态请求
        public int AA { get; set; }  //授权回答
        public int TC { get; set; } //表示可截断的
        public int RD { get; set; } //表示期望递归
        public int RA { get; set; } //表示可用递归
        public int Rcode { get; set; } //0表示没有错误,3表示名字错误

        public List<Query> Querys { get; set; }  //问题数
        public List<ResouceRecord>? ResouceRecords { get; set; }  //资源记录数
        public Int16 授权资源记录数 { get; set; }
        public Int16 额外资源记录数 { get; set; }

        public byte[] ToBytes()
        {
            var list = new List<byte>();
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(标志));
            list.AddRange(bytes);
            var b = new byte();
            b = b.SetBits(QR, 0, 1)
                .SetBits(Opcode, 1, 4)
                .SetBits(AA, 5, 1)
                .SetBits(TC, 6, 1);

            b = b.SetBits(RD, 7, 1);
            list.Add(b);
            b = new byte();
            b = b.SetBits(RA, 0, 1)
                .SetBits(0, 1, 3)
                .SetBits(Rcode, 4, 4);
            list.Add(b);

            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)Querys.Count)));
            if (ResouceRecords != null)
                list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)ResouceRecords.Count)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(授权资源记录数)));
            list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(额外资源记录数)));

            foreach (var q in Querys)
            {
                list.AddRange(q.ToBytes());
            }
            if (ResouceRecords != null)
            {
                foreach (var r in ResouceRecords)
                {
                    list.AddRange(r.ToBytes());
                }
            }

            return list.ToArray();
        }

        private int index;
        private readonly byte[] package;
        private byte ReadByte()
        {
            return package[index++];
        }
        private byte[] ReadBytes(int count = 1)
        {
            var bytes = new byte[count];
            for (var i = 0; i < count; i++)
                bytes[i] = ReadByte();
            return bytes;
        }

        public DNS(byte[] buffer, int length)
        {
            package = new byte[length];
            for (var i = 0; i < length; i++)
                package[i] = buffer[i];

            标志 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));

            var b1 = ReadByte();
            var b2 = ReadByte();

            QR = b1.GetBits(0, 1);
            Opcode = b1.GetBits(1, 4);
            AA = b1.GetBits(5, 1);
            TC = b1.GetBits(6, 1);
            RD = b1.GetBits(7, 1);

            RA = b2.GetBits(0, 1);
            Rcode = b2.GetBits(4, 4);

            var queryCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));
            var rrCount = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));

            授权资源记录数 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));
            额外资源记录数 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(2), 0));

            Querys = new List<Query>();
            for (var i = 0; i < queryCount; i++)
            {
                Querys.Add(new Query(ReadBytes));
            }

            for (var i = 0; i < rrCount; i++)
            {
                ResouceRecords?.Add(new ResouceRecord(ReadBytes));
            }
        }
    }

    public static class Extension
    {
        public static int GetBits(this byte b, int start, int length)
        {
            var temp = b >> (8 - start - length);
            var mask = 0;
            for (var i = 0; i < length; i++)
            {
                mask = (mask << 1) + 1;
            }

            return temp & mask;
        }

        public static byte SetBits(this byte b, int data, int start, int length)
        {
            var temp = b;

            var mask = 0xFF;
            for (var i = 0; i < length; i++)
            {
                mask -= (0x01 << (7 - (start + i)));
            }
            temp = (byte)(temp & mask);

            mask = ((byte)data).GetBits(8 - length, length);
            mask <<= (7 - start);

            return (byte)(temp | mask);
        }
    }

    internal class ClassDNS
    {
        public static void SetDns(string? dns)
        {
            using var key = Microsoft.Win32.Registry.LocalMachine;
            foreach (var item in DnsListen.dicDns)
            {
                var rk1 = key.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + item.Key);
                if (rk1 != null)
                {
                    rk1.SetValue("NameServer", string.IsNullOrEmpty(dns) ? item.Value.IPv4 : dns);
                    rk1.Close();
                }
                var rk2 = key.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\" + item.Key);
                if (rk2 != null)
                {
                    rk2.SetValue("NameServer", string.IsNullOrEmpty(dns) ? item.Value.IPv6 : "::");
                    rk2.Close();
                }
            }
        }

        public static string QueryLocation(string ip)
        {
            if (Regex.IsMatch(ip, @"^(127\.0\.0\.1)|(10\.\d{1,3}\.\d{1,3}\.\d{1,3})|(172\.((1[6-9])|(2\d)|(3[01]))\.\d{1,3}\.\d{1,3})|(192\.168\.\d{1,3}\.\d{1,3})$")) return "本地局域网IP";
            string html = ClassWeb.HttpResponseContent("https://www.ipshudi.com/" + ip + ".htm", "GET", null, null, null, 6000);
            Match result = Regex.Match(html, @"<tr>\n<td[^>]*>归属地</td>\n<td>\n<span>(?<location1>[^<]*)</span>\n<a[^>]*>上报纠错</a>\n</td>\n</tr>\n<tr><td[^>]*>运营商</td><td><span>(?<location2>[^<]+)</span></td></tr>");
            if (result.Success)
            {
                return result.Groups["location1"].Value.Trim() + result.Groups["location2"].Value.Trim() + " (来源：ip138.com)";
            }
            else
            {
                html = ClassWeb.HttpResponseContent("https://www.ip.cn/ip/" + ip + ".html", "GET", null, null, null, 6000);
                result = Regex.Match(html, @"<div id=""tab0_address"">(?<location>[^<]*)</div>");
                if (result.Success)
                {
                    return result.Groups["location"].Value.Trim() + " (来源：ip.cn)";
                }
            }
            return "";
        }

        public static string? HostToIP(string hostName, string? dnsServer = null)
        {
            string? ip = null;
            if (string.IsNullOrEmpty(dnsServer))
            {
                IPAddress[]? ipAddresses = null;
                try
                {
                    ipAddresses = Array.FindAll(Dns.GetHostEntry(hostName).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                }
                catch { }
                if (ipAddresses != null && ipAddresses.Length >= 1) ip = ipAddresses[0].ToString();
            }
            else
            {
                string resultInfo = string.Empty;
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("nslookup", "-ty=A " + hostName + " " + dnsServer)
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
                MatchCollection mc = Regex.Matches(resultInfo, @":\s*(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
                if (mc.Count == 2)
                    ip = mc[1].Groups["ip"].Value;
            }
            return ip;
        }

        public static string? DoH(string hostName, string dohServer = "223.5.5.5")
        {
            string? ip = null;
            string html = ClassWeb.HttpResponseContent("https://" + dohServer + "/resolve?name=" + ClassWeb.UrlEncode(hostName) + "&type=A", "GET", null, null, null, 6000);
            if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
            {
                try
                {
                    var json = JsonSerializer.Deserialize<ClassDNS.Api>(html, Form1.jsOptions);
                    if (json != null && json.Answer != null)
                    {
                        if (json.Status == 0 && json.Answer.Count >= 1)
                        {
                            ip = json.Answer.Where(x => x.Type == 1).Select(x => x.Data).FirstOrDefault();
                        }
                    }
                }
                catch { }
            }
            return ip;
        }

        public class Api
        {
            public int Status { get; set; }
            public bool TC { get; set; }
            public bool RD { get; set; }
            public bool RA { get; set; }
            public bool AD { get; set; }
            public bool CD { get; set; }
            public class Question
            {
                public string? Name { get; set; }
                public int Type { get; set; }
            }
            public List<Answer>? Answer { get; set; }
            public List<Answer>? Authority { get; set; }
            public List<Answer>? Additional { get; set; }
            public string? Edns_client_subnet { get; set; }
        }

        public class Answer
        {
            public string? Name { get; set; }
            public int TTL { get; set; }
            public int Type { get; set; }
            public string? Data { get; set; }
        }
    }
}
