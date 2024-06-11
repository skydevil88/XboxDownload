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
        private readonly Regex reDoHFilter = new("google|youtube|facebook|twitter|github");
        public static readonly List<ResouceRecord> lsEmptyIP = new();
        public static Regex reHosts = new(@"^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$");
        public static ConcurrentDictionary<String, List<ResouceRecord>> dicServiceV4 = new(), dicService2V4 = new(), dicHosts1V4 = new(), dicServiceV6 = new(), dicService2V6 = new(), dicHosts1V6 = new();
        public static ConcurrentDictionary<Regex, List<ResouceRecord>> dicHosts2V4 = new(), dicHosts2V6 = new();
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
                    MessageBox.Show($"启用DNS服务失败!\n错误信息: {ex.Message}\n\n两种解决方法：\n1、监听IP选择(Any)。\n2、使用netstat查看并解除 {port} 端口占用。", "启用DNS服务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }

            Byte[] localIP = IPAddress.Parse(Properties.Settings.Default.LocalIP).GetAddressBytes();
            Byte[]? comIP = null, cnIP = null, appIP = null, psIP = null, nsIP = null, eaIP = null, battleIP = null, epicIP = null, ubiIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
            {
                comIP = IPAddress.Parse(Properties.Settings.Default.ComIP).GetAddressBytes();
            }
            else
            {
                if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbComIP, Properties.Settings.Default.LocalIP);
                comIP = localIP;
            }
            Task[] tasks = new Task[8];
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
            string epicHosts = Properties.Settings.Default.EpicCDN ? "epicgames-download1-1251447533.file.myqcloud.com" : "epicgames-download1.akamaized.net";
            tasks[6] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
                {
                    epicIP = IPAddress.Parse(Properties.Settings.Default.EpicIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH(epicHosts) : ClassDNS.HostToIP(epicHosts, Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbEpicIP, ip);
                        epicIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            tasks[7] = new Task(() =>
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UbiIP))
                {
                    ubiIP = IPAddress.Parse(Properties.Settings.Default.UbiIP).GetAddressBytes();
                }
                else
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("uplaypc-s-ubisoft.cdn.ubionline.com.cn") : ClassDNS.HostToIP("uplaypc-s-ubisoft.cdn.ubionline.com.cn", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbUbiIP, ip);
                        ubiIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                }
            });
            Array.ForEach(tasks, x => x.Start());
            Task.WaitAll(tasks);
            if (!Form1.bServiceFlag) return;

            dicServiceV4.Clear();
            dicServiceV6.Clear();
            List<ResouceRecord> lsLocalIP = new() { new ResouceRecord { Datas = localIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
            if (Properties.Settings.Default.GameLink)
            {
                _ = dicServiceV4.TryAdd("xvcf1.xboxlive.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("assets1.xboxlive.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("d1.xboxlive.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("dlassets.xboxlive.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("assets1.xboxlive.cn", lsLocalIP);
                _ = dicServiceV4.TryAdd("d1.xboxlive.cn", lsLocalIP);
                _ = dicServiceV4.TryAdd("dlassets.xboxlive.cn", lsLocalIP);
                _ = dicServiceV6.TryAdd("xvcf1.xboxlive.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("assets1.xboxlive.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("d1.xboxlive.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("dlassets.xboxlive.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("assets1.xboxlive.cn", lsEmptyIP);
                _ = dicServiceV6.TryAdd("d1.xboxlive.cn", lsEmptyIP);
                _ = dicServiceV6.TryAdd("dlassets.xboxlive.cn", lsEmptyIP);
                if (comIP != null)
                {
                    if ((new IPAddress(comIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("xvcf2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("xvcf2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.com", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("xvcf2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("xvcf2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.com", lsEmptyIP);
                    }
                }
                if (cnIP != null)
                {
                    if ((new IPAddress(cnIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.cn", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.cn", lsEmptyIP);
                    }
                }
                if (appIP != null)
                {
                    if ((new IPAddress(appIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV4.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsLocalIP);
                        _ = dicServiceV4.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                        _ = dicServiceV6.TryAdd("dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.cn", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV6.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsLocalIP);
                        _ = dicServiceV6.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                        _ = dicServiceV4.TryAdd("dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.cn", lsEmptyIP);
                    }
                }
            }
            else
            {
                if (comIP != null)
                {
                    if ((new IPAddress(comIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("xvcf1.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("xvcf2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("assets1.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("d1.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("dlassets.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("xvcf1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("xvcf2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("assets1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.com", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsComIP = new() { new ResouceRecord { Datas = comIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("xvcf1.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("xvcf2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("assets1.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("d1.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("dlassets.xboxlive.com", lsComIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.com", lsComIP);
                        _ = dicServiceV4.TryAdd("xvcf1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("xvcf2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("assets1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d1.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets.xboxlive.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.com", lsEmptyIP);
                    }
                }
                if (cnIP != null)
                {
                    if ((new IPAddress(cnIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("assets1.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("d1.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("assets1.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d1.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.cn", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsCnIP = new() { new ResouceRecord { Datas = cnIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("assets1.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("assets2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("d1.xboxlive.cn", lsCnIP);
                        _ = dicServiceV6.TryAdd("d2.xboxlive.cn", lsCnIP);
                        _ = dicServiceV4.TryAdd("assets1.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("assets2.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d1.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("d2.xboxlive.cn", lsEmptyIP);
                    }
                }
                if (appIP != null)
                {
                    if ((new IPAddress(appIP)).AddressFamily == AddressFamily.InterNetwork)
                    {
                        List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                        _ = dicServiceV4.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV4.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV4.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV4.TryAdd("dlassets.xboxlive.cn", lsAppIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                        _ = dicServiceV6.TryAdd("dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.cn", lsEmptyIP);
                    }
                    else
                    {
                        List<ResouceRecord> lsAppIP = new() { new ResouceRecord { Datas = appIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                        _ = dicServiceV6.TryAdd("dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV6.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV6.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsAppIP);
                        _ = dicServiceV6.TryAdd("dlassets.xboxlive.cn", lsAppIP);
                        _ = dicServiceV6.TryAdd("dlassets2.xboxlive.cn", lsAppIP);
                        _ = dicServiceV4.TryAdd("dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("2.tlu.dl.delivery.mp.microsoft.com", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets.xboxlive.cn", lsEmptyIP);
                        _ = dicServiceV4.TryAdd("dlassets2.xboxlive.cn", lsEmptyIP);
                    }
                }
            }
            if (psIP != null)
            {
                if ((new IPAddress(psIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsPsIP = new() { new ResouceRecord { Datas = psIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd("gst.prod.dl.playstation.net", lsPsIP);
                    _ = dicServiceV4.TryAdd("gs2.ww.prod.dl.playstation.net", lsPsIP);
                    _ = dicServiceV4.TryAdd("zeus.dl.playstation.net", lsPsIP);
                    _ = dicServiceV4.TryAdd("ares.dl.playstation.net", lsPsIP);
                    _ = dicServiceV6.TryAdd("gst.prod.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("gs2.ww.prod.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("zeus.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("ares.dl.playstation.net", lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsPsIP = new() { new ResouceRecord { Datas = psIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd("gst.prod.dl.playstation.net", lsPsIP);
                    _ = dicServiceV6.TryAdd("gs2.ww.prod.dl.playstation.net", lsPsIP);
                    _ = dicServiceV6.TryAdd("zeus.dl.playstation.net", lsPsIP);
                    _ = dicServiceV6.TryAdd("ares.dl.playstation.net", lsPsIP);
                    _ = dicServiceV4.TryAdd("gst.prod.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("gs2.ww.prod.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("zeus.dl.playstation.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("ares.dl.playstation.net", lsEmptyIP);
                }
            }
            if (nsIP != null)
            {
                if ((new IPAddress(nsIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsNsIP = new() { new ResouceRecord { Datas = nsIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd("atum.hac.lp1.d4c.nintendo.net", lsNsIP);
                    _ = dicServiceV4.TryAdd("bugyo.hac.lp1.eshop.nintendo.net", lsNsIP);
                    _ = dicServiceV4.TryAdd("ctest-dl-lp1.cdn.nintendo.net", lsNsIP);
                    _ = dicServiceV4.TryAdd("ctest-ul-lp1.cdn.nintendo.net", lsNsIP);
                    _ = dicServiceV6.TryAdd("atum.hac.lp1.d4c.nintendo.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("bugyo.hac.lp1.eshop.nintendo.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("ctest-dl-lp1.cdn.nintendo.net", lsEmptyIP);
                    _ = dicServiceV6.TryAdd("ctest-ul-lp1.cdn.nintendo.net", lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsNsIP = new() { new ResouceRecord { Datas = nsIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd("atum.hac.lp1.d4c.nintendo.net", lsNsIP);
                    _ = dicServiceV6.TryAdd("bugyo.hac.lp1.eshop.nintendo.net", lsNsIP);
                    _ = dicServiceV6.TryAdd("ctest-dl-lp1.cdn.nintendo.net", lsNsIP);
                    _ = dicServiceV6.TryAdd("ctest-ul-lp1.cdn.nintendo.net", lsNsIP);
                    _ = dicServiceV4.TryAdd("atum.hac.lp1.d4c.nintendo.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("bugyo.hac.lp1.eshop.nintendo.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("ctest-dl-lp1.cdn.nintendo.net", lsEmptyIP);
                    _ = dicServiceV4.TryAdd("ctest-ul-lp1.cdn.nintendo.net", lsEmptyIP);
                }
            }
            _ = dicServiceV4.TryAdd("atum-eda.hac.lp1.d4c.nintendo.net", lsEmptyIP);
            _ = dicServiceV6.TryAdd("atum-eda.hac.lp1.d4c.nintendo.net", lsEmptyIP);
            if (eaIP != null)
            {
                if ((new IPAddress(eaIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsEaIP = new() { new ResouceRecord { Datas = eaIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd("origin-a.akamaihd.net", lsEaIP);
                    _ = dicServiceV6.TryAdd("origin-a.akamaihd.net", lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsEaIP = new() { new ResouceRecord { Datas = eaIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd("origin-a.akamaihd.net", lsEaIP);
                    _ = dicServiceV4.TryAdd("origin-a.akamaihd.net", lsEmptyIP);
                }
            }
            _ = dicServiceV4.TryAdd("ssl-lvlt.cdn.ea.com", lsEmptyIP);
            _ = dicServiceV6.TryAdd("ssl-lvlt.cdn.ea.com", lsEmptyIP);
            if (Properties.Settings.Default.BattleStore)
            {
                _ = dicServiceV4.TryAdd("us.cdn.blizzard.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("eu.cdn.blizzard.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("kr.cdn.blizzard.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("level3.blizzard.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("blizzard.gcdn.cloudn.co.kr", lsLocalIP);
                _ = dicServiceV4.TryAdd("level3.ssl.blizzard.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("us.cdn.blizzard.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("eu.cdn.blizzard.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("kr.cdn.blizzard.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("level3.blizzard.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("blizzard.gcdn.cloudn.co.kr", lsEmptyIP);
                _ = dicServiceV6.TryAdd("level3.ssl.blizzard.com", lsEmptyIP);
            }
            if (battleIP != null)
            {
                if ((new IPAddress(battleIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsBattleIP = new() { new ResouceRecord { Datas = battleIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd("blzddist1-a.akamaihd.net", lsBattleIP);
                    _ = dicServiceV6.TryAdd("blzddist1-a.akamaihd.net", lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsBattleIP = new() { new ResouceRecord { Datas = battleIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd("blzddist1-a.akamaihd.net", lsBattleIP);
                    _ = dicServiceV4.TryAdd("blzddist1-a.akamaihd.net", lsLocalIP);
                }
            }
            if (Properties.Settings.Default.EpicStore)
            {
                string localHost = !Properties.Settings.Default.EpicCDN ? "epicgames-download1-1251447533.file.myqcloud.com" : "epicgames-download1.akamaized.net";
                _ = dicServiceV4.TryAdd(localHost, lsLocalIP);
                _ = dicServiceV4.TryAdd("download.epicgames.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("fastly-download.epicgames.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("cloudflare.epicgamescdn.com", lsLocalIP);
                _ = dicServiceV6.TryAdd(localHost, lsEmptyIP);
                _ = dicServiceV6.TryAdd("download.epicgames.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("fastly-download.epicgames.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("cloudflare.epicgamescdn.com", lsEmptyIP);
            }
            if (epicIP != null)
            {
                if ((new IPAddress(epicIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsEpicIP = new() { new ResouceRecord { Datas = epicIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd(epicHosts, lsEpicIP);
                    _ = dicServiceV6.TryAdd(epicHosts, lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsEpicIP = new() { new ResouceRecord { Datas = epicIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd(epicHosts, lsEpicIP);
                    _ = dicServiceV4.TryAdd(epicHosts, lsEmptyIP);
                }
            }
            if (Properties.Settings.Default.UbiStore)
            {
                _ = dicServiceV4.TryAdd("uplaypc-s-ubisoft.cdn.ubi.com", lsLocalIP);
                _ = dicServiceV6.TryAdd("uplaypc-s-ubisoft.cdn.ubi.com", lsEmptyIP);
            }
            if (ubiIP != null)
            {
                if ((new IPAddress(ubiIP)).AddressFamily == AddressFamily.InterNetwork)
                {
                    List<ResouceRecord> lsUbiIP = new() { new ResouceRecord { Datas = ubiIP, TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                    _ = dicServiceV4.TryAdd("uplaypc-s-ubisoft.cdn.ubionline.com.cn", lsUbiIP);
                    _ = dicServiceV6.TryAdd("uplaypc-s-ubisoft.cdn.ubionline.com.cn", lsEmptyIP);
                }
                else
                {
                    List<ResouceRecord> lsUbiIP = new() { new ResouceRecord { Datas = ubiIP, TTL = 100, QueryClass = 1, QueryType = QueryType.AAAA } };
                    _ = dicServiceV6.TryAdd("uplaypc-s-ubisoft.cdn.ubionline.com.cn", lsUbiIP);
                    _ = dicServiceV4.TryAdd("uplaypc-s-ubisoft.cdn.ubionline.com.cn", lsLocalIP);
                }
            }
            if (Properties.Settings.Default.HttpService)
            {
                _ = dicServiceV4.TryAdd("packagespc.xboxlive.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("www.msftconnecttest.com", lsLocalIP);
                _ = dicServiceV4.TryAdd("ctest.cdn.nintendo.net", lsLocalIP);
                _ = dicServiceV6.TryAdd("packagespc.xboxlive.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("www.msftconnecttest.com", lsEmptyIP);
                _ = dicServiceV6.TryAdd("ctest.cdn.nintendo.net", lsEmptyIP);
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
                                    {
                                        if (dicServiceV4.TryGetValue(queryName, out List<ResouceRecord>? lsServiceIp))
                                        {
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsServiceIp;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsServiceIp.Count >= 1) parentForm.SaveLog("DNSv4 查询", queryName + " -> " + string.Join(", ", lsServiceIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x008000);
                                            return;
                                        }
                                        if (Properties.Settings.Default.BattleStore && Properties.Settings.Default.BattleNetease && queryName.EndsWith(".necdn.leihuo.netease.com"))
                                        {
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsLocalIP;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNSv4 查询", queryName + " -> " + string.Join(", ", lsLocalIP.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x008000);
                                            return;
                                        }
                                        if (dicHosts1V4.TryGetValue(queryName, out List<ResouceRecord>? lsHostsIp))
                                        {
                                            if (lsHostsIp.Count >= 2) lsHostsIp = lsHostsIp.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsHostsIp;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsHostsIp.Count >= 1) parentForm.SaveLog("DNSv4 查询", queryName + " -> " + string.Join(", ", lsHostsIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                            return;
                                        }
                                        var lsHostsIp2 = dicHosts2V4.Where(kvp => kvp.Key.IsMatch(queryName)).Select(x => x.Value).FirstOrDefault();
                                        if (lsHostsIp2 != null)
                                        {
                                            dicHosts1V4.TryAdd(queryName, lsHostsIp2);
                                            if (lsHostsIp2.Count >= 2) lsHostsIp2 = lsHostsIp2.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsHostsIp2;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsHostsIp2.Count >= 1) parentForm.SaveLog("DNSv4 查询", queryName + " -> " + string.Join(", ", lsHostsIp2.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                            return;
                                        }
                                        if (Properties.Settings.Default.DoH && !reDoHFilter.IsMatch(queryName))
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
                                                if (json != null)
                                                {
                                                    dns.QR = 1;
                                                    dns.RA = 1;
                                                    dns.RD = 1;
                                                    dns.ResouceRecords = new List<ResouceRecord>();
                                                    if (json.Status == 0 && json.Answer != null)
                                                    {
                                                        foreach (var answer in json.Answer)
                                                        {
                                                            if (answer.Type == 1 && IPAddress.TryParse(answer.Data, out IPAddress? ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetwork)
                                                            {
                                                                dns.ResouceRecords.Add(new ResouceRecord
                                                                {
                                                                    Datas = ipAddress.GetAddressBytes(),
                                                                    TTL = answer.TTL,
                                                                    QueryName = answer.Name,
                                                                    QueryClass = 1,
                                                                    QueryType = QueryType.A
                                                                });
                                                            }
                                                            if (dns.ResouceRecords.Count >= 16) break;
                                                        }
                                                    }
                                                    socket?.SendTo(dns.ToBytes(), client);
                                                    if (Properties.Settings.Default.RecordLog && dns.ResouceRecords.Count >= 1) parentForm.SaveLog("DNSv4 查询", queryName + " -> " + string.Join(", ", json.Answer!.Where(x => x.Type == 1).Select(x => x.Data)), ((IPEndPoint)client).Address.ToString());
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNSv4 查询", queryName, ((IPEndPoint)client).Address.ToString());
                                    break;
                                case QueryType.AAAA:
                                    {
                                        if (dicServiceV6.TryGetValue(queryName, out List<ResouceRecord>? lsServiceIp))
                                        {
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsServiceIp;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsServiceIp.Count >= 1) parentForm.SaveLog("DNSv6 查询", queryName + " -> " + string.Join(", ", lsServiceIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x008000);
                                            return;
                                        }
                                        if (Properties.Settings.Default.BattleStore && Properties.Settings.Default.BattleNetease && queryName.EndsWith(".necdn.leihuo.netease.com"))
                                        {
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsEmptyIP;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            return;
                                        }
                                        if (dicHosts1V6.TryGetValue(queryName, out List<ResouceRecord>? lsHostsIp))
                                        {
                                            if (lsHostsIp.Count >= 2) lsHostsIp = lsHostsIp.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsHostsIp;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsHostsIp.Count >= 1) parentForm.SaveLog("DNSv6 查询", queryName + " -> " + string.Join(", ", lsHostsIp.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                            return;
                                        }
                                        var lsHostsIp2 = dicHosts2V6.Where(kvp => kvp.Key.IsMatch(queryName)).Select(x => x.Value).FirstOrDefault();
                                        if (lsHostsIp2 != null)
                                        {
                                            dicHosts1V6.TryAdd(queryName, lsHostsIp2);
                                            if (lsHostsIp2.Count >= 2) lsHostsIp2 = lsHostsIp2.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsHostsIp2;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog && lsHostsIp2.Count >= 1) parentForm.SaveLog("DNSv6 查询", queryName + " -> " + string.Join(", ", lsHostsIp2.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), 0x0000FF);
                                            return;
                                        }
                                        if (!Properties.Settings.Default.DisableIPv6DNS)
                                        {
                                            if (Properties.Settings.Default.DoH && !reDoHFilter.IsMatch(queryName))
                                            {
                                                string html = ClassWeb.HttpResponseContent(this.dohServer + "/resolve?name=" + ClassWeb.UrlEncode(queryName) + "&type=AAAA", "GET", null, null, null, 6000);
                                                if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
                                                {
                                                    ClassDNS.Api? json = null;
                                                    try
                                                    {
                                                        json = JsonSerializer.Deserialize<ClassDNS.Api>(html, Form1.jsOptions);
                                                    }
                                                    catch { }
                                                    if (json != null)
                                                    {
                                                        dns.QR = 1;
                                                        dns.RA = 1;
                                                        dns.RD = 1;
                                                        dns.ResouceRecords = new List<ResouceRecord>();
                                                        if (json.Status == 0 && json.Answer != null)
                                                        {
                                                            foreach (var answer in json.Answer)
                                                            {
                                                                if (answer.Type == 28 && IPAddress.TryParse(answer.Data, out IPAddress? ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                                                                {
                                                                    dns.ResouceRecords.Add(new ResouceRecord
                                                                    {
                                                                        Datas = ipAddress.GetAddressBytes(),
                                                                        TTL = answer.TTL,
                                                                        QueryName = answer.Name,
                                                                        QueryClass = 1,
                                                                        QueryType = QueryType.AAAA
                                                                    });
                                                                }
                                                                if (dns.ResouceRecords.Count >= 16) break;
                                                            }
                                                        }
                                                        socket?.SendTo(dns.ToBytes(), client);
                                                        if (Properties.Settings.Default.RecordLog && dns.ResouceRecords.Count >= 1) parentForm.SaveLog("DNSv6 查询", queryName + " -> " + string.Join(", ", json.Answer!.Where(x => x.Type == 28).Select(x => x.Data)), ((IPEndPoint)client).Address.ToString());
                                                        return;
                                                    }
                                                }
                                            }
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNSv6 查询", queryName, ((IPEndPoint)client).Address.ToString());
                                            break;
                                        }
                                        else
                                        {
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsEmptyIP;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            return;
                                        }
                                    }
                            }
                        }
                        try
                        {
                            using UdpClient proxy = new(iPEndPoint.Address.AddressFamily);
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

        public void Close()
        {
            socket?.Close();
            socket?.Dispose();
            socket = null;
        }

        public static void SetAkamaiIP(string? ip = null)
        {
            List<string> hosts;
            if (Properties.Settings.Default.GameLink)
            {
                hosts = new List<string>() {
                    "xvcf2.xboxlive.com", "assets2.xboxlive.com", "d2.xboxlive.com", "dlassets2.xboxlive.com",
                    "assets2.xboxlive.cn","d2.xboxlive.cn", "dlassets2.xboxlive.cn",
                    "dl.delivery.mp.microsoft.com", "2.tlu.dl.delivery.mp.microsoft.com",
                    "gst.prod.dl.playstation.net", "gs2.ww.prod.dl.playstation.net", "zeus.dl.playstation.net", "ares.dl.playstation.net",
                    "atum.hac.lp1.d4c.nintendo.net", "bugyo.hac.lp1.eshop.nintendo.net", "ctest-dl-lp1.cdn.nintendo.net", "ctest-ul-lp1.cdn.nintendo.net",
                    "origin-a.akamaihd.net", "blzddist1-a.akamaihd.net",
                    "uplaypc-s-ubisoft.cdn.ubionline.com.cn","uplaypc-s-ubisoft.cdn.ubi.com"
                };
            }
            else
            {
                hosts = new List<string>()
                {
                    "xvcf1.xboxlive.com", "xvcf2.xboxlive.com", "assets1.xboxlive.com", "assets2.xboxlive.com", "d1.xboxlive.com", "d2.xboxlive.com", "dlassets.xboxlive.com", "dlassets2.xboxlive.com",
                    "assets1.xboxlive.cn", "assets2.xboxlive.cn", "d1.xboxlive.cn", "d2.xboxlive.cn", "dlassets.xboxlive.cn", "dlassets2.xboxlive.cn",
                    "dl.delivery.mp.microsoft.com", "tlu.dl.delivery.mp.microsoft.com", "2.tlu.dl.delivery.mp.microsoft.com",
                    "gst.prod.dl.playstation.net", "gs2.ww.prod.dl.playstation.net", "zeus.dl.playstation.net", "ares.dl.playstation.net",
                    "atum.hac.lp1.d4c.nintendo.net", "bugyo.hac.lp1.eshop.nintendo.net", "ctest-dl-lp1.cdn.nintendo.net", "ctest-ul-lp1.cdn.nintendo.net",
                    "origin-a.akamaihd.net", "blzddist1-a.akamaihd.net",
                    "uplaypc-s-ubisoft.cdn.ubionline.com.cn","uplaypc-s-ubisoft.cdn.ubi.com"
                };
            }
            if (!Properties.Settings.Default.EpicCDN) hosts.Add("epicgames-download1.akamaized.net");
            if (string.IsNullOrEmpty(ip))
            {
                foreach (string host in hosts)
                {
                    if (dicService2V4.TryGetValue(host, out List<ResouceRecord>? vlaue))
                    {
                        dicServiceV4.AddOrUpdate(host, vlaue, (oldkey, oldvalue) => vlaue);
                    }
                    else
                    {
                        dicServiceV4.TryRemove(host, out _);
                    }
                    if (dicService2V6.TryGetValue(host, out List<ResouceRecord>? vlaueV6))
                    {
                        dicServiceV6.AddOrUpdate(host, vlaueV6, (oldkey, oldvalue) => vlaueV6);
                    }
                    else
                    {
                        dicServiceV6.TryRemove(host, out _);
                    }
                }
                dicService2V4.Clear();
                dicService2V6.Clear();
            }
            else
            {
                dicService2V4.Clear();
                dicService2V6.Clear();
                List<ResouceRecord> lsIP = new() { new ResouceRecord { Datas = IPAddress.Parse(ip).GetAddressBytes(), TTL = 100, QueryClass = 1, QueryType = QueryType.A } };
                foreach (string host in hosts)
                {
                    if (dicServiceV4.TryGetValue(host, out List<ResouceRecord>? vlaue))
                    {
                        dicService2V4.TryAdd(host, vlaue);
                    }
                    dicServiceV4.AddOrUpdate(host, lsIP, (oldkey, oldvalue) => lsIP);
                    if (dicServiceV6.TryGetValue(host, out List<ResouceRecord>? vlaueV6))
                    {
                        dicService2V6.TryAdd(host, vlaueV6);
                    }
                    dicServiceV6.AddOrUpdate(host, lsEmptyIP, (oldkey, oldvalue) => lsEmptyIP);
                }
            }
        }

        public static void UpdateHosts(string? akamai = null)
        {
            dicHosts1V4.Clear();
            dicHosts1V6.Clear();
            dicHosts2V4.Clear();
            dicHosts2V6.Clear();
            DataTable dt = Form1.dtHosts.Copy();
            dt.RejectChanges();
            foreach (DataRow dr in dt.Rows)
            {
                if (!Convert.ToBoolean(dr["Enable"])) continue;
                string? host = dr["HostName"].ToString()?.Trim().ToLower();
                if (!string.IsNullOrEmpty(host) && IPAddress.TryParse(dr["IP"].ToString()?.Trim(), out IPAddress? ip))
                {
                    if (host.StartsWith("*."))
                    {
                        host = Regex.Replace(host, @"^\*\.", "");
                        Regex re = new("\\." + host.Replace(".", "\\.") + "$");
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (!dicHosts2V4.ContainsKey(re) && reHosts.IsMatch(host))
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
                                _ = dicHosts2V4.TryAdd(re, lsIp);
                            }
                        }
                        else
                        {
                            if (!dicHosts2V6.ContainsKey(re) && reHosts.IsMatch(host))
                            {
                                List<ResouceRecord> lsIp = new()
                                {
                                    new ResouceRecord
                                    {
                                        Datas = ip.GetAddressBytes(),
                                        TTL = 100,
                                        QueryClass = 1,
                                        QueryType = QueryType.AAAA
                                    }
                                };
                                _ = dicHosts2V6.TryAdd(re, lsIp);
                            }
                        }
                    }
                    else
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (!dicHosts1V4.TryGetValue(host, out List<ResouceRecord>? lsIp))
                            {
                                lsIp = new List<ResouceRecord>();
                                _ = dicHosts1V4.TryAdd(host, lsIp);
                            }
                            else if (lsIp.Where(x => new IPAddress(x.Datas!).ToString() == ip.ToString()).FirstOrDefault() != null) continue;
                            lsIp.Add(new ResouceRecord
                            {
                                Datas = ip.GetAddressBytes(),
                                TTL = 100,
                                QueryClass = 1,
                                QueryType = QueryType.A
                            });
                        }
                        else
                        {
                            if (!dicHosts1V6.TryGetValue(host, out List<ResouceRecord>? lsIp))
                            {
                                lsIp = new List<ResouceRecord>();
                                _ = dicHosts1V6.TryAdd(host, lsIp);
                            }
                            else if (lsIp.Where(x => new IPAddress(x.Datas!).ToString() == ip.ToString()).FirstOrDefault() != null) continue;
                            lsIp.Add(new ResouceRecord
                            {
                                Datas = ip.GetAddressBytes(),
                                TTL = 100,
                                QueryClass = 1,
                                QueryType = QueryType.AAAA
                            });
                        }
                    }
                }
            }
            foreach (Regex re in dicHosts2V4.Keys)
            {
                if (!dicHosts2V6.ContainsKey(re))
                    _ = dicHosts2V6.TryAdd(re, lsEmptyIP);
            }
            foreach (Regex re in dicHosts2V6.Keys)
            {
                if (!dicHosts2V4.ContainsKey(re))
                    _ = dicHosts2V4.TryAdd(re, lsEmptyIP);
            }
            foreach (string host in dicHosts1V4.Keys)
            {
                if (!dicHosts1V6.ContainsKey(host))
                    _ = dicHosts1V6.TryAdd(host, lsEmptyIP);
            }
            foreach (string host in dicHosts1V6.Keys)
            {
                if (!dicHosts1V4.ContainsKey(host))
                    _ = dicHosts1V4.TryAdd(host, lsEmptyIP);
            }

            List<ResouceRecord> lsIp2V4 = new(), lsIp2V6 = new();
            if (string.IsNullOrEmpty(akamai))
            {
                List<IPAddress> lsIpTmp = new();
                foreach (string str in Properties.Settings.Default.IpsAkamai.Replace("，", ",").Split(','))
                {
                    if (IPAddress.TryParse(str.Trim(), out IPAddress? ip))
                    {
                        if (!lsIpTmp.Contains(ip))
                        {
                            lsIpTmp.Add(ip);
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                            {
                                lsIp2V4.Add(new ResouceRecord
                                {
                                    Datas = ip.GetAddressBytes(),
                                    TTL = 100,
                                    QueryClass = 1,
                                    QueryType = QueryType.A
                                });
                            }
                            else
                            {
                                lsIp2V6.Add(new ResouceRecord
                                {
                                    Datas = ip.GetAddressBytes(),
                                    TTL = 100,
                                    QueryClass = 1,
                                    QueryType = QueryType.AAAA
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                lsIp2V4.Add(new ResouceRecord
                {
                    Datas = IPAddress.Parse(akamai).GetAddressBytes(),
                    TTL = 100,
                    QueryClass = 1,
                    QueryType = QueryType.A
                });
            }
            if (lsIp2V4.Count >= 1 || lsIp2V6.Count >= 1)
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
                            _ = dicHosts2V4.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V4);
                            _ = dicHosts2V6.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V6);
                        }
                    }
                    else if (reHosts.IsMatch(host))
                    {
                        _ = dicHosts1V4.TryAdd(host, lsIp2V4);
                        _ = dicHosts1V6.TryAdd(host, lsIp2V6);
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
                                _ = dicHosts2V4.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V4);
                                _ = dicHosts2V6.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V6);
                            }
                        }
                        else if (host.StartsWith("*"))
                        {
                            host = Regex.Replace(host, @"^\*", "");
                            if (reHosts.IsMatch(host))
                            {
                                _ = dicHosts1V4.TryAdd(host, lsIp2V4);
                                _ = dicHosts1V6.TryAdd(host, lsIp2V6);
                                _ = dicHosts2V4.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V4);
                                _ = dicHosts2V6.TryAdd(new Regex("\\." + host.Replace(".", "\\.") + "$"), lsIp2V6);

                            }
                        }
                        else if (reHosts.IsMatch(host))
                        {
                            _ = dicHosts1V4.TryAdd(host, lsIp2V4);
                            _ = dicHosts1V6.TryAdd(host, lsIp2V6);
                        }
                    }
                }
            }
        }

        public static void FlushDns()
        {
            using Process p = new();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            p.StandardInput.WriteLine("ipconfig /flushdns");
            p.StandardInput.WriteLine("exit");
            p.StandardInput.Close();
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
        public static void SetDns(string? dns = null)
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
            string html = ClassWeb.HttpResponseContent("https://www.ipshudi.com/" + ip + ".htm", "GET", null, null, null, 3000);
            Match result = Regex.Match(html, @"<tr>\n<td[^>]*>归属地</td>\n<td>\n<span>(?<location1>.+)</span>(\n?.+\n</td>\n</tr>\n<tr><td[^>]*>运营商</td><td><span>(?<location2>.+)</span></td></tr>)?");
            if (result.Success) return Regex.Replace(result.Groups["location1"].Value.Trim() + " " + result.Groups["location2"].Value.Trim(), @"<[^>]+>", "").Trim() + " (来源：ip138.com)";
            else
            {
                html = ClassWeb.HttpResponseContent("https://ip.zxinc.org/api.php?type=json&ip=" + ip, "GET", null, null, null, 3000);
                result = Regex.Match(html, @"""location"":""(?<location>[^""]+)""");
                if (result.Success) return Regex.Replace(result.Groups["location"].Value.Trim(), @"\\t", " ").Trim();
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
                MatchCollection mc = Regex.Matches(resultInfo, @":\s+(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})");
                if (mc.Count >= 1) ip = mc[^1].Groups["ip"].Value;
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
