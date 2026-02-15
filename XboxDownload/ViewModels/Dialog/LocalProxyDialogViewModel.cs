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
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
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
    private static string _rulesText = string.Empty, _rules2Text = string.Empty, _certDomain1Text = string.Empty, _certDomain2Text = string.Empty;

    public ObservableCollection<ProxyModels> ProxyRules { get; } = LocalProxyBuilder.GetProxyRulesList();

    public LocalProxyDialogViewModel(ServiceViewModel serviceViewModel)
    {
        _serviceViewModel = serviceViewModel;

        RulesText = string.Empty;
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

            if (sniProxy != null)
            {
                var sb = new StringBuilder();
                foreach (var item in sniProxy)
                {
                    if (item.Count != 3) continue;
                    var jeHosts = (JsonElement)item[0];
                    if (jeHosts.ValueKind != JsonValueKind.Array) continue;
                    var hosts = string.Join(", ", jeHosts.EnumerateArray().Select(x => x.GetString()));
                    if (string.IsNullOrEmpty(hosts)) continue;
                    var sni = item[1].ToString()?.Trim();
                    var jeIps = (JsonElement)item[2];

                    var list = new List<string>();
                    if (jeIps.ValueKind == JsonValueKind.Array)
                    {
                        list.Add(hosts);
                        if (!string.IsNullOrEmpty(sni))
                            list.Add(sni);
                        list.Add(string.Join(", ", jeIps.EnumerateArray().Select(x => x.GetString())));
                    }
                    else
                    {
                        var ip = jeIps.GetString();
                        if (string.IsNullOrEmpty(ip))
                        {
                            list.Add(hosts);
                            if (!string.IsNullOrEmpty(sni))
                                list.Add(sni);
                        }
                        else if (RegexHelper.IsValidDomain().IsMatch(ip))
                        {
                            list.Add(hosts + " -> " + ip);
                            if (!string.IsNullOrEmpty(sni))
                                list.Add(sni);
                        }
                        else
                        {
                            list.Add(hosts);
                            if (!string.IsNullOrEmpty(sni))
                                list.Add(sni);
                            list.Add(ip);
                        }
                    }
                    sb.AppendLine(string.Join(" | ", list));
                }
                RulesText = sb.ToString();
            }
        }

        foreach (var dohServer in _serviceViewModel.DohServersMappings)
        {
            if (dohServer.Id is "AlibabaCloud" or "TencentCloud" or "Qihoo360") continue;

            var model = new DohModels(
                dohServer.Id,
                dohServer.Name,
                App.Settings.SniProxyId.Contains(dohServer.Id)
            );

            model.CheckedChanged += (_, _) => RefreshDoHSelectionState();

            DohModelsMappings.Add(model);
        }

        RefreshDoHSelectionState();

        if (!DohModelsMappings.Any(a => a.IsChecked))
            DohModelsMappings.FirstOrDefault()!.IsChecked = true;

        Rules2Text = File.Exists(_serviceViewModel.SniProxy2FilePath) ? File.ReadAllText(_serviceViewModel.SniProxy2FilePath): string.Empty;

        using (var stream = AssetLoader.Open(new Uri($"avares://{nameof(XboxDownload)}/Resources/CertDomain.txt")))
        {
            using (var reader = new StreamReader(stream))
            {
                CertDomain1Text = reader.ReadToEnd().Trim() + Environment.NewLine;
            }
        }
        CertDomain2Text = File.Exists(_serviceViewModel.CertDomainFilePath) ? File.ReadAllText(_serviceViewModel.CertDomainFilePath) + Environment.NewLine : string.Empty;
    }

    [ObservableProperty]
    private int _selectionStart, _selectionEnd;

    [RelayCommand]
    private void ApplyProxyRule(ProxyModels models)
    {
        var rule = ProxyRules.FirstOrDefault(rule => rule.Display == models.Display)?.Rule;
        if (rule == null) return;

        SelectionStart = 0;
        SelectionEnd = 0;
        var sb = new StringBuilder();
        foreach (var line in RulesText.ReplaceLineEndings().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
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

    [ObservableProperty]
    private string _doHServers = string.Empty;

    private void RefreshDoHSelectionState()
    {
        var count = DohModelsMappings.Count(x => x.IsChecked);

        DoHServers = $"{ResourceHelper.GetString("Service.LocalProxy.DoHServers")} ({count}/8)";

        var disableUnselected = count >= 8;
        var enableState = !disableUnselected;

        foreach (var model in DohModelsMappings)
        {
            if (!model.IsChecked && model.IsEnabled != enableState)
            {
                model.IsEnabled = enableState;
            }
        }
    }

    [RelayCommand]
    private static async Task OpenUrl(string url)
    {
        await HttpClientHelper.OpenUrlAsync(url);
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
    private async Task SaveRulesAsync()
    {
        var lsSniProxy = new List<List<object>>();
        foreach (var line in RulesText.ReplaceLineEndings().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;
            
            if (!line.Contains('='))
            {
                var proxy = line.Split('|');
                if (proxy.Length == 0) continue;
                var arrHost = new ArrayList();
                var branch = string.Empty;
                var sni = string.Empty;
                var lsIPv6 = new List<IPAddress>();
                var lsIPv4 = new List<IPAddress>();
                if (proxy.Length >= 1)
                {
                    foreach (var hostRaw in proxy[0].Split([",", "，"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        var host = RegexHelper.ExtractDomainFromUrlRegex().Replace(hostRaw, "$2").Trim().ToLowerInvariant();
                        var parts = host.Split("->", StringSplitOptions.TrimEntries);
                        if (parts.Length == 2)
                        {
                            if (parts[0].StartsWith('.')) parts[0] = "*" + parts[0];
                            arrHost.Add(parts[0]);
                            branch = parts[1];
                        }
                        else
                        {
                            if (host.StartsWith('.')) host = "*" + host;
                            arrHost.Add(host);
                        }
                    }
                }

                if (proxy.Length == 2)
                {
                    var ips = proxy[1].Split([",", "，"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (ips.Length == 1)
                    {
                        if (IPAddress.TryParse(ips[0], out var ipAddress))
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
                        else
                        {
                            sni = RegexHelper.ExtractDomainFromUrlRegex().Replace(proxy[1], "$2").Trim().ToLowerInvariant();
                        }
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
                    var ips = proxy[2].Split([",", "，"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

                if (!string.IsNullOrEmpty(branch))
                {
                    lsSniProxy.Add([arrHost, sni, branch]);
                }
                else if (arrHost.Count >= 1)
                {
                    var ips = lsIPv4.Union(lsIPv6).Take(16).Select(ip => ip.ToString());
                    if (ips.Any())
                        lsSniProxy.Add([arrHost, sni, ips]);
                    else
                        lsSniProxy.Add([arrHost, sni, ""]);
                }
            }
            else
            {
                var proxy = line.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (proxy.Length != 2) continue;
                proxy[0] = proxy[0].Trim('"').Trim();
                proxy[1] = proxy[1].Trim('"').Trim();
                if (string.IsNullOrEmpty(proxy[0])) continue;

                var host = proxy[0].ToLowerInvariant();
                if (host.StartsWith('.')) host = "*" + host;
                var arrHost = new ArrayList { host };
                if (IPAddress.TryParse(proxy[1] , out var ipAddress))
                {
                    var lsIp = new List<string> { ipAddress.ToString() };
                    lsSniProxy.Add([arrHost, string.Empty, lsIp]);
                }
                else
                {
                    lsSniProxy.Add([arrHost, string.Empty, proxy[1]]);
                }
            }
        }

        if (lsSniProxy.Count >= 1)
        {
            await File.WriteAllTextAsync(_serviceViewModel.SniProxyFilePath, JsonSerializer.Serialize(lsSniProxy, JsonHelper.Indented));

            if (!OperatingSystem.IsWindows())
                await PathHelper.FixOwnershipAsync(_serviceViewModel.SniProxyFilePath);
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

    [RelayCommand]
    private async Task UpdateThirdPartyRulesAsync()
    {
        Rules2Text = string.Empty;

        const string url = "https://github.com/SpaceTimee/Cealing-Host/raw/main/Cealing-Host.json";
        using var cts = new CancellationTokenSource();

        var tasks = UpdateService.Proxies1.Concat([""]).ToArray().Select(async proxy => await HttpClientHelper.GetStringContentAsync(proxy + url, token: cts.Token, timeout: 6000)).ToList();
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            var responseString = await completedTask;
            if (!responseString.StartsWith('[')) continue;
            await cts.CancelAsync();
            Rules2Text = responseString;
        }

        if (!cts.IsCancellationRequested)
        {
            var responseString = await HttpClientHelper.GetStringContentAsync($"https://testingcf.jsdelivr.net/gh/SpaceTimee/Cealing-Host/Cealing-Host.json", token: CancellationToken.None);
            if (responseString.StartsWith('['))
            {
                Rules2Text = responseString;
            }
        }
    }

    [RelayCommand]
    private void ClearThirdPartyRules()
    {
        Rules2Text = string.Empty;
    }

    [RelayCommand]
    private async Task SaveThirdPartyRulesAsync()
    {
        Rules2Text = Rules2Text.Trim();
        if (string.IsNullOrEmpty(Rules2Text))
        {
            if (File.Exists(_serviceViewModel.SniProxy2FilePath))
                File.Delete(_serviceViewModel.SniProxy2FilePath);
        }
        else
        {
            try
            {
                using var doc = JsonDocument.Parse(Rules2Text);
            }
            catch (JsonException ex)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Service.LocalProxy.FailedDialogTitle2"),
                    string.Format(ResourceHelper.GetString("Service.LocalProxy.FailedDialogMessage2"), ex.Message),
                    Icon.Error);
                return;
            }

            await File.WriteAllTextAsync(_serviceViewModel.SniProxy2FilePath, Rules2Text);

            if (!OperatingSystem.IsWindows())
                await PathHelper.FixOwnershipAsync(_serviceViewModel.SniProxy2FilePath);
        }

        CloseDialog?.Invoke();
    }
    
    
    [RelayCommand]
    private async Task SaveCertVerifyRulesAsync()
    {
        CertDomain2Text = CertDomain2Text.Trim();
        if (string.IsNullOrEmpty(CertDomain2Text))
        {
            if (File.Exists(_serviceViewModel.CertDomainFilePath))
                File.Delete(_serviceViewModel.CertDomainFilePath);
        }
        else
        {
            await File.WriteAllTextAsync(_serviceViewModel.CertDomainFilePath, CertDomain2Text);

            if (!OperatingSystem.IsWindows())
                await PathHelper.FixOwnershipAsync(_serviceViewModel.CertDomainFilePath);
        }

        CloseDialog?.Invoke();
    }
}