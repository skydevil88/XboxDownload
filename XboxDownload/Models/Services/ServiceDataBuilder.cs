using System.Collections.ObjectModel;
using XboxDownload.Helpers.Resources;

namespace XboxDownload.Models.Services;

public static class ServiceDataBuilder
{
    public static ObservableCollection<ListeningIpOption> GetListeningIpOptions() =>
    [
        new ("LocalIp", ResourceHelper.GetString("Service.Service.LocalIp")),
        new ("AnyIp", ResourceHelper.GetString("Service.Service.AnyIp"))
    ];

    public static ObservableCollection<DohServerOption> GetDohServerList() =>
    [
        new("AlibabaCloud", ResourceHelper.GetString("Service.Service.SecureDns.AlibabaCloud"), "https://dns.alidns.com/resolve", "223.5.5.5", true, true),
        new("TencentCloud", ResourceHelper.GetString("Service.Service.SecureDns.TencentCloud"), "https://doh.pub/resolve", "1.12.12.12", true, true),
        new("Qihoo360", ResourceHelper.GetString("Service.Service.SecureDns.Qihoo360"), "https://doh.360.cn/resolve", "101.198.198.198", true, true),
        new("Google", "Google", "https://dns.google/resolve", "8.8.8.8", true),
        new("Cloudflare", "Cloudflare", "https://cloudflare-dns.com/dns-query", "1.1.1.1", true),
        new("DNS.SB", "DNS.SB (Global)", "https://doh.sb/dns-query", "185.222.222.222", true),
        
        // Asia
        new("DNS.SB HongKong", "DNS.SB (Hong Kong, China)", "https://hk-hkg.doh.sb/dns-query", "45.125.0.26"),
        new("DNS.SB Osaka", "DNS.SB (Osaka, Japan)", "https://jp-kix.doh.sb/dns-query", "202.5.222.2"),
        new("DNS.SB Tokyo", "DNS.SB (Tokyo, Japan)", "https://jp-nrt.doh.sb/dns-query", "103.121.210.210"),
        new("DNS.SB Seoul", "DNS.SB (Seoul, South Korea)", "https://kr-sel.doh.sb/dns-query", "3.34.32.82"),
        new("DNS.SB Singapore", "DNS.SB (Singapore)", "https://sg-sin.doh.sb/dns-query", "92.118.188.53"),
        new("DNS.SB Bengaluru", "DNS.SB (Bengaluru, India)", "https://in-blr.doh.sb/dns-query", "143.244.128.32"),
        
        // Oceania
        new("DNS.SB Sydney", "DNS.SB (Sydney, Australia)", "https://au-syd.doh.sb/dns-query", "146.19.0.66"),

        // Europe
        new("DNS.SB Düsseldorf", "DNS.SB (Düsseldorf, Germany)", "https://de-dus.doh.sb/dns-query", "62.133.35.15"),
        new("DNS.SB Frankfurt", "DNS.SB (Frankfurt, Germany)", "https://de-fra.doh.sb/dns-query", "147.78.178.170"),
        new("DNS.SB Berlin", "DNS.SB (Berlin, Germany)", "https://de-ber.doh.sb/dns-query", "45.142.247.194"),
        new("DNS.SB Amsterdam", "DNS.SB (Amsterdam, Netherlands)", "https://nl-ams.doh.sb/dns-query", "78.142.193.54"),
        new("DNS.SB London", "DNS.SB (London, United Kingdom)", "https://uk-lon.doh.sb/dns-query", "185.64.79.5"),
        new("DNS.SB Tallinn", "DNS.SB (Tallinn, Estonia)", "https://ee-tll.doh.sb/dns-query", "185.37.252.150"),
        new("DNS.SB Moscow", "DNS.SB (Moscow, Russia)", "https://ru-mow.doh.sb/dns-query", "23.105.231.66"),
        
        // Americas
        new("DNS.SB Chicago", "DNS.SB (Chicago, United States)", "https://us-chi.doh.sb/dns-query", "104.128.62.173"),
        new("DNS.SB NewYork", "DNS.SB (New York, United States)", "https://us-nyc.doh.sb/dns-query", "23.159.160.55"),
        new("DNS.SB SanJose", "DNS.SB (San Jose, United States)", "https://us-sjc.doh.sb/dns-query", "142.147.91.99"),
        new("DNS.SB Toronto", "DNS.SB (Toronto, Canada)", "https://ca-yyz.doh.sb/dns-query", "216.128.181.13")
    ];
}