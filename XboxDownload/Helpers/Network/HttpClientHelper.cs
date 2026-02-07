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
using Microsoft.Extensions.DependencyInjection;

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
        var client = HttpClientFactory?.CreateClient(name ?? "Default");
        if (client == null) return null;
        client.Timeout = TimeSpan.FromMilliseconds(timeout);
        if (headers != null)
        {
            foreach (var (key, value) in headers)
                client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
        }
        HttpRequestMessage httpRequestMessage = new()
        {
            Method = new HttpMethod(method),
            RequestUri = new Uri(url)
        };
        if (httpRequestMessage.Method == HttpMethod.Post || httpRequestMessage.Method == HttpMethod.Put)
            httpRequestMessage.Content = new StringContent(postData ?? string.Empty, Encoding.UTF8, contentType ?? "application/x-www-form-urlencoded");
        HttpResponseMessage? response;
        try
        {
            response = await client.SendAsync(httpRequestMessage, token);
        }
        catch (HttpRequestException ex)
        {
            response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = ex.Message
            };
        }
        catch (Exception ex)
        {
            response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = ex.Message
            };
        }
        return response;
    }

    public static async Task<string?> GetFastestProxy(string[] proxies, string path, Dictionary<string, string> headers, int timeout)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));

        var tasks = proxies.Select(async proxy =>
        {
            var url = proxy + (string.IsNullOrEmpty(proxy) ? path : path.Replace("https://", ""));
            using var response = await SendRequestAsync(url, headers: headers, timeout: timeout, name: "NoCache", token: cts.Token);
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
            CookieContainer = new CookieContainer(),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.All
        };

        SharedHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        SharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(XboxDownload));
    }

    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            if (OperatingSystem.IsLinux() && Program.UnixUserIsRoot())
            {
                // Get the original user when the application was started with sudo
                var user = Environment.GetEnvironmentVariable("SUDO_USER");

                var psi = new ProcessStartInfo();
                if (!string.IsNullOrEmpty(user))
                {
                    // When running as root, switch to the original user to execute xdg-open
                    psi.FileName = "runuser";
                    psi.Arguments = $"-u {user} -- xdg-open \"{url}\"";
                }
                else
                {
                    // If running as root but not started with sudo (e.g. launched with su root), try to call directly
                    psi.FileName = "xdg-open";
                    psi.Arguments = $"\"{url}\"";
                }

                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;

                Process.Start(psi);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL '{url}': {ex.Message}");
        }
    }
}