using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DnsClient;

namespace XboxDownload.Helpers.Network;

public static class DnsHelper
{
    public static DoHServer? CurrentDoH { get; set; }
    public const string DohProxyHost = "dh1.skydevil.xyz", DohProxyIp = "104.21.45.47"; //172.67.209.179

    public static string FormatIpForUrl(IPAddress ip) => ip.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{ip}]" : ip.ToString();

    public static DoHServer GetConfigureDoH(string url, string ip, bool useProxy)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/dns-json" }
        };

        var uri = new Uri(url);
        var finalUrl = url;

        if (useProxy)
        {
            var proxyIp = DohProxyIp;

            if (!string.IsNullOrWhiteSpace(App.Settings.DohServerProxyIp) && IPAddress.TryParse(App.Settings.DohServerProxyIp, out var ipAddress))
            {
                proxyIp = FormatIpForUrl(ipAddress);
            }

            finalUrl = $"https://{proxyIp}/{uri.Host}{uri.PathAndQuery}";

            headers["Host"] = DohProxyHost;
            headers["X-Organization"] = nameof(XboxDownload);
            headers["X-Author"] = "Devil";
        }
        else if (!string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out var targetIp))
        {
            finalUrl = new UriBuilder(uri) { Host = targetIp.ToString() }.ToString();
            headers["Host"] = uri.Host;
        }

        var doHServer = new DoHServer
        {
            UseProxy = useProxy,
            Url = finalUrl,
            Headers = headers
        };

        return doHServer;
    }

    public class DoHServer
    {
        public bool UseProxy { get; set; }
        public string Url { get; init; } = "";
        public Dictionary<string, string>? Headers { get; init; }
    }

    public static async Task<List<IPAddress>?> ResolveDohAsync(string queryName, DoHServer? doHServer, bool preferIPv6 = false)
    {
        if (doHServer == null) return null;

        var queryType = preferIPv6 ? "AAAA" : "A";
        var expectedType = preferIPv6 ? 28 : 1;
        var requestUrl = $"{doHServer.Url}?name={queryName}&type={queryType}";

        var responseString = await HttpClientHelper.GetStringContentAsync(
            requestUrl,
            headers: doHServer.Headers,
            timeout: 3000
        );

        if (string.IsNullOrWhiteSpace(responseString))
            return null;

        try
        {
            var dnsResponse = JsonSerializer.Deserialize<DnsResponse>(responseString);
            if (dnsResponse?.Status != 0)
                return null;

            var ipList = new List<IPAddress>();
            if (dnsResponse.Answer == null) return ipList;
            foreach (var answer in dnsResponse.Answer)
            {
                if (answer.Type != expectedType || !IPAddress.TryParse(answer.Data, out var ip)) continue;
                ipList.Add(ip);
            }

            return ipList;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<List<IPAddress>?> ResolveDnsAsync(string queryName, string? dnsIp = null, bool preferIPv6 = false)
    {
        LookupClient lookup;
        if (string.IsNullOrWhiteSpace(dnsIp))
        {
            lookup = new LookupClient();
        }
        else
        {
            var options = new LookupClientOptions(IPAddress.Parse(dnsIp))
            {
                Timeout = TimeSpan.FromSeconds(3),
                Retries = 1,
                UseTcpOnly = false,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false
            };
            lookup = new LookupClient(options);
        }

        try
        {
            if (preferIPv6)
            {
                var result = await lookup.QueryAsync(queryName, QueryType.AAAA);
                var records = result.Answers.AaaaRecords().Select(r => r.Address);
                return records.ToList();
            }
            else
            {
                var result = await lookup.QueryAsync(queryName, QueryType.A);
                var records = result.Answers.ARecords().Select(r => r.Address);
                return records.ToList();
            }
        }
        catch
        {
            return null;
        }
    }

    public class DnsResponse
    {
        public int Status { get; init; }

        public List<DnsAnswer>? Answer { get; init; }
    }

    public class DnsAnswer
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("TTL")]
        public uint Ttl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }
}