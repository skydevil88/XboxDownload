using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.UI;

namespace XboxDownload.Helpers.Network;


public class HttpClientHelper
{
    private static IHttpClientFactory? HttpClientFactory => App.Services?.GetRequiredService<IHttpClientFactory>();

    public static async Task<string> GetStringContentAsync(string url, string method = "GET", string? postData = null, string? contentType = null, Dictionary<string, string>? headers = null, int timeout = 30000, string? name = null, string? charset = null, CancellationToken token = default)
    {
        using var response = await SendRequestAsync(url, method, postData, contentType, headers, timeout, name, token);
        if (response is not { IsSuccessStatusCode: true }) return string.Empty;
        try
        {
            if (charset is null) return await response.Content.ReadAsStringAsync(token);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(token);
            return Encoding.GetEncoding(charset).GetString(responseBytes);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    public static async Task<HttpResponseMessage?> SendRequestAsync(string url, string method = "GET", string? postData = null, string? contentType = null, Dictionary<string, string>? headers = null, int timeout = 30000, string? name = null, CancellationToken token = default)
    {
        var client = HttpClientFactory?.CreateClient(name ?? HttpClientNames.Default);
        if (client == null) return null;

        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (headers != null)
        {
            foreach (var (key, value) in headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
        {
            request.Content = new StringContent(
                postData ?? string.Empty,
                Encoding.UTF8,
                contentType ?? "application/x-www-form-urlencoded");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        try
        {
            return await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);
        }
        catch
        {
            return null;
        }
    }
    
    public static async Task<string?> GetFastestProxyAsync(string[] proxies, string path, Dictionary<string, string> headers, int timeout)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));

        var tasks = proxies.Select(async proxy =>
        {
            var url = proxy + (string.IsNullOrEmpty(proxy) ? path : path.Replace("https://", ""));
            using var response = await SendRequestAsync(url, headers: headers, timeout: timeout, name: HttpClientNames.SpeedTest, token: cts.Token);
            if (response is not { IsSuccessStatusCode: true }) return null;
            using var ms = new MemoryStream();
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                await stream.CopyToAsync(ms, cts.Token);
                return url;
            }
            catch (TaskCanceledException) { }
            catch (Exception)
            {
                // ignored
            }
            return null;
        }).ToList();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            var fastestUrl = await completedTask;
            if (fastestUrl == null) continue;
            await cts.CancelAsync();
            return fastestUrl;
        }
        return null;
    }

    public static async Task<IPAddress?> GetFastestHttpsIpAsync(IPAddress[] ips, int port = 443, int timeout = 3000)
    {
        using var cts = new CancellationTokenSource(timeout);

        var tasks = ips.Select(async ip =>
        {
            using var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.SendTimeout = timeout;
            socket.ReceiveTimeout = timeout;

            try
            {
                await socket.ConnectAsync(ip, port, cts.Token);

                await using var networkStream = new NetworkStream(socket, ownsSocket: true);
                await using var ssl = new SslStream(networkStream, false, (_, _, _, _) => true);

                var options = new SslClientAuthenticationOptions
                {
                    TargetHost = ip.ToString(),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };

                await ssl.AuthenticateAsClientAsync(options, cts.Token);

                return ip;
            }
            catch
            {
                return null;
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            var fastestIp = await completedTask;
            if (fastestIp == null) continue;

            _ = cts.CancelAsync();
            return fastestIp;
        }

        return null;
    }
    
    public static async Task<(HttpResponseMessage? Response, long Latency)> MeasureHttpLatencyAsync(Uri uri, IPAddress ip, TimeSpan timeout, int rangeFrom = 0, int rangeTo = 0, string? userAgent = null, CancellationToken token = default)
    {
        using var handler = new SocketsHttpHandler();
        handler.UseProxy = false;
        handler.AllowAutoRedirect = false;
        handler.AutomaticDecompression = DecompressionMethods.None;
        handler.PooledConnectionLifetime = TimeSpan.Zero;
        handler.ConnectTimeout = timeout;
        handler.ConnectCallback = async (context, cancellationToken) =>
        {
            var tcp = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await tcp.ConnectAsync(ip, uri.Port, cancellationToken);
            }
            catch (SocketException)
            {
                tcp.Dispose();
                throw;
            }

            var networkStream = new NetworkStream(tcp, ownsSocket: true);

            if (context.InitialRequestMessage.RequestUri?.Scheme != Uri.UriSchemeHttps)
                return networkStream;
            
            var ssl = new SslStream(
                networkStream,
                leaveInnerStreamOpen: false,
                (_, _, _, errors) => errors == SslPolicyErrors.None
            );
            
            try
            {
                await ssl.AuthenticateAsClientAsync(
                    context.InitialRequestMessage.RequestUri.Host,
                    null,
                    checkCertificateRevocation: false
                );
            }
            catch
            {
                tcp.Dispose();
                await ssl.DisposeAsync();
                throw;
            }

            return ssl;
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        try
        {
            using var client = new HttpClient(handler, disposeHandler: true);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Range = new RangeHeaderValue(rangeFrom, rangeTo);
            request.Headers.UserAgent.ParseAdd(string.IsNullOrEmpty(userAgent) ? nameof(XboxDownload) : userAgent);
            
            var sw = Stopwatch.StartNew();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            sw.Stop();
            
            return (response, sw.ElapsedMilliseconds);
        }
        catch
        {
            return (null, -1);
        }
    }

    public static async Task OpenUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (OperatingSystem.IsLinux() && Program.UnixUserIsRoot())
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
             ? desktop.MainWindow
             : null;

            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(url);
            
            await DialogHelper.ShowInfoDialogAsync(
                "Notice",
                $"The link has been copied to the clipboard:\n{url}\n\n" +
                "The application is running with root privileges, so it cannot automatically open the browser.\n" +
                "Please manually paste the link into your browser to open it.",
                Icon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignored
        }
    }
}

public static class HttpClientNames
{
    public const string Default = nameof(Default);
    public const string XboxDownload = nameof(XboxDownload);
    public const string SpeedTest = nameof(SpeedTest);
}