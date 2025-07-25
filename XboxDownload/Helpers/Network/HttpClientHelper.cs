﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
    
    public static async Task<IPAddress?> GetFastestIp(IPAddress[] ips, int port, int timeout)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));

        var tasks = ips.Select(async ip =>
        {
            using var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = timeout;
            socket.ReceiveTimeout = timeout;
            try
            {
                var connectTask = Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, new IPEndPoint(ip, port), null);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeout, cts.Token));
                return (completedTask == connectTask && socket.Connected) ? ip : null;
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
            await cts.CancelAsync();
            return fastestIp;
        }
        return null;
    }
    
    public static readonly HttpClient SharedHttpClient;

    static HttpClientHelper()
    {
        var handler = new HttpClientHandler()
        {
            CookieContainer = new CookieContainer()
        };
        
        SharedHttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        SharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(nameof(XboxDownload));
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error opening URL: " + ex.Message);
        }
    }
}