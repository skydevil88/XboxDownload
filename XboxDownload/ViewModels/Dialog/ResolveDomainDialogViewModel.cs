using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Host;
using XboxDownload.Models.Services;
using XboxDownload.Services;

namespace XboxDownload.ViewModels.Dialog;

public partial class ResolveDomainDialogViewModel : ObservableObject
{
    private readonly HostViewModel _hostViewModel;

    private readonly ServiceViewModel _serviceViewModel;

    private ObservableCollection<DohServerOption> DohServersMappings => _serviceViewModel.DohServersMappings;

    public ObservableCollection<ResolveHostMappingEntry> ResolveHostMappings { get; } = [];

    public ResolveDomainDialogViewModel(ServiceViewModel serviceViewModel, HostViewModel hostViewModel)
    {
        _serviceViewModel = serviceViewModel;
        _hostViewModel = hostViewModel;

        foreach (var dohServer in DohServersMappings)
        {
            var useProxy = App.Settings.DohServerUseProxyId.Contains(dohServer.Id);
            ResolveHostMappings.Add(new ResolveHostMappingEntry(dohServer.IsChecked, dohServer.Id, dohServer.Name, dohServer.Url, dohServer.Ip, useProxy, dohServer.IsProxyDisabled));
        }
    }

    [ObservableProperty]
    private ResolveHostMappingEntry? _selectedItem;

    [ObservableProperty]
    private string _hostnameToResolve = string.Empty, _resolveIp = string.Empty;

    [RelayCommand]
    private void SetIpForHost(ResolveHostMappingEntry? item)
    {
        if (string.IsNullOrWhiteSpace(item?.ResolvedIp)) return;
        ResolveIp = item.ResolvedIp;
        RequestFocus?.Invoke("Ip");
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(HostnameToResolve))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Host.ResolveDomain.InvalidDomainNameDialogTitle"),
                ResourceHelper.GetString("Host.ResolveDomain.InvalidDomainNameDialogMessage"),
                Icon.Error);
            RequestFocus?.Invoke("Host");
            return;
        }

        if (string.IsNullOrWhiteSpace(ResolveIp))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Host.ResolveDomain.InvalidIpDialogTitle"),
                ResourceHelper.GetString("Host.ResolveDomain.InvalidIpDialogMessage"),
                Icon.Error);
            RequestFocus?.Invoke("Ip");
            return;
        }

        foreach (var entry in _hostViewModel.HostMappings.ToList().Where(entry => entry.HostName.Equals(HostnameToResolve)))
        {
            _hostViewModel.HostMappings.Remove(entry);
        }

        var newEntry = new HostMappingEntry(true, HostnameToResolve, ResolveIp, "");
        _hostViewModel.HostMappings.Add(newEntry);
        _hostViewModel.SelectedEntry = newEntry;

        var useProxyId = (from option in ResolveHostMappings where option.UseProxy select option.Id).ToList();
        if (!useProxyId.SequenceEqual(App.Settings.DohServerUseProxyId))
        {
            App.Settings.DohServerUseProxyId = useProxyId;
            SettingsManager.Save(App.Settings);
        }

        CloseDialog?.Invoke();
    }

    [RelayCommand]
    private void UpdateSelection(string? parameter)
    {
        switch (parameter)
        {
            case "SelectAll":
                foreach (var option in ResolveHostMappings)
                    option.IsSelect = true;
                break;

            case "InvertSelection":
                foreach (var option in ResolveHostMappings)
                    option.IsSelect = !option.IsSelect;
                break;

            case "EnableAllProxy":
                foreach (var option in ResolveHostMappings)
                {
                    if (!option.CanEditProxy) continue;
                    option.UseProxy = true;
                }
                break;

            case "ToggleProxy":
                foreach (var option in ResolveHostMappings)
                {
                    if (!option.CanEditProxy) continue;
                    option.UseProxy = !option.UseProxy;
                }
                break;
        }
    }

    public Action? CloseDialog { get; init; }

    public event Action<string>? RequestFocus;

    [ObservableProperty]
    private bool _preferIPv6;

    [RelayCommand]
    public async Task QueryAsync()
    {
        HostnameToResolve = RegexHelper.ExtractDomainFromUrlRegex().Replace(HostnameToResolve, "$2").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(HostnameToResolve))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Host.ResolveDomain.InvalidDomainNameDialogTitle"),
                ResourceHelper.GetString("Host.ResolveDomain.InvalidDomainNameDialogMessage"),
                Icon.Error);
            RequestFocus?.Invoke("Host");
            return;
        }

        var useProxyId = (from option in ResolveHostMappings where option.UseProxy select option.Id).ToList();
        if (!useProxyId.SequenceEqual(App.Settings.DohServerUseProxyId))
        {
            App.Settings.DohServerUseProxyId = useProxyId;
            SettingsManager.Save(App.Settings);
        }

        foreach (var selected in ResolveHostMappings.Where(o => o.IsSelect))
        {
            selected.ResolvedIp = selected.Location = null;
            selected.Delay = null;
        }

        // Dictionary for caching delay test tasks
        var delayTestCache = new ConcurrentDictionary<IPAddress, Task<(long? delay, string? location)>>();

        var tasks = ResolveHostMappings
            .Where(o => o.IsSelect)
            .Select(async selected =>
            {
                var uri = new Uri(selected.Url);
                var headers = new Dictionary<string, string> { { "Accept", "application/dns-json" } };
                var finalUrl = selected.Url;

                if (selected.UseProxy)
                {
                    var proxyIp = DnsHelper.DohProxyIp;

                    if (!string.IsNullOrWhiteSpace(App.Settings.DohServerProxyIp) && IPAddress.TryParse(App.Settings.DohServerProxyIp, out var ipAddress))
                    {
                        proxyIp = DnsHelper.FormatIpForUrl(ipAddress);
                    }

                    finalUrl = $"https://{proxyIp}/{uri.Host}{uri.PathAndQuery}";

                    headers["Host"] = DnsHelper.DohProxyHost;
                    headers["X-Organization"] = nameof(XboxDownload);
                    headers["X-Author"] = "Devil";
                }
                else if (!string.IsNullOrWhiteSpace(selected.Ip) && IPAddress.TryParse(selected.Ip, out var targetIp))
                {
                    finalUrl = new UriBuilder(uri) { Host = targetIp.ToString() }.ToString();
                    headers["Host"] = uri.Host;
                }

                var currentDoH = new DnsHelper.DoHServer
                {
                    UseProxy = selected.UseProxy,
                    Url = finalUrl,
                    Headers = headers
                };

                var ipAddresses = await DnsHelper.ResolveDohAsync(HostnameToResolve, currentDoH, preferIPv6: PreferIPv6);
                if (ipAddresses?.Count > 0)
                {
                    var ip = ipAddresses[0];

                    selected.ResolvedIp = ip.ToString();

                    // Get or create a delay test task
                    var delayTask = delayTestCache.GetOrAdd(ip, _ => HttpDelayTestWithLocationAsync(HostnameToResolve, ip));

                    // Wait for the result
                    var task = await delayTask;
                    selected.Delay = task.delay ?? -1;
                    selected.Location = task.location;
                }
            });

        await Task.WhenAll(tasks);
    }

    private static readonly ConcurrentDictionary<IPAddress, string?> IpLocationCache = new();

    private static async Task<(long? delay, string? location)> HttpDelayTestWithLocationAsync(string host, IPAddress ip, int timeout = 3000)
    {
        long? delay = null;
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            var uri = new Uri($"https://{ip}");

            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            request.Headers.TryAddWithoutValidation("Host", host);

            var sw = Stopwatch.StartNew();
            var delayResponse = await HttpClientHelper.SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            sw.Stop();

            if (delayResponse.IsSuccessStatusCode)
            {
                delay = sw.ElapsedMilliseconds;
            }
        }
        catch
        {
            // ignored
        }
        
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            bytes[3] = 0;
        }
        else
        {
            for (var i = 8; i < 16; i++)
            {
                bytes[i] = 0;
            }
        }
        ip = new IPAddress(bytes);
        if (IpLocationCache.TryGetValue(ip, out var cachedLocation)) return (delay, cachedLocation);

        var isSimplifiedChineseUser = App.Settings.Culture.Equals("zh-Hans");
        cachedLocation = await IpGeoHelper.GetIpLocationFromMultipleApisAsync(ip.ToString(), isSimplifiedChineseUser);
        if (!string.IsNullOrEmpty(cachedLocation))
        {
            IpLocationCache[ip] = cachedLocation;
        }

        return (delay, cachedLocation);
    }
}

public partial class ResolveHostMappingEntry : ObservableObject
{
    [ObservableProperty]
    private bool _isSelect;
    public string Id { get; }
    public string Name { get; }
    public string Url { get; }
    public string Ip { get; }

    [ObservableProperty]
    private string? _resolvedIp;

    [ObservableProperty]
    private long? _delay;

    [ObservableProperty]
    private string? _location;

    [ObservableProperty]
    private bool _useProxy;
    public bool CanEditProxy { get; }

    public ResolveHostMappingEntry(bool isSelect, string id, string name, string url, string ip, bool useProxy, bool isProxyDisabled)
    {
        IsSelect = isSelect;
        Id = id;
        Name = name;
        Url = url;
        Ip = ip;
        UseProxy = useProxy;
        CanEditProxy = !isProxyDisabled;
    }
}