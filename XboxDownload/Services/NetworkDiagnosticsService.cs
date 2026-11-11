using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.Network;
using XboxDownload.Models.SpeedTest;

namespace XboxDownload.Services;

public static class NetworkDiagnosticsService
{
    private const int AttemptCount = 3;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    public static async Task<NetworkDiagnosticsResult> RunAsync(
        string endpoint,
        string? selectedIp,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Failure(endpoint, string.Empty, false, null, stopwatch.Elapsed, "Invalid download endpoint.");
        }

        IPAddress[] resolvedAddresses;
        try
        {
            resolvedAddresses = (await Dns.GetHostAddressesAsync(uri.Host, cancellationToken))
                .Distinct()
                .ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(endpoint, uri.Host, false, null, stopwatch.Elapsed, ex.Message);
        }

        var ip = IPAddress.TryParse(selectedIp, out var selectedAddress)
            ? selectedAddress
            : resolvedAddresses.FirstOrDefault();
        if (ip == null)
            return Failure(endpoint, uri.Host, false, null, stopwatch.Elapsed, "No address was resolved.");

        var latencies = new List<long>(AttemptCount);
        for (var attempt = 0; attempt < AttemptCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(AttemptTimeout);

            var (response, latency) = await HttpClientHelper.MeasureHttpLatencyAsync(
                uri,
                ip,
                AttemptTimeout,
                token: timeout.Token);
            response?.Dispose();
            if (latency >= 0)
                latencies.Add(latency);
        }

        stopwatch.Stop();
        var success = latencies.Count > 0;
        return new NetworkDiagnosticsResult(
            success,
            endpoint,
            uri.Host,
            true,
            ip,
            AttemptCount,
            latencies.Count,
            success ? (long)latencies.Average() : null,
            stopwatch.Elapsed,
            success ? string.Empty : "The selected address was unreachable.");
    }

    private static NetworkDiagnosticsResult Failure(
        string endpoint,
        string host,
        bool dnsResolved,
        IPAddress? ip,
        TimeSpan duration,
        string reason) => new(
            false,
            endpoint,
            host,
            dnsResolved,
            ip,
            0,
            0,
            null,
            duration,
            reason);
}
