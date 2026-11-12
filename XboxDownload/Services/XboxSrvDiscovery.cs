using System.Collections.Generic;
using System.Net;
using System.Text;
using XboxDownload.Models.Dns;

namespace XboxDownload.Services;

internal static class XboxSrvDiscovery
{
    private const string Target = "assets1.xboxlive.com";
    private static readonly byte[] SrvData = BuildSrvData();

    private static byte[] BuildSrvData()
    {
        // SRV RDATA: priority=0, weight=0, port=80, uncompressed DNS target.
        var data = new List<byte> { 0, 0, 0, 0, 0, 80 };
        foreach (var label in Target.Split('.'))
        {
            data.Add((byte)label.Length);
            data.AddRange(Encoding.ASCII.GetBytes(label));
        }
        data.Add(0);
        return [.. data];
    }

    internal static ResourceRecord CreateRecord() => new()
    {
        QueryType = QueryType.SRV,
        QueryClass = 1,
        Ttl = 100,
        Data = SrvData
    };

    internal static string FormatRecord(ResourceRecord record) => record.QueryType switch
    {
        QueryType.A or QueryType.AAAA => new IPAddress(record.Data ?? []).ToString(),
        QueryType.SRV => $"0 0 80 {Target}",
        _ => record.QueryType.ToString()
    };
}
