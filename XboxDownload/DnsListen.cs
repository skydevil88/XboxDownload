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
        private readonly string dohServer = Environment.OSVersion.Version.Major >= 10 ? "https://223.5.5.5" : "http://223.5.5.5";
        private readonly Regex reDoHBlacklist = new("google|youtube|facebook|twitter");
        public static Regex reHosts = new(@"^[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$");
        public static ConcurrentDictionary<String, List<ResouceRecord>> dicHosts1 = new(), dicCdn1 = new();
        public static ConcurrentDictionary<Regex, List<ResouceRecord>> dicHosts2 = new(), dicCdn2 = new();
        public static ConcurrentDictionary<String, String> dicDns = new();
        private Byte[]? comIP = null, cnIP = null, cnIP2 = null, appIP = null;

        public DnsListen(Form1 parentForm)
        {
            this.parentForm = parentForm;
        }

        public Byte[]? ComIP
        {
            set { this.comIP = value; }
        }

        public Byte[]? CnIP
        {
            set { this.cnIP = value; }
        }

        public Byte[]? CnIP2
        {
            set { this.cnIP2 = value; }
        }

        public Byte[]? AppIP
        {
            set { this.appIP = value; }
        }

        public void Listen()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && !x.Description.Contains("Virtual")).ToArray();
            if (Properties.Settings.Default.SetDns)
            {
                dicDns.Clear();
                using var key = Microsoft.Win32.Registry.LocalMachine;
                foreach (NetworkInterface adapter in adapters)
                {
                    var rk = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + adapter.Id);
                    if (rk != null)
                    {
                        string? dns = rk.GetValue("NameServer", null) as string;
                        if (dns == Properties.Settings.Default.LocalIP) dns = null;
                        dicDns.TryAdd(adapter.Id, dns ?? string.Empty);
                        rk.Close();
                    }
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
            if (!Form1.bServiceFlag) return;

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

            if (!string.IsNullOrEmpty(Properties.Settings.Default.ComIP))
            {
                comIP = IPAddress.Parse(Properties.Settings.Default.ComIP).GetAddressBytes();
            }
            else
            {
                if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbComIP, Properties.Settings.Default.LocalIP);
                comIP = IPAddress.Parse(Properties.Settings.Default.LocalIP).GetAddressBytes();
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP))
            {
                cnIP = IPAddress.Parse(Properties.Settings.Default.CnIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("assets1.xboxlive.cn") : ClassDNS.HostToIP("assets1.xboxlive.cn", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbCnIP, ip);
                        cnIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.CnIP2))
            {
                cnIP2 = IPAddress.Parse(Properties.Settings.Default.CnIP2).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("dlassets.xboxlive.cn") : ClassDNS.HostToIP("dlassets.xboxlive.cn", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbCnIP2, ip);
                        cnIP2 = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.AppIP))
            {
                appIP = IPAddress.Parse(Properties.Settings.Default.AppIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("tlu.dl.delivery.mp.microsoft.com") : ClassDNS.HostToIP("tlu.dl.delivery.mp.microsoft.com", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbAppIP, ip);
                        appIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }

            Byte[]? psIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.PSIP))
            {
                psIP = IPAddress.Parse(Properties.Settings.Default.PSIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("gst.prod.dl.playstation.net") : ClassDNS.HostToIP("gst.prod.dl.playstation.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbPSIP, ip);
                        psIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            Byte[]? nsIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.NSIP))
            {
                nsIP = IPAddress.Parse(Properties.Settings.Default.NSIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("atum.hac.lp1.d4c.nintendo.net") : ClassDNS.HostToIP("atum.hac.lp1.d4c.nintendo.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbNSIP, ip);
                        nsIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            Byte[]? eaIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.EAIP))
            {
                eaIP = IPAddress.Parse(Properties.Settings.Default.EAIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("origin-a.akamaihd.net") : ClassDNS.HostToIP("origin-a.akamaihd.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbEAIP, ip);
                        eaIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            Byte[]? battleIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.BattleIP))
            {
                battleIP = IPAddress.Parse(Properties.Settings.Default.BattleIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("blzddist1-a.akamaihd.net") : ClassDNS.HostToIP("blzddist1-a.akamaihd.net", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbBattleIP, ip);
                        battleIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            Byte[]? epicIP = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.EpicIP))
            {
                epicIP = IPAddress.Parse(Properties.Settings.Default.EpicIP).GetAddressBytes();
            }
            else
            {
                Task.Run(() =>
                {
                    string? ip = Properties.Settings.Default.DoH ? ClassDNS.DoH("epicgames-download1-1251447533.file.myqcloud.com") : ClassDNS.HostToIP("epicgames-download1-1251447533.file.myqcloud.com", Properties.Settings.Default.DnsIP);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (Form1.bServiceFlag) parentForm.SetTextBox(parentForm.tbEpicIP, ip);
                        epicIP = IPAddress.Parse(ip).GetAddressBytes();
                    }
                });
            }
            if (Properties.Settings.Default.SetDns) ClassDNS.SetDns(Properties.Settings.Default.LocalIP);
            while (Form1.bServiceFlag)
            {
                try
                {
                    var client = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                    var buff = new byte[512];
                    int read = socket.ReceiveFrom(buff, ref client);
                    Task.Factory.StartNew(() =>
                    {
                        var dns = new DNS(buff, read);
                        if (dns.QR == 0 && dns.Opcode == 0 && dns.Querys.Count == 1 && (dns.Querys[0].QueryType == QueryType.A || dns.Querys[0].QueryType == QueryType.AAAA))
                        {
                            if (dns.Querys[0].QueryType == QueryType.A)
                            {
                                string queryName = (dns.Querys[0].QueryName ?? string.Empty).ToLower();
                                Byte[]? byteIP = null;
                                int argb = 0;
                                switch (queryName)
                                {
                                    case "assets1.xboxlive.com":
                                    case "assets2.xboxlive.com":
                                    case "dlassets.xboxlive.com":
                                    case "dlassets2.xboxlive.com":
                                    case "d1.xboxlive.com":
                                    case "d2.xboxlive.com":
                                    case "xvcf1.xboxlive.com":
                                    case "xvcf2.xboxlive.com":
                                        byteIP = comIP;
                                        argb = 0x008000;
                                        break;
                                    case "assets1.xboxlive.cn":
                                    case "assets2.xboxlive.cn":
                                    case "d1.xboxlive.cn":
                                    case "d2.xboxlive.cn":
                                        byteIP = cnIP;
                                        argb = 0x008000;
                                        break;
                                    case "dlassets.xboxlive.cn":
                                    case "dlassets2.xboxlive.cn":
                                        byteIP = cnIP2;
                                        argb = 0x008000;
                                        break;
                                    case "dl.delivery.mp.microsoft.com":
                                    case "tlu.dl.delivery.mp.microsoft.com":
                                        byteIP = appIP;
                                        argb = 0x008000;
                                        break;
                                    case "gst.prod.dl.playstation.net":
                                    case "gs2.ww.prod.dl.playstation.net":
                                    case "zeus.dl.playstation.net":
                                    case "ares.dl.playstation.net":
                                        byteIP = psIP;
                                        argb = 0x008000;
                                        break;
                                    case "atum.hac.lp1.d4c.nintendo.net":
                                    case "bugyo.hac.lp1.eshop.nintendo.net":
                                    case "ctest-dl-lp1.cdn.nintendo.net":
                                    case "ctest-ul-lp1.cdn.nintendo.net":
                                        byteIP = nsIP;
                                        argb = 0x008000;
                                        break;
                                    case "atum-eda.hac.lp1.d4c.nintendo.net":
                                        byteIP = new byte[4];
                                        argb = 0x008000;
                                        break;
                                    case "origin-a.akamaihd.net":
                                        byteIP = eaIP;
                                        argb = 0x008000;
                                        break;
                                    case "blzddist1-a.akamaihd.net":
                                    case "blzddist2-a.akamaihd.net":
                                    case "blzddist3-a.akamaihd.net":
                                        byteIP = battleIP;
                                        argb = 0x008000;
                                        break;
                                    case "epicgames-download1-1251447533.file.myqcloud.com":
                                        byteIP = epicIP;
                                        argb = 0x008000;
                                        break;
                                    case "www.msftconnecttest.com":
                                    case "ctest.cdn.nintendo.net":
                                        if (Properties.Settings.Default.HttpService)
                                        {
                                            byteIP = IPAddress.Parse(Properties.Settings.Default.LocalIP).GetAddressBytes();
                                            argb = 0x008000;
                                        }
                                        break;
                                }
                                if (byteIP != null)
                                {
                                    dns.QR = 1;
                                    dns.RA = 1;
                                    dns.RD = 1;
                                    dns.ResouceRecords = new List<ResouceRecord>
                                    {
                                        new ResouceRecord
                                        {
                                            Datas = byteIP,
                                            TTL = 100,
                                            QueryClass = 1,
                                            QueryType = QueryType.A
                                        }
                                    };
                                    socket?.SendTo(dns.ToBytes(), client);
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + (new IPAddress(byteIP)), ((IPEndPoint)client).Address.ToString(), argb);
                                    return;
                                }
                                if (dicHosts1.TryGetValue(queryName, out List<ResouceRecord>? lsIp))
                                {
                                    argb = 0x0000FF;
                                    List<ResouceRecord> lsResouceRecord = lsIp.ToList();
                                    dns.QR = 1;
                                    dns.RA = 1;
                                    dns.RD = 1;
                                    dns.ResouceRecords = lsResouceRecord;
                                    socket?.SendTo(dns.ToBytes(), client);
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsResouceRecord.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), argb);
                                    return;
                                }
                                foreach (var item in dicHosts2)
                                {
                                    if (item.Key.IsMatch(queryName))
                                    {
                                        DnsListen.dicHosts1.TryAdd(queryName, item.Value);
                                        argb = 0x0000FF;
                                        List<ResouceRecord> lsResouceRecord = item.Value.ToList();
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResouceRecords = lsResouceRecord;
                                        socket?.SendTo(dns.ToBytes(), client);
                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsResouceRecord.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), argb);
                                        return;
                                    }
                                }
                                if (Properties.Settings.Default.EnableCdnIP)
                                {
                                    if (dicCdn1.TryGetValue(queryName, out List<ResouceRecord>? lsIp2))
                                    {
                                        argb = 0x0000FF;
                                        List<ResouceRecord> lsResouceRecord = lsIp2.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResouceRecords = lsResouceRecord;
                                        socket?.SendTo(dns.ToBytes(), client);
                                        if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsResouceRecord.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), argb);
                                        return;
                                    }
                                    foreach (var item in dicCdn2)
                                    {
                                        if (item.Key.IsMatch(queryName))
                                        {
                                            DnsListen.dicCdn1.TryAdd(queryName, item.Value);
                                            argb = 0x0000FF;
                                            List<ResouceRecord> lsResouceRecord = item.Value.OrderBy(a => Guid.NewGuid()).Take(16).ToList();
                                            dns.QR = 1;
                                            dns.RA = 1;
                                            dns.RD = 1;
                                            dns.ResouceRecords = lsResouceRecord;
                                            socket?.SendTo(dns.ToBytes(), client);
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", lsResouceRecord.Select(a => new IPAddress(a.Datas ?? Array.Empty<byte>()).ToString()).ToArray()), ((IPEndPoint)client).Address.ToString(), argb);
                                            return;
                                        }
                                    }
                                }
                                if (Properties.Settings.Default.DoH && !reDoHBlacklist.IsMatch(queryName))
                                {
                                    string html = ClassWeb.HttpResponseContent(this.dohServer + "/resolve?name=" + ClassWeb.UrlEncode(queryName) + "&type=A", "GET", null, null, null, 6000);
                                    if (Regex.IsMatch(html.Trim(), @"^{.+}$"))
                                    {
                                        ClassDNS.Api? json = null;
                                        try
                                        {
                                            json = JsonSerializer.Deserialize<ClassDNS.Api>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName + " -> " + string.Join(", ", arrIp.ToArray()), ((IPEndPoint)client).Address.ToString(), argb);
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                                if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", queryName, ((IPEndPoint)client).Address.ToString(), argb);
                            }
                            else // 屏蔽IPv6
                            {
                                socket?.SendTo(Array.Empty<byte>(), client);
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
                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("DNS 查询", ex.Message, ((IPEndPoint)client).Address.ToString());
                        }
                    });
                }
                catch { }
            }
        }

        public void Close()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;
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
            using (var key = Microsoft.Win32.Registry.LocalMachine)
            {
                foreach (var item in DnsListen.dicDns)
                {
                    var rk = key.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + item.Key);
                    if (rk != null)
                    {
                        rk.SetValue("NameServer", string.IsNullOrEmpty(dns) ? item.Value : dns);
                        rk.Close();
                    }
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
                if (string.IsNullOrEmpty(dns))
                {
                    p.StandardInput.WriteLine("enable-NetAdapterBinding -Name * -ComponentID ms_tcpip6");
                    //p.StandardInput.WriteLine("Get-NetAdapter -Physical | Set-DnsClientServerAddress -ResetServerAddresses");
                }
                else
                {
                    p.StandardInput.WriteLine("disable-NetAdapterBinding -Name * -ComponentID ms_tcpip6");
                    //p.StandardInput.WriteLine("Get-NetAdapter -Physical | Set-DnsClientServerAddress -ServerAddresses ('" + dns + "')");
                }
                p.StandardInput.WriteLine("exit");
            }
            catch { }
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
                IPAddress[] ipAddresses = Array.FindAll(Dns.GetHostEntry(hostName).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipAddresses.Length >= 1) ip = ipAddresses[0].ToString();
            }
            else
            {
                string resultInfo = string.Empty;
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("nslookup", hostName + " " + dnsServer)
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
                    var json = JsonSerializer.Deserialize<ClassDNS.Api>(html, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
