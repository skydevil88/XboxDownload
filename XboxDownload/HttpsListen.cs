using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace XboxDownload
{
    class HttpsListen
    {
        private readonly Form1 parentForm;
        private readonly X509Certificate2 certificate;
        Socket? socket = null;

        public HttpsListen(Form1 parentForm)
        {
            this.parentForm = parentForm;
            this.certificate = new X509Certificate2(Properties.Resource.XboxDownload);
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
                    MessageBox.Show(String.Format("启用HTTPS服务失败!\n错误信息: {0}\n\n解决方法：1、停用占用 {1} 端口的服务。2、监听IP选择(Any)", ex.Message, port), "启用HTTPS服务失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }

            X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
            X509Certificate2 certificate = new(Properties.Resource.Xbox下载助手);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
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

        private void TcpThread(Socket mySocket)
        {
            if (mySocket.Connected)
            {
                mySocket.SendTimeout = 30000;
                mySocket.ReceiveTimeout = 30000;
                using SslStream ssl = new(new NetworkStream(mySocket), false);
                try
                {
                    ssl.AuthenticateAsServer(this.certificate, false, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                    ssl.WriteTimeout = 30000;
                    ssl.ReadTimeout = 30000;
                    if (ssl.IsAuthenticated)
                    {
                        while (Form1.bServiceFlag && mySocket.Connected && mySocket.Poll(3000000, SelectMode.SelectRead))
                        {
                            Byte[] _receive = new Byte[4096];
                            int _num = ssl.Read(_receive, 0, _receive.Length);
                            string _buffer = Encoding.ASCII.GetString(_receive, 0, _num);
                            Match result = Regex.Match(_buffer, @"(?<method>GET|POST|OPTIONS) (?<path>[^\s]+)");
                            if (!result.Success)
                            {
                                mySocket.Close();
                                continue;
                            }
                            if (_buffer.StartsWith("POST") && _buffer.EndsWith("\r\n\r\n") && !_buffer.Contains("Content-Length: 0"))
                            {
                                _num = ssl.Read(_receive, 0, _receive.Length);
                                _buffer += Encoding.ASCII.GetString(_receive, 0, _num);
                            }
                            string _method = result.Groups["method"].Value;
                            string _filePath = Regex.Replace(result.Groups["path"].Value.Trim(), @"^https?://[^/]+", "");
                            result = Regex.Match(_buffer, @"Host:(.+)");
                            if (!result.Success)
                            {
                                mySocket.Close();
                                continue;
                            }
                            string _hosts = result.Groups[1].Value.Trim().ToLower();
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
                                using (FileStream fs = new(_localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    using BinaryReader br = new(fs);
                                    string _contentRange = string.Empty, _status = "200 OK";
                                    long _fileLength = br.BaseStream.Length, _startPosition = 0;
                                    long _endPosition = _fileLength;
                                    result = Regex.Match(_buffer, @"Range: bytes=(?<StartPosition>\d+)(-(?<EndPosition>\d+))?");
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
                                }
                                if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("本地上传", _localPath, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty);
                            }
                            else
                            {
                                bool bFileNotFound = true;
                                switch (_hosts)
                                {
                                    case "api1.origin.com":
                                        //if (Properties.Settings.Default.EAStore)
                                        {
                                            string? ip = ClassDNS.DoH(_hosts);
                                            if (ip != null)
                                            {
                                                bool decode = false;
                                                if (_filePath.StartsWith("/ecommerce2/downloadURL"))
                                                {
                                                    decode = true;
                                                    if (Properties.Settings.Default.EACDN)
                                                    {
                                                        _filePath = Regex.Replace(_filePath, @"&cdnOverride=[^&]+", "");
                                                        _filePath += "&cdnOverride=akamai";
                                                    }
                                                    if (Properties.Settings.Default.EAProtocol)
                                                    {
                                                        _filePath = Regex.Replace(_filePath, @"&https=[^&]+", "");
                                                        //_filePath += "&https=false"; 
                                                    }
                                                    _buffer = Regex.Replace(_buffer, @"^" + _method + " .+", _method + " " + _filePath + " HTTP/1.1");
                                                }
                                                string _url = "https://" + _hosts + _filePath;
                                                Uri uri = new(_url);
                                                SocketPackage socketPackage = ClassWeb.SslRequest(uri, Encoding.ASCII.GetBytes(_buffer), ip, decode);
                                                if (string.IsNullOrEmpty(socketPackage.Err))
                                                {
                                                    bFileNotFound = false;
                                                    string str = socketPackage.Headers;
                                                    str = Regex.Replace(str, @"(Content-Encoding|Transfer-Encoding|Content-Length): .+\r\n", "");
                                                    str = Regex.Replace(str, @"\r\n\r\n", "\r\nContent-Length: " + socketPackage.Buffer.Length + "\r\n\r\n");
                                                    Byte[] _headers = Encoding.ASCII.GetBytes(str);
                                                    ssl.Write(_headers);
                                                    ssl.Write(socketPackage.Buffer);
                                                    ssl.Flush();
                                                    if (Properties.Settings.Default.RecordLog)
                                                    {
                                                        Match m1 = Regex.Match(socketPackage.Headers, @"^HTTP[^\s]+\s([^\s]+)");
                                                        if (m1.Success) parentForm.SaveLog("HTTP " + m1.Groups[1].Value, _url, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty);
                                                        if (_filePath.StartsWith("/ecommerce2/downloadURL"))
                                                        {
                                                            m1 = Regex.Match(socketPackage.Html, @"<url>(?<url>.+)</url>");
                                                            if (m1.Success) parentForm.SaveLog("下载地址", m1.Groups["url"].Value, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty, 0x008000);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case "epicgames-download1.akamaized.net":
                                        {
                                            bFileNotFound = false;
                                            string _url = "https://epicgames-download1-1251447533.file.myqcloud.com" + _filePath;
                                            StringBuilder sb = new();
                                            sb.Append("HTTP/1.1 301 Moved Permanently\r\n");
                                            sb.Append("Content-Type: text/html\r\n");
                                            sb.Append("Location: " + _url + "\r\n");
                                            sb.Append("Content-Length: 0\r\n\r\n");
                                            Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                            ssl.Write(_headers);
                                            ssl.Flush();
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 301", _url, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty);
                                        }
                                        break;
                                    case "store.steampowered.com":
                                    case "steamcommunity.com":
                                        //if (Properties.Settings.Default.SteamStore)
                                        {
                                            string _hosts2 = string.Empty;
                                            if (Regex.IsMatch(_filePath, @"^\/login\/(getrsakey|dologin|transfer)") || _filePath.Contains("/logout"))
                                            {
                                                _hosts2 = "help.steampowered.com";
                                            }
                                            else
                                            {
                                                if (_hosts == "store.steampowered.com")
                                                {
                                                    if (_filePath.StartsWith("/login/"))
                                                    {
                                                        if (Environment.OSVersion.Version.Major >= 10)
                                                        {
                                                            _filePath = _filePath.Replace("/login/?", "/login?");
                                                            _hosts2 = "store.akamai.steamstatic.com";
                                                            _buffer = Regex.Replace(_buffer, @"GET [^\s]+", "GET " + _filePath);
                                                        }
                                                        else _hosts2 = "help.steampowered.com";
                                                    }
                                                    else
                                                    {
                                                        //_hosts2 = "store.akamai.steamstatic.com"; //年龄限制页面不正常
                                                        _hosts2 = "store.cloudflare.steamstatic.com";
                                                        //_hosts2 = "store.st.dl.pinyuncloud.com";
                                                        //_hosts2 = "store.st.dl.eccdnx.com";
                                                    }
                                                }
                                                else if (_hosts == "steamcommunity.com")
                                                {
                                                    if (_filePath.StartsWith("/login/"))
                                                    {
                                                        if (Environment.OSVersion.Version.Major >= 10)
                                                        {
                                                            _filePath = _filePath.Replace("/login/home/?", "/login?");
                                                            _hosts2 = "community.akamai.steamstatic.com";
                                                            _buffer = Regex.Replace(_buffer, @"GET [^\s]+", "GET " + _filePath);
                                                        }
                                                        else _hosts2 = "help.steampowered.com";
                                                    }
                                                    else
                                                    {
                                                        //_hosts2 = "community.akamai.steamstatic.com";
                                                        _hosts2 = "community.cloudflare.steamstatic.com";
                                                    }
                                                }
                                            }
                                            if (_filePath == "/")
                                            {
                                                _filePath = "/default";
                                                _buffer = Regex.Replace(_buffer, @"GET [^\s]+", "GET " + _filePath);
                                            }
                                            else if (_filePath.StartsWith("/?"))
                                            {
                                                _filePath = _filePath.Replace("/?", "/default?");
                                                _buffer = Regex.Replace(_buffer, @"GET [^\s]+", "GET " + _filePath);
                                            }
                                            _buffer = Regex.Replace(_buffer, @"Host: .+", "Host: " + _hosts2);
                                            Uri uri = new("https://" + _hosts2 + _filePath);
                                            SocketPackage socketPackage = ClassWeb.SslRequest(uri, Encoding.ASCII.GetBytes(_buffer));
                                            if (string.IsNullOrEmpty(socketPackage.Err))
                                            {
                                                bFileNotFound = false;
                                                string str = socketPackage.Headers;
                                                str = Regex.Replace(str, @"(Content-Encoding|Transfer-Encoding|Content-Length): .+\r\n", "");
                                                str = Regex.Replace(str, @"\r\n\r\n", "\r\nContent-Length: " + socketPackage.Buffer.Length + "\r\n\r\n");
                                                Byte[] _headers = Encoding.ASCII.GetBytes(str);
                                                ssl.Write(_headers);
                                                ssl.Write(socketPackage.Buffer);
                                                ssl.Flush();
                                            }
                                            if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("Proxy", "https://" + _hosts + _filePath, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty);
                                        }
                                        break;
                                }
                                if (bFileNotFound)
                                {
                                    string _url = "https://" + _hosts + _filePath;
                                    Byte[] _response = Encoding.ASCII.GetBytes("File not found.");
                                    StringBuilder sb = new();
                                    sb.Append("HTTP/1.1 404 Not Found\r\n");
                                    sb.Append("Content-Type: text/html\r\n");
                                    sb.Append("Content-Length: " + _response.Length + "\r\n\r\n");
                                    Byte[] _headers = Encoding.ASCII.GetBytes(sb.ToString());
                                    ssl.Write(_headers);
                                    ssl.Write(_response);
                                    ssl.Flush();
                                    if (Properties.Settings.Default.RecordLog) parentForm.SaveLog("HTTP 404", _url, mySocket.RemoteEndPoint != null ? ((IPEndPoint)mySocket.RemoteEndPoint).Address.ToString() : string.Empty);
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
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;

                X509Store store = new(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                foreach (var item in store.Certificates)
                {
                    if (item.SubjectName.Name == "CN=Xbox下载助手")
                    {
                        store.Remove(item);
                        break;
                    }
                }
                store.Close();
            }
        }
    }
}
