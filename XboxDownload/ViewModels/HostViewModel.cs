using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Host;

namespace XboxDownload.ViewModels;

public partial class HostViewModel : ObservableObject
{
    public ObservableCollection<HostMappingEntry> HostMappings { get; } = [];

    private readonly ServiceViewModel _serviceViewModel;

    public HostViewModel(ServiceViewModel serviceViewModel)
    {
        _serviceViewModel = serviceViewModel;

        _ = LoadHostsFromJsonAsync();
    }


    [RelayCommand]
    private async Task LoadHostsFromJsonAsync()
    {
        HostMappings.Clear();

        var jsonPath = _serviceViewModel.HostFilePath;

        if (!File.Exists(jsonPath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var jsonEntries = JsonSerializer.Deserialize<List<HostMappingEntry>>(json);

            if (jsonEntries is null)
                return;

            foreach (var entry in jsonEntries)
            {
                HostMappings.Add(entry);
            }
        }
        catch
        {
            // ignored
        }
    }

    [ObservableProperty]
    private HostMappingEntry? _selectedEntry;

    [RelayCommand]
    private void AddHost()
    {
        var newEntry = new HostMappingEntry(true, "", "", "");
        HostMappings.Add(newEntry);
        SelectedEntry = newEntry;
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedEntry == null || !HostMappings.Contains(SelectedEntry)) return;
        HostMappings.Remove(SelectedEntry);
        SelectedEntry = null;
    }

    [RelayCommand]
    private void ClearAll()
    {
        HostMappings.Clear();
    }

    [RelayCommand]
    private async Task SaveHostToJsonAsync()
    {
        var toRemove = new List<HostMappingEntry>();
        var invalidEntries = new List<string>();

        foreach (var entry in HostMappings)
        {
            entry.HostName = RegexHelper.ExtractDomainFromUrlRegex().Replace(entry.HostName, "$2").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(entry.HostName) && string.IsNullOrWhiteSpace(entry.Ip))
            {
                toRemove.Add(entry);
                continue;
            }

            var ips = entry.Ip.Split([",", "，"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            invalidEntries.AddRange(from ip in ips where !IPAddress.TryParse(ip, out _) select $"{entry.HostName}: {ip}");

            entry.HostName = entry.HostName.Trim().ToLowerInvariant();
            entry.Ip = string.Join(", ", ips.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            entry.Note = entry.Note.Trim();
        }

        if (invalidEntries.Count > 0)
        {
            var message = string.Format(
                ResourceHelper.GetString("Host.InvalidIpDialogMessage"),
                string.Join("\n", invalidEntries)
            );
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Host.InvalidIpDialogTitle"),
                message,
                Icon.Error);
            return;
        }

        foreach (var entry in toRemove)
            HostMappings.Remove(entry);

        var jsonPath = _serviceViewModel.HostFilePath;

        if (HostMappings.Count == 0)
        {
            if (!File.Exists(jsonPath)) return;
            try
            {
                File.Delete(jsonPath);
            }
            catch
            {
                // ignored
            }
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(HostMappings);
            await File.WriteAllTextAsync(jsonPath, json);

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(jsonPath);
        }
        catch
        {
            // ignored
        }

        if (_serviceViewModel.IsListening)
        {
            await _serviceViewModel.DnsConnectionListener.LoadHostAndAkamaiMapAsync();
            if (_serviceViewModel.IsSetLocalDnsEnabled) CommandHelper.FlushDns();
        }
    }
}


