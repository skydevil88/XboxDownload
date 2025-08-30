using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.LocalProxy;
using XboxDownload.Services;

namespace XboxDownload.ViewModels.Dialog;

public partial class LocalProxyDialogViewModel : ObservableObject
{
    private readonly ServiceViewModel _serviceViewModel;
    
    public static bool IsLinux => OperatingSystem.IsLinux();

    public ObservableCollection<DohModels> DohModelsMappings { get; } = [];

    [ObservableProperty]
    private static string _rulesText = string.Empty;

    public ObservableCollection<ProxyModels> ProxyRules { get; } = LocalProxyBuilder.GetProxyRulesList();

    public LocalProxyDialogViewModel(ServiceViewModel serviceViewModel)
    {
        _serviceViewModel = serviceViewModel;

        if (File.Exists(_serviceViewModel.SniProxyFilePath))
        {
            List<List<object>>? sniProxy = null;
            try
            {
                sniProxy = JsonSerializer.Deserialize<List<List<object>>>(File.ReadAllText(_serviceViewModel.SniProxyFilePath));
            }
            catch
            {
                // ignored
            }

            if (sniProxy == null) return;

            var sb = new StringBuilder();
            foreach (var item in sniProxy)
            {
                if (item.Count != 3) continue;
                var jeHosts = (JsonElement)item[0];
                if (jeHosts.ValueKind != JsonValueKind.Array) continue;
                var hosts = string.Join(", ", jeHosts.EnumerateArray().Select(x => x.GetString()?.Trim()));
                if (string.IsNullOrEmpty(hosts)) continue;
                var fakeHost = item[1].ToString()?.Trim();
                var ip = item[2].ToString()?.Trim();
                if (string.IsNullOrEmpty(fakeHost) && string.IsNullOrEmpty(ip))
                    sb.AppendLine(hosts);
                else if (!string.IsNullOrEmpty(fakeHost) && !string.IsNullOrEmpty(ip))
                    sb.AppendLine(hosts + " | " + fakeHost + " | " + ip);
                else
                    sb.AppendLine(hosts + " | " + fakeHost + ip);
            }
            RulesText = sb.ToString();
        }

        foreach (var dohServers in _serviceViewModel.DohServersMappings)
        {
            if (dohServers.Id is not ("AlibabaCloud" or "TencentCloud" or "Qihoo360"))
            {
                DohModelsMappings.Add(new DohModels(dohServers.Id, dohServers.Name, App.Settings.SniProxyId.Contains(dohServers.Id)));
            }
        }
        if (!DohModelsMappings.Any(a => a.IsChecked))
            DohModelsMappings.FirstOrDefault()!.IsChecked = true;
    }

    [ObservableProperty]
    private int _selectionStart, _selectionEnd;

    [RelayCommand]
    private void CheckBox(ProxyModels models)
    {
        var rule = ProxyRules.FirstOrDefault(rule => rule.Display == models.Display)?.Rule;
        if (rule == null) return;

        SelectionStart = 0;
        SelectionEnd = 0;
        var sb = new StringBuilder();
        foreach (var line in RulesText.ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (!Array.Exists(rule, element => element.Equals(line)))
                sb.AppendLine(line);
        }
        if (models.IsChecked)
        {
            var hosts = string.Join(Environment.NewLine, rule);
            RulesText = hosts + Environment.NewLine + sb;
            SelectionEnd = hosts.Length;
        }
        else
        {
            RulesText = sb.ToString();
        }
    }
    
    [RelayCommand]
    private static void OpenWebStore()
    {
        HttpClientHelper.OpenUrl("https://github.com/skydevil88/XboxDownload/discussions/128");
    }
    
    [RelayCommand]
    private static async Task SaveCertificateAsync()
    {
        if (!File.Exists(CertificateHelper.RootCrt))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Service.LocalProxy.FailedDialogTitle"),
                ResourceHelper.GetString("Service.LocalProxy.FailedDialogMessage"),
                Icon.Error);
            return;
        }
        
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null)
            return;
        
        var currentWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime currentDesktop
            ? currentDesktop.MainWindow
            : null;

        currentWindow?.Hide();
        
        var resourceDirectory = Path.Combine(AppContext.BaseDirectory, "Resource");
        if (!Directory.Exists(resourceDirectory))
        {
            Directory.CreateDirectory(resourceDirectory);

            if (!OperatingSystem.IsWindows())
                await PathHelper.FixOwnershipAsync(resourceDirectory, true);
        }
        
        var startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(resourceDirectory);
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Certificate",
            SuggestedFileName = "XboxDownload.crt",
            DefaultExtension = "crt",
            ShowOverwritePrompt = true,
            SuggestedStartLocation = startLocation,
            FileTypeChoices =
            [
                new FilePickerFileType("Certificate File")
                {
                    Patterns = ["*.crt"]
                }
            ]
        });
        
        currentWindow?.Show(); 

        var localPath = file?.TryGetLocalPath();
        if (localPath == null) return;
        
        if (File.Exists(localPath))
            File.Delete(localPath);
        
        File.Copy(CertificateHelper.RootCrt, localPath);
        if (!OperatingSystem.IsWindows())
            _ = PathHelper.FixOwnershipAsync(localPath);
        
        await DialogHelper.ShowInfoDialogAsync(
            ResourceHelper.GetString("Service.LocalProxy.SuccessDialogTitle"),
            string.Format(ResourceHelper.GetString("Service.LocalProxy.SuccessDialogMessage"), localPath),
            Icon.Success);
    }

    public Action? CloseDialog { get; init; }

    [RelayCommand]
    private void Save()
    {
        var lsSniProxy = new List<List<object>>();
        foreach (var line in RulesText.ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var proxy = line.Split('|');
            if (proxy.Length == 0) continue;
            var arrHost = new ArrayList();
            var sni = string.Empty;
            var lsIPv6 = new List<IPAddress>();
            var lsIPv4 = new List<IPAddress>();
            if (proxy.Length >= 1)
            {
                foreach (var hostRaw in proxy[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    var host = RegexHelper.ExtractDomainFromUrlRegex().Replace(hostRaw, "$2").Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(host))
                    {
                        arrHost.Add(ArrowSymbolRegex().Replace(host, " -> "));
                    }
                }
            }
            if (proxy.Length == 2)
            {
                proxy[1] = proxy[1].Trim();
                var ips = proxy[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length == 1)
                {
                    if (IPAddress.TryParse(proxy[1], out var ipAddress))
                    {
                        switch (ipAddress.AddressFamily)
                        {
                            case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                lsIPv6.Add(ipAddress);
                                break;
                            case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                lsIPv4.Add(ipAddress);
                                break;
                        }
                    }
                    else sni = RegexHelper.ExtractDomainFromUrlRegex().Replace(proxy[1], "$2").Trim().ToLowerInvariant();
                }
                else
                {
                    foreach (var ip in ips)
                    {
                        if (IPAddress.TryParse(ip.Trim(), out var ipAddress))
                        {
                            switch (ipAddress.AddressFamily)
                            {
                                case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                    lsIPv6.Add(ipAddress);
                                    break;
                                case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                    lsIPv4.Add(ipAddress);
                                    break;
                            }
                        }
                    }
                }
            }
            else if (proxy.Length >= 3)
            {
                sni = RegexHelper.ExtractDomainFromUrlRegex().Replace(proxy[1], "$2").Trim().ToLowerInvariant();
                var ips = proxy[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var ip in ips)
                {
                    if (IPAddress.TryParse(ip.Trim(), out var ipAddress))
                    {
                        switch (ipAddress.AddressFamily)
                        {
                            case AddressFamily.InterNetworkV6 when !lsIPv6.Contains(ipAddress):
                                lsIPv6.Add(ipAddress);
                                break;
                            case AddressFamily.InterNetwork when !lsIPv4.Contains(ipAddress):
                                lsIPv4.Add(ipAddress);
                                break;
                        }
                    }
                }
            }
            if (arrHost.Count >= 1)
            {
                lsSniProxy.Add([arrHost, sni, string.Join(", ", lsIPv6.Union(lsIPv4).Take(16).ToList())]);
            }
        }

        if (lsSniProxy.Count >= 1)
        {
            File.WriteAllText(_serviceViewModel.SniProxyFilePath, JsonSerializer.Serialize(lsSniProxy, JsonHelper.Indented));

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(_serviceViewModel.SniProxyFilePath);
        }
        else if (File.Exists(_serviceViewModel.SniProxyFilePath))
        {
            File.Delete(_serviceViewModel.SniProxyFilePath);
        }

        if (!DohModelsMappings.Any(a => a.IsChecked))
            DohModelsMappings.FirstOrDefault()!.IsChecked = true;
        var selectedDohIds = DohModelsMappings.Where(d => d.IsChecked).Select(d => d.Id).ToList();
        App.Settings.SniProxyId = selectedDohIds;
        SettingsManager.Save(App.Settings);

        CloseDialog?.Invoke();
    }

    [GeneratedRegex(@"\s*->\s*")]
    private static partial Regex ArrowSymbolRegex();
}

