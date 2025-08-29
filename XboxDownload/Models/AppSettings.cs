using System;
using System.Collections.Generic;

namespace XboxDownload.Models;

public class AppSettings
{
    public string Culture { get; set; } = string.Empty;
    public string Theme { get; set; } = "Default";
    public DateTime NextUpdate { get; set; }
    public string DnsIp { get; set; } = string.Empty;
    public string XboxGlobalIp { get; set; } = string.Empty;
    public string XboxCn1Ip { get; set; } = string.Empty;
    public string XboxCn2Ip { get; set; } = string.Empty;
    public string XboxAppIp { get; set; } = string.Empty;
    public string PsIp { get; set; } = string.Empty;
    public string NsIp { get; set; } = string.Empty;
    public string EaIp { get; set; } = string.Empty;
    public string BattleIp { get; set; } = string.Empty;
    public string EpicIp { get; set; } = string.Empty;
    public string UbisoftIp { get; set; } = string.Empty;
    public bool IsXboxGameDownloadLinksShown { get; set; } = true;
    public bool IsLocalUploadEnabled { get; set; }
    public string LocalUploadPath { get; set; } = string.Empty;
    public string ListeningIp { get; set; } = string.Empty;
    public bool IsDnsServiceEnabled { get; set; } = true;
    public bool IsHttpServiceEnabled { get; set; } = true;
    public bool IsSetLocalDnsEnabled { get; set; } = true;
    public bool IsSystemSleepPrevented { get; set; } = true;
    public bool IsDoHEnabled { get; set; }
    public bool IsIPv6DomainFilterEnabled { get; set; }
    public bool IsLocalProxyEnabled { get; set; }
    public string DohServerId { get; set; } = string.Empty;
    public string DohServerProxyIp { get; set; } = string.Empty;
    public List<string> DohServerUseProxyId { get; set; } = [];
    public string LocalIp { get; set; } = string.Empty;
    public bool IsLogging { get; set; } = true;
    public List<string> SniProxyId { get; set; } = ["Google", "Cloudflare", "DNS.SB", "DNS.SB HongKong", "DNS.SB Osaka", "DNS.SB Tokyo", "DNS.SB Seoul", "DNS.SB Singapore"];
    public string SearchLocation { get; set; } = string.Empty;
    public bool UploadAkamaiIpsEnabled { get; set; } = true;
    public string AkamaiCdnIp { get; set; } = string.Empty;
    public string StoreRegion { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string Authorization { get; set; } = string.Empty;
}