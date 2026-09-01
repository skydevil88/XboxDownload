using System;
using System.Net;

namespace XboxDownload.Models.SpeedTest;

public sealed record NetworkDiagnosticsResult(
    bool Success,
    string Endpoint,
    string Host,
    bool DnsResolved,
    IPAddress? SelectedIp,
    int AttemptCount,
    int SuccessfulAttempts,
    long? AverageLatencyMilliseconds,
    TimeSpan Duration,
    string FailureReason);
