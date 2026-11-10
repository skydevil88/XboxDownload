using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using XboxDownload.Models.Dns;

namespace XboxDownload.Services;

internal static class XboxSrvDiscovery
{
    internal const string Target = "assets1.xboxlive.com";

    // Limit interception to the observed Xbox discovery chain, never shared CDN hosts.
    internal static string? GetForwardReason(Query query, bool httpEnabled, bool redirectEnabled,
        IPAddress localIp, IEnumerable<ResourceRecord> targetRecords)
    {
        if (query.QueryClass != 1 || query.QueryType != QueryType.SRV)
            return "Service.Listening.SrvNotInQuery";
        if (query.QueryName?.TrimEnd('.').ToLowerInvariant() is not
            ("assets-srvs.xboxlive.com" or "assets-srvs-net.trafficmanager.net" or
             "assets-srvs-na.delivery.microsoft.com"))
            return "Service.Listening.SrvOtherName";
        if (!httpEnabled) return "Service.Listening.SrvHttpDisabled";
        if (!redirectEnabled) return "Service.Listening.SrvRedirectDisabled";
        if (!targetRecords.Any(r => r.QueryType == QueryType.A && r.Data != null &&
                                    r.Data.SequenceEqual(localIp.GetAddressBytes())))
            return "Service.Listening.SrvTargetNotLocal";
        return null;
    }

    internal static ResourceRecord CreateRecord()
    {
        // SRV RDATA: priority=0, weight=0, port=80, uncompressed DNS target.
        var data = new List<byte> { 0, 0, 0, 0, 0, 80 };
        foreach (var label in Target.Split('.'))
        {
            data.Add((byte)label.Length);
            data.AddRange(Encoding.ASCII.GetBytes(label));
        }
        data.Add(0);
        return new ResourceRecord { QueryType = QueryType.SRV, QueryClass = 1, Ttl = 100, Data = [.. data] };
    }

    internal static string FormatRecord(ResourceRecord record) => record.QueryType switch
    {
        QueryType.A or QueryType.AAAA => new IPAddress(record.Data ?? []).ToString(),
        QueryType.SRV => $"0 0 80 {Target}",
        _ => record.QueryType.ToString()
    };
}
