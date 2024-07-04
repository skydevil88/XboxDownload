using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    internal class ClassWeb
    {
        public static string language = Thread.CurrentThread.CurrentCulture.Name;
        private static IHttpClientFactory? httpClientFactory;

        public static void HttpClientFactory()
        {
            ServiceCollection services = new();
            services.AddHttpClient("default").ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            });
            services.AddHttpClient("XboxDownload").ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "XboxDownload/" + Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
                httpClient.DefaultRequestHeaders.Add("X-Organization", "XboxDownload");
                httpClient.DefaultRequestHeaders.Add("X-Author", "Devil");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            });
            httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        }

        public static string HttpResponseContent(string url, string method = "GET", string? postData = null, string? contentType = null, Dictionary<string, string>? headers = null, int timeOut = 30000, string? name = null, CancellationToken? cts = null, string? charset = null)
        {
            using HttpResponseMessage? response = HttpResponseMessage(url, method, postData, contentType, headers, timeOut, name, cts);
            if (response != null && response.IsSuccessStatusCode)
            {
                if (charset == null)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    return Encoding.GetEncoding(charset).GetString(response.Content.ReadAsByteArrayAsync().Result);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public static HttpResponseMessage? HttpResponseMessage(string url, string method = "GET", string? postData = null, string? contentType = null, Dictionary<string, string>? headers = null, int timeOut = 30000, string? name = null, CancellationToken? cts = null)
        {
            HttpResponseMessage? response = null;
            var client = httpClientFactory?.CreateClient(name ?? "default");
            if (client != null)
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeOut);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (string.IsNullOrEmpty(header.Value)) continue;
                        switch (header.Key)
                        {
                            case "Host":
                                client.DefaultRequestHeaders.Host = header.Value;
                                break;
                            case "Range":
                                {
                                    Match result = Regex.Match(header.Value, @"^bytes=(\d+)-(\d+)$");
                                    if (result.Success)
                                        client.DefaultRequestHeaders.Range = new RangeHeaderValue(long.Parse(result.Groups[1].Value), long.Parse(result.Groups[2].Value));
                                }
                                break;
                            default:
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                                break;
                        }
                    }
                }
                HttpRequestMessage httpRequestMessage = new()
                {
                    Method = new HttpMethod(method),
                    RequestUri = new Uri(url),
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };
                if (postData != null && httpRequestMessage.Method == HttpMethod.Post)
                    httpRequestMessage.Content = new StringContent(postData, Encoding.UTF8, contentType ?? "application/x-www-form-urlencoded");
                try
                {
                    if (cts == null)
                        response = client.SendAsync(httpRequestMessage).Result;
                    else
                        response = client.SendAsync(httpRequestMessage, (CancellationToken)cts).Result;
                }
                catch (Exception ex)
                {
                    response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        ReasonPhrase = ex.Message
                    };
                    Debug.WriteLine(ex.Message + " " + httpRequestMessage.RequestUri);
                }
            }
            return response;
        }


        public static async Task<HttpResponseMessage?> HttpResponseMessageAsync(string url, string method = "GET", string? postData = null, string? contentType = null, Dictionary<string, string>? headers = null, int timeOut = 30000, string? name = null, CancellationToken? cts = null)
        {
            HttpResponseMessage? response = null;
            var client = httpClientFactory?.CreateClient(name ?? "default");
            if (client != null)
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeOut);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (string.IsNullOrEmpty(header.Value)) continue;
                        switch (header.Key)
                        {
                            case "Host":
                                client.DefaultRequestHeaders.Host = header.Value;
                                break;
                            case "Range":
                                {
                                    Match result = Regex.Match(header.Value, @"^bytes=(\d+)-(\d+)$");
                                    if (result.Success)
                                        client.DefaultRequestHeaders.Range = new RangeHeaderValue(long.Parse(result.Groups[1].Value), long.Parse(result.Groups[2].Value));
                                }
                                break;
                            default:
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                                break;
                        }
                    }
                }
                HttpRequestMessage httpRequestMessage = new()
                {
                    Method = new HttpMethod(method),
                    RequestUri = new Uri(url),
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };
                if (postData != null && (httpRequestMessage.Method == HttpMethod.Post || httpRequestMessage.Method == HttpMethod.Put))
                    httpRequestMessage.Content = new StringContent(postData, Encoding.UTF8, contentType ?? "application/x-www-form-urlencoded");
                try
                {
                    if (cts == null)
                    response = await client.SendAsync(httpRequestMessage);
                else
                    response = await client.SendAsync(httpRequestMessage, (CancellationToken)cts);
                }
                catch (Exception ex)
                {
                    response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        ReasonPhrase = ex.Message
                    };
                    Debug.WriteLine(ex.Message + " " + httpRequestMessage.RequestUri);
                }
            }
            return response;
        }

        public static String UrlEncode(string str)
        {
            if (String.IsNullOrEmpty(str)) return string.Empty;
            return UrlEncoder.Default.Encode(str);
        }

        public static string GetMimeMapping(string path)
        {
            ClassContentType.TryGetContentType(path, out string? contentType);
            return contentType ?? "application/octet-stream";
        }

        public static bool Proxy(string ip, Byte[] send, SslStream clent, out string errMessage)
        {
            bool isOK = true;
            errMessage = string.Empty;
            String contentencoding = string.Empty;
            List<Byte> list = new();
            using (Socket mySocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, true);
                mySocket.SendTimeout = 6000;
                mySocket.ReceiveTimeout = 6000;
                try
                {
                    mySocket.Connect(ip, 443);
                }
                catch (Exception ex)
                {
                    isOK = false;
                    errMessage = ex.Message;
                }
                if (mySocket.Connected)
                {
                    using SslStream ssl = new(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) { return true; }), null);
                    ssl.WriteTimeout = 6000;
                    ssl.ReadTimeout = 6000;
                    try
                    {
                        ssl.AuthenticateAsClient(ip, null, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true);
                        if (ssl.IsAuthenticated)
                        {
                            Byte[] bReceive = new Byte[4096];
                            long count = 0;
                            Int32 len = -1;
                            long ContentLength = -1;
                            string headers = string.Empty;
                            String TransferEncoding = "";
                            ssl.Write(send);
                            ssl.Flush();
                            while ((len = ssl.Read(bReceive, 0, bReceive.Length)) > 0)
                            {
                                count += len;
                                Byte[] dest = new Byte[len];
                                if (len == bReceive.Length)
                                {
                                    dest = bReceive;
                                    if (String.IsNullOrEmpty(headers)) list.AddRange(bReceive);
                                }
                                else
                                {
                                    Buffer.BlockCopy(bReceive, 0, dest, 0, len);
                                    if (String.IsNullOrEmpty(headers)) list.AddRange(dest);
                                }
                                clent.Write(dest);
                                if (String.IsNullOrEmpty(headers))
                                {
                                    Byte[] bytes = list.ToArray();
                                    for (int i = 1; i <= bytes.Length - 4; i++)
                                    {
                                        if (BitConverter.ToString(bytes, i, 4) == "0D-0A-0D-0A")
                                        {
                                            headers = Encoding.ASCII.GetString(bytes, 0, i + 4);
                                            count = bytes.Length - i - 4;
                                            list.Clear();
                                            Match result = Regex.Match(headers, @"Content-Length:\s*(?<ContentLength>\d+)", RegexOptions.IgnoreCase);
                                            if (result.Success)
                                            {
                                                ContentLength = Convert.ToInt32(result.Groups["ContentLength"].Value);
                                            }
                                            result = Regex.Match(headers, @"Transfer-Encoding:\s*(?<TransferEncoding>.+)", RegexOptions.IgnoreCase);
                                            if (result.Success)
                                            {
                                                TransferEncoding = result.Groups["TransferEncoding"].Value.Trim();
                                            }
                                            break;
                                        }
                                    }
                                }
                                if (!String.IsNullOrEmpty(headers))
                                {
                                    if (TransferEncoding == "chunked")
                                    {
                                        if (dest.Length >= 5 && BitConverter.ToString(dest, dest.Length - 5) == "30-0D-0A-0D-0A")
                                        {
                                            break;
                                        }
                                    }
                                    else if (ContentLength >= 0)
                                    {
                                        if (count == ContentLength) break;
                                    }
                                    else break;
                                }
                            }
                            clent.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        isOK = false;
                        errMessage = ex.Message;
                    }
                    finally
                    {
                        ssl.Close();
                    }
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
            return isOK;
        }

        public static SocketPackage TcpRequest(Uri uri, Byte[] send, String? host = null, Boolean decode = false, String? charset = null, Int32 timeout = 30000, CancellationTokenSource? cts = null)
        {
            SocketPackage socketPackage = new()
            {
                Uri = uri
            };
            String contentencoding = string.Empty;
            List<Byte> list = new();
            DateTime endtime = DateTime.Now.AddMilliseconds(timeout);
            using (Socket mySocket = new(host != null && Regex.IsMatch(host, @"^([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4}$") ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, true);
                mySocket.SendTimeout = 6000;
                mySocket.ReceiveTimeout = 6000;
                try
                {
                    if (host == null)
                        mySocket.Connect(uri.Host, uri.Port);
                    else
                        mySocket.Connect(host, uri.Port);
                }
                catch (Exception ex)
                {
                    socketPackage.Err = ex.Message;
                }
                if (mySocket.Connected)
                {
                    Byte[] bReceive = new Byte[4096];
                    Int32 len = -1;
                    long ContentLength = -1;
                    String TransferEncoding = "";
                    mySocket.Send(send, 0, send.Length, SocketFlags.None, out SocketError errorCode);
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
                        if ((cts != null && cts.IsCancellationRequested) || DateTime.Compare(endtime, DateTime.Now) < 0) break;
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
                    finally
                    {
                        mySocket.Close();
                    }
                }
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

        public static SocketPackage TlsRequest(Uri uri, Byte[] send, String? host = null, Boolean decode = false, String? charset = null, Int32 timeout = 30000, CancellationTokenSource? cts = null)
        {
            SocketPackage socketPackage = new()
            {
                Uri = uri
            };
            String contentencoding = string.Empty;
            List<Byte> list = new();
            DateTime endtime = DateTime.Now.AddMilliseconds(timeout);
            using (Socket mySocket = new(host != null && Regex.IsMatch(host, @"^([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4}$") ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, true);
                mySocket.SendTimeout = 6000;
                mySocket.ReceiveTimeout = 6000;
                try
                {
                    if (host == null)
                        mySocket.Connect(uri.Host, uri.Port);
                    else
                        mySocket.Connect(host, uri.Port);
                }
                catch (Exception ex)
                {
                    socketPackage.Err = ex.Message;
                }
                if (mySocket.Connected)
                {
                    using SslStream ssl = new(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) { return sslPolicyErrors == SslPolicyErrors.None; }), null);
                    ssl.WriteTimeout = timeout;
                    ssl.ReadTimeout = timeout;
                    try
                    {
                        ssl.AuthenticateAsClient(uri.Host, null, SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true);
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
                                if ((cts != null && cts.IsCancellationRequested) || DateTime.Compare(endtime, DateTime.Now) < 0) break;
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
            socketPackage.Buffer = DeCompress(list.ToArray(), contentencoding);
            if (decode)
            {
                if (String.IsNullOrEmpty(charset)) charset = "utf-8";
                socketPackage.Html = Encoding.GetEncoding(charset).GetString(socketPackage.Buffer);
            }
            return socketPackage;
        }

        public static bool ConnectTest(Uri uri, IPAddress ip, out string errMsg)
        {
            bool verified = false;
            errMsg = "";
            using (Socket mySocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, true);
                mySocket.SendTimeout = 6000;
                mySocket.ReceiveTimeout = 6000;
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
                    using SslStream ssl = new(new NetworkStream(mySocket), false, new RemoteCertificateValidationCallback(delegate (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) { return sslPolicyErrors == SslPolicyErrors.None; }), null);
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
                mySocket.Close();
                mySocket.Dispose();
            }
            return verified;
        }

        public static Byte[] DeCompress(Byte[] buffer, String contentencoding)
        {
            switch (contentencoding)
            {
                case "gzip":
                    using (MemoryStream memoryStream = new(buffer))
                    {
                        using GZipStream zipStream = new(memoryStream, CompressionMode.Decompress);
                        using MemoryStream outputStream = new();
                        try
                        {
                            zipStream.CopyTo(outputStream);
                        }
                        catch { }
                        return outputStream.ToArray();
                    }
                case "deflate":
                    using (MemoryStream memoryStream = new(buffer))
                    {
                        using DeflateStream zipStream = new(memoryStream, CompressionMode.Decompress);
                        using MemoryStream outputStream = new();
                        try
                        {
                            zipStream.CopyTo(outputStream);
                        }
                        catch { }
                        return outputStream.ToArray();
                    }
                case "br":
                    using (MemoryStream memoryStream = new(buffer))
                    {
                        using BrotliStream zipStream = new(memoryStream, CompressionMode.Decompress);
                        using MemoryStream outputStream = new();
                        try
                        {
                            zipStream.CopyTo(outputStream);
                        }
                        catch { }
                        return outputStream.ToArray();
                    }
                default:
                    return buffer;
            }
        }

        internal static Object docLock = new();
        internal static WebBrowser? webb = null;
        internal static HtmlDocument? doc = null;

        public static void SetHtmlDocument(string strHtml, bool executeScript)
        {
            if (Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke(new MethodInvoker(() => { SetHtmlDocument(strHtml, executeScript); }));
                return;
            }
            if (!executeScript)
            {
                strHtml = Regex.Replace(strHtml, "<script", "<!--<script", RegexOptions.IgnoreCase);
                strHtml = Regex.Replace(strHtml, "</script>", "</script>!-->", RegexOptions.IgnoreCase);
            }
            webb = new WebBrowser() { ScriptErrorsSuppressed = true };
            webb.Navigate("about:blank");
            doc = webb.Document.OpenNew(true);
            doc.Write(strHtml);
        }

        public static void ObjectDisposed()
        {
            if (Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke(new MethodInvoker(() => { ObjectDisposed(); }));
                return;
            }
            doc = null;
            webb?.Dispose();
            webb = null;
        }
    }

    internal class SocketPackage
    {
        public Uri? Uri;
        public String Err = "", Headers = "", Html = "";
        public Byte[]? Buffer;

        public String All
        {
            get { return "==========Uri==========\r\n" + this.Uri + "\r\n==========Err==========\r\n" + this.Err + "\r\n==========Headers==========\r\n" + this.Headers + "\r\n==========Html==========\r\n" + this.Html; }
        }
    }
}
