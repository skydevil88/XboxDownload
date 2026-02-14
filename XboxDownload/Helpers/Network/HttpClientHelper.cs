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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
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
        if (charset is null) return await response.Content.ReadAsStringAsync(token);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(token);
        return Encoding.GetEncoding(charset).GetString(responseBytes);
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


    public static async Task<string?> GetFastestProxy(string[] proxies, string path, Dictionary<string, string> headers, int timeout)
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

    public static async Task<IPAddress?> GetFastestHttpsIp(IPAddress[] ips, int port = 443, int timeout = 3000)
    {
        using var cts = new CancellationTokenSource(timeout);

        var tasks = ips.Select(async ip =>
        {
            using var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = timeout;
            socket.ReceiveTimeout = timeout;

            try
            {
                await socket.ConnectAsync(ip, port, cts.Token);

                await using var networkStream = new NetworkStream(socket, ownsSocket: false);
                await using var sslStream = new SslStream(networkStream, false, (_, _, _, _) => true);

                var options = new SslClientAuthenticationOptions
                {
                    TargetHost = ip.ToString(),
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };

                await sslStream.AuthenticateAsClientAsync(options, cts.Token);

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

    public static readonly HttpClient SharedHttpClient;

    static HttpClientHelper()
    {
        var handler = new SocketsHttpHandler
        {
            //CookieContainer = new CookieContainer(),
            //AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
        };

        SharedHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        SharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(XboxDownload));
    }

    public static async void OpenUrl(string url)
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