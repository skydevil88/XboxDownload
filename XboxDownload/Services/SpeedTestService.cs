using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

        string[] testUrls =
        [
            "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt",
            "http://gst.prod.dl.playstation.net/networktest/get_192m",
            "http://ctest-dl-lp1.cdn.nintendo.net/30m"
        ];

        var selectedTestUri = new Uri(testUrls[Random.Shared.Next(testUrls.Length)]);
        var userAgent = selectedTestUri.Host.EndsWith(".nintendo.net")
            ? $"{nameof(XboxDownload)}/Nintendo NX"
            : nameof(XboxDownload);

        const long totalSize = 30L * 1024 * 1024;      // 30MB
        const long chunkSize = 10L * 1024 * 1024;      // 10MB
        const long maxStart = totalSize - chunkSize;
        var random = Random.Shared;
        var rangeFrom = random.NextInt64(0, maxStart + 1);
        var rangeTo = rangeFrom + chunkSize - 1;

        var timeout = TimeSpan.FromSeconds(10);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        var token = cts.Token;

        var result = new TaskCompletionSource<IpItem?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var ipResults = new ConcurrentDictionary<IpItem, long>();

        var tasks = items.Select(async ipItem =>
        {
            long totalBytes = 0;
            var (response, _) = await HttpClientHelper.MeasureHttpLatencyAsync(
                selectedTestUri,
                IPAddress.Parse(ipItem.Ip),
                timeout,
                rangeFrom: rangeFrom,
                rangeTo: rangeTo,
                userAgent: userAgent,
                token);

            if (response != null)
            {
                try
                {
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(token);
                    var buffer = new byte[64 * 1024];

                    while (true)
                    {
                        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                        if (bytesRead == 0) break;
                        totalBytes += bytesRead;
                    }

                    if (result.TrySetResult(ipItem))
                    {
                        _ = cts.CancelAsync();
                    }
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    // Even if the request fails, record the amount of data that was downloaded.
                    response.Dispose();
                    ipResults[ipItem] = totalBytes;
                }
            }

        }).ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }

        if (result.Task.IsCompletedSuccessfully)
        {
            return result.Task.Result;
        }

        var best = ipResults
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault();

        return best.Key;
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
        catch (OperationCanceledException)
        {
            // ignored
        }

        // 返回结果
        return bag.Count >= 5 ? [.. bag] : [.. items.OrderBy(_ => Random.Shared.Next()).Take(30)];
    }

    public static async Task PingAndTestAsync(IpItem item, Uri uri, long rangeTo, TimeSpan timeout, string userAgent, CancellationToken token)
    {
        item.Ttl = null;
        item.RoundtripTime = null;
        item.Speed = null;

        if (!IPAddress.TryParse(item.Ip, out var ip)) return;

        var pingTask = PingAsync(item, ip, token);

        Task? speedTask = null;
        if (!token.IsCancellationRequested)
        {
            speedTask = TestDownloadSpeedAsync(item, uri, rangeTo, timeout, userAgent, token);
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
        }
        catch
        {
            // ignored
        }
    }

    private static async Task TestDownloadSpeedAsync(IpItem item, Uri uri, long rangeTo, TimeSpan timeout, string userAgent, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        var stopwatch = Stopwatch.StartNew();

        var (response, _) = await HttpClientHelper.MeasureHttpLatencyAsync(
                uri,
                IPAddress.Parse(item.Ip),
                timeout,
                rangeTo: rangeTo,
                userAgent: userAgent,
                token: token);

        if (response != null)
        {
            try
            {
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);

                var buffer = new byte[64 * 1024];
                long totalBytes = 0;

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
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch
            {
                item.Speed ??= 0;
            }
            finally
            {
                response.Dispose();
            }
        }
        else if (!token.IsCancellationRequested)
        {
            item.Speed = 0;
        }

        stopwatch.Stop();
    }
}