using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace XboxDownload
{
    class HttpsListen
    {
        public static X509Certificate2? certificate;
        public static readonly ConcurrentDictionary<String, SniProxy> dicSniProxy = new();
        public static readonly ConcurrentDictionary<Regex, SniProxy> dicSniProxy2 = new();

        public class SniProxy
        {
            public string? Branch { get; set; }
            public string? Sni { get; set; }
            public IPAddress[]? IPs { get; set; }
            public bool CustomIP { get; set; }
            public DateTime Expired { get; set; }
            public SemaphoreSlim Semaphore = new(1, 1);
        }

        public static void CreateCertificate()
        {
            dicSniProxy.Clear();
            dicSniProxy2.Clear();
            // 生成证书
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=XboxDownload", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("packagespc.xboxlive.com");
            sanBuilder.AddDnsName("*.akamai.net");
            sanBuilder.AddDnsName("*.akamaihd.net");
            sanBuilder.AddDnsName("*.akamaized.net");
            sanBuilder.AddDnsName("epicgames-download1-1251447533.file.myqcloud.com");
            sanBuilder.AddDnsName("download.epicgames.com");
            sanBuilder.AddDnsName("fastly-download.epicgames.com");
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
                            if (jeHosts.ValueKind == JsonValueKind.Array)
                            {
                                string? sni = item[1]?.ToString();
                                string? ips = item[2]?.ToString();
                                List<IPAddress>? lsIP = new();
                                if (!string.IsNullOrEmpty(ips))
                                {
                                    foreach (string _ip in ips.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        if (IPAddress.TryParse(_ip.Trim(), out var ip))
                                        {
                                            if (!lsIP.Contains(ip))
                                                lsIP.Add(ip);
                                        }
                                    }
                                }
                                bool customIp = lsIP.Count >= 1;
                                foreach (var str in jeHosts.EnumerateArray())
                                {
                                    string[] splitArray = Regex.Split(str.ToString().Trim(), @"\s*->\s*");
                                    string host = splitArray[0].Trim();
                                    if (string.IsNullOrEmpty(host) || host.StartsWith('#')) continue;
                                    string? branch = splitArray.Length > 1 ? splitArray[1].Trim() : null;
                                    SniProxy proyx = new()
                                    {
                                        Branch = branch,
                                        Sni = sni,
                                        IPs = lsIP.Count >= 1 ? lsIP.ToArray() : null,
                                        CustomIP = customIp
                                    };
                                    if (host.StartsWith('*'))
                                    {
                                        host = host[1..];
                                        if (!host.StartsWith('.'))
                                        {
                                            sanBuilder.AddDnsName(host);
                                            dicSniProxy.TryAdd(host, proyx);
                                            host = "." + host;
                                        }
                                        sanBuilder.AddDnsName('*' + host);
                                        dicSniProxy2.TryAdd(new(host.Replace(".", "\\.") + "$"), proyx);
                                    }
                                    else
                                    {
                                        sanBuilder.AddDnsName(host);
                                        dicSniProxy.TryAdd(host, proyx);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            req.CertificateExtensions.Add(sanBuilder.Build());
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            certificate = new(cert.Export(X509ContentType.Pfx));
        }

        private readonly Form1 parentForm;
        Socket? socket = null;

        public HttpsListen(Form1 parentForm)
        {
            this.parentForm = parentForm;
            CreateCertificate();
        }

        public void Listen()
        {
            int port = 443;
            IPEndPoint ipe = new(Properties.Settings.Default.ListenIP == 0 ? IPAddress.Parse(Properties.Settings.Default.LocalIP) : IPAddress.Any, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(ipe);
                socket.Listen(100);
            }
            catch (SocketException ex)
            {
                parentForm.Invoke(new Action(() =>
                {
                    parentForm.pictureBox1.Image = Properties.Resource.Xbox3;
                    MessageBox.Show($"启用HTTPS服务失败!\n错误信息: {ex.Message}\n\n两种解决方法：\n1、监听IP选择(Any)。\n2、使用netstat查看并解除 {port} 端口占用。", "启用HTTPS服务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }

            X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate!);
            store.Close();

            while (Form1.bServiceFlag)
            {
                try
                {
                    Socket mySocket = socket.Accept();
                    ThreadPool.QueueUserWorkItem(delegate { TcpThread(mySocket); });
                }
                catch { }
            }
        }

        private async void TcpThread(Socket mySocket)
        {
            if (mySocket.Connected)
            {
                mySocket.SendTimeout = 30000;
                mySocket.ReceiveTimeout = 30000;
                using SslStream ssl = new(new NetworkStream(mySocket), false);
                try
                {
                    ssl.AuthenticateAsServer(certificate!, false, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                    ssl.WriteTimeout = 30000;
                    ssl.ReadTimeout = 30000;
                    if (ssl.IsAuthenticated)
                    {
                        while (Form1.bServiceFlag && mySocket.Connected && mySocket.Poll(3000000, SelectMode.SelectRead))
                        {
                            Byte[] _receive = new Byte[4096];
                            int _num = ssl.Read(_receive, 0, _receive.Length);
                            string headers = string.Empty;
                            long contentLength = 0, bodyLength = 0;
                            List<Byte> list = new();
                            for (int i = 1; i <= _num - 4; i++)
                            {
                                if (BitConverter.ToString(_receive, i, 4) == "0D-0A-0D-0A")
                                {
                                    headers = Encoding.ASCII.GetString(_receive, 0, i + 4);
                                    Match m1 = Regex.Match(headers, @"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase);
                                    if (m1.Success)
                                    {
                                        contentLength = Convert.ToInt32(m1.Groups["ContentLength"].Value);
                                    }
                                    Byte[] dest = new Byte[_num - i - 4];
                                    Buffer.BlockCopy(_receive, i + 4, dest, 0, dest.Length);
                                    list.AddRange(dest);
                                    bodyLength = dest.Length;
                                    break;
                                }
                            }
                            while (bodyLength < contentLength)
                            {
                                _num = ssl.Read(_receive, 0, _receive.Length);
                                Byte[] dest = new Byte[_num];
                                Buffer.BlockCopy(_receive, 0, dest, 0, dest.Length);
                                list.AddRange(dest);
                                bodyLength += _num;
                            }
                            Match result = Regex.Match(headers, @"(?<method>GET|POST|PUP|DELETE|OPTIONS|HEAD) (?<path>[^\s]+)");
                            if (!result.Success)
                            {
                                mySocket.Close();
                                continue;
                            }
                            string _method = result.Groups["method"].Value;
                            string _filePath = Regex.Replace(result.Groups["path"].Value.Trim(), @"^https?://[^/]+", "");
                            result = Regex.Match(headers, @"Host:(.+)");
                            if (!result.Success)
                            {
                                mySocket.Close();
                                continue;
                            }
                            string _host = result.Groups[1].Value.Trim().ToLower();
                            string _tmpPath = Regex.Replace(_filePath, @"\?.+$", ""), _localPath = string.Empty;
                            if (Properties.Settings.Default.LocalUpload)
                            {
                                if (File.Exists(Properties.Settings.Default.LocalPath + _tmpPath))
                                    _localPath = Properties.Settings.Default.LocalPath + _tmpPath.Replace("/", "\\");
                                else if (File.Exists(Properties.Settings.Default.LocalPath + "\\" + Path.GetFileName(_tmpPath)))
                                    _localPath = Properties.Settings.Default.LocalPath + "\\" + Path.GetFileName(_tmpPath);
                            }
                            string _extension = Path.GetExtension(_tmpPath).ToLowerInvariant();
                            if (Properties.Settings.Default.LocalUpload && !string.IsNullOrEmpty(_localPath))
                            {
                                FileStream? fs = null;
                                try
                                {
                                    fs = new(_localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                }
                                catch (Exception ex)
                                {
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("本地上传", ex.Message, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0xFF0000);
                                }
                                if (fs != null)
                                {
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("本地上传", _localPath, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString());
                                    using BinaryReader br = new(fs);
                                    string _contentRange = string.Empty, _status = "200 OK";
                                    long _fileLength = br.BaseStream.Length, _startPosition = 0;
                                    long _endPosition = _fileLength;
                                    result = Regex.Match(headers, @"Range: bytes=(?<StartPosition>\d+)(-(?<EndPosition>\d+))?");
                                    if (result.Success)
                                    {
                                        _startPosition = long.Parse(result.Groups["StartPosition"].Value);
                                        if (_startPosition > br.BaseStream.Length) _startPosition = 0;
                                        if (!string.IsNullOrEmpty(result.Groups["EndPosition"].Value))
                                            _endPosition = long.Parse(result.Groups["EndPosition"].Value) + 1;
                                        _contentRange = "bytes " + _startPosition + "-" + (_endPosition - 1) + "/" + _fileLength;
                                        _status = "206 Partial Content";
                                    }

                                    StringBuilder sb = new();
                                    sb.Append("HTTP/1.1 " + _status + "\r\n");
                                    sb.Append("Content-Type: " + ClassWeb.GetMimeMapping(_tmpPath) + "\r\n");
                                    sb.Append("Content-Length: " + (_endPosition - _startPosition) + "\r\n");
                                    if (_contentRange != null) sb.Append("Content-Range: " + _contentRange + "\r\n");
                                    sb.Append("Accept-Ranges: bytes\r\n\r\n");

                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    ssl.Write(_headers);

                                    br.BaseStream.Position = _startPosition;
                                    int _size = 4096;
                                    while (Form1.bServiceFlag && mySocket.Connected)
                                    {
                                        long _remaining = _endPosition - br.BaseStream.Position;
                                        byte[] _response = new byte[_remaining <= _size ? _remaining : _size];
                                        br.Read(_response, 0, _response.Length);
                                        ssl.Write(_response);
                                        if (_remaining <= _size) break;
                                    }
                                    ssl.Flush();
                                    fs.Close();
                                    fs.Dispose();
                                }
                                else
                                {
                                    Byte[] _response = Encoding.ASCII.GetBytes("Internal Server Error");
                                    StringBuilder sb = new();
                                    sb.Append("HTTP/1.1 500 Server Error\r\n");
                                    sb.Append("Content-Type: text/html\r\n");
                                    sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    ssl.Write(_headers);
                                    ssl.Write(_response);
                                }
                            }
                            else
                            {
                                bool bFileFound = false;
                                string _url = "https://" + _host + _filePath;
                                switch (_host)
                                {
                                    case "packagespc.xboxlive.com":
                                        {
                                            string? ip = string.Empty;
                                            if (DnsListen.dicHosts1V6.TryGetValue(_host, out List<ResouceRecord>? lsHostsIpV6) && lsHostsIpV6.Count >= 1)
                                                ip = "[" + new IPAddress(lsHostsIpV6[0].Datas!).ToString() + "]";
                                            else if (DnsListen.dicHosts1V4.TryGetValue(_host, out List<ResouceRecord>? lsHostsIpV4) && lsHostsIpV4.Count >= 1)
                                                ip = new IPAddress(lsHostsIpV4[0].Datas!).ToString();
                                            else if (DnsListen.dicDoHServer.TryGetValue(_host, out DnsListen.DoHServer? doh))
                                                ip = ClassDNS.DoH(_host, doh.Website, doh.Headers);
                                            else
                                                ip = ClassDNS.DoH(_host);
                                            if (!string.IsNullOrEmpty(ip))
                                            {
                                                bFileFound = true;
                                                Match m1 = Regex.Match(headers, @"Authorization:(.+)");
                                                if (m1.Success)
                                                {
                                                    Properties.Settings.Default.Authorization = m1.Groups[1].Value.Trim();
                                                    Properties.Settings.Default.Save();
                                                }
                                                using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(_url.Replace(_host, ip), "GET", null, null, new() { { "Host", _host }, { "Authorization", Properties.Settings.Default.Authorization } });
                                                if (response != null && response.IsSuccessStatusCode)
                                                {
                                                    byte[] buffer = response.Content.ReadAsByteArrayAsync().Result;
                                                    Byte[] _headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n" + response.Content.Headers + response.Headers + "\r\n");
                                                    ssl.Write(_headers);
                                                    ssl.Write(buffer);
                                                    ssl.Flush();
                                                    string html = response.Content.ReadAsStringAsync().Result;
                                                    if (Regex.IsMatch(html, @"^{.+}$"))
                                                    {
                                                        string? contentId = null;
                                                        XboxGameDownload.PackageFiles? packageFiles = null;
                                                        try
                                                        {
                                                            var json = JsonSerializer.Deserialize<XboxGameDownload.Game>(html, Form1.jsOptions);
                                                            if (json != null && json.PackageFound)
                                                            {
                                                                contentId = json.ContentId;
                                                                packageFiles = json.PackageFiles.Where(x => x.RelativeUrl.EndsWith(".msixvc", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                                            }
                                                        }
                                                        catch { }
                                                        if (packageFiles != null)
                                                        {
                                                            string url = packageFiles.CdnRootPaths[0].Replace(".xboxlive.cn", ".xboxlive.com") + packageFiles.RelativeUrl;
                                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("下载链接", url, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0x008000);
                                                            Match m2 = Regex.Match(url, @"(?<version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}");
                                                            if (m2.Success)
                                                            {
                                                                Version version = new(m2.Groups["version"].Value);
                                                                string key = (contentId ?? string.Empty).ToLower();
                                                                if (XboxGameDownload.dicXboxGame.TryGetValue(key, out XboxGameDownload.Products? XboxGame))
                                                                {
                                                                    if (XboxGame.Version >= version) return;
                                                                }
                                                                XboxGame = new XboxGameDownload.Products
                                                                {
                                                                    Version = version,
                                                                    FileSize = packageFiles.FileSize,
                                                                    Url = url
                                                                };
                                                                XboxGameDownload.dicXboxGame.AddOrUpdate(key, XboxGame, (oldkey, oldvalue) => XboxGame);
                                                                XboxGameDownload.SaveXboxGame();
                                                                _ = ClassWeb.HttpResponseContent(UpdateFile.website + "/Game/AddGameUrl?url=" + ClassWeb.UrlEncode(XboxGame.Url), "PUT", null, null, null, 30000, "XboxDownload");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (Properties.Settings.Default.RecordLog)
                                                {
                                                    parentForm.SaveLog("HTTP 500", "域名 packagespc.xboxlive.com 没法访问，请把此域名加入强制加密DNS列表，选择使用国外DoH服务器、并且勾上“设置本机 DNS”。", ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0xFF0000);
                                                }
                                            }
                                        }
                                        break;
                                    case "epicgames-download1-1251447533.file.myqcloud.com":
                                    case "epicgames-download1.akamaized.net":
                                    case "download.epicgames.com":
                                    case "fastly-download.epicgames.com":
                                        if (_filePath.Contains(".manifest") && _host != "epicgames-download1-1251447533.file.myqcloud.com")
                                        {
                                            string? ip = ClassDNS.DoH(_host);
                                            if (!string.IsNullOrEmpty(ip))
                                            {
                                                using HttpResponseMessage? response = ClassWeb.HttpResponseMessage(_url.Replace(_host, ip), "GET", null, null, new() { { "Host", _host } });
                                                if (response != null && response.IsSuccessStatusCode)
                                                {
                                                    bFileFound = true;
                                                    Byte[] _headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n" + response.Content.Headers + response.Headers + "\r\n");
                                                    Byte[] _response = response.Content.ReadAsByteArrayAsync().Result;
                                                    ssl.Write(_headers);
                                                    ssl.Write(_response);
                                                    ssl.Flush();
                                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 200", _url, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0x008000);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            bFileFound = true;
                                            _url = "https://" + (Properties.Settings.Default.EpicCDN ? "epicgames-download1-1251447533.file.myqcloud.com" : "epicgames-download1.akamaized.net") + _filePath;
                                            StringBuilder sb = new();
                                            sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                                            sb.Append("Content-Type: text/html\r\n");
                                            sb.Append("Location: " + _url + "\r\n");
                                            sb.Append("Content-Length: 0\r\n\r\n");
                                            Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                            ssl.Write(_headers);
                                            ssl.Flush();
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 302", _url, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0x008000);
                                        }
                                        break;
                                    default:
                                        if (Properties.Settings.Default.SniProxy)
                                        {
                                            if (!dicSniProxy.TryGetValue(_host, out SniProxy? proxy))
                                            {
                                                var proxy2 = dicSniProxy2.Where(kvp => kvp.Key.IsMatch(_host)).Select(x => x.Value).FirstOrDefault();
                                                if (proxy2 != null)
                                                {
                                                    proxy = new()
                                                    {
                                                        Branch = proxy2.Branch,
                                                        Sni = proxy2.Sni,
                                                        IPs = proxy2.IPs,
                                                        CustomIP = proxy2.CustomIP
                                                    };
                                                    dicSniProxy.TryAdd(_host, proxy);
                                                }
                                            }
                                            if (proxy != null)
                                            {
                                                if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("Proxy", _url, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString(), 0x008000);
                                                bFileFound = true;
                                                IPAddress[]? ips = null;
                                                if (proxy.IPs == null || proxy.IPs.Length == 0 || (!proxy.CustomIP && DateTime.Compare(proxy.Expired, DateTime.Now) < 0))
                                                {
                                                    await proxy.Semaphore.WaitAsync();
                                                    if (proxy.IPs == null || proxy.IPs.Length == 0 || (!proxy.CustomIP && DateTime.Compare(proxy.Expired, DateTime.Now) < 0))
                                                    {
                                                        proxy.IPs = null;
                                                        string domain = proxy.Branch ?? _host;
                                                        string[] dohs = Properties.Settings.Default.SniProxys.Split(',');
                                                        if (dohs.Length == 1)
                                                        {
                                                            int index = int.Parse(dohs[0]);
                                                            if (index >= DnsListen.dohs.GetLongLength(0)) index = 3;
                                                            string dohServer = DnsListen.dohs[index, 1];
                                                            Dictionary<string, string>? dohHeaders = null;
                                                            if (!string.IsNullOrEmpty(DnsListen.dohs[index, 2])) dohHeaders = new() { { "Host", DnsListen.dohs[index, 2] } };
                                                            IPAddress[]? ipV6 = null, ipV4 = null;
                                                            Task[] tasks = new Task[2];
                                                            tasks[0] = Task.Run(() => {
                                                                if (Properties.Settings.Default.SniProxysIPv6 && Form1.bIPv6Support)
                                                                    ipV6 = ClassDNS.DoH2(domain, dohServer, dohHeaders, false);
                                                            });
                                                            tasks[1] = Task.Run(() => {
                                                                ipV4 = ClassDNS.DoH2(domain, dohServer, dohHeaders, true);
                                                            });
                                                            await Task.WhenAll(tasks);
                                                            proxy.IPs = ipV6 ?? ipV4;
                                                        }
                                                        else
                                                        {
                                                            ConcurrentBag<IPAddress> lsIPv6 = new(), lsIPv4 = new();
                                                            Task[] tasks = new Task[2];
                                                            tasks[0] = Task.Run(async () => {
                                                                if (Properties.Settings.Default.SniProxysIPv6 && Form1.bIPv6Support)
                                                                {
                                                                    var tasks2 = dohs.Select(i => Task.Run(() =>
                                                                    {
                                                                        int index = int.Parse(i);
                                                                        if (index < DnsListen.dohs.GetLongLength(0))
                                                                        {
                                                                            IPAddress[]? iPAddresses = ClassDNS.DoH2(domain, DnsListen.dohs[index, 1], string.IsNullOrEmpty(DnsListen.dohs[index, 2]) ? null : new() { { "Host", DnsListen.dohs[index, 2] } }, false, 3000);
                                                                            if (iPAddresses != null)
                                                                            {
                                                                                foreach (var item in iPAddresses)
                                                                                {
                                                                                    if (!lsIPv6.Contains(item)) lsIPv6.Add(item);
                                                                                }
                                                                            }
                                                                        }
                                                                    })).ToArray();
                                                                    await Task.WhenAll(tasks2);
                                                                }
                                                            });
                                                            tasks[1] = Task.Run(async () => {
                                                                var tasks2 = dohs.Select(i => Task.Run(() =>
                                                                {
                                                                    int index = int.Parse(i);
                                                                    if (index < DnsListen.dohs.GetLongLength(0))
                                                                    {
                                                                        IPAddress[]? iPAddresses = ClassDNS.DoH2(domain, DnsListen.dohs[index, 1], string.IsNullOrEmpty(DnsListen.dohs[index, 2]) ? null : new() { { "Host", DnsListen.dohs[index, 2] } }, true, 3000);
                                                                        if (iPAddresses != null)
                                                                        {
                                                                            foreach (var item in iPAddresses)
                                                                            {
                                                                                if (!lsIPv4.Contains(item)) lsIPv4.Add(item);
                                                                            }
                                                                        }
                                                                    }
                                                                })).ToArray();
                                                                await Task.WhenAll(tasks2);
                                                            });
                                                            await Task.WhenAll(tasks);
                                                            proxy.IPs = !lsIPv6.IsEmpty ? lsIPv6.ToArray() : lsIPv4.ToArray();
                                                        }
                                                        if (Properties.Settings.Default.SniPorxyOptimized && proxy.IPs?.Length >= 2)
                                                        {
                                                            CancellationTokenSource cts = new();
                                                            var tasks = proxy.IPs.Select(ip => Task.Run(async () =>
                                                            {
                                                                Socket socket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                                                try
                                                                {
                                                                    await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, new IPEndPoint(ip, 443), null);
                                                                    socket.Close();
                                                                    socket.Dispose();
                                                                    if (!cts.IsCancellationRequested)
                                                                    {
                                                                        return ip;
                                                                    }
                                                                    else
                                                                    {
                                                                        return null;
                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    socket.Dispose();
                                                                    if (!cts.IsCancellationRequested)
                                                                        await Task.Delay(1000, cts.Token);
                                                                    return null;
                                                                }
                                                            })).ToArray();
                                                            IPAddress? ip = await Task.WhenAny(tasks).Result;
                                                            cts.Cancel();
                                                            if (ip != null) ips = proxy.IPs = new IPAddress[1] { ip };
                                                        }
                                                        proxy.Expired = DateTime.Now.AddMinutes(Properties.Settings.Default.SniPorxyExpired);
                                                    }
                                                    proxy.Semaphore.Release();
                                                }
                                                ips ??= proxy.IPs?.Length >= 2 ? proxy.IPs.OrderBy(a => Guid.NewGuid()).Take(16).ToArray() : proxy.IPs;

                                                string? errMessae = null;
                                                if (ips != null && ips.Length >= 1)
                                                {
                                                    if (!ClassWeb.SniProxy(ips, proxy.Sni, Encoding.ASCII.GetBytes(headers), list.ToArray(), ssl, out IPAddress? remoteIP, out errMessae))
                                                    {
                                                        if (!proxy.CustomIP && remoteIP == null) proxy.IPs = null;
                                                    }
                                                }
                                                else errMessae = "Unable to query domain " + _host + ".";
                                                if (!string.IsNullOrEmpty(errMessae))
                                                {
                                                    Byte[] _response = Encoding.ASCII.GetBytes(errMessae);
                                                    StringBuilder sb = new();
                                                    sb.Append("HTTP/1.1 500 Server Error\r\n");
                                                    sb.Append("Content-Type: text/html\r\n");
                                                    sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                                    ssl.Write(Encoding.ASCII.GetBytes(sb.ToString()));
                                                    ssl.Write(_response);
                                                    ssl.Flush();
                                                }
                                            }
                                        }
                                        break;
                                }
                                if (!bFileFound)
                                {
                                    Byte[] _response = Encoding.ASCII.GetBytes("File not found.");
                                    StringBuilder sb = new();
                                    sb.Append("HTTP/1.1 404 Not Found\r\n");
                                    sb.Append("Content-Type: text/html\r\n");
                                    sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    ssl.Write(_headers);
                                    ssl.Write(_response);
                                    ssl.Flush();
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 404", _url, ((IPEndPoint)mySocket.RemoteEndPoint!).Address.ToString());
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            if (mySocket.Connected)
            {
                try
                {
                    mySocket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    mySocket.Close();
                }
            }
            mySocket.Dispose();
        }

        public void Close()
        {
            socket?.Close();
            socket?.Dispose();
            socket = null;

            using X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=XboxDownload", false);
            if (certificates.Count > 0) store.RemoveRange(certificates);
            store.Close();
        }
    }
}
