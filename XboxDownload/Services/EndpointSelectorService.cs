using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.Network;
using XboxDownload.Models.SpeedTest;

namespace XboxDownload.Services;

/// <summary>
/// Selects the best-performing endpoint from a candidate IP list using a
/// cheap-to-expensive funnel: ICMP reachability/latency → small HTTP latency
/// probe → small ranged speed test → rank → automatic selection.
///
/// This complements <see cref="SpeedTestService"/> (full per-IP speed test on
/// the Speed Test tab) and <see cref="NetworkDiagnosticsService"/> (single
/// endpoint diagnostics) by providing automatic, low-bandwidth endpoint
/// discovery that does not assume the upstream author's preferred IP is
/// optimal for every user.
/// </summary>
public static class EndpointSelectorService
{
    /// <summary>Maximum candidates kept after the ICMP reachability stage.</summary>
    private const int MaxReachableCandidates = 12;

    /// <summary>Number of candidates that proceed to the ranged speed test.</summary>
    private const int MaxSpeedTestCandidates = 5;

    /// <summary>Hard cap for the concurrent ICMP stage.</summary>
    private static readonly TimeSpan PingStageTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Per-probe timeout for the HTTP latency stage.</summary>
    private static readonly TimeSpan HttpLatencyTimeout = TimeSpan.FromSeconds(4);

    /// <summary>Number of HTTP latency probes per survivor (used for packet-loss estimation).</summary>
    private const int HttpLatencyProbes = 2;

    /// <summary>Small ranged download size (4 MB) for the speed-test stage.</summary>
    private const long SpeedTestRangeBytes = 4L * 1024 * 1024;

    /// <summary>Hard cap for the speed-test stage.</summary>
    private static readonly TimeSpan SpeedTestStageTimeout = TimeSpan.FromSeconds(12);

    /// <summary>
    /// Result of ranking a single candidate through the funnel.
    /// </summary>
    public sealed record EndpointRank(
        IpItem Item,
        long? PingRoundtripMilliseconds,
        long? HttpLatencyMilliseconds,
        int HttpProbeSuccesses,
        int HttpProbeAttempts,
        double? SpeedMiBPerSecond);

    /// <summary>
    /// Automatically selects the best-performing endpoint from <paramref name="candidates"/>
    /// using a cheap-to-expensive funnel. Returns <c>null</c> if no candidate is reachable,
    /// in which case the caller should keep its current endpoint (sensible fallback).
    /// </summary>
    /// <param name="candidates">Candidate IPs (typically loaded from an IP file).</param>
    /// <param name="testUri">Download URI used for the HTTP latency and speed-test stages.</param>
    /// <param name="userAgent">User-Agent header for the HTTP probes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<IpItem?> SelectBestEndpointAsync(
        List<IpItem> candidates,
        Uri testUri,
        string userAgent,
        CancellationToken cancellationToken)
    {
        if (candidates is null || candidates.Count == 0)
            return null;

        // Stage 1: ICMP reachability + latency (cheap, concurrent, hard-capped).
        var reachable = await PingFilterAsync(candidates, cancellationToken);
        if (reachable.Count == 0)
            return null;

        // Stage 2: small HTTP latency probes (cheap, no large download).
        var probed = await HttpLatencyFilterAsync(reachable, testUri, userAgent, cancellationToken);
        if (probed.Count == 0)
        {
            // Every HTTP probe failed, but ICMP succeeded. Fall back to the
            // lowest-latency reachable candidate rather than declaring failure.
            return reachable.OrderBy(c => c.RoundtripTime ?? long.MaxValue).FirstOrDefault();
        }

        // Stage 3: pick the most promising candidates for the expensive stage.
        var finalists = probed
            .OrderBy(r => r.HttpLatencyMilliseconds ?? long.MaxValue)
            .ThenBy(r => r.Item.RoundtripTime ?? long.MaxValue)
            .Take(MaxSpeedTestCandidates)
            .ToList();

        // Stage 4: small ranged speed test on the finalists.
        var ranked = await SpeedTestRankAsync(finalists, testUri, userAgent, cancellationToken);

        // Stage 5: rank by speed, tie-break by HTTP latency then ICMP latency.
        var best = ranked
            .Where(r => r.SpeedMiBPerSecond.HasValue && r.SpeedMiBPerSecond > 0)
            .OrderByDescending(r => r.SpeedMiBPerSecond)
            .ThenBy(r => r.HttpLatencyMilliseconds ?? long.MaxValue)
            .ThenBy(r => r.Item.RoundtripTime ?? long.MaxValue)
            .FirstOrDefault();

        if (best != null)
            return best.Item;

        // Speed test produced no usable throughput; fall back to best latency.
        return probed
            .OrderBy(r => r.HttpLatencyMilliseconds ?? long.MaxValue)
            .Select(r => r.Item)
            .FirstOrDefault();
    }

    /// <summary>
    /// Stage 1: concurrent ICMP ping with a hard timeout. Keeps reachable
    /// candidates (up to <see cref="MaxReachableCandidates"/>) and records
    /// RTT/TTL on each <see cref="IpItem"/> via <see cref="SpeedTestService.PingAsync"/>.
    /// </summary>
    private static async Task<List<IpItem>> PingFilterAsync(
        List<IpItem> candidates,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(PingStageTimeout);

        var bag = new ConcurrentBag<IpItem>();
        var successCount = 0;

        var startSignal = new TaskCompletionSource<bool>();

        var tasks = candidates.Select(async item =>
        {
            await startSignal.Task;
            if (cts.IsCancellationRequested) return;

            if (!IPAddress.TryParse(item.Ip, out var ip)) return;

            try
            {
                await SpeedTestService.PingAsync(item, ip, cts.Token);
                if (item.RoundtripTime.HasValue)
                {
                    var current = Interlocked.Increment(ref successCount);
                    if (current <= MaxReachableCandidates)
                        bag.Add(item);
                    if (current == MaxReachableCandidates)
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

        if (bag.Count >= 5)
            return [.. bag];

        // Too few pings succeeded; keep whichever succeeded, plus a few random
        // candidates so the next stage still has something to probe.
        var fallback = bag.ToList();
        if (fallback.Count < 5)
        {
            var extra = candidates
                .Where(c => !fallback.Contains(c))
                .OrderBy(_ => Random.Shared.Next())
                .Take(MaxReachableCandidates - fallback.Count);
            fallback.AddRange(extra);
        }
        return fallback;
    }

    /// <summary>
    /// Stage 2: small HTTP latency probes (tiny Range request, no body download).
    /// Estimates packet-loss across <see cref="HttpLatencyProbes"/> attempts and
    /// drops candidates that never respond.
    /// </summary>
    private static async Task<List<EndpointRank>> HttpLatencyFilterAsync(
        List<IpItem> items,
        Uri testUri,
        string userAgent,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(HttpLatencyTimeout);

        var results = new ConcurrentBag<EndpointRank>();

        var tasks = items.Select(async item =>
        {
            if (!IPAddress.TryParse(item.Ip, out var ip)) return;

            long totalLatency = 0;
            var successes = 0;
            for (var attempt = 0; attempt < HttpLatencyProbes; attempt++)
            {
                if (cts.IsCancellationRequested) break;
                using var probe = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                probe.CancelAfter(HttpLatencyTimeout);

                var (response, latency) = await HttpClientHelper.MeasureHttpLatencyAsync(
                    testUri, ip, HttpLatencyTimeout,
                    rangeFrom: 0, rangeTo: 1023,
                    userAgent: userAgent,
                    token: probe.Token);
                response?.Dispose();
                if (latency >= 0)
                {
                    totalLatency += latency;
                    successes++;
                }
            }

            if (successes > 0)
            {
                results.Add(new EndpointRank(
                    item,
                    item.RoundtripTime,
                    totalLatency / successes,
                    successes,
                    HttpLatencyProbes,
                    null));
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

        return [.. results];
    }

    /// <summary>
    /// Stage 4: small ranged speed test on the finalists. Downloads at most
    /// <see cref="SpeedTestRangeBytes"/> per candidate (not the full 30/50 MB
    /// used by the Speed Test tab) to minimize bandwidth while still producing
    /// a comparable throughput figure.
    /// </summary>
    private static async Task<List<EndpointRank>> SpeedTestRankAsync(
        List<EndpointRank> finalists,
        Uri testUri,
        string userAgent,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(SpeedTestStageTimeout);

        var updated = new ConcurrentBag<EndpointRank>();

        var tasks = finalists.Select(async rank =>
        {
            if (!IPAddress.TryParse(rank.Item.Ip, out var ip)) return;

            using var probe = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            probe.CancelAfter(SpeedTestStageTimeout);

            var (response, _) = await HttpClientHelper.MeasureHttpLatencyAsync(
                testUri, ip, SpeedTestStageTimeout,
                rangeFrom: 0, rangeTo: SpeedTestRangeBytes - 1,
                userAgent: userAgent,
                token: probe.Token);

            if (response is null)
                return;

            try
            {
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(probe.Token);

                var buffer = new byte[64 * 1024];
                long totalBytes = 0;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                while (true)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), probe.Token);
                    if (read == 0) break;
                    totalBytes += read;
                }

                sw.Stop();
                var mebibytes = totalBytes / 1048576.0;
                var seconds = sw.Elapsed.TotalSeconds;
                var speed = seconds > 0.1 ? mebibytes / seconds : 0;

                updated.Add(rank with { SpeedMiBPerSecond = speed });
            }
            catch
            {
                // ignored
            }
            finally
            {
                response.Dispose();
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

        return [.. updated];
    }
}
