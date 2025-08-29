using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Services;
using XboxDownload.Services;

namespace XboxDownload.ViewModels.Dialog;

public partial class DohServerDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _dohServerProxyIp = App.Settings.DohServerProxyIp;


    private readonly ServiceViewModel _serviceViewModel;

    [ObservableProperty]
    private DohServerOption? _selectedDohServer;

    public ObservableCollection<DohServerOption> DohServersMappings => _serviceViewModel.DohServersMappings;

    public ObservableCollection<SpeedTestMappingEntry> SpeedTestMappings { get; } = [];

    public DohServerDialogViewModel(ServiceViewModel serviceViewModel)
    {
        _serviceViewModel = serviceViewModel;
        _selectedDohServer = _serviceViewModel.SelectedDohServer;

        foreach (var dohServer in DohServersMappings)
        {
            var useProxy = App.Settings.DohServerUseProxyId.Contains(dohServer.Id);
            SpeedTestMappings.Add(new SpeedTestMappingEntry(dohServer.IsSelected, dohServer.Id, dohServer.Name, dohServer.Url, dohServer.Ip, useProxy, dohServer.IsProxyDisabled));
        }

        _ = LoadDohHostsFromJsonAsync();
    }

    [RelayCommand]
    private void UpdateSelection(string? parameter)
    {
        switch (parameter)
        {
            case "SelectAll":
                foreach (var option in SpeedTestMappings)
                    option.IsSelect = true;
                break;

            case "InvertSelection":
                foreach (var option in SpeedTestMappings)
                    option.IsSelect = !option.IsSelect;
                break;

            case "EnableAllProxy":
                foreach (var option in SpeedTestMappings)
                {
                    if (!option.CanEditProxy) continue;
                    option.UseProxy = true;
                }
                break;

            case "ToggleProxy":
                foreach (var option in SpeedTestMappings)
                {
                    if (!option.CanEditProxy) continue;
                    option.UseProxy = !option.UseProxy;
                }
                break;
        }
    }

    [RelayCommand]
    private async Task SpeedTestAsync()
    {
        DohServerProxyIp = DohServerProxyIp.Trim();

        IPAddress? candidateIp = null;
        if (!string.IsNullOrEmpty(DohServerProxyIp) && !IPAddress.TryParse((string?)DohServerProxyIp, out candidateIp))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Service.SecureDns.InvalidCloudflareProxyIpTitle"),
                ResourceHelper.GetString("Service.SecureDns.InvalidCloudflareProxyIpMessage"),
                Icon.Error);
            RequestProxyIpFocus?.Invoke();
            return;
        }
        if (candidateIp != null) DohServerProxyIp = candidateIp.ToString();

        foreach (var selected in SpeedTestMappings.Where(o => o.IsSelect))
        {
            selected.Website1 = selected.Website2 = selected.Website3 = null;
            selected.Tip1 = selected.Tip2 = selected.Tip3 = string.Empty;
        }

        var useProxyId = (from option in SpeedTestMappings where option.UseProxy select option.Id).ToList();
        if (!useProxyId.SequenceEqual(App.Settings.DohServerUseProxyId))
        {
            App.Settings.DohServerUseProxyId = useProxyId;
            SettingsManager.Save(App.Settings);
        }

        var tasks = SpeedTestMappings
            .Where(o => o.IsSelect)
            .Select(async selected =>
            {
                var uri = new Uri(selected.Url);
                var headers = new Dictionary<string, string> { { "Accept", "application/dns-json" } };
                var finalUrl = selected.Url;

                if (selected.UseProxy)
                {
                    var proxyIp = DnsHelper.DohProxyIp;

                    if (candidateIp != null)
                    {
                        proxyIp = DnsHelper.FormatIpForUrl(candidateIp);
                    }

                    finalUrl = $"https://{proxyIp}/{uri.Host}{uri.PathAndQuery}";
                    headers["Host"] = DnsHelper.DohProxyHost;
                    headers["X-Organization"] = nameof(XboxDownload);
                    headers["X-Author"] = "Devil";
                }
                else if (!string.IsNullOrWhiteSpace(selected.Ip) && IPAddress.TryParse(selected.Ip, out var ipAddress))
                {
                    finalUrl = new UriBuilder(uri) { Host = ipAddress.ToString() }.ToString();
                    headers["Host"] = uri.Host;
                }

                var currentDoH = new DnsHelper.DoHServer
                {
                    UseProxy = selected.UseProxy,
                    Url = finalUrl,
                    Headers = headers
                };

                // Warm-up (ignore result to avoid cold-start overhead)
                _ = await DnsHelper.ResolveDohAsync("www.xbox.com", currentDoH);
                await Task.Delay(200);

                (selected.Website1, selected.Tip1) = await TestDomainAsync("www.xbox.com", currentDoH);
                (selected.Website2, selected.Tip2) = await TestDomainAsync("www.playstation.com", currentDoH);
                (selected.Website3, selected.Tip3) = await TestDomainAsync("www.nintendo.com", currentDoH);
            });

        await Task.WhenAll(tasks);
    }

    private static async Task<(int Time, string Tip)> TestDomainAsync(string domain, DnsHelper.DoHServer dohServer)
    {
        var stopwatch = Stopwatch.StartNew();
        var ipAddresses = await DnsHelper.ResolveDohAsync(domain, dohServer);
        stopwatch.Stop();

        if (!(ipAddresses?.Count > 0)) return (-1, string.Empty);
        var tip = string.Join(Environment.NewLine, ipAddresses);
        return ((int)stopwatch.ElapsedMilliseconds, tip);
    }

    public Action? CloseDialog { get; init; }

    public Action? RequestProxyIpFocus { get; set; }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var proxyIp = DohServerProxyIp.Trim();
        if (proxyIp != App.Settings.DohServerProxyIp)
        {
            if (!string.IsNullOrEmpty(proxyIp))
            {
                if (!IPAddress.TryParse((string?)proxyIp, out var ipAddress))
                {
                    await DialogHelper.ShowInfoDialogAsync(
                        ResourceHelper.GetString("Service.SecureDns.InvalidCloudflareProxyIpTitle"),
                        ResourceHelper.GetString("Service.SecureDns.InvalidCloudflareProxyIpMessage"),
                        Icon.Error);
                    RequestProxyIpFocus?.Invoke();
                    return;
                }
                App.Settings.DohServerProxyIp = ipAddress.ToString();
            }
            else
            {
                App.Settings.DohServerProxyIp = string.Empty;
            }
            SettingsManager.Save(App.Settings);

            _serviceViewModel.SelectedDohServer = null;
        }

        var useProxyId = (from option in SpeedTestMappings where option.UseProxy select option.Id).ToList();
        if (!useProxyId.SequenceEqual(App.Settings.DohServerUseProxyId))
        {
            App.Settings.DohServerUseProxyId = useProxyId;
            SettingsManager.Save(App.Settings);

            if (_serviceViewModel.SelectedDohServer == SelectedDohServer)
                _serviceViewModel.SelectedDohServer = null;
        }

        _serviceViewModel.SelectedDohServer = SelectedDohServer;

        if (_serviceViewModel.IsListening) await _serviceViewModel.DnsConnectionListener.LoadForceEncryptedDomainMapAsync();

        CloseDialog?.Invoke();
    }

    [ObservableProperty]
    private DohHostMappingEntry? _selectedEntry;

    [RelayCommand]
    private async Task LoadDohHostsFromJsonAsync()
    {
        DohHostMappings.Clear();

        var jsonPath = _serviceViewModel.ForceEncryptionDomainFilePath;

        if (!File.Exists(jsonPath))
            return;

        var json = await File.ReadAllTextAsync(jsonPath);

        var jsonEntries = JsonSerializer.Deserialize<List<DohHostEntryJson>>(json);
        if (jsonEntries is null)
            return;

        foreach (var entry in jsonEntries.Select(jsonEntry => DohHostMappingEntry.FromJson(jsonEntry, DohServersMappings)))
        {
            DohHostMappings.Add(entry);
        }
    }

    [RelayCommand]
    private void AddDohHost()
    {
        var newEntry = new DohHostMappingEntry
        {
            IsEnabled = true,
            Domain = string.Empty,
            DohServer = SelectedDohServer ?? DohServersMappings.FirstOrDefault(),
            Note = string.Empty
        };
        DohHostMappings.Add(newEntry);
        SelectedEntry = newEntry;
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedEntry == null || !DohHostMappings.Contains(SelectedEntry)) return;
        DohHostMappings.Remove(SelectedEntry);
        SelectedEntry = null;
    }

    [RelayCommand]
    private void ClearAll()
    {
        DohHostMappings.Clear();
    }


    [RelayCommand]
    private async Task SaveDohHostsToJsonAsync()
    {
        var entriesToSave = DohHostMappings
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.Domain) ||
                !string.IsNullOrWhiteSpace(e.Note))
            .Select(e => e.ToJson())
            .ToList();

        var jsonPath = _serviceViewModel.ForceEncryptionDomainFilePath;

        if (entriesToSave.Count == 0)
        {
            if (File.Exists(jsonPath))
            {
                try
                {
                    File.Delete(jsonPath);
                }
                catch
                {
                    // ignored
                }
            }

            CloseDialog?.Invoke();
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(entriesToSave);
            await File.WriteAllTextAsync(jsonPath, json);

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(jsonPath);
        }
        catch
        {
            // ignored
        }

        if (_serviceViewModel.IsListening) await _serviceViewModel.DnsConnectionListener.LoadForceEncryptedDomainMapAsync();

        CloseDialog?.Invoke();
    }

    public ObservableCollection<DohHostMappingEntry> DohHostMappings { get; } = [];

    public class DohHostMappingEntry
    {
        public bool IsEnabled { get; set; }
        public string Domain { get; set; } = string.Empty;
        public DohServerOption? DohServer { get; set; }
        public string Note { get; set; } = string.Empty;
        private string DohServerId => DohServer?.Id ?? string.Empty;

        public DohHostEntryJson ToJson() => new()
        {
            IsEnabled = IsEnabled,
            Domain = RegexHelper.ExtractDomainFromUrlRegex().Replace(Domain, "$2").Trim().ToLowerInvariant(),
            DohServerId = DohServerId,
            Remark = Truncate(Note.Trim(), 50)
        };

        public static DohHostMappingEntry FromJson(DohHostEntryJson json, IEnumerable<DohServerOption> dohList)
        {
            var doh = dohList.FirstOrDefault(d => d.Id == json.DohServerId);
            return new DohHostMappingEntry
            {
                IsEnabled = json.IsEnabled,
                Domain = json.Domain,
                DohServer = doh,
                Note = json.Remark
            };
        }

        private static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Length > maxLength ? input[..maxLength] : input;
        }
    }

    public class DohHostEntryJson
    {
        public bool IsEnabled { get; init; }
        public string Domain { get; init; } = string.Empty;
        public string DohServerId { get; init; } = string.Empty;
        public string Remark { get; init; } = string.Empty;
    }

}

public partial class SpeedTestMappingEntry : ObservableObject
{
    [ObservableProperty]
    private bool _isSelect;

    public string Id { get; }
    public string Name { get; }
    public string Url { get; }
    public string Ip { get; }

    [ObservableProperty]
    private int? _website1, _website2, _website3;

    [ObservableProperty]
    private string _tip1 = string.Empty;

    [ObservableProperty]
    private string _tip2 = string.Empty;

    [ObservableProperty]
    private string _tip3 = string.Empty;

    [ObservableProperty]
    private bool _useProxy;

    public bool CanEditProxy { get; }

    public SpeedTestMappingEntry(bool isSelect, string id, string name, string url, string ip, bool useProxy, bool isProxyDisabled)
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