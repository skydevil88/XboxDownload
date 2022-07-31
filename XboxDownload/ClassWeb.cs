using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    class SocketPackage
    {
        public Uri Uri;
        public String Err = "", Headers = "", Html = "";
        public Byte[] Buffer;

        public String All
        {
            get { return "==========Uri==========\r\n" + this.Uri + "\r\n==========Err==========\r\n" + this.Err + "\r\n==========Headers==========\r\n" + this.Headers + "\r\n==========Html==========\r\n" + this.Html; }
        }
    }

    class ClassWeb
    {
        public static string useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36 Edg/96.0.1054.62";

        public static SocketPackage HttpRequest(String url, String method, String postdata, String referer, Boolean redirect, Boolean ajax, Boolean decode, String charset, String contenttype, String[] headers, String useragent, String accept, CookieContainer cookies, IPEndPoint localEP, String proxyaddress, Int32 proxyport, String proxyauthorization, Int32 sendtimeout = 30000, Int32 receivetimeout = 30000, Int32 autoredirect = 1, String connectHost = null, Boolean speedtest = false)
        {
            SocketPackage socketPackage;
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (Exception ex)
            {
                socketPackage = new SocketPackage
                {
                    Err = ex.Message
                };
                return socketPackage;
            }
            string host = uri.Host;
            if (string.IsNullOrEmpty(connectHost)) connectHost = host;
            string strCookie = cookies?.GetCookieHeader(uri).Replace("%2C", ",");

            StringBuilder sSend = new StringBuilder();
            if (String.IsNullOrEmpty(proxyaddress))
            {
                sSend.Append(method + " " + uri.PathAndQuery + " HTTP/1.1\r\n");
                sSend.Append("Host: " + host + "\r\n");
                sSend.Append("User-Agent: " + useragent + "\r\n");
                if (string.IsNullOrEmpty(accept))
                    sSend.Append("Accept: */*\r\n");
                else
                    sSend.Append("Accept: " + accept + "\r\n");
                sSend.Append("Accept-Language: zh-CN,zh;q=0.9,en;q=0.8,zh-TW;q=0.7,ja;q=0.6\r\n");
                if (!speedtest) sSend.Append("Accept-Encoding: gzip, deflate\r\n");
                sSend.Append("DNT: 1\r\n");
                if (ajax) sSend.Append("X-Requested-With: XMLHttpRequest\r\n");
                if (headers != null)
                {
                    foreach (string h in headers)
                    {
                        sSend.Append(h + "\r\n");
                    }
                }
                if (!String.IsNullOrEmpty(referer)) sSend.Append("Referer: " + referer + "\r\n");
                if (!String.IsNullOrEmpty(strCookie)) sSend.Append("Cookie: " + strCookie + "\r\n");
                sSend.Append("Connection: keep-alive\r\n");
                if (method == "POST")
                {
                    if (String.IsNullOrEmpty(contenttype)) sSend.Append("Content-Type: application/x-www-form-urlencoded\r\n");
                    else sSend.Append("Content-Type: " + contenttype + "\r\n");
                    if (String.IsNullOrEmpty(postdata))
                    {
                        sSend.Append("Content-Length: 0\r\n\r\n");
                    }
                    else
                    {
                        sSend.Append("Content-Length: " + postdata.Length + "\r\n\r\n");
                        sSend.Append(postdata);
                    }
                }
                else sSend.Append("\r\n");
                if (uri.Scheme == "https")
                    socketPackage = SslRequest(uri, null, sSend, host, connectHost, uri.Port, redirect, decode, charset, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect, headers, speedtest);
                else
                    socketPackage = TcpRequest(uri, sSend, host, connectHost, uri.Port, redirect, decode, charset, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect, headers, speedtest);
            }
            else
            {
                if (uri.Scheme == "https")
                {
                    StringBuilder sTunnels = new StringBuilder();
                    sTunnels.Append("CONNECT " + host + ":" + uri.Port + " HTTP/1.1\r\n");
                    if (!String.IsNullOrEmpty(proxyauthorization))
                        sTunnels.Append("Proxy-Authorization: Basic " + proxyauthorization + "\r\n");
                    sTunnels.Append("\r\n");

                    sSend.Append(method + " " + uri.PathAndQuery + " HTTP/1.1\r\n");
                    sSend.Append("Host: " + host + "\r\n");
                    sSend.Append("User-Agent: " + useragent + "\r\n");
                    if (string.IsNullOrEmpty(accept))
                        sSend.Append("Accept: */*\r\n");
                    else
                        sSend.Append("Accept: " + accept + "\r\n");
                    sSend.Append("Accept: text/html, application/xhtml+xml, */*\r\n");
                    sSend.Append("Accept-Language: zh-CN\r\n");
                    if (!speedtest) sSend.Append("Accept-Encoding: gzip, deflate\r\n");
                    sSend.Append("DNT: 1\r\n");
                    if (ajax) sSend.Append("X-Requested-With: XMLHttpRequest\r\n");
                    if (headers != null)
                    {
                        foreach (string h in headers)
                        {
                            sSend.Append(h + "\r\n");
                        }
                    }
                    if (!String.IsNullOrEmpty(referer)) sSend.Append("Referer: " + referer + "\r\n");
                    if (!String.IsNullOrEmpty(strCookie)) sSend.Append("Cookie: " + strCookie + "\r\n");
                    sSend.Append("Connection: keep-alive\r\n");
                    if (method == "POST")
                    {
                        if (String.IsNullOrEmpty(contenttype)) sSend.Append("Content-Type: application/x-www-form-urlencoded\r\n");
                        else sSend.Append("Content-Type: " + contenttype + "\r\n");
                        if (String.IsNullOrEmpty(postdata))
                        {
                            sSend.Append("Content-Length: 0\r\n\r\n");
                        }
                        else
                        {
                            sSend.Append("Content-Length: " + postdata.Length + "\r\n\r\n");
                            sSend.Append(postdata);
                        }
                    }
                    else sSend.Append("\r\n");
                    socketPackage = SslRequest(uri, sTunnels, sSend, host, proxyaddress, proxyport, redirect, decode, charset, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect, headers, speedtest);
                }
                else
                {
                    sSend.Append(method + " " + url + " HTTP/1.1\r\n");
                    sSend.Append("Host: " + host + "\r\n");
                    if (!String.IsNullOrEmpty(proxyauthorization))
                        sSend.Append("Proxy-Authorization: Basic " + proxyauthorization + "\r\n");
                    if (string.IsNullOrEmpty(accept))
                        sSend.Append("Accept: */*\r\n");
                    else
                        sSend.Append("Accept: " + accept + "\r\n");
                    sSend.Append("Accept: text/html, application/xhtml+xml, */*\r\n");
                    sSend.Append("Accept-Language: zh-CN\r\n");
                    sSend.Append("Accept-Encoding: gzip, deflate\r\n");
                    sSend.Append("DNT: 1\r\n");
                    if (ajax) sSend.Append("X-Requested-With: XMLHttpRequest\r\n");
                    if (headers != null)
                    {
                        foreach (string h in headers)
                        {
                            sSend.Append(h + "\r\n");
                        }
                    }
                    if (!String.IsNullOrEmpty(referer)) sSend.Append("Referer: " + referer + "\r\n");
                    if (!String.IsNullOrEmpty(strCookie)) sSend.Append("Cookie: " + strCookie + "\r\n");
                    sSend.Append("Connection: keep-alive\r\n");
                    if (method == "POST")
                    {
                        if (String.IsNullOrEmpty(contenttype)) sSend.Append("Content-Type: application/x-www-form-urlencoded\r\n");
                        else sSend.Append("Content-Type: " + contenttype + "\r\n");
                        if (String.IsNullOrEmpty(postdata))
                        {
                            sSend.Append("Content-Length: 0\r\n\r\n");
                        }
                        else
                        {
                            sSend.Append("Content-Length: " + postdata.Length + "\r\n\r\n");
                            sSend.Append(postdata);
                        }
                    }
                    else sSend.Append("\r\n");
                    socketPackage = TcpRequest(uri, sSend, host, proxyaddress, proxyport, redirect, decode, charset, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect, headers, speedtest);
                }
            }
            return socketPackage;
        }

        private static SocketPackage TcpRequest(Uri uri, StringBuilder sSend, String targetHost, String connectHost, Int32 connectPort, Boolean redirect, Boolean decode, String charset, String useragent, String accept, CookieContainer cookies, IPEndPoint localEP, String proxyaddress, Int32 proxyport, String proxyauthorization, Int32 receivetimeout, Int32 sendtimeout, Int32 autoredirect, String[] headers, Boolean speedtest)
        {
            SocketPackage socketPackage = new SocketPackage
            {
                Uri = uri
            };
            String contentencoding = null;
            List<Byte> list = new List<Byte>();
            String location = null;
            using (Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SendTimeout = sendtimeout;
                mySocket.ReceiveTimeout = receivetimeout;
                try
                {
                    if (localEP != null) mySocket.Bind(localEP);
                    mySocket.Connect(connectHost, connectPort);
                }
                catch (Exception ex)
                {
                    socketPackage.Err = ex.Message;
                }
                if (mySocket.Connected)
                {
                    Byte[] bSend = Encoding.ASCII.GetBytes(sSend.ToString());
                    Byte[] bReceive = new Byte[4096];
                    Int32 len = -1;
                    long ContentLength = -1;
                    String TransferEncoding = "";
                    mySocket.Send(bSend, 0, bSend.Length, SocketFlags.None, out SocketError errorCode);
                    DateTime endtime = DateTime.Now.AddSeconds(30);
                    while ((len = mySocket.Receive(bReceive, 0, bReceive.Length, SocketFlags.None, out errorCode)) > 0)
                    {
                        if (len == bReceive.Length) list.AddRange(bReceive);
                        else
                        {
                            Byte[] dest = new Byte[len];
                            Buffer.BlockCopy(bReceive, 0, dest, 0, len);
                            list.AddRange(dest);
                        }
                        if (String.IsNullOrEmpty(socketPackage.Headers))
                        {
                            Byte[] bytes = list.ToArray();
                            for (int i = 1; i <= bytes.Length - 4; i++)
                            {
                                if (BitConverter.ToString(bytes, i, 4) == "0D-0A-0D-0A")
                                {
                                    list.Clear();
                                    Byte[] dest = new Byte[bytes.Length - i - 4];
                                    Buffer.BlockCopy(bytes, i + 4, dest, 0, dest.Length);
                                    list.AddRange(dest);

                                    socketPackage.Headers = Encoding.ASCII.GetString(bytes, 0, i + 4);
                                    Match result = Regex.Match(socketPackage.Headers, @"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase);
                                    if (result.Success)
                                    {
                                        ContentLength = Convert.ToInt64(result.Groups["ContentLength"].Value);
                                    }
                                    result = Regex.Match(socketPackage.Headers, @"Transfer-Encoding:\s*(?<TransferEncoding>.+)", RegexOptions.IgnoreCase);
                                    if (result.Success)
                                    {
                                        TransferEncoding = result.Groups["TransferEncoding"].Value.Trim();
                                    }
                                    result = Regex.Match(socketPackage.Headers, @"Content-Encoding:\s*(?<ContentEncoding>.+)", RegexOptions.IgnoreCase);
                                    if (result.Success)
                                    {
                                        contentencoding = result.Groups["ContentEncoding"].Value.Trim().ToLower();
                                    }
                                    result = Regex.Match(socketPackage.Headers, @"Location:\s*(?<Location>.+)", RegexOptions.IgnoreCase);
                                    if (result.Success)
                                    {
                                        location = result.Groups["Location"].Value.Trim();
                                    }
                                    if (decode && String.IsNullOrEmpty(charset) && String.IsNullOrEmpty(location))
                                    {
                                        result = Regex.Match(socketPackage.Headers, @"Content-Type:.*charset=(?<charset>.+)", RegexOptions.IgnoreCase);
                                        if (result.Success)
                                        {
                                            charset = result.Groups["charset"].Value.Trim();
                                        }
                                    }
                                    if (cookies != null) SetCookie(targetHost, socketPackage.Headers, cookies);
                                    break;
                                }
                            }
                        }
                        if (!String.IsNullOrEmpty(socketPackage.Headers))
                        {
                            if (TransferEncoding == "chunked")
                            {
                                Byte[] bytes = list.ToArray();
                                if (bytes.Length >= 5 && BitConverter.ToString(bytes, bytes.Length - 5) == "30-0D-0A-0D-0A")
                                {
                                    list.Clear();
                                    int step = 0;
                                    for (int i = 1; i < bytes.Length - 1; i++)
                                    {
                                        if (BitConverter.ToString(bytes, i, 2) == "0D-0A")
                                        {
                                            Int32.TryParse(Encoding.ASCII.GetString(bytes, step, i - step), System.Globalization.NumberStyles.HexNumber, null, out int chunk);
                                            if (chunk == 0) break;

                                            Byte[] dest = new Byte[chunk];
                                            Buffer.BlockCopy(bytes, i + 2, dest, 0, dest.Length);
                                            list.AddRange(dest);

                                            i = step = i + 2 + chunk;
                                        }
                                    }
                                    break;
                                }
                            }
                            else if (ContentLength >= 0)
                            {
                                if (list.Count == ContentLength) break;
                            }
                            else break;
                        }
                        if (speedtest && DateTime.Compare(endtime, DateTime.Now) < 0) break;
                    }
                    if (errorCode.ToString() != "Success")
                        socketPackage.Err = errorCode.ToString();
                }
                if (mySocket.Connected)
                {
                    try
                    {
                        mySocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }
                mySocket.Close();
                mySocket.Dispose();
            }
            if (!String.IsNullOrEmpty(location) && redirect)
            {
                if (autoredirect <= 5)
                {
                    if (!Regex.IsMatch(location, @"^https?:\/\/"))
                    {
                        location = new Uri(uri, location).ToString();
                    }
                    socketPackage = HttpRequest(location, "GET", null, socketPackage.Uri.ToString(), true, false, decode, charset, null, headers, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect++);
                }
                else
                {
                    socketPackage.Err = "此页面不能正确地重定向";
                }
            }
            else
            {
                socketPackage.Buffer = ClassWeb.DeCompress(list.ToArray(), contentencoding);
                if (decode)
                {
                    if (String.IsNullOrEmpty(charset)) charset = "utf-8";
                    socketPackage.Html = Encoding.GetEncoding(charset).GetString(socketPackage.Buffer);
                }
            }
            return socketPackage;
        }

        private static SocketPackage SslRequest(Uri uri, StringBuilder sTunnels, StringBuilder sSend, String targetHost, String connectHost, Int32 connectPort, Boolean redirect, Boolean decode, String charset, String useragent, String accept, CookieContainer cookies, IPEndPoint localEP, String proxyaddress, Int32 proxyport, String proxyauthorization, Int32 receivetimeout, Int32 sendtimeout, Int32 autoredirect, String[] headers, Boolean speedtest)
        {
            SocketPackage socketPackage = new SocketPackage
            {
                Uri = uri
            };
            String contentencoding = null;
            List<Byte> list = new List<Byte>();
            String location = null;
            using (Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SendTimeout = sendtimeout;
                mySocket.ReceiveTimeout = receivetimeout;
                try
                {
                    if (localEP != null) mySocket.Bind(localEP);
                    mySocket.Connect(connectHost, connectPort);
                }
                catch (Exception ex)
                {
                    socketPackage.Err = ex.Message;
                }
                if (mySocket.Connected)
                {
                    bool bTunnels = true;
                    if (sTunnels != null)
                    {
                        Byte[] bSend = Encoding.ASCII.GetBytes(sTunnels.ToString());
                        Byte[] bReceive = new Byte[512];
                        mySocket.Send(bSend, 0, bSend.Length, SocketFlags.None, out SocketError errorCode);
                        Int32 len = mySocket.Receive(bReceive, 0, bReceive.Length, SocketFlags.None, out errorCode);
                        string tmp = Encoding.ASCII.GetString(bReceive, 0, len);
                        if (!Regex.IsMatch(tmp, @"^HTTP/1\.\d 200 Connection established", RegexOptions.IgnoreCase))
                        {
                            bTunnels = false;
                            socketPackage.Err = errorCode.ToString();
                            socketPackage.Html = tmp;
                        }
                    }
                    if (bTunnels)
                    {
                        using (SslStream ssl = new SslStream(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; }), null))
                        {
                            ssl.WriteTimeout = sendtimeout;
                            ssl.ReadTimeout = receivetimeout;
                            try
                            {
                                ssl.AuthenticateAsClient(targetHost, null, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                                if (ssl.IsAuthenticated)
                                {
                                    Byte[] bSend = Encoding.ASCII.GetBytes(sSend.ToString());
                                    Byte[] bReceive = new Byte[4096];
                                    Int32 len = -1;
                                    long ContentLength = -1;
                                    String TransferEncoding = "";
                                    ssl.Write(bSend);
                                    ssl.Flush();
                                    DateTime endtime = DateTime.Now.AddSeconds(30);
                                    while ((len = ssl.Read(bReceive, 0, bReceive.Length)) > 0)
                                    {
                                        if (len == bReceive.Length) list.AddRange(bReceive);
                                        else
                                        {
                                            Byte[] dest = new Byte[len];
                                            Buffer.BlockCopy(bReceive, 0, dest, 0, len);
                                            list.AddRange(dest);
                                        }
                                        if (String.IsNullOrEmpty(socketPackage.Headers))
                                        {
                                            Byte[] bytes = list.ToArray();
                                            for (int i = 1; i <= bytes.Length - 4; i++)
                                            {
                                                if (BitConverter.ToString(bytes, i, 4) == "0D-0A-0D-0A")
                                                {
                                                    list.Clear();
                                                    Byte[] dest = new Byte[bytes.Length - i - 4];
                                                    Buffer.BlockCopy(bytes, i + 4, dest, 0, dest.Length);
                                                    list.AddRange(dest);

                                                    socketPackage.Headers = Encoding.ASCII.GetString(bytes, 0, i + 4);
                                                    Match result = Regex.Match(socketPackage.Headers, @"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase);
                                                    if (result.Success)
                                                    {
                                                        ContentLength = Convert.ToInt32(result.Groups["ContentLength"].Value);
                                                    }
                                                    result = Regex.Match(socketPackage.Headers, @"Transfer-Encoding:\s*(?<TransferEncoding>.+)", RegexOptions.IgnoreCase);
                                                    if (result.Success)
                                                    {
                                                        TransferEncoding = result.Groups["TransferEncoding"].Value.Trim();
                                                    }
                                                    result = Regex.Match(socketPackage.Headers, @"Content-Encoding:\s*(?<ContentEncoding>.+)", RegexOptions.IgnoreCase);
                                                    if (result.Success)
                                                    {
                                                        contentencoding = result.Groups["ContentEncoding"].Value.Trim().ToLower();
                                                    }
                                                    result = Regex.Match(socketPackage.Headers, @"Location:\s*(?<Location>.+)", RegexOptions.IgnoreCase);
                                                    if (result.Success)
                                                    {
                                                        location = result.Groups["Location"].Value.Trim();
                                                    }
                                                    if (decode && String.IsNullOrEmpty(charset) && String.IsNullOrEmpty(location))
                                                    {
                                                        result = Regex.Match(socketPackage.Headers, @"Content-Type:.*charset=(?<charset>.+)", RegexOptions.IgnoreCase);
                                                        if (result.Success)
                                                        {
                                                            charset = result.Groups["charset"].Value.Trim();
                                                        }
                                                    }
                                                    if (cookies != null) SetCookie(targetHost, socketPackage.Headers, cookies);
                                                    break;
                                                }
                                            }
                                        }
                                        if (!String.IsNullOrEmpty(socketPackage.Headers))
                                        {
                                            if (TransferEncoding == "chunked")
                                            {
                                                Byte[] bytes = list.ToArray();
                                                if (bytes.Length >= 5 && BitConverter.ToString(bytes, bytes.Length - 5) == "30-0D-0A-0D-0A")
                                                {
                                                    list.Clear();
                                                    int step = 0;
                                                    for (int i = 1; i < bytes.Length - 1; i++)
                                                    {
                                                        if (BitConverter.ToString(bytes, i, 2) == "0D-0A")
                                                        {
                                                            Int32.TryParse(Encoding.ASCII.GetString(bytes, step, i - step), System.Globalization.NumberStyles.HexNumber, null, out int chunk);
                                                            if (chunk == 0) break;

                                                            Byte[] dest = new Byte[chunk];
                                                            Buffer.BlockCopy(bytes, i + 2, dest, 0, dest.Length);
                                                            list.AddRange(dest);

                                                            i = step = i + 2 + chunk;
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                            else if (ContentLength >= 0)
                                            {
                                                if (list.Count == ContentLength) break;
                                            }
                                            else break;
                                        }
                                        if (speedtest && DateTime.Compare(endtime, DateTime.Now) < 0) break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                socketPackage.Err = ex.Message;
                            }
                            finally
                            {
                                ssl.Close();
                            }
                        }
                    }
                }
                if (mySocket.Connected)
                {
                    try
                    {
                        mySocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }
                mySocket.Close();
                mySocket.Dispose();
            }
            if (!String.IsNullOrEmpty(location) && redirect)
            {
                if (autoredirect <= 5)
                {
                    if (!Regex.IsMatch(location, @"^https?:\/\/"))
                    {
                        location = new Uri(uri, location).ToString();
                    }
                    socketPackage = HttpRequest(location, "GET", null, socketPackage.Uri.ToString(), redirect, false, decode, charset, null, headers, useragent, accept, cookies, localEP, proxyaddress, proxyport, proxyauthorization, sendtimeout, receivetimeout, autoredirect++);
                }
                else
                {
                    socketPackage.Err = "此页面不能正确地重定向";
                }
            }
            else
            {
                socketPackage.Buffer = ClassWeb.DeCompress(list.ToArray(), contentencoding);
                if (decode)
                {
                    if (String.IsNullOrEmpty(charset)) charset = "utf-8";
                    socketPackage.Html = Encoding.GetEncoding(charset).GetString(socketPackage.Buffer);
                }
            }
            return socketPackage;
        }

        public static SocketPackage SslRequest(Uri uri, Byte[] send, String connectHost = null, Boolean decode = false, String charset = null, Int32 receivetimeout = 30000, Int32 sendtimeout = 30000)
        {
            SocketPackage socketPackage = new SocketPackage
            {
                Uri = uri
            };
            String contentencoding = null;
            List<Byte> list = new List<Byte>();
            using (Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SendTimeout = sendtimeout;
                mySocket.ReceiveTimeout = receivetimeout;
                try
                {
                    mySocket.Connect(connectHost ?? uri.Host, uri.Port);
                }
                catch (Exception ex)
                {
                    socketPackage.Err = ex.Message;
                }
                if (mySocket.Connected)
                {
                    using (SslStream ssl = new SslStream(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; }), null))
                    {
                        ssl.WriteTimeout = sendtimeout;
                        ssl.ReadTimeout = receivetimeout;
                        try
                        {
                            ssl.AuthenticateAsClient(uri.Host, null, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                            if (ssl.IsAuthenticated)
                            {
                                Byte[] bReceive = new Byte[4096];
                                Int32 len = -1;
                                long ContentLength = -1;
                                String TransferEncoding = "";
                                ssl.Write(send);
                                ssl.Flush();
                                while ((len = ssl.Read(bReceive, 0, bReceive.Length)) > 0)
                                {
                                    if (len == bReceive.Length) list.AddRange(bReceive);
                                    else
                                    {
                                        Byte[] dest = new Byte[len];
                                        Buffer.BlockCopy(bReceive, 0, dest, 0, len);
                                        list.AddRange(dest);
                                    }
                                    if (String.IsNullOrEmpty(socketPackage.Headers))
                                    {
                                        Byte[] bytes = list.ToArray();
                                        for (int i = 1; i <= bytes.Length - 4; i++)
                                        {
                                            if (BitConverter.ToString(bytes, i, 4) == "0D-0A-0D-0A")
                                            {
                                                list.Clear();
                                                Byte[] dest = new Byte[bytes.Length - i - 4];
                                                Buffer.BlockCopy(bytes, i + 4, dest, 0, dest.Length);
                                                list.AddRange(dest);

                                                socketPackage.Headers = Encoding.ASCII.GetString(bytes, 0, i + 4);
                                                Match result = Regex.Match(socketPackage.Headers, @"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase);
                                                if (result.Success)
                                                {
                                                    ContentLength = Convert.ToInt32(result.Groups["ContentLength"].Value);
                                                }
                                                result = Regex.Match(socketPackage.Headers, @"Transfer-Encoding:\s*(?<TransferEncoding>.+)", RegexOptions.IgnoreCase);
                                                if (result.Success)
                                                {
                                                    TransferEncoding = result.Groups["TransferEncoding"].Value.Trim();
                                                }
                                                result = Regex.Match(socketPackage.Headers, @"Content-Encoding:\s*(?<ContentEncoding>.+)", RegexOptions.IgnoreCase);
                                                if (result.Success)
                                                {
                                                    contentencoding = result.Groups["ContentEncoding"].Value.Trim().ToLower();
                                                }
                                                if (decode && String.IsNullOrEmpty(charset))
                                                {
                                                    result = Regex.Match(socketPackage.Headers, @"Content-Type:.*charset=(?<charset>.+)", RegexOptions.IgnoreCase);
                                                    if (result.Success)
                                                    {
                                                        charset = result.Groups["charset"].Value.Trim();
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    if (!String.IsNullOrEmpty(socketPackage.Headers))
                                    {
                                        if (TransferEncoding == "chunked")
                                        {
                                            Byte[] bytes = list.ToArray();
                                            if (bytes.Length >= 5 && BitConverter.ToString(bytes, bytes.Length - 5) == "30-0D-0A-0D-0A")
                                            {
                                                list.Clear();
                                                int step = 0;
                                                for (int i = 1; i < bytes.Length - 1; i++)
                                                {
                                                    if (BitConverter.ToString(bytes, i, 2) == "0D-0A")
                                                    {
                                                        Int32.TryParse(Encoding.ASCII.GetString(bytes, step, i - step), System.Globalization.NumberStyles.HexNumber, null, out int chunk);
                                                        if (chunk == 0) break;

                                                        Byte[] dest = new Byte[chunk];
                                                        Buffer.BlockCopy(bytes, i + 2, dest, 0, dest.Length);
                                                        list.AddRange(dest);

                                                        i = step = i + 2 + chunk;
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        else if (ContentLength >= 0)
                                        {
                                            if (list.Count == ContentLength) break;
                                        }
                                        else break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            socketPackage.Err = ex.Message;
                        }
                        finally
                        {
                            ssl.Close();
                        }
                    }
                }
                if (mySocket.Connected)
                {
                    try
                    {
                        mySocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }
                mySocket.Close();
                mySocket.Dispose();
            }
            socketPackage.Buffer = ClassWeb.DeCompress(list.ToArray(), contentencoding);
            if (decode)
            {
                if (String.IsNullOrEmpty(charset)) charset = "utf-8";
                socketPackage.Html = Encoding.GetEncoding(charset).GetString(socketPackage.Buffer);
            }
            return socketPackage;
        }

        public static bool VerifySslCertificate(Uri uri, IPAddress ip, out string errMsg)
        {
            bool verified = false;
            errMsg = "";
            List<Byte> list = new List<Byte>();
            using (Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SendTimeout = 150000;
                mySocket.ReceiveTimeout = 15000;
                try
                {
                    mySocket.Connect(ip, uri.Port);
                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                }
                if (mySocket.Connected)
                {
                    using (SslStream ssl = new SslStream(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return sslPolicyErrors == SslPolicyErrors.None; }), null))
                    {
                        try
                        {
                            ssl.AuthenticateAsClient(uri.Host, null, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true);
                            if (ssl.IsAuthenticated)
                            {
                                verified = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            errMsg = ex.Message;
                        }
                        finally
                        {
                            ssl.Close();
                        }
                    }
                }
                mySocket.Close();
                mySocket.Dispose();
            }
            return verified;
        }

        private static void SetCookie(String domain, String headers, CookieContainer cookies)
        {
            headers = headers.Replace("\r\n ", " ");
            Match result = Regex.Match(headers, @"Set-Cookie:(?<cookie>.+)", RegexOptions.IgnoreCase);
            while (result.Success)
            {
                Cookie ck = new Cookie();
                foreach (string s in result.Groups["cookie"].Value.Split(';'))
                {
                    string str = s.Trim();
                    switch (str)
                    {
                        case "Secure":
                            ck.Secure = true;
                            break;
                        case "HttpOnly":
                            ck.HttpOnly = true;
                            break;
                        default:
                            Match m1 = Regex.Match(str, @"(?<name>[^=]+)=(?<value>.*)");
                            while (m1.Success)
                            {
                                string name = m1.Groups["name"].Value.Trim();
                                if (!Regex.IsMatch(name, "^(Comment|CommentUri|Discard|Equals|Expired|Port|TimeStamp|Version|SameSite)$", RegexOptions.IgnoreCase))
                                {
                                    string value = m1.Groups["value"].Value.Trim();
                                    switch (name.ToLower())
                                    {
                                        case "domain":
                                            ck.Domain = value;
                                            break;
                                        case "expires":
                                            DateTime expires;
                                            if (DateTime.TryParse(value, out expires))
                                                ck.Expires = expires;
                                            break;
                                        case "max-age":
                                            int maxage;
                                            if (Int32.TryParse(value, out maxage))
                                                ck.Expires = DateTime.Now.AddSeconds(maxage);
                                            break;
                                        case "path":
                                            ck.Path = value;
                                            break;
                                        default:
                                            ck.Name = name;
                                            ck.Value = value.Replace(",", "%2C");
                                            break;
                                    }
                                }
                                m1 = m1.NextMatch();
                            }
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(ck.Name))
                {
                    if (string.IsNullOrEmpty(ck.Domain))
                        ck.Domain = domain;
                    try
                    {
                        cookies.Add(ck);
                    }
                    catch { }
                }
                result = result.NextMatch();
            }
        }

        public static String UrlEncode(String str)
        {
            if (String.IsNullOrEmpty(str)) return "";
            return Regex.Replace(System.Web.HttpUtility.UrlEncode(str), "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());
        }

        public static Byte[] DeCompress(Byte[] buffer, String contentencoding)
        {
            switch (contentencoding)
            {
                case "gzip":
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            using (MemoryStream vMemory = new MemoryStream())
                            {
                                try
                                {
                                    zipStream.CopyTo(vMemory);
                                }
                                catch { }
                                return vMemory.ToArray();
                            }
                        }
                    }
                case "deflate":
                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        using (DeflateStream zipStream = new DeflateStream(stream, CompressionMode.Decompress))
                        {
                            using (MemoryStream vMemory = new MemoryStream())
                            {
                                try
                                {
                                    zipStream.CopyTo(vMemory);
                                }
                                catch { }
                                return vMemory.ToArray();
                            }
                        }
                    }
                default:
                    return buffer;
            }
        }

        internal static Object docLock = new Object();
        internal static System.Windows.Forms.WebBrowser webb = null;
        internal static System.Windows.Forms.HtmlDocument doc = null;

        public static void SetHtmlDocument(string strHtml, bool executeScript)
        {
            if (System.Windows.Forms.Application.OpenForms[0].InvokeRequired)
            {
                System.Windows.Forms.Application.OpenForms[0].Invoke(new System.Windows.Forms.MethodInvoker(() => { SetHtmlDocument(strHtml, executeScript); }));
                return;
            }
            if (!executeScript)
            {
                strHtml = Regex.Replace(strHtml, "<script", "<!--<script", RegexOptions.IgnoreCase);
                strHtml = Regex.Replace(strHtml, "</script>", "</script>!-->", RegexOptions.IgnoreCase);
            }
            webb = new System.Windows.Forms.WebBrowser() { ScriptErrorsSuppressed = true };
            webb.Navigate("about:blank");
            doc = webb.Document.OpenNew(true);
            doc.Write(strHtml);
        }

        public static void ObjectDisposed()
        {
            if (System.Windows.Forms.Application.OpenForms[0].InvokeRequired)
            {
                System.Windows.Forms.Application.OpenForms[0].Invoke(new System.Windows.Forms.MethodInvoker(() => { ObjectDisposed(); }));
                return;
            }
            doc = null;
            webb.Dispose();
            webb = null;
        }
    }
}