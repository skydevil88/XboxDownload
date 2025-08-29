using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Models.Host;
using RegexHelper = XboxDownload.Helpers.Utilities.RegexHelper;

namespace XboxDownload.ViewModels.Dialog;

public partial class ImportHostDialogViewModel(HostViewModel hostViewModel) : ObservableObject
{
    public Action? CloseDialog { get; init; }

    [ObservableProperty]
    private string _content = string.Empty;

    [RelayCommand]
    private void Confirm()
    {
        Dictionary<string, (List<IPAddress>, string)> dic = new();

        var lines = Content.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var hostsMatch = RegexHelper.HostsAndDnsmasqRegex().Match(line);
            if (!hostsMatch.Success) continue;

            var ip = hostsMatch.Groups["IP"].Value.Trim();
            var hostName = hostsMatch.Groups["HostName"].Value.Trim().ToLowerInvariant();
            var comment = hostsMatch.Groups["Comment"].Value.Trim();

            if (!RegexHelper.IsValidDomainOrHostname().IsMatch(hostName)) continue;
            if (!IPAddress.TryParse(ip, out var ipAddress)) continue;

            if (!dic.TryGetValue(hostName, out var value))
            {
                value = (new List<IPAddress>(), comment);
                dic[hostName] = value;
            }

            if (!value.Item1.Contains(ipAddress))
            {
                value.Item1.Add(ipAddress);
            }
        }

        foreach (var entry in hostViewModel.HostMappings.ToList())
        {
            if (dic.ContainsKey(entry.HostName))
            {
                hostViewModel.HostMappings.Remove(entry);
            }
        }

        foreach (var variable in dic)
        {
            var ipAddresses = string.Join(", ", variable.Value.Item1.Select(ip => ip.ToString()));
            var comment = variable.Value.Item2;

            var newEntry = new HostMappingEntry(true, variable.Key, ipAddresses, comment);
            hostViewModel.HostMappings.Add(newEntry);
            hostViewModel.SelectedEntry = newEntry;
        }

        CloseDialog?.Invoke();
    }

    [RelayCommand]
    private async Task ReadFileAsync()
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null)
            return;

        var options = new FilePickerOpenOptions
        {
            Title = ResourceHelper.GetString("Host.ImportHost.SelectFile"),
            FileTypeFilter =
            [
                new FilePickerFileType(ResourceHelper.GetString("Host.ImportHost.TxtFile"))
                {
                    Patterns = ["*.txt"]
                },
                new FilePickerFileType(ResourceHelper.GetString("Host.ImportHost.AllFile"))
                {
                    Patterns = ["*"]
                }
            ]
        };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            //var filePath = files[0].Path.LocalPath;
            try
            {
                if ((await files[0].GetBasicPropertiesAsync()).Size <= 1024 * 1024 * 1)
                {
                    await using var readStream = await files[0].OpenReadAsync();
                    using var reader = new StreamReader(readStream);
                    Content = await reader.ReadToEndAsync();
                }
                else
                {
                    throw new Exception(ResourceHelper.GetString("Host.ImportHost.FileExceeded1MbLimit"));
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Host.ImportHost.FileReadingFailedDialogTitle"),
                    string.Format(ResourceHelper.GetString("Host.ImportHost.FileReadingFailedDialogMessage"), ex.Message),
                    Icon.Error);
            }
        }
    }
}