using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.Network;
using XboxDownload.Models.SpeedTest;

namespace XboxDownload.Services;

public static class SpeedTestService
{
    public static async Task<IpItem?> FindFastestOrBestAkamaiIpAsync(List<IpItem> items, CancellationToken cancellationToken)
    {
        if (items.Count > 10)
        {
            items = await PingFastest10Async(items, cancellationToken);
        }
        
        string[] test =
        [
            "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt",
            "http://gst.prod.dl.playstation.net/networktest/get_192m",
            "http://ctest-dl-lp1.cdn.nintendo.net/30m"
        ];

        Uri baseUri = new(test[Random.Shared.Next(test.Length)]);

        var userAgent = baseUri.Host.EndsWith(".nintendo.net") ? "XboxDownload/Nintendo NX" : "XboxDownload";
        var headers = new Dictionary<string, string>
        {
            { "Host", baseUri.Host },
            { "User-Agent", userAgent },
            { "Range", "bytes=0-10485759"} // 10MB
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(15));
        var token = cts.Token;

        var result = new TaskCompletionSource<IpItem?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var ipResults = new ConcurrentDictionary<IpItem, long>();

        var startSignal = new TaskCompletionSource<bool>(); // 控制统一开始

        // ReSharper disable once MethodSupportsCancellation
        var tasks = items.Select(ipItem => Task.Run(async () =>
        {
            var builder = new UriBuilder(baseUri) { Host = ipItem.Ip };
            long totalBytes = 0;

            try
            {
                await startSignal.Task; // 等待统一启动信号

                using var request = CreateSpeedTestRequest(builder.Uri, headers);
                using var response = await HttpClientHelper.SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(token);
                var buffer = new byte[8192];

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0) break;
                    totalBytes += bytesRead;
                }

                if (result.TrySetResult(ipItem))
                {
                    // ReSharper disable once AccessToDisposedClosure
                    _ = cts.CancelAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SpeedTest failed for {builder.Uri}: {ex.Message}");
            }
            finally
            {
                // Even if the request fails, record the amount of data that was downloaded.
                ipResults[ipItem] = totalBytes;
            }
        })).ToList();

        // ✅ 所有任务创建完后再启动测速
        startSignal.SetResult(true); // 发出统一开始信号

        try
        {
            await Task.WhenAll(tasks);

            if (result.Task.IsCompletedSuccessfully)
            {
                return await result.Task;
            }

            // If all speed tests fail or are canceled, select the one with the highest totalBytes downloaded.
            var best = ipResults.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).FirstOrDefault();
            return best.Key;
        }
        finally
        {
            cts.Dispose();
        }
    }
    
    private static async Task<List<IpItem>> PingFastest10Async(List<IpItem> items, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        var bag = new ConcurrentBag<IpItem>();
        var successCount = 0;

        var startSignal = new TaskCompletionSource<bool>();
        
        var tasks = items.Select(async item =>
        {
            await startSignal.Task; // 等待统一启动
            
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(item.Ip, TimeSpan.FromSeconds(1), null, null, cts.Token);

                if (reply.Status == IPStatus.Success)
                {
                    item.Ttl = reply.Options?.Ttl;
                    item.RoundtripTime = reply.RoundtripTime;

                    var current = Interlocked.Increment(ref successCount);

                    if (current <= 10)
                        bag.Add(item);

                    if (current == 10)
                        _ = cts.CancelAsync();
                }
            }
            catch
            {
                // ignored
            }
        }).ToList();

        startSignal.SetResult(true);

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { }

        // 返回结果
        return bag.Count >= 5 ? bag.ToList() : items.OrderBy(_ => Random.Shared.Next()).Take(30).ToList();
    }

    public static async Task PingAndTestAsync(IpItem item, Uri? baseUri, Dictionary<string, string>? headers, TimeSpan timeout, CancellationToken token)
    {
        item.IsRedirect = false;
        item.Ttl = null;
        item.RoundtripTime = null;
        item.Speed = null;

        if (!IPAddress.TryParse(item.Ip, out var ip)) return;

        var pingTask = PingAsync(item, ip, token);

        Task? speedTask = null;
        if (baseUri != null && !token.IsCancellationRequested)
        {
            var builder = new UriBuilder(baseUri) { Host = ip.ToString() };
            speedTask = TestDownloadSpeedAsync(item, builder.Uri, headers, timeout, token);
        }

        if (speedTask is not null)
            await Task.WhenAll(pingTask, speedTask);
        else
            await pingTask;
    }

    private static async Task PingAsync(IpItem item, IPAddress ip, CancellationToken token)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, TimeSpan.FromSeconds(1), null, null, token);
            if (reply.Status == IPStatus.Success)
            {
                item.Ttl = reply.Options?.Ttl;
                item.RoundtripTime = reply.RoundtripTime;
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ping failed for {item.Ip}: {ex.Message}");
        }
    }

    private static async Task TestDownloadSpeedAsync(IpItem item, Uri uri, Dictionary<string, string>? headers, TimeSpan timeout, CancellationToken token)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);

            using var request = CreateSpeedTestRequest(uri, headers);
            using var response = await HttpClientHelper.SharedHttpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            if (!Equals(response.RequestMessage?.RequestUri, uri))
                item.IsRedirect = true;

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);

            var buffer = new byte[8192];
            long totalBytes = 0;
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
                if (bytesRead == 0)
                    break;

                totalBytes += bytesRead;

                var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                if (elapsedSeconds > 0.1) // 100毫秒阈值，防止除数过小导致数值异常
                {
                    item.Speed = totalBytes / (1048576 * elapsedSeconds); // bytes to MiB per second
                }
            }

            stopwatch.Stop();
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SpeedTest failed for {item.Ip}: {ex.Message}");
        }
        finally
        {
            item.Speed ??= 0;
        }
    }

    private static HttpRequestMessage CreateSpeedTestRequest(Uri uri, Dictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
        request.Headers.Pragma.TryParseAdd("no-cache");

        if (headers is null) return request;

        foreach (var (key, value) in headers)
            request.Headers.TryAddWithoutValidation(key, value);

        return request;
    }
}