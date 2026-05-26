using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.Utilities;
using XboxDownload.ViewModels;

namespace XboxDownload.Services;

public partial class TcpConnectionListener(ServiceViewModel serviceViewModel)
{
    private static X509Certificate2? _certificate;
    private static Socket? _httpSocket;
    private static Socket? _httpsSocket;
    private const int HttpPort = 80;
    private const int HttpsPort = 443;
    private bool _isSimplifiedChinese;

    public static readonly ConcurrentDictionary<string, (SniProxy, string[]?)> DicSniProxy = new();
    public static readonly ConcurrentDictionary<string, (SniProxy, string[]?)> DicSniProxy2 = new();

    public class SniProxy
    {
        public string? Branch { get; init; }
        public string? Sni { get; init; }
        public IPAddress[]? IpAddresses { get; set; }
        public IPAddress[]? IpAddressesV4 { get; init; }
        public IPAddress[]? IpAddressesV6 { get; init; }
        public bool UseCustomIpAddress { get; init; }
        public readonly SemaphoreSlim Semaphore = new(1, 1);
    }

    public async Task GenerateServerCertificate()
    {
        DicSniProxy.Clear();
        DicSniProxy2.Clear();

        if (!OperatingSystem.IsWindows())
        {
            // Ensure Root CA is created
            await CertificateHelper.CreateRootCertificate();
            if (!File.Exists(CertificateHelper.RootPfxPath)) return;
        }

        using var serverRsa = RSA.Create(2048);
        var serverReq = new CertificateRequest($"CN={nameof(XboxDownload)}, O={nameof(XboxDownload)}, OU={nameof(XboxDownload)}", serverRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Subject Alternative Names (SAN)
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("*.akamai.net");
        sanBuilder.AddDnsName("*.akamaihd.net");
        sanBuilder.AddDnsName("*.akamaized.net");

        // Load SNI proxy file if exists
        if (App.Settings.IsLocalProxyEnabled)
        {
            string certDomainText;
            await using (var stream = AssetLoader.Open(new Uri($"avares://{nameof(XboxDownload)}/Resources/CertDomain.txt")))
            {
                using var reader = new StreamReader(stream);
                certDomainText = (await reader.ReadToEndAsync()).Trim();
            }
            if (File.Exists(serviceViewModel.CertDomainFilePath))
                certDomainText += Environment.NewLine + await File.ReadAllTextAsync(serviceViewModel.CertDomainFilePath);

            var certDomainMap = new ConcurrentDictionary<string, string[]?>();
            var wildcardDomainMap = new List<KeyValuePair<string, string[]?>>();

            foreach (var line in certDomainText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim().Trim('"').Trim().ToLowerInvariant();
                string[] value;

                try
                {
                    value = JsonSerializer.Deserialize<string[]>(parts[1])!;
                    value = [.. value.Select(v => v.Trim().ToLowerInvariant())];
                }
                catch
                {
                    continue;
                }

                if (key.StartsWith('*'))
                {
                    key = key[1..];
                    if (!key.StartsWith('.'))
                    {
                        certDomainMap.TryAdd(key, value);
                        key = "." + key;
                    }

                    if (!wildcardDomainMap.Any(kv => kv.Key.Equals(key)))
                        wildcardDomainMap.Add(new KeyValuePair<string, string[]?>(key, value));
                }
                else
                {
                    certDomainMap.TryAdd(key, value);
                }
            }

            wildcardDomainMap = [.. wildcardDomainMap.OrderByDescending(kv => kv.Key.Length)];

            foreach (var path in new[] { serviceViewModel.SniProxyFilePath, serviceViewModel.SniProxy2FilePath })
            {
                if (!File.Exists(path)) continue;
                List<List<object>>? sniProxy = null;
                try
                {
                    sniProxy = JsonSerializer.Deserialize<List<List<object>>>(await File.ReadAllTextAsync(path));
                }
                catch
                {
                    // ignored
                }

                if (sniProxy == null) continue;

                foreach (var item in sniProxy)
                {
                    if (item.Count != 3) continue;

                    var jeHosts = (JsonElement)item[0];
                    if (jeHosts.ValueKind != JsonValueKind.Array) continue;

                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    var sni = item[1]?.ToString();
                    var branch = string.Empty;
                    var lsIPv6 = new List<IPAddress>();
                    var lsIPv4 = new List<IPAddress>();

                    var jeIps = (JsonElement)item[2];
                    if (jeIps.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ip in jeIps.EnumerateArray())
                        {
                            if (IPAddress.TryParse(ip.ToString(), out var ipAddress))
                            {
                                switch (ipAddress.AddressFamily)
                                {
                                    case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                        lsIPv6.Add(ipAddress);
                                        break;
                                    case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                        lsIPv4.Add(ipAddress);
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var ips = jeIps.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (ips.Length == 1)
                        {
                            if (IPAddress.TryParse(ips[0], out var ipAddress))
                            {
                                switch (ipAddress.AddressFamily)
                                {
                                    case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                        lsIPv6.Add(ipAddress);
                                        break;
                                    case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                        lsIPv4.Add(ipAddress);
                                        break;
                                }
                            }
                            else
                            {
                                branch = ips[0];
                            }
                        }
                        else
                        {
                            foreach (var ip in ips)
                            {
                                if (IPAddress.TryParse(ip, out var ipAddress))
                                {
                                    switch (ipAddress.AddressFamily)
                                    {
                                        case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                            lsIPv6.Add(ipAddress);
                                            break;
                                        case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                            lsIPv4.Add(ipAddress);
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    var customIp = lsIPv4.Count >= 1 || lsIPv6.Count >= 1;

                    foreach (var str in jeHosts.EnumerateArray())
                    {
                        var splitArray = str.ToString().Trim().Split("->", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        var host = splitArray[0].Trim();
                        if (string.IsNullOrEmpty(host) || host.StartsWith('#')) continue;

                        if (string.IsNullOrEmpty(branch))
                            branch = splitArray.Length >= 2 ? splitArray[1].Trim() : null;
                        SniProxy proyx = new()
                        {
                            Branch = branch,
                            Sni = sni,
                            IpAddressesV4 = lsIPv4.Count >= 1 ? [.. lsIPv4] : null,
                            IpAddressesV6 = lsIPv6.Count >= 1 ? [.. lsIPv6] : null,
                            UseCustomIpAddress = customIp
                        };

                        if (host.StartsWith('*'))
                        {
                            host = host[1..];
                            if (!host.StartsWith('.'))
                            {
                                sanBuilder.AddDnsName(host);
                                if (!certDomainMap.TryGetValue(host, out var expectedHosts))
                                {
                                    expectedHosts = wildcardDomainMap.Where(kv => host.EndsWith(kv.Key)).Select(kv => kv.Value).FirstOrDefault();
                                }
                                DicSniProxy.TryAdd(host, (proyx, expectedHosts));
                                host = "." + host;
                            }
                            sanBuilder.AddDnsName('*' + host);
                            var wildcardExpectedHosts = wildcardDomainMap.Where(kv => host.EndsWith(kv.Key)).Select(kv => kv.Value).FirstOrDefault();
                            DicSniProxy2.TryAdd(host, (proyx, wildcardExpectedHosts));
                        }
                        else if (host.StartsWith('.'))
                        {
                            sanBuilder.AddDnsName('*' + host);
                            var wildcardExpectedHosts = wildcardDomainMap.Where(kv => host.EndsWith(kv.Key)).Select(kv => kv.Value).FirstOrDefault();
                            DicSniProxy2.TryAdd(host, (proyx, wildcardExpectedHosts));
                        }
                        else
                        {
                            sanBuilder.AddDnsName(host);
                            if (!certDomainMap.TryGetValue(host, out var expectedHosts))
                            {
                                expectedHosts = wildcardDomainMap.Where(kv => host.EndsWith(kv.Key)).Select(kv => kv.Value).FirstOrDefault();
                            }
                            DicSniProxy.TryAdd(host, (proyx, expectedHosts));
                        }
                    }
                }
            }
        }

        serverReq.CertificateExtensions.Add(sanBuilder.Build());

        if (OperatingSystem.IsWindows())
        {
            var utcNow = DateTimeOffset.UtcNow;
            var cert = serverReq.CreateSelfSigned(utcNow, utcNow.AddYears(1));
            var pfxData = cert.Export(X509ContentType.Pfx);
            _certificate = X509CertificateLoader.LoadPkcs12(pfxData, password: null);

            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var existing = store.Certificates.Find(X509FindType.FindBySubjectName, nameof(XboxDownload), false);
            if (existing.Count > 0) store.RemoveRange(existing);
            store.Add(_certificate);
        }
        else
        {
            // Basic extensions
            serverReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            serverReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            var oids = new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                new Oid("1.3.6.1.5.5.7.3.2")  // Client Authentication
            };
            serverReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oids, false));

            // Load Root CA
            using var caCert = X509CertificateLoader.LoadPkcs12FromFile(CertificateHelper.RootPfxPath, password: null, keyStorageFlags: X509KeyStorageFlags.Exportable);

            var notBefore = DateTimeOffset.UtcNow;
            if (notBefore < caCert.NotBefore)
                notBefore = caCert.NotBefore;

            var notAfter = notBefore.AddYears(1);
            if (notAfter > caCert.NotAfter)
                notAfter = caCert.NotAfter;

            // Issue server certificate
            var serial = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var serverCertNoKey = serverReq.Create(caCert, notBefore, notAfter, serial);
            var serverCert = serverCertNoKey.CopyWithPrivateKey(serverRsa);

            // Generate full chain (Leaf + Root)
            var exportCollection = new X509Certificate2Collection
            {
                serverCert,
                caCert
            };

            var pfxData = exportCollection.Export(X509ContentType.Pfx);
            _certificate = X509CertificateLoader.LoadPkcs12(pfxData!, password: null);
        }
    }

    public string Start()
    {
        var ipAddress = App.Settings.ListeningIp == "LocalIp"
            ? IPAddress.Parse(App.Settings.LocalIp)
            : IPAddress.Any;

        _httpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var httpEndPoint = new IPEndPoint(ipAddress, HttpPort);

        _httpsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var httpsEndPoint = new IPEndPoint(ipAddress, HttpsPort);
        try
        {
            _httpSocket.Bind(httpEndPoint);
            _httpSocket.Listen(100);

            _httpsSocket.Bind(httpsEndPoint);
            _httpsSocket.Listen(100);
        }
        catch (SocketException ex)
        {
            serviceViewModel.IsListeningFailed = true;
            return string.Format(ResourceHelper.GetString("Service.Listening.TcpStartFailedDialogMessage"), ex.Message);
        }

        _isSimplifiedChinese = App.Settings.Culture == "zh-Hans";

        _ = Task.Run(() => Listening(_httpSocket, false));
        _ = Task.Run(() => Listening(_httpsSocket, true));

        return string.Empty;
    }

    public static void Stop()
    {
        var httpS = Interlocked.Exchange(ref _httpSocket, null);
        var httpsS = Interlocked.Exchange(ref _httpsSocket, null);

        SafeShutdown(httpS);
        SafeShutdown(httpsS);

        httpS?.Dispose();
        httpsS?.Dispose();

        if (!OperatingSystem.IsWindows()) return;
        
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        var existing = store.Certificates.Find(X509FindType.FindBySubjectName, nameof(XboxDownload), false);
        if (existing.Count > 0) store.RemoveRange(existing);
    }

    private static void SafeShutdown(Socket? socket)
    {
        if (socket is not { Connected: true }) return;

        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Optional
        }
    }

    private async Task Listening(Socket? socket, bool isHttps)
    {
        while (serviceViewModel.IsListening)
        {
            if (socket == null) break;

            try
            {
                var clientSocket = await socket.AcceptAsync(serviceViewModel.ListeningToken);
                _ = isHttps ? Task.Run(() => HttpsThread(clientSocket)) : Task.Run(() => HttpThread(clientSocket));
            }
            catch
            {
                // ignored
            }
        }
    }

    private async Task HttpThread(Socket socket)
    {
        if (socket.Connected)
        {
            socket.SendTimeout = 30000;
            socket.ReceiveTimeout = 30000;
            try
            {
                while (serviceViewModel.IsListening && socket.Connected)
                {
                    if (!TryReadHttpRequestStart(socket, out var request)) break;
                    var headers = request.Headers;

                    var result = HttpRequestMethodAndPathRegex().Match(headers);
                    if (!result.Success) break;
                    var filePath = BaseUrlRegex().Replace(result.Groups["path"].Value.Trim(), "");

                    result = HostHeaderRegex().Match(headers);
                    if (!result.Success) break;
                    var host = result.Groups["host"].Value.Trim().ToLowerInvariant();

                    var tmpPath = QueryStringRegex().Replace(filePath, "");
                    if (TryGetLocalUploadPath(tmpPath, out var localPath))
                    {
                        SendLocalUploadFile(headers, filePath, localPath, new SocketResponseWriter(socket), ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                    }
                    else
                    {
                        var redirect = false;
                        var newHost = string.Empty;
                        switch (host)
                        {
                            case "xvcf1.xboxlive.com":
                            case "assets1.xboxlive.com":
                            case "d1.xboxlive.com":
                                if (App.Settings.IsXboxGameDownloadLinksShown)
                                {
                                    redirect = true;
                                    newHost = DnsConnectionListener.Ipv4ServiceMapBackup.IsEmpty && (_isSimplifiedChinese || App.Settings.XboxGlobalIp == App.Settings.LocalIp)
                                        ? "assets2.xboxlive.cn"
                                        : "assets2.xboxlive.com";
                                }
                                else if (_isSimplifiedChinese)
                                {
                                    redirect = true;
                                    newHost = "assets1.xboxlive.cn";
                                }
                                if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                    _ = UpdateGameUrl(host, tmpPath);
                                break;
                            case "xvcf2.xboxlive.com":
                            case "assets2.xboxlive.com":
                            case "d2.xboxlive.com":
                                redirect = true;
                                newHost = "assets2.xboxlive.cn";
                                if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                    _ = UpdateGameUrl(host, tmpPath);
                                break;
                            case "dlassets.xboxlive.com":
                                if (App.Settings.IsXboxGameDownloadLinksShown)
                                {
                                    redirect = true;
                                    newHost = DnsConnectionListener.Ipv4ServiceMapBackup.IsEmpty && (_isSimplifiedChinese || App.Settings.XboxGlobalIp == App.Settings.LocalIp)
                                        ? "dlassets2.xboxlive.cn"
                                        : "dlassets2.xboxlive.com";
                                }
                                else if (_isSimplifiedChinese)
                                {
                                    redirect = true;
                                    newHost = "dlassets.xboxlive.cn";
                                }
                                if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                    _ = UpdateGameUrl(host, tmpPath);
                                break;
                            case "dlassets2.xboxlive.com":
                                redirect = true;
                                newHost = "dlassets2.xboxlive.cn";
                                if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                    _ = UpdateGameUrl(host, tmpPath);
                                break;
                            case "assets1.xboxlive.cn":
                            case "d1.xboxlive.cn":
                                if (App.Settings.IsXboxGameDownloadLinksShown)
                                {
                                    redirect = true;
                                    newHost = "assets2.xboxlive.cn";
                                    if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                        _ = UpdateGameUrl(host, tmpPath);
                                }
                                break;
                            case "dlassets.xboxlive.cn":
                                if (App.Settings.IsXboxGameDownloadLinksShown)
                                {
                                    redirect = true;
                                    newHost = "dlassets2.xboxlive.cn";
                                    if (_gameFilePaths.TryAdd(tmpPath, string.Empty))
                                        _ = UpdateGameUrl(host, tmpPath);
                                }
                                break;

                            case "us.cdn.blizzard.com":
                            case "eu.cdn.blizzard.com":
                            case "kr.cdn.blizzard.com":
                            case "level3.blizzard.com":
                            case "blizzard.gcdn.cloudn.co.kr":
                                redirect = true;
                                newHost = "blzddist1-a.akamaihd.net";
                                break;
                        }
                        if (redirect)
                        {
                            var url = $"http://{newHost}{filePath}";
                            var sb = new StringBuilder();
                            sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                            sb.Append("Content-Type: text/html\r\n");
                            sb.Append($"Location: {url}\r\n");
                            sb.Append("Content-Length: 0\r\n\r\n");
                            var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                            socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                            if (serviceViewModel.IsLogging)
                                serviceViewModel.AddLog("HTTP 302", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                        }
                        else
                        {
                            var fileFound = false;
                            var url = $"http://{host}{filePath}";
                            switch (host)
                            {
                                case "www.msftconnecttest.com":
                                    if (tmpPath.Equals("/connecttest.txt", StringComparison.CurrentCultureIgnoreCase)) // 网络连接 (NCSI)，修复 Xbox、Windows 系统网络正常却显示离线
                                    {
                                        fileFound = true;
                                        var response = Encoding.ASCII.GetBytes("Microsoft Connect Test");
                                        var sb = new StringBuilder();
                                        sb.Append("HTTP/1.1 200 OK\r\n");
                                        sb.Append("Content-Type: text/plain\r\n");
                                        sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                                        var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                        socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                                        socket.Send(response, 0, response.Length, SocketFlags.None, out _);
                                        if (serviceViewModel.IsLogging)
                                            serviceViewModel.AddLog("HTTP 200", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                    }
                                    break;
                                case "blzddist1-a.akamaihd.net":
                                    {
                                        if (IPAddress.TryParse(App.Settings.BattleIp, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6)
                                        {
                                            var httpHeaders = new Dictionary<string, string>() { { "Host", host } };
                                            result = RangeHeaderRegex().Match(headers);
                                            if (result.Success) httpHeaders.Add("Range", result.Groups[1].Value.Trim());
                                            using var response = await HttpClientHelper.SendRequestAsync(url.Replace(host, "[" + address + "]"), headers: httpHeaders);
                                            if (response is { IsSuccessStatusCode: true })
                                            {
                                                fileFound = true;
                                                var headersBytes = response.StatusCode == HttpStatusCode.PartialContent
                                                    ? Encoding.ASCII.GetBytes($"HTTP/1.1 206 Partial Content\r\n{response.Content.Headers}{response.Headers}\r\n")
                                                    : Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{response.Content.Headers}{response.Headers}\r\n");
                                                socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);

                                                var dataBuffer = new byte[65536];
                                                var stream = await response.Content.ReadAsStreamAsync();
                                                int readLength;
                                                while ((readLength = await stream.ReadAsync(dataBuffer)) > 0)
                                                {
                                                    if (!socket.Connected) break;
                                                    socket.Send(dataBuffer, 0, readLength, SocketFlags.None, out _);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    if (App.Settings.IsLocalProxyEnabled && (DicSniProxy.ContainsKey(host) || DicSniProxy2.Any(kvp => kvp.Key.EndsWith(host))))
                                    {
                                        fileFound = true;
                                        url = $"https://{host}{filePath}";
                                        StringBuilder sb = new();
                                        sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                                        sb.Append("Content-Type: text/html\r\n");
                                        sb.Append($"Location: {url}\r\n");
                                        sb.Append("Content-Length: 0\r\n\r\n");
                                        var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                        socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                                        if (serviceViewModel.IsLogging)
                                            serviceViewModel.AddLog("HTTP 302", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                    }
                                    break;
                            }
                            if (!fileFound)
                            {
                                var response = Encoding.ASCII.GetBytes("File not found.");
                                StringBuilder sb = new();
                                sb.Append("HTTP/1.1 404 Not Found\r\n");
                                sb.Append("Content-Type: text/html\r\n");
                                sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                                var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                                socket.Send(response, 0, response.Length, SocketFlags.None, out _);
                                if (serviceViewModel.IsLogging)
                                    serviceViewModel.AddLog("HTTP 404", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                socket.Close();
                socket.Dispose();
            }
        }
    }

    private readonly ConcurrentDictionary<string, string> _gameFilePaths = new();

    private async Task UpdateGameUrl(string host, string tmpPath)
    {
        var extension = Path.GetExtension(tmpPath).ToLowerInvariant();
        if (extension is ".phf" or ".xsp") return;

        var result = ContentIdVersionRegex().Match(tmpPath);
        if (!result.Success) return;

        var key = result.Groups["ContentId"].Value.ToLowerInvariant();
        if (MsiXvcRegex().IsMatch(tmpPath))
        {
            //WindeosPC
        }
        else if (XsXvcRegex().IsMatch(tmpPath))
        {
            key += "_xs";
        }
        else
        {
            key += "_x";
        }
        var version = new Version(result.Groups["Version"].Value);
        if (XboxGameManager.Dictionary.TryGetValue(key, out var xboxGame))
        {
            if (xboxGame.Version >= version) return;
        }

        host = host switch
        {
            "xvcf1.xboxlive.com" or "xvcf2.xboxlive.com" or "assets2.xboxlive.com"
                or "d1.xboxlive.com" or "d2.xboxlive.com" or "assets1.xboxlive.cn" or "assets2.xboxlive.cn"
                or "d1.xboxlive.cn" or "d2.xboxlive.cn" => "assets1.xboxlive.com",
            "dlassets2.xboxlive.com" or "dlassets.xboxlive.cn" or "dlassets2.xboxlive.cn" => "dlassets.xboxlive.com",
            _ => host
        };

        const string tagHost = "assets2.xboxlive.cn";
        var ipAddresses = App.Settings.IsDoHEnabled
            ? await DnsHelper.ResolveDohAsync(tagHost, DnsHelper.CurrentDoH)
            : await DnsHelper.ResolveDnsAsync(tagHost, serviceViewModel.DnsIp);

        if (ipAddresses?.Count > 0)
        {
            var headers = new Dictionary<string, string>() { { "Host", tagHost } };
            using var response = await HttpClientHelper.SendRequestAsync($"http://{ipAddresses[0]}{tmpPath}", method: "HEAD", headers: headers);
            if (response is { IsSuccessStatusCode: true })
            {
                if (response.Content.Headers.TryGetValues("Content-Length", out var values))
                {
                    var filesize = long.Parse(values.FirstOrDefault() ?? "0");
                    xboxGame = new XboxGameManager.Product
                    {
                        Version = version,
                        FileSize = filesize,
                        Url = $"http://{host}{tmpPath}"
                    };
                    XboxGameManager.Dictionary.AddOrUpdate(key, xboxGame, (_, _) => xboxGame);
                    _ = XboxGameManager.SaveAsync();
                    _ = HttpClientHelper.GetStringContentAsync(
                        $"Game/AddGameUrl?url={HttpUtility.UrlEncode(xboxGame.Url)}",
                        method: "PUT", 
                        name: HttpClientNames.XboxDownload);
                }
            }
        }
    }

    private async Task HttpsThread(Socket socket)
    {
        if (socket.Connected)
        {
            socket.SendTimeout = 30000;
            socket.ReceiveTimeout = 30000;

            await using SslStream ssl = new(new NetworkStream(socket), false);
            var options = new SslServerAuthenticationOptions
            {
                ServerCertificate = _certificate,
                ClientCertificateRequired = false,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };
            try
            {
                await ssl.AuthenticateAsServerAsync(options);
                if (ssl.IsAuthenticated)
                {
                    while (serviceViewModel.IsListening && socket.Connected)
                    {
                        if (!TryReadHttpRequestStart(ssl, out var request)) break;
                        var headers = request.Headers;
                        var result = HttpRequestMethodAndPathRegex().Match(headers);
                        if (!result.Success) break;
                        var filePath = BaseUrlRegex().Replace(result.Groups["path"].Value.Trim(), "");
                        result = HostHeaderRegex().Match(headers);
                        if (!result.Success) break;
                        var host = result.Groups["host"].Value.Trim().ToLowerInvariant();

                        var tmpPath = QueryStringRegex().Replace(filePath, "");
                        if (TryGetLocalUploadPath(tmpPath, out var localPath))
                        {
                            SendLocalUploadFile(headers, filePath, localPath, new StreamResponseWriter(ssl, socket), ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                        }
                        else
                        {
                            var fileFound = false;
                            var url = $"https://{host}{filePath}";
                            if (App.Settings.IsLocalProxyEnabled)
                            {
                                if (host == "github.com" && filePath.Contains("/releases/download/"))
                                {
                                    var fastestUrl = await HttpClientHelper.GetFastestProxyAsync(UpdateService.Proxies1, url, new Dictionary<string, string> { { "Range", "bytes=0-10239" } }, 3000);
                                    if (fastestUrl != null)
                                    {
                                        fileFound = true;
                                        StringBuilder sb = new();
                                        sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                                        sb.Append("Content-Type: text/html\r\n");
                                        sb.Append($"Location: {fastestUrl}\r\n");
                                        sb.Append("Content-Length: 0\r\n\r\n");
                                        var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                        ssl.Write(headersBytes);
                                        ssl.Flush();
                                    }
                                }

                                if (!fileFound)
                                {
                                    SniProxy? proxy = null;
                                    string[]? expectedHosts = null;
                                    if (DicSniProxy.TryGetValue(host, out var tuple))
                                    {
                                        proxy = tuple.Item1;
                                        expectedHosts = tuple.Item2;
                                    }
                                    else
                                    {
                                        tuple = DicSniProxy2.Where(kvp => host.EndsWith(kvp.Key)).Select(x => x.Value).FirstOrDefault();
                                        if (tuple.Item1 != null)
                                        {
                                            proxy = new SniProxy
                                            {
                                                Branch = tuple.Item1.Branch,
                                                Sni = tuple.Item1.Sni,
                                                IpAddressesV4 = tuple.Item1.IpAddressesV4,
                                                IpAddressesV6 = tuple.Item1.IpAddressesV6,
                                                UseCustomIpAddress = tuple.Item1.UseCustomIpAddress
                                            };
                                            expectedHosts = tuple.Item2;
                                            DicSniProxy.TryAdd(host, (proxy, expectedHosts));
                                        }
                                    }

                                    if (proxy != null)
                                    {
                                        if (serviceViewModel.IsLogging)
                                            serviceViewModel.AddLog("Proxy", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());

                                        fileFound = true;
                                        IPAddress[]? ips = null;
                                        if (proxy is { UseCustomIpAddress: true, IpAddresses: null })
                                        {
                                            IPAddress[]? ipV6 = proxy.IpAddressesV6, ipV4 = proxy.IpAddressesV4;
                                            proxy.IpAddresses = serviceViewModel.IsIPv6Support switch
                                            {
                                                true when ipV6 != null && ipV4 != null => [.. ipV6, .. ipV4],
                                                true => ipV6 ?? ipV4,
                                                _ => ipV4
                                            };
                                            if (proxy.IpAddresses?.Length >= 2)
                                            {
                                                await proxy.Semaphore.WaitAsync();
                                                try
                                                {
                                                    if (proxy.IpAddresses?.Length >= 2)
                                                    {
                                                        var fastestIp = await HttpClientHelper.GetFastestHttpsIpAsync(proxy.IpAddresses);
                                                        if (fastestIp != null)
                                                            ips = proxy.IpAddresses = [fastestIp];
                                                    }
                                                }
                                                finally
                                                {
                                                    proxy.Semaphore.Release();
                                                }
                                            }
                                        }
                                        else if (proxy.IpAddresses == null)
                                        {
                                            await proxy.Semaphore.WaitAsync();
                                            try
                                            {
                                                if (proxy.IpAddresses == null)
                                                {
                                                    var domain = proxy.Branch ?? host;

                                                    var ipAddresses = new ConcurrentBag<IPAddress>();
                                                    var tasks = new List<Task>();
                                                    foreach (var sniProxyId in App.Settings.SniProxyId)
                                                    {
                                                        var selectedDohServer = serviceViewModel.DohServersMappings.FirstOrDefault(d => d.Id == sniProxyId);
                                                        if (selectedDohServer == null) continue;
                                                        var useProxy = App.Settings.DohServerUseProxyId.Contains(selectedDohServer.Id) && !selectedDohServer.IsProxyDisabled;
                                                        var doHServer = DnsHelper.GetConfigureDoH(selectedDohServer.Url, selectedDohServer.Ip, useProxy);
                                                        if (serviceViewModel.IsIPv6Support)
                                                        {
                                                            tasks.Add(Task.Run(async () =>
                                                            {
                                                                var ipV6 = await DnsHelper.ResolveDohAsync(domain, doHServer, true);
                                                                if (ipV6 != null)
                                                                {
                                                                    foreach (var ip in ipV6)
                                                                        ipAddresses.Add(ip);
                                                                }
                                                            }));
                                                        }
                                                        tasks.Add(Task.Run(async () =>
                                                        {
                                                            var ipV4 = await DnsHelper.ResolveDohAsync(domain, doHServer);
                                                            if (ipV4 != null)
                                                            {
                                                                foreach (var ip in ipV4)
                                                                    ipAddresses.Add(ip);
                                                            }
                                                        }));
                                                    }
                                                    await Task.WhenAll(tasks);
                                                    if (!ipAddresses.IsEmpty)
                                                        proxy.IpAddresses = [.. ipAddresses.Distinct()];

                                                    if (proxy.IpAddresses?.Length >= 2)
                                                    {
                                                        var fastestIp = await HttpClientHelper.GetFastestHttpsIpAsync(proxy.IpAddresses);
                                                        if (fastestIp != null) ips = proxy.IpAddresses = [fastestIp];
                                                    }
                                                }
                                            }
                                            finally
                                            {
                                                proxy.Semaphore.Release();
                                            }
                                        }
                                        ips ??= proxy.IpAddresses?.Length >= 2 ? [.. proxy.IpAddresses.OrderBy(_ => Random.Shared.Next()).Take(16)] : proxy.IpAddresses;

                                        string? errMessae;
                                        if (ips != null)
                                        {
                                            if (!ExecuteSniProxy(host, ips, proxy.Sni, expectedHosts, request, ssl, out errMessae))
                                            {
                                                proxy.IpAddresses = null;
                                            }
                                        }
                                        else errMessae = $"Unable to query domain {host}. Please check whether the DoH server is reachable. If necessary, enable proxy forwarding for the request.";
                                        if (!string.IsNullOrEmpty(errMessae))
                                        {
                                            var response = Encoding.UTF8.GetBytes(errMessae);
                                            StringBuilder sb = new();
                                            sb.Append("HTTP/1.1 500 Server Error\r\n");
                                            sb.Append("Content-Type: text/html; charset=utf-8\r\n");
                                            sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                                            ssl.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                                            ssl.Write(response);
                                            ssl.Flush();
                                        }
                                    }
                                }
                            }
                            if (!fileFound)
                            {
                                var response = "File not found."u8.ToArray();
                                StringBuilder sb = new();
                                sb.Append("HTTP/1.1 404 Not Found\r\n");
                                sb.Append("Content-Type: text/html\r\n");
                                sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                                var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                ssl.Write(headersBytes);
                                ssl.Write(response);
                                ssl.Flush();
                                if (serviceViewModel.IsLogging)
                                    serviceViewModel.AddLog("HTTP 404", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                socket.Close();
                socket.Dispose();
            }
        }
    }

    private sealed class HttpRequestStart
    {
        public required string Headers { get; init; }
        public required byte[] HeaderBytes { get; init; }
        public required byte[] InitialBodyBytes { get; init; }
    }

    private interface IResponseWriter
    {
        bool IsConnected { get; }
        void Write(byte[] bytes, int length);
        void Flush();
    }

    private readonly struct SocketResponseWriter(Socket socket) : IResponseWriter
    {
        public bool IsConnected => socket.Connected;

        public void Write(byte[] bytes, int length) => socket.Send(bytes, 0, length, SocketFlags.None, out _);

        public void Flush()
        {
        }
    }

    private readonly struct StreamResponseWriter(Stream stream, Socket socket) : IResponseWriter
    {
        public bool IsConnected => socket.Connected;

        public void Write(byte[] bytes, int length) => stream.Write(bytes, 0, length);

        public void Flush() => stream.Flush();
    }

    private bool TryGetLocalUploadPath(string requestPath, out string localPath)
    {
        localPath = string.Empty;
        if (!serviceViewModel.IsLocalUploadEnabled) return false;

        var directPath = serviceViewModel.LocalUploadPath + requestPath;
        var fileNamePath = Path.Combine(serviceViewModel.LocalUploadPath, Path.GetFileName(requestPath));
        if (File.Exists(directPath))
        {
            localPath = OperatingSystem.IsWindows() ? directPath.Replace("/", "\\") : directPath;
        }
        else if (File.Exists(fileNamePath))
        {
            localPath = fileNamePath;
        }

        return !string.IsNullOrEmpty(localPath);
    }

    private void SendLocalUploadFile(string headers, string filePath, string localPath, IResponseWriter writer, string remoteAddress)
    {
        FileStream? fs = null;
        try
        {
            fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex)
        {
            if (serviceViewModel.IsLogging)
                serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), ex.Message, remoteAddress);
        }

        if (fs == null)
        {
            var response = "Internal Server Error"u8.ToArray();
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 500 Server Error\r\n");
            sb.Append("Content-Type: text/html\r\n");
            sb.Append($"Content-Length: {response.Length}\r\n\r\n");
            WriteBytes(writer, Encoding.ASCII.GetBytes(sb.ToString()));
            WriteBytes(writer, response);
            writer.Flush();
            return;
        }

        using (fs)
        using (var br = new BinaryReader(fs))
        {
            if (serviceViewModel.IsLogging)
                serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), localPath, remoteAddress);

            string contentRange = string.Empty, status = "200 OK";
            long fileLength = br.BaseStream.Length, startPosition = 0;
            var endPosition = fileLength;
            var result = RangeHeaderRegex2().Match(headers);
            if (result.Success)
            {
                startPosition = long.Parse(result.Groups["StartPosition"].Value);
                if (startPosition > br.BaseStream.Length) startPosition = 0;
                if (!string.IsNullOrEmpty(result.Groups["EndPosition"].Value))
                    endPosition = long.Parse(result.Groups["EndPosition"].Value) + 1;
                endPosition = Math.Min(endPosition, fileLength);
                contentRange = "bytes " + startPosition + "-" + (endPosition - 1) + "/" + fileLength;
                status = "206 Partial Content";
            }

            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 " + status + "\r\n");
            sb.Append($"Content-Type: {ContentTypeHelper.GetMimeMapping(filePath)}\r\n");
            sb.Append($"Content-Length: {endPosition - startPosition}\r\n");
            if (!string.IsNullOrEmpty(contentRange)) sb.Append($"Content-Range: {contentRange}\r\n");
            sb.Append("Accept-Ranges: bytes\r\n\r\n");

            WriteBytes(writer, Encoding.ASCII.GetBytes(sb.ToString()));

            br.BaseStream.Position = startPosition;
            var buffer = new byte[4096];
            while (serviceViewModel.IsListening && writer.IsConnected)
            {
                var remaining = endPosition - br.BaseStream.Position;
                var readLength = br.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (readLength <= 0) break;

                writer.Write(buffer, readLength);
                if (remaining <= readLength) break;
            }
        }

        writer.Flush();
    }

    private static void WriteBytes(IResponseWriter writer, byte[] bytes) => writer.Write(bytes, bytes.Length);

    private static bool TryReadHttpRequestStart(Socket socket, out HttpRequestStart requestStart) =>
        TryReadHttpRequestStart((buffer, count) => socket.Receive(buffer, 0, count, SocketFlags.None, out _), out requestStart);

    private static bool TryReadHttpRequestStart(SslStream ssl, out HttpRequestStart requestStart) =>
        TryReadHttpRequestStart((buffer, count) => ssl.Read(buffer, 0, count), out requestStart);

    private static bool TryReadHttpRequestStart(Func<byte[], int, int> read, out HttpRequestStart requestStart)
    {
        requestStart = null!;

        const int maxHeaderBytes = 64 * 1024;
        var buffer = new byte[4096];
        using var request = new MemoryStream(buffer.Length);
        var headerLength = -1;
        var scanStart = 0;

        while (headerLength < 0)
        {
            var previousLength = (int)request.Length;
            var len = read(buffer, buffer.Length);
            if (len <= 0) return false;

            request.Write(buffer, 0, len);
            if (request.Length > maxHeaderBytes) return false;

            var requestBytes = request.GetBuffer();
            headerLength = FindHeaderLength(requestBytes, (int)request.Length, scanStart);
            scanStart = Math.Max(0, previousLength - 3);
        }

        var allRequestBytes = request.ToArray();
        var headerBytes = allRequestBytes[..headerLength];
        var headers = Encoding.ASCII.GetString(headerBytes);

        requestStart = new HttpRequestStart
        {
            Headers = headers,
            HeaderBytes = headerBytes,
            InitialBodyBytes = allRequestBytes[headerLength..]
        };
        return true;
    }

    private static int FindHeaderLength(byte[] bytes, int length, int start)
    {
        for (var i = start; i <= length - 4; i++)
        {
            if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n')
                return i + 4;
        }

        return -1;
    }

    private static bool ExecuteSniProxy(string targetHost, IPAddress[] ips, string? sni, string[]? expectedHosts, HttpRequestStart request, SslStream client, out string? errMessage)
    {
        var isOk = true;
        errMessage = null;
        using Socket socket = new(ips[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.SendTimeout = 6000;
        socket.ReceiveTimeout = 6000;
        try
        {
            socket.Connect(ips[0], 443);
        }
        catch (Exception ex)
        {
            isOk = false;
            errMessage = ex.Message;
        }
        if (socket.Connected)
        {
            using SslStream ssl = new(new NetworkStream(socket), false, (_, _, _, _) => true, null);
            ssl.WriteTimeout = 30000;
            ssl.ReadTimeout = 30000;
            try
            {
                var options = new SslClientAuthenticationOptions
                {
                    TargetHost = string.IsNullOrEmpty(sni) ? ips[0].ToString() : sni,
                    CertificateRevocationCheckMode = X509RevocationMode.Online
                };
                ssl.AuthenticateAsClient(options);
                if (ssl.IsAuthenticated)
                {
                    if (expectedHosts == null || expectedHosts.Length > 0)
                    {
                        var useCustomValidation = expectedHosts is { Length: > 0 };
                        if (ssl.RemoteCertificate == null)
                        {
                            isOk = false;
                            errMessage = "Server certificate not received.";
                        }
                        else
                        {
                            var domainMatched = false;

                            var cert2 = new X509Certificate2(ssl.RemoteCertificate);

                            // Get all DNS names from the certificate
                            var dnsNames = cert2.Extensions
                                .Where(e => e.Oid?.Value == "2.5.29.17")
                                .SelectMany(ext =>
                                {
                                    var reader = new AsnReader(ext.RawData, AsnEncodingRules.DER);
                                    var seq = reader.ReadSequence();
                                    var result = new List<string>();

                                    while (seq.HasData)
                                    {
                                        var tag = seq.PeekTag();

                                        if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
                                        {
                                            var dns = seq.ReadCharacterString(
                                                UniversalTagNumber.IA5String,
                                                new Asn1Tag(TagClass.ContextSpecific, 2)
                                            );
                                            result.Add(dns);

                                            if (!useCustomValidation && !domainMatched && CertificateHostMatches(targetHost, dns))
                                                domainMatched = true;
                                        }
                                        else
                                        {
                                            seq.ReadEncodedValue();
                                        }
                                    }
                                    return result;
                                })
                                .ToList();

                            // Check if a matching domain exists
                            if (!domainMatched && useCustomValidation)
                                domainMatched = dnsNames.Any(dns => CertificateHostMatches(targetHost, dns)) ||
                                                expectedHosts!.Any(host => dnsNames.Any(dns => CertificateHostMatches(host, dns)));

                            if (!domainMatched)
                            {
                                isOk = false;

                                var issuedFor = $"[\"{string.Join("\", \"", dnsNames)}\"]";
                                var expectedFor = expectedHosts != null
                                    ? $"[\"{string.Join("\", \"", expectedHosts)}\"]"
                                    : null;
                                errMessage = expectedFor != null
                                    ? $"Certificate domain mismatch.<br>The server's certificate is issued for {issuedFor},<br>but the expected domain was {expectedFor}."
                                    : $"Certificate domain mismatch.<br>The server's certificate is issued for {issuedFor}.";
                            }
                        }
                    }

                    if (isOk)
                    {
                        ssl.Write(request.HeaderBytes);
                        if (request.InitialBodyBytes.Length > 0)
                            ssl.Write(request.InitialBodyBytes);
                        ssl.Flush();

                        RelayStreams(client, ssl);
                    }
                }
            }
            catch (Exception ex)
            {
                isOk = false;
                errMessage = ex.Message;
            }
            finally
            {
                ssl.Close();
            }
        }

        if (socket.Connected)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close();
            }
        }

        return isOk;
    }

    private static bool CertificateHostMatches(string host, string certificateHost)
    {
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        certificateHost = certificateHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.Equals(certificateHost)) return true;
        if (!certificateHost.StartsWith("*.")) return false;

        var suffix = certificateHost[1..];
        return host.EndsWith(suffix) && host.Length > suffix.Length;
    }

    private static void RelayStreams(SslStream client, SslStream upstream)
    {
        var clientToUpstream = Task.Run(() => CopyStream(client, upstream));
        var upstreamToClient = Task.Run(() => CopyStream(upstream, client));

        try
        {
            Task.WaitAny(clientToUpstream, upstreamToClient);
        }
        catch
        {
            // CopyStream handles connection errors; this is a last-resort guard.
        }
        finally
        {
            SafeClose(client);
            SafeClose(upstream);
        }
    }

    private static void CopyStream(Stream source, Stream destination)
    {
        var buffer = new byte[65536];
        try
        {
            while (true)
            {
                var len = source.Read(buffer, 0, buffer.Length);
                if (len <= 0) break;

                destination.Write(buffer, 0, len);
            }
        }
        catch
        {
            // Connection closed by either side.
        }
        finally
        {
            SafeClose(destination);
        }
    }

    private static void SafeClose(Stream stream)
    {
        try
        {
            stream.Close();
        }
        catch
        {
            // Optional
        }
    }

    [GeneratedRegex(@"(?<method>GET|POST|PUT|PATCH|DELETE|OPTIONS|HEAD) (?<path>[^\s]+)", RegexOptions.Compiled)]
    private static partial Regex HttpRequestMethodAndPathRegex();

    [GeneratedRegex(@"^https?://[^/]+", RegexOptions.Compiled)]
    private static partial Regex BaseUrlRegex();

    [GeneratedRegex(@"^Host:\s*(?<host>[^\r\n]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex HostHeaderRegex();

    [GeneratedRegex(@"\?.*$", RegexOptions.Compiled)]
    private static partial Regex QueryStringRegex();

    [GeneratedRegex(@"Range: (bytes=.+)")]
    private static partial Regex RangeHeaderRegex();

    [GeneratedRegex(@"Range: bytes=(?<StartPosition>\d+)(-(?<EndPosition>\d+))?", RegexOptions.Compiled)]
    private static partial Regex RangeHeaderRegex2();

    [GeneratedRegex(@"/(?<ContentId>\w{8}-\w{4}-\w{4}-\w{4}-\w{12})/(?<Version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}", RegexOptions.Compiled)]
    private static partial Regex ContentIdVersionRegex();

    [GeneratedRegex(@"_xs(-?\d+)?\.xvc$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex XsXvcRegex();

    [GeneratedRegex(@"\.(msixvc|msi)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MsiXvcRegex();

}
