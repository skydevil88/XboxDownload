using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Dns;
using XboxDownload.Models.Host;
using XboxDownload.ViewModels;

namespace XboxDownload.Services;

public class DnsConnectionListener(ServiceViewModel serviceViewModel)
{
    private static Socket? _socket;
    private const int Port = 53;
    private static readonly ConcurrentDictionary<string, DnsServer> NetworkInterfaceDnsMap = new();
    private const string TcpIpV4Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\";
    private const string TcpIpV6Path = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\";

    private static List<ResourceRecord>? _localIpRecords;
    private static readonly List<ResourceRecord> EmptyIpRecords = [];

    private static readonly ConcurrentDictionary<string, DnsHelper.DoHServer> ForceEncryptedDomainMap = new();
    public static readonly ConcurrentDictionary<string, List<ResourceRecord>> Ipv4ServiceMap = new(), Ipv6ServiceMap = new(), Ipv4ServiceMapBackup = new();
    private static readonly ConcurrentDictionary<string, List<ResourceRecord>> Ipv4HostMap = new(), Ipv6ServiceMapBackup = new(), Ipv6HostMap = new();
    private static readonly List<KeyValuePair<string, List<ResourceRecord>>> Ipv4WildcardHostMap = [];
    private static readonly List<KeyValuePair<string, List<ResourceRecord>>> Ipv6WildcardHostMap = [];

    private record DnsServer
    {
        // ReSharper disable once InconsistentNaming
        public string IPv4 { get; init; } = "";

        // ReSharper disable once InconsistentNaming
        public string IPv6 { get; init; } = "";
    }

    public async Task LoadForceEncryptedDomainMapAsync()
    {
        ForceEncryptedDomainMap.Clear();

        var jsonPath = serviceViewModel.ForceEncryptionDomainFilePath;

        if (!File.Exists(jsonPath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                return;

            foreach (var element in root.EnumerateArray())
            {
                var isEnabled = element.GetProperty("IsEnabled").GetBoolean();
                if (!isEnabled)
                    continue;

                var domain = element.GetProperty("Domain").GetString();
                var dohServerId = element.GetProperty("DohServerId").GetString();

                if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(dohServerId))
                    continue;

                var dohServer = serviceViewModel.DohServersMappings.FirstOrDefault(d => d.Id == dohServerId);
                if (dohServer == null) continue;

                var useProxy = App.Settings.DohServerUseProxyId.Contains(dohServerId) && !dohServer.IsProxyDisabled;
                var doHServer = DnsHelper.GetConfigureDoH(dohServer.Url, dohServer.Ip, useProxy);

                ForceEncryptedDomainMap.TryAdd(domain, doHServer);
            }
        }
        catch
        {
            // ignored
        }
    }

    public async Task LoadHostAndAkamaiMapAsync(string? akamaiIp = null)
    {
        Ipv4HostMap.Clear();
        Ipv6HostMap.Clear();
        Ipv4WildcardHostMap.Clear();
        Ipv6WildcardHostMap.Clear();

        var wildcardV4Temp = new ConcurrentDictionary<string, List<ResourceRecord>>();
        var wildcardV6Temp = new ConcurrentDictionary<string, List<ResourceRecord>>();

        var jsonPath = serviceViewModel.HostFilePath;
        if (File.Exists(jsonPath))
        {
            List<HostMappingEntry>? entries = null;
            try
            {
                var json = await File.ReadAllTextAsync(jsonPath);
                entries = JsonSerializer.Deserialize<List<HostMappingEntry>>(json);
            }
            catch
            {
                // ignored
            }

            if (entries?.Count > 0)
            {
                foreach (var entry in entries)
                {
                    if (!entry.IsEnabled || string.IsNullOrWhiteSpace(entry.HostName) || string.IsNullOrWhiteSpace(entry.Ip))
                        continue;

                    var hostNameRaw = entry.HostName.Trim().ToLowerInvariant();
                    var isWildcard = hostNameRaw.StartsWith('*');
                    var isDotAfterStar = isWildcard && hostNameRaw.Length > 1 && hostNameRaw[1] == '.';
                    var hostNameNoStar = isWildcard ? hostNameRaw.TrimStart('*') : hostNameRaw;

                    var ipAddresses = entry.Ip
                        .Split([",", "，"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(ip => IPAddress.TryParse(ip, out var addr) ? addr : null)
                        .Where(addr => addr is not null)
                        .Cast<IPAddress>()
                        .ToList();

                    if (ipAddresses.Count == 0) continue;

                    var v4Records = new List<ResourceRecord>();
                    var v6Records = new List<ResourceRecord>();

                    foreach (var ip in ipAddresses)
                    {
                        var record = new ResourceRecord
                        {
                            Data = ip.GetAddressBytes(),
                            TTL = 100,
                            QueryClass = 1,
                            QueryType = ip.AddressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA
                        };

                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            v4Records.Add(record);
                        else
                            v6Records.Add(record);
                    }

                    if (isWildcard)
                    {
                        if (isDotAfterStar)
                        {
                            AddOrUpdateMap(wildcardV4Temp, hostNameNoStar, v4Records);
                            AddOrUpdateMap(wildcardV6Temp, hostNameNoStar, v6Records);
                        }
                        else
                        {
                            // *example.com → host["example.com"] + wildcard[".example.com"]
                            AddOrUpdateMap(Ipv4HostMap, hostNameNoStar, v4Records);
                            AddOrUpdateMap(Ipv6HostMap, hostNameNoStar, v6Records);

                            AddOrUpdateMap(wildcardV4Temp, "." + hostNameNoStar, v4Records);
                            AddOrUpdateMap(wildcardV6Temp, "." + hostNameNoStar, v6Records);
                        }
                    }
                    else
                    {
                        AddOrUpdateMap(Ipv4HostMap, hostNameRaw, v4Records);
                        AddOrUpdateMap(Ipv6HostMap, hostNameRaw, v6Records);
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(akamaiIp) && !string.IsNullOrEmpty(App.Settings.AkamaiCdnIp))
            akamaiIp = App.Settings.AkamaiCdnIp;

        if (!string.IsNullOrEmpty(akamaiIp))
        {
            var akamaiIpAddresses = akamaiIp
           .Split([",", "，"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Select(ip => IPAddress.TryParse(ip, out var addr) ? addr : null)
           .Where(addr => addr is not null)
           .Cast<IPAddress>()
           .ToList();

            if (akamaiIpAddresses.Count > 0)
            {
                var v4Records = new List<ResourceRecord>();
                var v6Records = new List<ResourceRecord>();

                foreach (var ip in akamaiIpAddresses)
                {
                    var record = new ResourceRecord
                    {
                        Data = ip.GetAddressBytes(),
                        TTL = 100,
                        QueryClass = 1,
                        QueryType = ip.AddressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA
                    };

                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        v4Records.Add(record);
                    else
                        v6Records.Add(record);
                }

                if (v4Records.Count > 0 || v6Records.Count > 0)
                {
                    var cdnViewModel = Ioc.Default.GetRequiredService<CdnViewModel>();
                    var akamakHost = cdnViewModel.AkamaiHost1;
                    if (File.Exists(serviceViewModel.AkamaiFilePath))
                        akamakHost += '\n' + await File.ReadAllTextAsync(serviceViewModel.AkamaiFilePath);

                    var lines = akamakHost.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var hostEntry in lines)
                    {
                        if (hostEntry.StartsWith('#')) continue;
                        var hostNameRaw = new string(hostEntry.TakeWhile(c => c != '#').ToArray()).Trim().ToLower();
                        var isWildcard = hostNameRaw.StartsWith('*');
                        var isDotAfterStar = isWildcard && hostNameRaw.Length > 1 && hostNameRaw[1] == '.';
                        var hostNameNoStar = isWildcard ? hostNameRaw.TrimStart('*') : hostNameRaw;

                        if (!RegexHelper.IsValidDomain().IsMatch(hostNameNoStar.TrimStart('.'))) continue;

                        if (isWildcard)
                        {
                            if (isDotAfterStar)
                            {
                                wildcardV4Temp.TryAdd(hostNameNoStar, v4Records);
                                wildcardV6Temp.TryAdd(hostNameNoStar, v6Records);
                            }
                            else
                            {
                                // *example.com → host["example.com"] + wildcard[".example.com"]
                                Ipv4HostMap.TryAdd(hostNameNoStar, v4Records);
                                Ipv6HostMap.TryAdd(hostNameNoStar, v6Records);

                                wildcardV4Temp.TryAdd("." + hostNameNoStar, v4Records);
                                wildcardV6Temp.TryAdd("." + hostNameNoStar, v6Records);
                            }
                        }
                        else
                        {
                            Ipv4HostMap.TryAdd(hostNameRaw, v4Records);
                            Ipv6HostMap.TryAdd(hostNameRaw, v6Records);
                        }
                    }
                }
            }
        }

        if (App.Settings.IsLocalProxyEnabled)
        {
            foreach (var hostName in TcpConnectionListener.DicSniProxy2.Keys)
            {
                wildcardV4Temp.TryAdd(hostName, _localIpRecords!);
                wildcardV6Temp.TryAdd(hostName, EmptyIpRecords);
            }
        }

        // Sort wildcard maps by key length descending
        Ipv4WildcardHostMap.AddRange(wildcardV4Temp.OrderByDescending(x => x.Key.Length).ThenByDescending(x => x.Key));
        Ipv6WildcardHostMap.AddRange(wildcardV6Temp.OrderByDescending(x => x.Key.Length).ThenByDescending(x => x.Key));
    }

    private static void AddOrUpdateMap(ConcurrentDictionary<string, List<ResourceRecord>> map, string host, List<ResourceRecord> records)
    {
        if (map.TryGetValue(host, out var existing))
            map[host] = existing.Concat(records).DistinctBy(r => BitConverter.ToString(r.Data ?? [])).ToList();
        else
            map[host] = records;
    }

    public void ApplyAkamaiIpOverride(string? akamaiIp = null)
    {
        if (string.IsNullOrEmpty(akamaiIp))
        {
            // Restore mode: restore previous IPv4/IPv6 mappings from backup
            foreach (var hostsMap in DnsMappingGenerator.HostsMap)
            {
                if (serviceViewModel is { IsHttpServiceEnabled: true, IsXboxGameDownloadLinksShown: true } &&
                    (hostsMap.Value is "XboxGlobal" or "XboxCn1" or "XboxCn2") &&
                    !hostsMap.Key.Contains('2'))
                {
                    continue;
                }

                if (Ipv4ServiceMapBackup.TryGetValue(hostsMap.Key, out var v4))
                {
                    Ipv4ServiceMap.AddOrUpdate(hostsMap.Key, v4, (_, _) => v4);
                }
                else
                {
                    Ipv4ServiceMap.TryRemove(hostsMap.Key, out _);
                }

                if (Ipv6ServiceMapBackup.TryGetValue(hostsMap.Key, out var v6))
                {
                    Ipv6ServiceMap.AddOrUpdate(hostsMap.Key, v6, (_, _) => v6);
                }
                else
                {
                    Ipv6ServiceMap.TryRemove(hostsMap.Key, out _);
                }
            }

            // Clear the backups after restoration
            Ipv4ServiceMapBackup.Clear();
            Ipv6ServiceMapBackup.Clear();
        }
        else
        {
            // Override mode: force all Akamai-related hostnames to use the specified IP

            Ipv4ServiceMapBackup.Clear();
            Ipv6ServiceMapBackup.Clear();

            foreach (var hostsMap in DnsMappingGenerator.HostsMap)
            {
                // Skip "1" Xbox domains when game download links are shown
                if (serviceViewModel is { IsHttpServiceEnabled: true, IsXboxGameDownloadLinksShown: true } &&
                    (hostsMap.Value is "XboxGlobal" or "XboxCn1" or "XboxCn2") &&
                    !hostsMap.Key.Contains('2'))
                {
                    continue;
                }

                var ipRecords = new List<ResourceRecord>
                {
                    new() { Data = IPAddress.Parse(akamaiIp).GetAddressBytes(), TTL = 100, QueryClass = 1, QueryType = QueryType.A }
                };

                // Backup current IPv4 mapping if exists
                if (Ipv4ServiceMap.TryGetValue(hostsMap.Key, out var v4))
                {
                    Ipv4ServiceMapBackup.TryAdd(hostsMap.Key, v4);
                }

                // Use local IP record for EpicCn, override with specified IP otherwise
                if (hostsMap.Value == "EpicCn")
                {
                    Ipv4ServiceMap.AddOrUpdate(hostsMap.Key, _localIpRecords!, (_, _) => _localIpRecords!);
                }
                else
                {
                    Ipv4ServiceMap.AddOrUpdate(hostsMap.Key, ipRecords, (_, _) => ipRecords);
                }

                // Backup current IPv6 mapping if exists
                if (Ipv6ServiceMap.TryGetValue(hostsMap.Key, out var v6))
                {
                    Ipv6ServiceMapBackup.TryAdd(hostsMap.Key, v6);
                }

                // Override IPv6 with empty response to disable IPv6 resolution
                Ipv6ServiceMap.AddOrUpdate(hostsMap.Key, EmptyIpRecords, (_, _) => EmptyIpRecords);
            }
        }
    }

    public async Task<string> StartAsync()
    {
        _serviceName = null;
        NetworkInterfaceDnsMap.Clear();
        var isSimplifiedChinese = App.Settings.Culture == "zh-Hans";

        IPEndPoint? iPEndPoint = null;
        if (string.IsNullOrEmpty(serviceViewModel.DnsIp))
        {
            if (serviceViewModel.SelectedAdapter?.Adapter != null)
            {
                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
                {
                    var adapterProperties = NetworkInterface
                        .GetAllNetworkInterfaces()
                        .FirstOrDefault(x => x.Id == serviceViewModel.SelectedAdapter.Adapter.Id)?.GetIPProperties();
                    if (adapterProperties != null)
                    {
                        foreach (var dns in adapterProperties.DnsAddresses)
                        {
                            if (dns.AddressFamily != AddressFamily.InterNetwork) continue;
                            if (dns.ToString() == App.Settings.LocalIp || IPAddress.IsLoopback(dns))
                                continue;
                            iPEndPoint = new IPEndPoint(dns, Port);
                            break;
                        }
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    var dns = await GetDnsServers(serviceViewModel.SelectedAdapter.Adapter.Name);
                    if (dns.AddressFamily == AddressFamily.InterNetwork &&
                        dns.ToString() != App.Settings.LocalIp &&
                        dns.ToString() != "255.255.255.255" &&
                        !IPAddress.IsLoopback(dns))
                    {
                        iPEndPoint = new IPEndPoint(dns, Port);
                    }
                }
            }
            iPEndPoint ??= new IPEndPoint(IPAddress.Parse(isSimplifiedChinese ? "114.114.114.114" : "8.8.8.8"), Port);
            serviceViewModel.DnsIp = iPEndPoint.Address.ToString();
        }
        else
        {
            iPEndPoint = new IPEndPoint(IPAddress.Parse(serviceViewModel.DnsIp), Port);
        }

        var ipe = new IPEndPoint(App.Settings.ListeningIp == "LocalIp" ? IPAddress.Parse(App.Settings.LocalIp) : IPAddress.Any, Port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            _socket.Bind(ipe);
        }
        catch (SocketException ex)
        {
            serviceViewModel.IsListeningFailed = true;
            return string.Format(ResourceHelper.GetString("Service.Listening.DnsStartFailedDialogMessage"), ex.Message);
        }

        var localIp = IPAddress.Parse(App.Settings.LocalIp).GetAddressBytes();
        _localIpRecords = [new ResourceRecord { Data = localIp, TTL = 100, QueryClass = 1, QueryType = QueryType.A }];

        byte[]? xboxGlobalIp = null, xboxCn1Ip = null, xboxCn2Ip = null, xboxAppIp = null, psIp = null, nsIp = null, eaIp = null, battleIp = null, epicIp = null, ubisoftIp = null;
        var ipMap = new List<(string host, Func<string?> get, Action<string> set, Action<byte[]?> setBytes)>
        {
            ("assets2.xboxlive.cn", () => serviceViewModel.XboxCn1Ip, val => serviceViewModel.XboxCn1Ip = val, val => xboxCn1Ip = val),
            ("dlassets2.xboxlive.cn", () => serviceViewModel.XboxCn2Ip, val => serviceViewModel.XboxCn2Ip = val, val => xboxCn2Ip = val),
            ("tlu.dl.delivery.mp.microsoft.com", () => serviceViewModel.XboxAppIp, val => serviceViewModel.XboxAppIp = val, val => xboxAppIp = val),
            ("gst.prod.dl.playstation.net", () => serviceViewModel.PsIp, val => serviceViewModel.PsIp = val, val => psIp = val),
            ("atum.hac.lp1.d4c.nintendo.net", () => serviceViewModel.NsIp, val => serviceViewModel.NsIp = val, val => nsIp = val),
            ("origin-a.akamaihd.net", () => serviceViewModel.EaIp, val => serviceViewModel.EaIp = val, val => eaIp = val),
            ("blzddist1-a.akamaihd.net", () => serviceViewModel.BattleIp, val => serviceViewModel.BattleIp = val, val => battleIp = val),
            (isSimplifiedChinese && serviceViewModel.IsHttpServiceEnabled ? "epicgames-download1-1251447533.file.myqcloud.com" : "epicgames-download1.akamaized.net",
                () => serviceViewModel.EpicIp, val => serviceViewModel.EpicIp = val, val => epicIp = val),
            (isSimplifiedChinese && serviceViewModel.IsHttpServiceEnabled ? "uplaypc-s-ubisoft.cdn.ubionline.com.cn" : "uplaypc-s-ubisoft.cdn.ubi.com",
                () => serviceViewModel.UbisoftIp, val => serviceViewModel.UbisoftIp = val, val => ubisoftIp = val),
        };
        if (isSimplifiedChinese)
        {
            if (!string.IsNullOrEmpty(serviceViewModel.XboxGlobalIp))
            {
                xboxGlobalIp = IPAddress.Parse(serviceViewModel.XboxGlobalIp).GetAddressBytes();
            }
            else
            {
                serviceViewModel.XboxGlobalIp = App.Settings.LocalIp;
                xboxGlobalIp = localIp;
            }
        }
        else
        {
            ipMap.Add(("assets2.xboxlive.com", () => serviceViewModel.XboxGlobalIp, val => serviceViewModel.XboxGlobalIp = val, val => xboxGlobalIp = val));
        }

        var ipResolveTasks = ipMap.Select(async tuple =>
        {
            var (host, get, set, setBytes) = tuple;
            var ip = get();
            var (ipBytes, resolved) = await ResolveIpAsync(ip, host, serviceViewModel.DnsIp, serviceViewModel.IsDoHEnabled);
            setBytes(ipBytes);
            if (string.IsNullOrEmpty(ip) && resolved is not null)
                set(resolved);
        }).ToArray();
        var epicTask = IpGeoHelper.IsMainlandChinaAsync(serviceViewModel.EpicIp);
        var ubisoftTask = IpGeoHelper.IsMainlandChinaAsync(serviceViewModel.UbisoftIp);
        await Task.WhenAll(ipResolveTasks.Concat([epicTask, ubisoftTask]));
        var isEpicMainlandChina = epicTask.Result;
        var isUbisoftMainlandChina = ubisoftTask.Result;

        Ipv4ServiceMap.Clear();
        Ipv6ServiceMap.Clear();

        AddDnsMappings("XboxGlobal", xboxGlobalIp, _localIpRecords, EmptyIpRecords, serviceViewModel.IsXboxGameDownloadLinksShown, isSimplifiedChinese);
        AddDnsMappings("XboxCn1", xboxCn1Ip, _localIpRecords, EmptyIpRecords, serviceViewModel.IsXboxGameDownloadLinksShown);
        AddDnsMappings("XboxCn2", xboxCn2Ip, _localIpRecords, EmptyIpRecords, serviceViewModel.IsXboxGameDownloadLinksShown);
        AddDnsMappings("XboxApp", xboxAppIp, _localIpRecords, EmptyIpRecords);
        AddDnsMappings("Ps", psIp, _localIpRecords, EmptyIpRecords);
        AddDnsMappings("Ns", nsIp, _localIpRecords, EmptyIpRecords);
        AddDnsMappings("Ea", eaIp, _localIpRecords, EmptyIpRecords);
        AddDnsMappings("Battle", battleIp, _localIpRecords, EmptyIpRecords);
        AddDnsMappings("Epic", epicIp, _localIpRecords, EmptyIpRecords, isEpicMainlandChina);
        AddDnsMappings("Ubisoft", ubisoftIp, _localIpRecords, EmptyIpRecords, isUbisoftMainlandChina);

        if (serviceViewModel.IsSetLocalDnsEnabled)
        {
            if (OperatingSystem.IsWindows())
            {
                using var localMachine = Microsoft.Win32.Registry.LocalMachine;
                var adapters = NetworkAdapterHelper.GetValidAdapters();
                foreach (var adapter in adapters)
                {
                    var dns = new DnsServer
                    {
                        IPv4 = GetRegistryDns(localMachine, TcpIpV4Path + adapter.Id, App.Settings.LocalIp),
                        IPv6 = GetRegistryDns(localMachine, TcpIpV6Path + adapter.Id, "::")
                    };
                    NetworkInterfaceDnsMap.TryAdd(adapter.Id, dns);
                }
                await ApplyDns(App.Settings.LocalIp);
            }
            else if (OperatingSystem.IsMacOS())
            {
                var serviceName = await GetNetworkServiceNameAsync(serviceViewModel.SelectedAdapter!.Adapter.Name);
                if (!string.IsNullOrEmpty(serviceName))
                {
                    var ips = new List<IPAddress>();
                    var dnsServers =
                        await CommandHelper.RunCommandWithOutputAsync("networksetup",
                            $"-getdnsservers \"{serviceName}\"");
                    foreach (var line in dnsServers)
                    {
                        if (IPAddress.TryParse(line, out var ip) && ip.ToString() != App.Settings.LocalIp)
                            ips.Add(ip);
                    }

                    if (ips.Count > 0)
                    {
                        var dns = new DnsServer
                        {
                            IPv4 = string.Join(" ", ips)
                        };
                        NetworkInterfaceDnsMap.TryAdd(serviceName, dns);
                    }

                    await CommandHelper.RunCommandAsync("networksetup", $"-setdnsservers \"{serviceName}\" {App.Settings.LocalIp}");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                await CommandHelper.RunCommandAsync("resolvectl", $"dns {serviceViewModel.SelectedAdapter!.Adapter.Name} {App.Settings.LocalIp}");
            }
        }

        if (App.Settings.IsHttpServiceEnabled)
        {
            if (OperatingSystem.IsWindows())
                AddMapping("packagespc.xboxlive.com", _localIpRecords, EmptyIpRecords);
            AddMapping("www.msftconnecttest.com", _localIpRecords, EmptyIpRecords);
        }

        if (App.Settings.IsLocalProxyEnabled)
        {
            foreach (var hostname in TcpConnectionListener.DicSniProxy.Keys)
            {
                AddMapping(hostname, _localIpRecords, EmptyIpRecords);
            }
        }

        await LoadHostAndAkamaiMapAsync();
        await LoadForceEncryptedDomainMapAsync();
        serviceViewModel.IsDnsReady = true;
        _ = Task.Run(() => Listening(iPEndPoint));
        return string.Empty;
    }

    public static async Task StopAsync()
    {
        await ApplyDns();

        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Optional
        }

        // On non-Windows platforms, send a dummy packet to unblock ReceiveFrom
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                using var dummy = new UdpClient();
                dummy.Client.SendTimeout = 100;
                dummy.Client.ReceiveTimeout = 100;

                await dummy.SendAsync(new byte[1], 1, new IPEndPoint(IPAddress.Parse(App.Settings.LocalIp), Port));
            }
            catch
            {
                // Optional
            }
        }
        _socket?.Close();
        _socket?.Dispose();
        _socket = null;
    }

    private static async Task<IPAddress> GetDnsServers(string name)
    {
        if (File.Exists("/usr/bin/nmcli"))
        {
            var result = await CommandHelper.RunCommandWithOutputAsync("nmcli", $"device show {name} ");
            foreach (var parts in from line in result where !string.IsNullOrWhiteSpace(line) && line.Contains(':') select line.Split(':', 2) into parts let key = parts[0].Trim() let value = parts[1].Trim() where key.StartsWith("IP4.DNS") select parts)
            {
                if (IPAddress.TryParse(parts[1].Trim(), out var address))
                    return address;
            }
        }
        if (File.Exists("/usr/bin/resolvectl"))
        {
            var result = await CommandHelper.RunCommandWithOutputAsync("resolvectl", $"status {name}");
            foreach (var parts in from line in result where !string.IsNullOrWhiteSpace(line) && line.Contains(':') select line.Split(':', 2) into parts let key = parts[0].Trim() let value = parts[1].Trim() where key.StartsWith("DNS Servers") select parts)
            {
                if (IPAddress.TryParse(parts[1].Trim(), out var address))
                    return address;
            }
        }
        return IPAddress.None;
    }

    public string DnsV4Query = ResourceHelper.GetString("Service.Listening.DnsV4Query"),
        DnsV6Query = ResourceHelper.GetString("Service.Listening.DnsV6Query"),
        DnsQueryFailed = ResourceHelper.GetString("Service.Listening.DnsQueryFailed");

    private void Listening(IPEndPoint iPEndPoint)
    {
        while (serviceViewModel.IsListening)
        {
            try
            {
                EndPoint client = new IPEndPoint(IPAddress.Any, 0);
                var buff = new byte[512];
                var read = _socket!.ReceiveFrom(buff, ref client);
                _ = Task.Run(async () =>
                {
                    var dns = new DnsMessage(buff, read);
                    if (dns is { QR: 0, Opcode: 0, Queries.Count: 1 })
                    {
                        var queryName = (dns.Queries[0].QueryName ?? string.Empty).ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(queryName)) return;

                        switch (dns.Queries[0].QueryType)
                        {
                            case QueryType.A:
                                {
                                    var wasDnsHandled = HandleDnsQuery(Ipv4ServiceMap, Ipv4HostMap, Ipv4WildcardHostMap, dns, client, queryName, QueryType.A);
                                    if (wasDnsHandled) return;

                                    if (ForceEncryptedDomainMap.TryGetValue(queryName, out var doHServer) || serviceViewModel.IsDoHEnabled)
                                    {
                                        var wasDohHandled = await HandleDohQueryAsync(dns, client, queryName, QueryType.A, doHServer);
                                        if (wasDohHandled) return;
                                    }

                                    if (serviceViewModel.IsLogging)
                                        serviceViewModel.AddLog(DnsV4Query, queryName, ((IPEndPoint)client).Address.ToString());
                                    break;
                                }
                            case QueryType.AAAA:
                                {
                                    var wasHandled = HandleDnsQuery(Ipv6ServiceMap, Ipv6HostMap, Ipv6WildcardHostMap, dns, client, queryName, QueryType.AAAA);
                                    if (wasHandled) return;

                                    if (serviceViewModel.IsIPv6DomainFilterEnabled)
                                    {
                                        dns.QR = 1;
                                        dns.RA = 1;
                                        dns.RD = 1;
                                        dns.ResourceRecords = EmptyIpRecords;
                                        _socket.SendTo(dns.ToBytes(), client);
                                        return;
                                    }

                                    if (ForceEncryptedDomainMap.TryGetValue(queryName, out var doHServer) || serviceViewModel.IsDoHEnabled)
                                    {
                                        var wasDohHandled = await HandleDohQueryAsync(dns, client, queryName, QueryType.AAAA, doHServer);
                                        if (wasDohHandled) return;
                                    }

                                    if (serviceViewModel.IsLogging)
                                        serviceViewModel.AddLog(DnsV6Query, queryName, ((IPEndPoint)client).Address.ToString());
                                    break;
                                }
                        }
                    }

                    try
                    {
                        using var proxy = new UdpClient(iPEndPoint.Address.AddressFamily);
                        proxy.Client.ReceiveTimeout = 3000;
                        await proxy.SendAsync(buff, read, iPEndPoint);
                        var result = proxy.Receive(ref iPEndPoint);
                        _socket.SendTo(result, client);
                    }
                    catch (Exception ex)
                    {
                        if (serviceViewModel.IsLogging)
                            serviceViewModel.AddLog(DnsQueryFailed, ex.Message, ((IPEndPoint)client).Address.ToString());
                    }
                });
            }
            catch
            {
                // ignored
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static string GetRegistryDns(Microsoft.Win32.RegistryKey root, string path, string fallback)
    {
        try
        {
            using var key = root.OpenSubKey(path);
            var value = key?.GetValue("NameServer") as string;
            return string.IsNullOrWhiteSpace(value) || value == fallback ? "" : value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read registry key '{path}': {ex.Message}");
            return "";
        }
    }

    private static async Task ApplyDns(string? dns = null)
    {
        if (OperatingSystem.IsWindows())
        {
            if (NetworkInterfaceDnsMap.IsEmpty) return;
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine;
                foreach (var item in NetworkInterfaceDnsMap)
                {
                    try
                    {
                        var subKeyV4 = key.CreateSubKey(TcpIpV4Path + item.Key);
                        subKeyV4.SetValue("NameServer", string.IsNullOrEmpty(dns) ? item.Value.IPv4 : dns);
                        subKeyV4.Close();

                        var subKeyV6 = key.CreateSubKey(TcpIpV6Path + item.Key);
                        subKeyV6.SetValue("NameServer", string.IsNullOrEmpty(dns) ? item.Value.IPv6 : "::");
                        subKeyV6.Close();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"No permission to write registry key for interface {item.Key}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing registry for interface {item.Key}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to access registry: {ex}");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (!string.IsNullOrEmpty(_serviceName))
            {
                if (NetworkInterfaceDnsMap.TryGetValue(_serviceName, out var doHServer))
                {
                    await CommandHelper.RunCommandAsync("networksetup", $"-setdnsservers \"{_serviceName}\" {doHServer.IPv4}");
                }
                else
                {
                    await CommandHelper.RunCommandAsync("networksetup", $"-setdnsservers \"{_serviceName}\" \"Empty\"");
                }
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            if (!Program.UnixUserIsRoot()) return;

            await CommandHelper.RunCommandAsync("systemctl", "restart systemd-resolved");
        }
    }

    private static string? _serviceName;
    private static async Task<string?> GetNetworkServiceNameAsync(string device)
    {
        if (!string.IsNullOrEmpty(_serviceName)) return _serviceName;

        var result = await CommandHelper.RunCommandWithOutputAsync("networksetup", "-listnetworkserviceorder");
        var currentService = string.Empty;

        foreach (var line in result)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith('('))
            {
                var idx = line.IndexOf(')');
                if (idx > 0 && idx + 1 < line.Length)
                {
                    currentService = line[(idx + 1)..].Trim();
                }
            }

            if (string.IsNullOrEmpty(currentService) || !line.Contains($"Device: {device}")) continue;
            _serviceName = currentService;
            break;
        }

        return _serviceName;
    }

    private static async Task<(byte[]? ipBytes, string? resolved)> ResolveIpAsync(string? cachedIp, string host, string dnsIp, bool isDoHEnabled)
    {
        if (!string.IsNullOrWhiteSpace(cachedIp) && IPAddress.TryParse(cachedIp, out var parsed))
            return (parsed.GetAddressBytes(), cachedIp);

        var ipAddresses = isDoHEnabled
            ? await DnsHelper.ResolveDohAsync(host, DnsHelper.CurrentDoH)
            : await DnsHelper.ResolveDnsAsync(host, dnsIp);

        return ipAddresses?.Count > 0 ? (ipAddresses[0].GetAddressBytes(), ipAddresses[0].ToString()) : (null, null);
    }

    private void AddDnsMappings(string ruleKey, byte[]? ip, List<ResourceRecord> localIpRecords, List<ResourceRecord> emptyIpRecords, bool isXboxGameDownloadLinksShownOrMainlandChina = false, bool isSimplifiedChinese = false)
    {
        if (ip is null) return;
        if (!DnsMappingGenerator.HostRules.TryGetValue(ruleKey, out var rule)) return;

        var addressFamily = new IPAddress(ip).AddressFamily;
        var isV4 = addressFamily == AddressFamily.InterNetwork;

        var ipRecords = new List<ResourceRecord>
        {
            new() { Data = ip, TTL = 100, QueryClass = 1, QueryType = isV4 ? QueryType.A : QueryType.AAAA }
        };

        var ipv4Records = isV4 ? ipRecords : emptyIpRecords;
        var ipv6Records = isV4 ? emptyIpRecords : ipRecords;

        switch (ruleKey)
        {
            case "Epic":
                if (serviceViewModel.IsHttpServiceEnabled && isXboxGameDownloadLinksShownOrMainlandChina)
                {
                    foreach (var hostname in rule.Hosts)
                        AddMapping(hostname, localIpRecords, emptyIpRecords);
                    if (DnsMappingGenerator.HostRules.TryGetValue("EpicCn", out var epicCnRule))
                    {
                        foreach (var hostname in epicCnRule.Hosts)
                            AddMapping(hostname, ipv4Records, ipv6Records);
                    }
                }
                else
                {
                    foreach (var hostname in rule.Hosts)
                        AddMapping(hostname, ipv4Records, ipv6Records);
                    if (serviceViewModel.IsHttpServiceEnabled)
                    {
                        if (DnsMappingGenerator.HostRules.TryGetValue("EpicCn", out var epicCnRule))
                        {
                            foreach (var hostname in epicCnRule.Hosts)
                                AddMapping(hostname, localIpRecords, emptyIpRecords);
                        }
                    }
                }
                break;
            case "Ubisoft":
                foreach (var hostname in rule.Hosts)
                {
                    if (serviceViewModel.IsHttpServiceEnabled && isXboxGameDownloadLinksShownOrMainlandChina)
                        AddMapping(hostname, localIpRecords, emptyIpRecords);
                    else
                        AddMapping(hostname, ipv4Records, ipv6Records);
                }
                if (DnsMappingGenerator.HostRules.TryGetValue("UbisoftCn", out var ubisoftCnRule))
                {
                    foreach (var hostname in ubisoftCnRule.Hosts)
                        AddMapping(hostname, ipv4Records, ipv6Records);
                }
                break;
            default:
                foreach (var hostname in rule.Hosts)
                {
                    if (serviceViewModel.IsHttpServiceEnabled && isXboxGameDownloadLinksShownOrMainlandChina)
                    {
                        if (isSimplifiedChinese)
                        {
                            AddMapping(hostname, localIpRecords, emptyIpRecords);
                        }
                        else
                        {
                            if (hostname.Contains('2'))
                                AddMapping(hostname, ipv4Records, ipv6Records);
                            else
                                AddMapping(hostname, localIpRecords, emptyIpRecords);
                        }
                    }
                    else
                    {
                        AddMapping(hostname, ipv4Records, ipv6Records);
                    }
                }
                break;
        }

        if (serviceViewModel.IsHttpServiceEnabled)
        {
            foreach (var hostname in rule.Redirects)
                AddMapping(hostname, localIpRecords, emptyIpRecords);
        }

        foreach (var hostname in rule.Blacklist)
            AddMapping(hostname, emptyIpRecords, emptyIpRecords);
    }

    private static void AddMapping(string hostname, List<ResourceRecord> ipv4Records, List<ResourceRecord> ipv6Records)
    {
        Ipv4ServiceMap.TryAdd(hostname, ipv4Records);
        Ipv6ServiceMap.TryAdd(hostname, ipv6Records);
    }

    private async Task<bool> HandleDohQueryAsync(DnsMessage dnsMessage, EndPoint client, string queryName, QueryType queryType, DnsHelper.DoHServer? doHServer = null)
    {
        doHServer ??= DnsHelper.CurrentDoH;
        if (doHServer == null) return false;

        var requestUrl = $"{doHServer.Url}?name={queryName}&type={queryType}";
        var responseString = await HttpClientHelper.GetStringContentAsync(
            requestUrl,
            headers: doHServer.Headers,
            timeout: 3000,
            token: serviceViewModel.ListeningToken
        );

        if (string.IsNullOrWhiteSpace(responseString))
            return false;

        try
        {
            var dnsResponse = JsonSerializer.Deserialize<DnsHelper.DnsResponse>(responseString);
            if (dnsResponse?.Status != 0)
                return false;

            var expectedType = queryType == QueryType.AAAA ? 28 : 1;
            dnsMessage.QR = 1;
            dnsMessage.RA = 1;
            dnsMessage.RD = 1;
            dnsMessage.ResourceRecords = [];
            if (dnsResponse.Answer != null)
            {
                foreach (var answer in dnsResponse.Answer)
                {
                    if (answer.Type != expectedType || !IPAddress.TryParse(answer.Data, out var ip)) continue;
                    dnsMessage.ResourceRecords.Add(new ResourceRecord
                    {
                        QueryName = answer.Name,
                        Data = ip.GetAddressBytes(),
                        TTL = answer.Ttl,
                        QueryClass = 1,
                        QueryType = queryType
                    });
                }
            }

            _socket?.SendTo(dnsMessage.ToBytes(), client);

            if (serviceViewModel.IsLogging && dnsMessage.ResourceRecords.Count > 0)
            {
                serviceViewModel.AddLog(queryType == QueryType.AAAA ? DnsV6Query : DnsV4Query,
                    queryName + " -> " + string.Join(", ", dnsResponse.Answer!.Where(x => x.Type == expectedType).Select(x => x.Data)),
                    ((IPEndPoint)client).Address.ToString());
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool HandleDnsQuery(ConcurrentDictionary<string, List<ResourceRecord>> serviceMap, ConcurrentDictionary<string, List<ResourceRecord>> hostMap, List<KeyValuePair<string, List<ResourceRecord>>> wildcardHostMap, DnsMessage dnsMessage, EndPoint client, string queryName, QueryType queryType)
    {
        var queryTypeText = queryType == QueryType.AAAA ? DnsV6Query : DnsV4Query;
        if (serviceMap.TryGetValue(queryName, out var serviceRecords))
        {
            SendDnsResponse(dnsMessage, client, serviceRecords, queryTypeText, queryName);
            return true;
        }

        if (hostMap.TryGetValue(queryName, out var hostRecords))
        {
            if (hostRecords.Count > 1)
                hostRecords = hostRecords.OrderBy(_ => Random.Shared.Next()).Take(16).ToList();

            SendDnsResponse(dnsMessage, client, hostRecords, queryTypeText, queryName);
            return true;
        }

        foreach (var kv in wildcardHostMap)
        {
            if (!queryName.EndsWith(kv.Key, StringComparison.OrdinalIgnoreCase)) continue;

            var wildcardHost = kv.Value;

            if (queryType == QueryType.AAAA)
                Ipv6HostMap.TryAdd(queryName, wildcardHost);
            else
                Ipv4HostMap.TryAdd(queryName, wildcardHost);

            if (wildcardHost.Count > 1)
                wildcardHost = wildcardHost.OrderBy(_ => Random.Shared.Next()).Take(16).ToList();

            SendDnsResponse(dnsMessage, client, wildcardHost, queryTypeText, queryName);
            return true;
        }

        return false;
    }

    private void SendDnsResponse(DnsMessage dnsMessage, EndPoint client, List<ResourceRecord> records, string queryTypeText, string queryName)
    {
        dnsMessage.QR = 1;
        dnsMessage.RA = 1;
        dnsMessage.RD = 1;
        dnsMessage.ResourceRecords = records;
        foreach (var record in dnsMessage.ResourceRecords)
            record.QueryName = queryName;

        var bytes = dnsMessage.ToBytes();
        _socket?.SendTo(bytes, client);

        if (!serviceViewModel.IsLogging || dnsMessage.ResourceRecords.Count == 0) return;
        serviceViewModel.AddLog(queryTypeText,
            $"{queryName} -> {string.Join(", ", records.Select(r => new IPAddress(r.Data ?? []).ToString()))}",
            ((IPEndPoint)client).Address.ToString());
    }
}