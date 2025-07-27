using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.UI;
using XboxDownload.ViewModels;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Web;
using Avalonia.Threading;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.Services;

public partial class TcpConnectionListener(ServiceViewModel serviceViewModel)
{
    private static X509Certificate2? _certificate;
    private static Socket? _httpSocket;
    private static Socket? _httpsSocket;
    private const int HttpPort = 80;
    private const int HttpsPort = 443;
    private bool _isSimplifiedChinese;

    private static void CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=XboxDownload", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("packagespc.xboxlive.com");
        sanBuilder.AddDnsName("*.akamai.net");
        sanBuilder.AddDnsName("*.akamaihd.net");
        sanBuilder.AddDnsName("*.akamaized.net");
        sanBuilder.AddDnsName("epicgames-download1-1251447533.file.myqcloud.com");
        sanBuilder.AddDnsName("download.epicgames.com");
        req.CertificateExtensions.Add(sanBuilder.Build());
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        _certificate =  new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
    
    public async Task StartAsync()
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
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Service.Listening.TcpStartFailedDialogTitle"),
                    string.Format( ResourceHelper.GetString("Service.Listening.TcpStartFailedDialogMessage"), ex.Message),
                    Icon.Error);
            });
            return;
        }
        
        if (_certificate == null) CreateCertificate();
        if (OperatingSystem.IsWindows())
        {
            X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(_certificate!);
            store.Close();
        }
        
        _isSimplifiedChinese = App.Settings.Culture == "zh-Hans";

        _ = Task.Run(() => Listening(_httpSocket, false));
        _ = Task.Run(() => Listening(_httpsSocket, true));
    }
    
    public static void Stop()
    {
        try
        {
            _httpSocket?.Shutdown(SocketShutdown.Both);
            _httpsSocket?.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignored
        }

        _httpSocket?.Close();
        _httpsSocket?.Close();
        _httpSocket?.Dispose();
        _httpsSocket?.Dispose();
        _httpSocket = null;
        _httpsSocket = null;
        
        if (OperatingSystem.IsWindows())
        {
            using X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var certificates =
                store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=XboxDownload", false);
            if (certificates.Count > 0) store.RemoveRange(certificates);
            store.Close();
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
                    var receive = new byte[4096];
                    var num = socket.Receive(receive, 0, receive.Length, SocketFlags.None, out _);
                    if (num == 0) break;

                    var headers = Encoding.ASCII.GetString(receive, 0, num);
                    
                    var result = HttpRequestMethodAndPathRegex().Match(headers);
                    if (!result.Success) break;
                    //var method = result.Groups["method"].Value;
                    var filePath = BaseUrlRegex().Replace(result.Groups["path"].Value.Trim(), "");
                    
                    result = HostHeaderRegex().Match(headers);
                    if (!result.Success) break;
                    var host = result.Groups[1].Value.Trim().ToLower();
                    
                    string tmpPath = QueryStringRegex().Replace(filePath, ""), localPath = string.Empty;
                    if (serviceViewModel.IsLocalUploadEnabled)
                    {
                        var tmpPath1 = serviceViewModel.LocalUploadPath + tmpPath;
                        var tmpPath2 = Path.Combine(serviceViewModel.LocalUploadPath, Path.GetFileName(tmpPath));
                        if (File.Exists(tmpPath1))
                        {
                            if (OperatingSystem.IsWindows()) tmpPath1 = tmpPath1.Replace("/", "\\");
                            localPath = tmpPath1;
                        }
                        else if (File.Exists(tmpPath2))
                            localPath = tmpPath2;
                    }

                    if (serviceViewModel.IsLocalUploadEnabled && !string.IsNullOrEmpty(localPath))
                    {
                         FileStream? fs = null;
                         try
                         {
                             fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                         }
                         catch (Exception ex)
                         {
                             if (serviceViewModel.IsLogging)
                                 serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), ex.Message, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                         }
                         if (fs != null)
                         {
                             if (serviceViewModel.IsLogging)
                                 serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), localPath, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                             using var br = new BinaryReader(fs);
                             string contentRange = string.Empty, status = "200 OK";
                             long fileLength = br.BaseStream.Length, startPosition = 0;
                             var endPosition = fileLength;
                             result = RangeHeaderRegex().Match(headers);
                             if (result.Success)
                             {
                                 startPosition = long.Parse(result.Groups["StartPosition"].Value);
                                 if (startPosition > br.BaseStream.Length) startPosition = 0;
                                 if (!string.IsNullOrEmpty(result.Groups["EndPosition"].Value))
                                     endPosition = long.Parse(result.Groups["EndPosition"].Value) + 1;
                                 contentRange = "bytes " + startPosition + "-" + (endPosition - 1) + "/" + fileLength;
                                 status = "206 Partial Content";
                             }

                             var sb = new StringBuilder();
                             sb.Append("HTTP/1.1 " + status + "\r\n");
                             sb.Append($"Content-Type: {ContentTypeHelper.GetMimeMapping(filePath)}\r\n");
                             sb.Append($"Content-Length: {endPosition - startPosition}\r\n");
                             if (!string.IsNullOrEmpty(contentRange)) sb.Append($"Content-Range: {contentRange}\r\n");
                             sb.Append("Accept-Ranges: bytes\r\n\r\n");

                             var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                             socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);

                             br.BaseStream.Position = startPosition;
                             const int size = 4096;
                             while (serviceViewModel.IsListening && socket.Connected)
                             {
                                 var remaining = endPosition - br.BaseStream.Position;
                                 var response = new byte[remaining <= size ? remaining : size];
                                 _ = br.Read(response, 0, response.Length);
                                 socket.Send(response, 0, response.Length, SocketFlags.None, out _);
                                 if (remaining <= size) break;
                             }
                             fs.Close();
                             await fs.DisposeAsync();
                         }
                         else
                         {
                             var response = Encoding.ASCII.GetBytes("Internal Server Error");
                             var sb = new StringBuilder();
                             sb.Append("HTTP/1.1 500 Server Error\r\n");
                             sb.Append("Content-Type: text/html\r\n");
                             sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                             var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                             socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                             socket.Send(response, 0, response.Length, SocketFlags.None, out _);
                         }
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
                            
                            case "uplaypc-s-ubisoft.cdn.ubi.com":
                                redirect = true;
                                newHost = "uplaypc-s-ubisoft.cdn.ubionline.com.cn";
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
                                case "epicgames-download1-1251447533.file.myqcloud.com":
                                case "epicgames-download1.akamaized.net":
                                case "download.epicgames.com":
                                case "fastly-download.epicgames.com":
                                case "cloudflare.epicgamescdn.com":
                                    if (filePath.Contains(".manifest") && !host.Equals("epicgames-download1-1251447533.file.myqcloud.com"))
                                    {
                                        var ipAddresses = App.Settings.IsDoHEnabled
                                            ? await DnsHelper.ResolveDohAsync(host, DnsHelper.CurrentDoH)
                                            : await DnsHelper.ResolveDnsAsync(host, serviceViewModel.DnsIp);
                                        if (ipAddresses?.Count > 0)
                                        {
                                            var httpHeaders = new Dictionary<string, string>() { { "Host", host } };
                                            using var response = await HttpClientHelper.SendRequestAsync(url.Replace(host, ipAddresses[0].ToString()), headers: httpHeaders);
                                            if (response is { IsSuccessStatusCode: true })
                                            {
                                                fileFound = true;
                                                var headersBytes = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{response.Content.Headers}{response.Headers}\r\n");
                                                var responseData = await response.Content.ReadAsByteArrayAsync();
                                                socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                                                socket.Send(responseData, 0, responseData.Length, SocketFlags.None, out _);
                                                if (serviceViewModel.IsLogging)
                                                    serviceViewModel.AddLog("HTTP 200", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fileFound = true;
                                        url = $"http://{(host == "epicgames-download1-1251447533.file.myqcloud.com" ? "epicgames-download1.akamaized.net" : "epicgames-download1-1251447533.file.myqcloud.com")}{filePath}";
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
                                    break;
                                case "blzddist1-a.akamaihd.net":
                                {
                                    if (IPAddress.TryParse(App.Settings.BattleIp, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6)
                                    {
                                        var httpHeaders = new Dictionary<string, string>() { { "Host", host } };
                                        result = Regex.Match(headers, @"Range: (bytes=.+)");
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
                                case "uplaypc-s-ubisoft.cdn.ubionline.com.cn":
                                {
                                    if (IPAddress.TryParse(App.Settings.UbisoftIp, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6)
                                    {
                                        var httpHeaders = new Dictionary<string, string>() { { "Host", host } };
                                        using var response = await HttpClientHelper.SendRequestAsync(url.Replace(host, "[" + address + "]"), headers: httpHeaders);
                                        if (response is { IsSuccessStatusCode: true })
                                        {
                                            fileFound = true;
                                            var responseBytes = await response.Content.ReadAsByteArrayAsync();
                                            StringBuilder sb = new();
                                            sb.Append("HTTP/1.1 200 OK\r\n");
                                            sb.Append("Content-Type: text/plain\r\n");
                                            sb.Append("Connection: keep-alive\r\n");
                                            sb.Append($"Content-Length: {responseBytes.Length}\r\n\r\n");
                                            var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                            socket.Send(headersBytes, 0, headersBytes.Length, SocketFlags.None, out _);
                                            socket.Send(responseBytes, 0, responseBytes.Length, SocketFlags.None, out _);
                                        }
                                    }
                                    break;
                                }
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
                
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Both);
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
        
        var key = result.Groups["ContentId"].Value.ToLower();
        if (XvcRegex().IsMatch(tmpPath))
            key += "_xs";
        else if (!MsiXvcRegex().IsMatch(tmpPath))
            key += "_x";
        var version = new Version(result.Groups["Version"].Value);
        if (XboxGameManager.Dictionary.TryGetValue(key, out var xboxGame))
        {
            if (xboxGame.Version >= version) return;
        }

        host = host switch
        {
            "xvcf1.xboxlive.com" or "xvcf2.xboxlive.com" or "assets2.xboxlive.com" or "d1.xboxlive.com"
                or "d2.xboxlive.com" or "assets1.xboxlive.cn" or "assets2.xboxlive.cn" or "d1.xboxlive.cn"
                or "d2.xboxlive.cn" => "assets1.xboxlive.com", 
            "dlassets2.xboxlive.com" or "dlassets.xboxlive.cn" or "dlassets2.xboxlive.cn" => "dlassets.xboxlive.com",
            _ => host
        };
        
        var ipAddresses = App.Settings.IsDoHEnabled
            ? await DnsHelper.ResolveDohAsync("assets2.xboxlive.cn", DnsHelper.CurrentDoH)
            : await DnsHelper.ResolveDnsAsync("assets2.xboxlive.cn", serviceViewModel.DnsIp);
        
        if (ipAddresses?.Count > 0)
        {
            var headers = new Dictionary<string, string>() { { "Host", "assets2.xboxlive.cn" } };
            using var response = await HttpClientHelper.SendRequestAsync($"http://{ipAddresses[0].ToString()}{tmpPath}", method: "HEAD", headers: headers);
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
                    _ = HttpClientHelper.GetStringContentAsync(UpdateService.Website + "/Game/AddGameUrl?url=" + HttpUtility.UrlEncode(xboxGame.Url), method: "PUT", name: "XboxDownload");
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
            try
            {
                await ssl.AuthenticateAsServerAsync(_certificate!, false, SslProtocols.Tls13 | SslProtocols.Tls12, false);
                if (ssl.IsAuthenticated)
                {
                    while (serviceViewModel.IsListening && socket.Connected)
                    {
                        var receive = new byte[4096];
                        var num = ssl.Read(receive, 0, receive.Length);
                        var headers = string.Empty;
                        long contentLength = 0, bodyLength = 0;
                        //var list = new List<byte>();
                        for (var i = 1; i <= num - 4; i++)
                        {
                            if (BitConverter.ToString(receive, i, 4) != "0D-0A-0D-0A") continue;
                            headers = Encoding.ASCII.GetString(receive, 0, i + 4);
                            var m1 = ContentLengthHeaderRegex().Match(headers);
                            if (m1.Success)
                            {
                                contentLength = Convert.ToInt32(m1.Groups["ContentLength"].Value);
                            }
                            var dest = new byte[num - i - 4];
                            Buffer.BlockCopy(receive, i + 4, dest, 0, dest.Length);
                            //list.AddRange(dest);
                            bodyLength = dest.Length;
                            break;
                        }
                        while (bodyLength < contentLength)
                        {
                            num = ssl.Read(receive, 0, receive.Length);
                            var dest = new byte[num];
                            Buffer.BlockCopy(receive, 0, dest, 0, dest.Length);
                            //list.AddRange(dest);
                            bodyLength += num;
                        }
                        var result = HttpRequestMethodAndPathRegex().Match(headers);
                        if (!result.Success) break;
                        //var method = result.Groups["method"].Value;
                        var filePath = BaseUrlRegex().Replace(result.Groups["path"].Value.Trim(), "");
                        result = HostHeaderRegex().Match(headers);
                        if (!result.Success) break;
                        var host = result.Groups[1].Value.Trim().ToLower();
                        
                        string tmpPath = QueryStringRegex().Replace(filePath, ""), localPath = string.Empty;
                        if (serviceViewModel.IsLocalUploadEnabled)
                        {
                            var tmpPath1 = serviceViewModel.LocalUploadPath + tmpPath;
                            var tmpPath2 = Path.Combine(serviceViewModel.LocalUploadPath, Path.GetFileName(tmpPath));
                            if (File.Exists(tmpPath1))
                            {
                                if (OperatingSystem.IsWindows()) tmpPath1 = tmpPath1.Replace("/", "\\");
                                localPath = tmpPath1;
                            }
                            else if (File.Exists(tmpPath2))
                                localPath = tmpPath2;
                        }
                        
                        if (serviceViewModel.IsLocalUploadEnabled && !string.IsNullOrEmpty(localPath))
                        {
                            FileStream? fs = null;
                            try
                            {
                                fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            }
                            catch (Exception ex)
                            {
                                if (serviceViewModel.IsLogging)
                                    serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), ex.Message, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                            }
                            if (fs != null)
                            {
                                if (serviceViewModel.IsLogging)
                                    serviceViewModel.AddLog(ResourceHelper.GetString("Service.Listening.LocalUpload"), localPath, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                using var br = new BinaryReader(fs);
                                string contentRange = string.Empty, status = "200 OK";
                                long fileLength = br.BaseStream.Length, startPosition = 0;
                                var endPosition = fileLength;
                                result = RangeHeaderRegex().Match(headers);
                                if (result.Success)
                                {
                                    startPosition = long.Parse(result.Groups["StartPosition"].Value);
                                    if (startPosition > br.BaseStream.Length) startPosition = 0;
                                    if (!string.IsNullOrEmpty(result.Groups["EndPosition"].Value))
                                        endPosition = long.Parse(result.Groups["EndPosition"].Value) + 1;
                                    contentRange = "bytes " + startPosition + "-" + (endPosition - 1) + "/" + fileLength;
                                    status = "206 Partial Content";
                                }

                                var sb = new StringBuilder();
                                sb.Append("HTTP/1.1 " + status + "\r\n");
                                sb.Append($"Content-Type: {ContentTypeHelper.GetMimeMapping(filePath)}\r\n");
                                sb.Append($"Content-Length: {endPosition - startPosition}\r\n");
                                if (!string.IsNullOrEmpty(contentRange)) sb.Append($"Content-Range: {contentRange}\r\n");
                                sb.Append("Accept-Ranges: bytes\r\n\r\n");

                                var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                ssl.Write(headersBytes);

                                br.BaseStream.Position = startPosition;
                                const int size = 4096;
                                while (serviceViewModel.IsListening && socket.Connected)
                                {
                                    var remaining = endPosition - br.BaseStream.Position;
                                    var response = new byte[remaining <= size ? remaining : size];
                                    _ = br.Read(response, 0, response.Length);
                                    ssl.Write(response);
                                    if (remaining <= size) break;
                                }
                                ssl.Flush();
                                fs.Close();
                                await fs.DisposeAsync();
                            }
                            else
                            {
                                var response = Encoding.ASCII.GetBytes("Internal Server Error");
                                var sb = new StringBuilder();
                                sb.Append("HTTP/1.1 500 Server Error\r\n");
                                sb.Append("Content-Type: text/html\r\n");
                                sb.Append($"Content-Length: {response.Length}\r\n\r\n");
                                var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                ssl.Write(headersBytes);
                                ssl.Write(response);
                            }
                        }
                        else
                        {
                            var fileFound = false;
                            var url = $"https://{host}{filePath}";
                            switch (host)
                            {
                                case "packagespc.xboxlive.com":
                                {
                                    var ipAddresses = App.Settings.IsDoHEnabled
                                        ? await DnsHelper.ResolveDohAsync(host, DnsHelper.CurrentDoH)
                                        : await DnsHelper.ResolveDnsAsync(host, serviceViewModel.DnsIp);
                                    if (ipAddresses?.Count > 0)
                                    {
                                        fileFound = true;
                                        var m1 = AuthorizationRegex().Match(headers);
                                        if (m1.Success)
                                        {
                                            App.Settings.Authorization = m1.Groups[1].Value.Trim();
                                            SettingsManager.Save(App.Settings);
                                        }
                                        var httpHeaders = new Dictionary<string, string>() { { "Host", host } , { "Authorization", App.Settings.Authorization }};
                                        using var response = await HttpClientHelper.SendRequestAsync(url.Replace(host, ipAddresses[0].ToString()), headers: httpHeaders);
                                        if (response is { IsSuccessStatusCode: true })
                                        {
                                            var responseBytes = await response.Content.ReadAsByteArrayAsync();
                                            var headersBytes = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{response.Content.Headers}{response.Headers}\r\n");
                                            ssl.Write(headersBytes);
                                            ssl.Write(responseBytes);
                                            ssl.Flush();
                                        }
                                        else
                                        {
                                            StringBuilder sb = new();
                                            sb.Append("HTTP/1.1 500 Server Error\r\n");
                                            sb.Append("Content-Type: text/html\r\n");
                                            sb.Append("Content-Length: 0\r\n\r\n");
                                            ssl.Write(Encoding.ASCII.GetBytes(sb.ToString()));
                                            ssl.Flush();
                                            if (serviceViewModel.IsLogging)
                                                serviceViewModel.AddLog("HTTP 500", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                        }
                                    }
                                    break;
                                }
                                case "epicgames-download1-1251447533.file.myqcloud.com":
                                case "epicgames-download1.akamaized.net":
                                case "download.epicgames.com":
                                case "fastly-download.epicgames.com": 
                                    if (filePath.Contains(".manifest") && !host.Equals("epicgames-download1-1251447533.file.myqcloud.com"))
                                    {
                                        var ipAddresses = App.Settings.IsDoHEnabled
                                            ? await DnsHelper.ResolveDohAsync(host, DnsHelper.CurrentDoH)
                                            : await DnsHelper.ResolveDnsAsync(host, serviceViewModel.DnsIp);
                                        if (ipAddresses?.Count > 0)
                                        {
                                            var httpHeaders = new Dictionary<string, string>() { { "Host", host } };
                                            using var response = await HttpClientHelper.SendRequestAsync(url.Replace(host, ipAddresses[0].ToString()), headers: httpHeaders);
                                            if (response is { IsSuccessStatusCode: true })
                                            {
                                                fileFound = true;
                                                var headersBytes = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{response.Content.Headers}{response.Headers}\r\n");
                                                var responseData = await response.Content.ReadAsByteArrayAsync();
                                                ssl.Write(headersBytes);
                                                ssl.Write(responseData);
                                                ssl.Flush();
                                                if (serviceViewModel.IsLogging)
                                                    serviceViewModel.AddLog("HTTP 200", url, ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fileFound = true;
                                        url = $"http://{(host == "epicgames-download1-1251447533.file.myqcloud.com" ? "epicgames-download1.akamaized.net" : "epicgames-download1-1251447533.file.myqcloud.com")}{filePath}";
                                        var sb = new StringBuilder();
                                        sb.Append("HTTP/1.1 302 Moved Temporarily\r\n");
                                        sb.Append("Content-Type: text/html\r\n");
                                        sb.Append($"Location: {url}\r\n");
                                        sb.Append("Content-Length: 0\r\n\r\n");
                                        var headersBytes = Encoding.ASCII.GetBytes(sb.ToString());
                                        ssl.Write(headersBytes);
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

    [GeneratedRegex(@"(?<method>GET|POST|PUP|DELETE|OPTIONS|HEAD) (?<path>[^\s]+)")]
    private static partial Regex HttpRequestMethodAndPathRegex();
    
    [GeneratedRegex(@"^https?://[^/]+")]
    private static partial Regex BaseUrlRegex();
    
    [GeneratedRegex(@"Host:(.+)")]
    private static partial Regex HostHeaderRegex();
    
    [GeneratedRegex(@"\?.*$")]
    private static partial Regex QueryStringRegex();
    
    [GeneratedRegex(@"Range: bytes=(?<StartPosition>\d+)(-(?<EndPosition>\d+))?")]
    private static partial Regex RangeHeaderRegex();
    
    [GeneratedRegex(@"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex ContentLengthHeaderRegex();
    
    [GeneratedRegex(@"/(?<ContentId>\w{8}-\w{4}-\w{4}-\w{4}-\w{12})/(?<Version>\d+\.\d+\.\d+\.\d+)\.\w{8}-\w{4}-\w{4}-\w{4}-\w{12}")]
    private static partial Regex ContentIdVersionRegex();
    
    [GeneratedRegex(@"_xs(-\d+)?\.xvc$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex XvcRegex();
    
    [GeneratedRegex(@"\.msixvc$", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex MsiXvcRegex();
    
    [GeneratedRegex(@"Authorization:(.+)")]
    private static partial Regex AuthorizationRegex();
}

