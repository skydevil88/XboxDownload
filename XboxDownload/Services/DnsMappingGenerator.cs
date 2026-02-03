using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XboxDownload.Services;

public static class DnsMappingGenerator
{
    /// <summary>
    /// Host rules for DNS mapping.
    /// </summary>
    public static readonly Dictionary<string, (string Description, string[] Hosts, string[] Redirects, string[] Blacklist)> HostRules = new()
    {
        ["XboxGlobal"] = (
            "Xbox Global",
            [
                "assets1.xboxlive.com", "assets2.xboxlive.com",
                "xvcf1.xboxlive.com", "xvcf2.xboxlive.com",
                "d1.xboxlive.com", "d2.xboxlive.com",
                "dlassets.xboxlive.com", "dlassets2.xboxlive.com"
            ],
            [],
            []
        ),
        ["XboxCn1"] = (
            "Xbox CN1",
            [
                "assets1.xboxlive.cn", "assets2.xboxlive.cn",
                "d1.xboxlive.cn", "d2.xboxlive.cn"
            ],
            [],
            []
        ),
        ["XboxCn2"] = (
            "Xbox CN2",
            [
                "dlassets.xboxlive.cn", "dlassets2.xboxlive.cn"
            ],
            [],
            []
        ),
        ["XboxApp"] = (
            "MS Store App",
            [
                "dl.delivery.mp.microsoft.com", "tlu.dl.delivery.mp.microsoft.com", "1d.tlu.dl.delivery.mp.microsoft.com"
            ],
            [],
            []
        ),
        ["Ps"] = (
            "Sony PlayStation",
            [
                "gst.prod.dl.playstation.net",
                "gs2.ww.prod.dl.playstation.net",
                "zeus.dl.playstation.net",
                "ares.dl.playstation.net"
            ],
            [],
            []
        ),
        ["Ns"] = (
            "Nintendo Switch",
            [
                "atum.hac.lp1.d4c.nintendo.net",
                "nemof.p01.lp1.nemo.srv.nintendo.net",
                "nemof.hac.lp1.nemo.srv.nintendo.net",
                "ctest-dl.p01.lp1.ctest.srv.nintendo.net",
                "ctest-ul.p01.lp1.ctest.srv.nintendo.net",
                "ctest-ul-lp1.cdn.nintendo.net",
                "ctest-dl-lp1.cdn.nintendo.net"
            ],
            [],
            [
                "atum-eda.hac.lp1.d4c.nintendo.net",
                "atum-4ff.hac.lp1.d4c.nintendo.net"
            ]
        ),
        ["Ea"] = (
            "Electronic Arts",
            [
                "origin-a.akamaihd.net"
            ],
            [],
            [
                "prod.cloudflare.cdn.ea.com",
                "ssl-lvlt.cdn.ea.com"
            ]
        ),
        ["Battle"] = (
            "Battle.net",
            [
                "blzddist1-a.akamaihd.net",
                "downloader.battle.net"

            ],
            [
                "us.cdn.blizzard.com",
                "eu.cdn.blizzard.com",
                "kr.cdn.blizzard.com",
                "level3.blizzard.com",
                "blizzard.gcdn.cloudn.co.kr"
            ],
            [
                "level3.ssl.blizzard.com"
            ]
        ),
        ["Epic"] = (
            "Epic Games",
            [
                "epicgames-download1.akamaized.net"
            ],
            [
                "download.epicgames.com",
                "fastly-download.epicgames.com",
                //"cloudflare.epicgamescdn.com"
            ],
            []
        ),
        ["EpicCn"] = (
            "Epic Games (Tencent Cloud)",
            [
                "epicgames-download1-1251447533.file.myqcloud.com"
            ],
            [],
            []
        ),
        ["Ubisoft"] = (
            "Ubisoft Entertainment",
            [
                "uplaypc-s-ubisoft.cdn.ubi.com",
            ],
            [],
            [
                //"ubisoftconnect.cdn.ubi.com",
            ]
        ),
        ["UbisoftCn"] = (
            "Ubisoft Entertainment CN",
            [
                "uplaypc-s-ubisoft.cdn.ubionline.com.cn"
            ],
            [],
            []
        ),
    };

    /// <summary>
    /// Builds a regex pattern string to match DNS mapping lines for specified host groups.
    /// Matches lines like "IP domain" with optional hostname types.
    /// </summary>
    public static string GenerateHostRegexPattern(string? key, bool includeHosts = true, bool includeRedirects = true, bool includeBlacklist = true)
    {
        var patterns = new List<string>();
        var keys = string.IsNullOrWhiteSpace(key) ? HostRules.Keys.ToArray() : ResolveKeys(key);

        foreach (var hostKey in keys)
        {
            if (!HostRules.TryGetValue(hostKey, out var value)) continue;

            if (includeHosts)
                patterns.AddRange(value.Hosts.Select(h => $@"[^\s]+\s+{Regex.Escape(h)}(\s+.*)?\r?\n"));

            if (includeRedirects)
                patterns.AddRange(value.Redirects.Select(h => $@"[^\s]+\s+{Regex.Escape(h)}(\s+.*)?\r?\n"));

            if (includeBlacklist)
                patterns.AddRange(value.Blacklist.Select(h => $@"[^\s]+\s+{Regex.Escape(h)}(\s+.*)?\r?\n"));
        }

        return string.Join("|", patterns);
    }

    /// <summary>
    /// Returns hostnames for the given key(s).
    /// </summary>
    public static List<string> GenerateHostList(string? key, bool includeHosts = true, bool includeRedirects = false, bool includeBlacklist = false)
    {
        var result = new List<string>();
        var keys = string.IsNullOrWhiteSpace(key) ? HostRules.Keys.ToArray() : ResolveKeys(key);

        foreach (var hostKey in keys)
        {
            if (!HostRules.TryGetValue(hostKey, out var value)) continue;

            if (includeHosts)
                result.AddRange(value.Hosts);

            if (includeRedirects)
                result.AddRange(value.Redirects);

            if (includeBlacklist)
                result.AddRange(value.Blacklist);
        }

        return result;
    }

    /// <summary>
    /// Generates DNS mapping output.
    /// </summary>
    public static string GenerateDnsMapping(string key, string ip, string exportFormat, string outputMode)
    {
        var sb = new StringBuilder();
        var keys = ResolveKeys(key);
        var isExport = outputMode == "Export";
        var isDnsmasq = exportFormat == "dnsmasq";

        for (var i = 0; i < keys.Length; i++)
        {
            var hostKey = keys[i];

            if (HostRules.TryGetValue(hostKey, out var value))
            {
                if (isExport)
                    sb.AppendLine($"# {value.Description}");

                foreach (var host in value.Hosts)
                    AppendLine(host, false);

                foreach (var host in value.Blacklist)
                    AppendLine(host, true);
            }
            else
            {
                sb.AppendLine($"# Unknown host key: {hostKey}");
            }

            if (isExport && i < keys.Length - 1)
                sb.AppendLine();
        }

        return sb.ToString();

        void AppendLine(string host, bool blacked)
        {
            var ipToUse = blacked ? "0.0.0.0" : ip;
            var comment = isExport ? "" : $" # {nameof(XboxDownload)}";

            if (isDnsmasq)
                sb.AppendLine($"address=/{host}/{ipToUse}{comment}");
            else
                sb.AppendLine($"{ipToUse} {host}{comment}");
        }
    }

    /// <summary>
    /// Resolves aliases into host keys.
    /// </summary>
    private static string[] ResolveKeys(string key)
    {
        return key.StartsWith("Akamai")
            ? ["XboxGlobal", "XboxApp", "Ps", "Ns", "Ea", "Battle", "Epic", "Ubisoft"]
            : [key];
    }


    // Mapping of all "Hosts" entries: hostname => rule key
    public static readonly Dictionary<string, string> HostsMap = CreateHostToRuleMap(HostRuleType.Hosts);

    // Mapping of all "Redirects" entries: hostname => rule key
    public static readonly Dictionary<string, string> RedirectsMap = CreateHostToRuleMap(HostRuleType.Redirects);

    // Mapping of all "Blacklist" entries: hostname => rule key
    public static readonly Dictionary<string, string> BlacklistMap = CreateHostToRuleMap(HostRuleType.Blacklist);

    // Mapping of all hostnames across all types (Hosts + Redirects + Blacklist): hostname => rule key
    public static readonly Dictionary<string, string> AllHostMap = CreateHostToRuleMap(HostRuleType.All);

    /// <summary>
    /// Builds a hostname-to-rule mapping dictionary from the HostRules, filtered by rule type.
    /// </summary>
    /// <param name="type">The rule type to include in the mapping (Hosts, Redirects, Blacklist, or All).</param>
    /// <returns>A dictionary mapping hostname to rule name (key in HostRules).</returns>
    private static Dictionary<string, string> CreateHostToRuleMap(HostRuleType type)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (ruleName, rule) in HostRules)
        {
            // Include host entries
            if (type is HostRuleType.Hosts or HostRuleType.All)
            {
                foreach (var host in rule.Hosts)
                    map[host] = ruleName;
            }

            // Include redirect entries
            if (type is HostRuleType.Redirects or HostRuleType.All)
            {
                foreach (var redirect in rule.Redirects)
                    map[redirect] = ruleName;
            }

            // Include blacklist entries
            if (type is HostRuleType.Blacklist or HostRuleType.All)
            {
                foreach (var black in rule.Blacklist)
                    map[black] = ruleName;
            }
        }

        return map;
    }

    /// <summary>
    /// Represents the type of host rule for mapping purposes.
    /// </summary>
    private enum HostRuleType
    {
        Hosts,      // Direct host mappings (used for normal DNS resolution)
        Redirects,  // Alternate redirect domains
        Blacklist,  // Domains that should be blocked (e.g., mapped to 0.0.0.0)
        All         // All of the above combined
    }

}
