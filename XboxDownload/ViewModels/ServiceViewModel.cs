using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Services;
using XboxDownload.Models.SpeedTest;
using XboxDownload.Services;

namespace XboxDownload.ViewModels;

public partial class ServiceViewModel : ObservableObject
{
    public readonly string ForceEncryptionDomainFilePath = PathHelper.GetLocalFilePath("ForceEncryptionDomain.json");
    public readonly string HostFilePath = PathHelper.GetLocalFilePath("Host.json");
    public readonly string AkamaiFilePath = PathHelper.GetLocalFilePath("Akamai.txt");
    public readonly string SniProxyFilePath = PathHelper.GetLocalFilePath("SniProxy.json");
    public readonly string SniProxy2FilePath = PathHelper.GetLocalFilePath("Cealing-Host.json");
    public readonly string CertDomainFilePath = PathHelper.GetLocalFilePath("CertDomainMapping.txt");

    public readonly DnsConnectionListener DnsConnectionListener;
    public readonly TcpConnectionListener TcpConnectionListener;

    public ServiceViewModel()
    {
        DnsConnectionListener = new DnsConnectionListener(this);
        TcpConnectionListener = new TcpConnectionListener(this);

        var adapters = NetworkAdapterHelper.GetValidAdapters();
        foreach (var adapter in adapters)
        {
            var ipv4 = adapter.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();

            if (string.IsNullOrEmpty(ipv4))
                continue;

            var adapt = new AdapterInfo(ipv4, adapter);
            AdapterList.Add(adapt);

            if (string.IsNullOrEmpty(App.Settings.LocalIp))
                continue;

            // If the IP matches exactly, or if no adapter is selected and the subnet prefix matches (e.g., 192.168.x.)
            if (string.Equals(App.Settings.LocalIp, ipv4) ||
                (SelectedAdapter == null && App.Settings.LocalIp.StartsWith(ipv4[..(ipv4.LastIndexOf('.') + 1)])))
            {
                SelectedAdapter = adapt;
            }
        }
        SelectedAdapter ??= AdapterList.FirstOrDefault();

        SelectedListeningIp = ListeningIpOptions.FirstOrDefault(x => x.Key == App.Settings.ListeningIp)
                              ?? (OperatingSystem.IsMacOS() ? ListeningIpOptions.LastOrDefault() : ListeningIpOptions.FirstOrDefault());

        SelectedDohServer = DohServersMappings.FirstOrDefault(s => s.Id == App.Settings.DohServerId) ?? DohServersMappings.FirstOrDefault();

        if (string.IsNullOrEmpty(LocalUploadPath)) LocalUploadPath = Path.Combine(AppContext.BaseDirectory, "Upload");

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += OnTick;
        timer.Start();

        ToggleImage();

        _ = TestIPv6();
        _ = DeliveryOptimization();
        if (DateTime.UtcNow > App.Settings.NextUpdate)
        {
            _ = UpdateService.Start(true);
        }
    }

    public bool IsIPv6Support;

    private async Task TestIPv6()
    {
        IPAddress[] ips =
        [
            IPAddress.Parse("2400:3200::1"), //Alibaba Cloud
            IPAddress.Parse("2402:4e00::"), //Tencent Cloud
            IPAddress.Parse("2001:4860:4860::8888"), //Google
            IPAddress.Parse("2606:4700:4700::1111") //Cloudflare 
        ];
        var fastestIp = await HttpClientHelper.GetFastestIp(ips, 443, 3000);
        IsIPv6Support = fastestIp != null;
        if (IsIPv6Support)
        {
            AddLog(ResourceHelper.GetString("Service.Service.NoticeTitle"), ResourceHelper.GetString("Service.Service.IPv6SupportMessage"), "System");
        }
    }

    private async Task DeliveryOptimization()
    {
        var doConfigOutput = await CommandHelper.RunCommandWithOutputAsync("powershell.exe", "Get-DOConfig");

        var dic = new Dictionary<string, string>();
        foreach (var line in doConfigOutput)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains(':'))
                continue;
            var parts = line.Split(':', 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            dic[key] = value;
        }

        var sb = new StringBuilder();
        AppendLimit("DownBackLimitBpsProvider", "DownBackLimitBps", "Service.Service.BackgroundLimit",
            v => $"{Math.Round(double.Parse(v) / 131072, 1, MidpointRounding.AwayFromZero)}Mbps");
        AppendLimit("DownloadForegroundLimitBpsProvider", "DownloadForegroundLimitBps", "Service.Service.ForegroundLimit",
            v => $"{Math.Round(double.Parse(v) / 131072, 1, MidpointRounding.AwayFromZero)}Mbps");
        AppendLimit("DownBackLimitPctProvider", "DownBackLimitPct", "Service.Service.BackgroundLimit",
            v => $"{v}%");
        AppendLimit("DownloadForegroundLimitPctProvider", "DownloadForegroundLimitPct", "Service.Service.ForegroundLimit",
            v => $"{v}%");
        if (sb.Length > 0)
        {
            AddLog(
                ResourceHelper.GetString("Service.Service.WarningTitle"),
                string.Format(ResourceHelper.GetString("Service.Service.WarningMessage"), sb),
                "System"
            );
        }
        return;

        void AppendLimit(string providerKey, string valueKey, string resourceKey, Func<string, string> format)
        {
            if (dic.TryGetValue(providerKey, out var provider) && provider == "SettingsProvider" &&
                dic.TryGetValue(valueKey, out var value))
            {
                sb.Append(ResourceHelper.GetString(resourceKey) + " " + format(value) + "，");
            }
        }
    }

    public void LanguageChanged()
    {
        foreach (var option in ListeningIpOptions)
        {
            option.Display = ResourceHelper.GetString($"Service.Service.{option.Key}");
        }

        foreach (var option in DohServersMappings)
        {
            option.Name = option.Id switch
            {
                "AlibabaCloud" or "TencentCloud" or "Qihoo360" => ResourceHelper.GetString(
                    $"Service.Service.SecureDns.{option.Id}"),
                _ => option.Name
            };
        }

        OnPropertyChanged(nameof(ListeningStatusText));
        UpdateAdapterInfo();

        DnsConnectionListener.DnsV4Query = ResourceHelper.GetString("Service.Listening.DnsV4Query");
        DnsConnectionListener.DnsV6Query = ResourceHelper.GetString("Service.Listening.DnsV6Query");
        DnsConnectionListener.DnsQueryFailed = ResourceHelper.GetString("Service.Listening.DnsQueryFailed");
    }

    #region Service

    [ObservableProperty]
    private ListeningIpOption? _selectedListeningIp;

    [ObservableProperty]
    private bool _isDnsServiceEnabled = App.Settings.IsDnsServiceEnabled, _isHttpServiceEnabled = App.Settings.IsHttpServiceEnabled,
        _isSetLocalDnsEnabled = App.Settings.IsSetLocalDnsEnabled, _isSystemSleepPrevented = App.Settings.IsSystemSleepPrevented,
        _isDoHEnabled = App.Settings.IsDoHEnabled, _isIPv6DomainFilterEnabled = App.Settings.IsIPv6DomainFilterEnabled,
        _isLocalProxyEnabled = App.Settings.IsLocalProxyEnabled, _isFastestAkamaiIp;

    partial void OnIsDoHEnabledChanged(bool value)
    {
        if (!value) return;
        IsDnsServiceEnabled = true;
    }

    partial void OnIsSetLocalDnsEnabledChanged(bool value)
    {
        if (!value) return;
        IsDnsServiceEnabled = true;
    }

    partial void OnIsLocalProxyEnabledChanged(bool value)
    {
        if (!value) return;
        IsDnsServiceEnabled = IsHttpServiceEnabled = IsSetLocalDnsEnabled = true;
    }

    [RelayCommand]
    private async Task RepairLocalDnsAsync()
    {
        var confirm = await DialogHelper.ShowConfirmDialogAsync(
            ResourceHelper.GetString("Service.Service.RepairLocalDnsDialogTitle"),
            ResourceHelper.GetString("Service.Service.RepairLocalDnsDialogMessage"),
            Icon.Question);
        if (!confirm) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                await CommandHelper.RunCommandAsync("powershell", "Get-NetAdapter -Physical | Set-DnsClientServerAddress -ResetServerAddresses");
            }
            else if (OperatingSystem.IsMacOS())
            {
                var dic = new Dictionary<string, string>(); // key: device(enX), value: service name
                var currentService = string.Empty;
                var result = await CommandHelper.RunCommandWithOutputAsync("networksetup", "-listnetworkserviceorder");
                foreach (var line in result)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        currentService = string.Empty;
                        continue;
                    }

                    if (line.StartsWith('(') && !line.Contains("Device:"))
                    {
                        currentService = line[(line.IndexOf(')') + 1)..].Trim();
                    }
                    else if (line.Contains("Device:"))
                    {
                        var idx = line.IndexOf("Device:", StringComparison.Ordinal);
                        if (idx < 0) continue;
                        var device = line.Substring(idx + "Device:".Length).Trim().TrimEnd(')');
                        dic[device] = currentService;
                    }
                }

                foreach (var adapter in AdapterList)
                {
                    if (dic.TryGetValue(adapter.Adapter.Name, out var serviceName))
                    {
                        await CommandHelper.RunCommandAsync("networksetup", $"-setdnsservers \"{serviceName}\" \"Empty\"");
                    }
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                await CommandHelper.RunCommandAsync("systemctl", "restart systemd-resolved");
            }

            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Service.Service.RepairLocalDnsSuccessTitle"),
                ResourceHelper.GetString("Service.Service.RepairLocalDnsSuccessMessage"),
                Icon.Success);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Service.Service.RepairLocalDnsFailedTitle"),
                ResourceHelper.GetString("Service.Service.RepairLocalDnsFailedMessage") + Environment.NewLine + Environment.NewLine + ex.Message,
                Icon.Error);
        }
    }

    [ObservableProperty]
    private bool _isListening, _isListeningFailed, _isDnsReady;

    public string ListeningStatusText =>
        IsListening
            ? ResourceHelper.GetString("Service.Service.StopListening")
            : ResourceHelper.GetString("Service.Service.StartListening");

    partial void OnIsListeningChanged(bool value)
    {
        OnPropertyChanged(nameof(ListeningStatusText));
        ToggleImage();
        _ = value;
    }

    partial void OnIsListeningFailedChanged(bool value)
    {
        ToggleImage();
        _ = value;
    }

    public ObservableCollection<ListeningIpOption> ListeningIpOptions { get; } = ServiceDataBuilder.GetListeningIpOptions();

    public ObservableCollection<DohServerOption> DohServersMappings { get; } = ServiceDataBuilder.GetDohServerList();

    [ObservableProperty]
    private DohServerOption? _selectedDohServer;

    partial void OnSelectedDohServerChanged(DohServerOption? oldValue, DohServerOption? newValue)
    {
        if (ReferenceEquals(oldValue, newValue) || newValue == null) return;

        var useProxy = App.Settings.DohServerUseProxyId.Contains(newValue.Id) && !newValue.IsProxyDisabled;
        DnsHelper.CurrentDoH = DnsHelper.GetConfigureDoH(newValue.Url, newValue.Ip, useProxy);

        if (string.Equals(App.Settings.DohServerId, newValue.Id)) return;
        App.Settings.DohServerId = newValue.Id;
        SettingsManager.Save(App.Settings);
    }

    public event Action<string>? RequestFocus;

    [RelayCommand]
    public void FocusText(string targetName)
    {
        RequestFocus?.Invoke(targetName);
    }

    private CancellationTokenSource? _listeningCts;
    public CancellationToken ListeningToken => _listeningCts?.Token ?? CancellationToken.None;

    [RelayCommand]
    public async Task ToggleListeningAsync()
    {
        if (IsListening)
        {
            IsListening = IsListeningFailed = IsDnsReady = IsFastestAkamaiIp = false;

            DnsIp = App.Settings.DnsIp;
            XboxGlobalIp = App.Settings.XboxGlobalIp;
            XboxCn1Ip = App.Settings.XboxCn1Ip;
            XboxCn2Ip = App.Settings.XboxCn2Ip;
            XboxAppIp = App.Settings.XboxAppIp;
            PsIp = App.Settings.PsIp;
            NsIp = App.Settings.NsIp;
            EaIp = App.Settings.EaIp;
            BattleIp = App.Settings.BattleIp;
            EpicIp = App.Settings.EpicIp;
            UbisoftIp = App.Settings.UbisoftIp;

            _ = HostsHelper.ApplySystemHostsAsync();
            if (IsDnsServiceEnabled) _ = DnsConnectionListener.StopAsync();
            if (IsHttpServiceEnabled) TcpConnectionListener.Stop();

            SystemSleepHelper.RestoreSleep();

            if (Interlocked.Exchange(ref _listeningCts, null) is { } cts)
            {
                await cts.CancelAsync();
                cts.Dispose();
            }
        }
        else
        {
            var fieldNames = new[]
            {
                nameof(DnsIp), nameof(XboxGlobalIp), nameof(XboxCn1Ip), nameof(XboxCn2Ip), nameof(XboxAppIp),
                nameof(PsIp), nameof(NsIp), nameof(EaIp), nameof(BattleIp), nameof(EpicIp), nameof(UbisoftIp)
            };

            foreach (var fieldName in fieldNames)
            {
                var property = GetType().GetProperty(fieldName);
                if (property is null || property.PropertyType != typeof(string)) continue;

                var ipValue = property.GetValue(this) as string;
                if (string.IsNullOrWhiteSpace(ipValue)) continue;

                if (IPAddress.TryParse(ipValue.Trim(), out var ipAddress))
                {
                    // Format and assign the value back to the property
                    property.SetValue(this, ipAddress.ToString());
                    continue;
                }

                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Service.Service.InvalidIpTitle"),
                    ResourceHelper.GetString($"Service.Service.Invalid{fieldName}Message"),
                    Icon.Error);

                FocusText(fieldName);
                return;
            }

            LocalUploadPath = LocalUploadPath.Trim().TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(LocalUploadPath)) LocalUploadPath = Path.Combine(AppContext.BaseDirectory, "Upload");

            App.Settings.DnsIp = DnsIp.Trim();
            App.Settings.XboxGlobalIp = XboxGlobalIp.Trim();
            App.Settings.XboxCn1Ip = XboxCn1Ip.Trim();
            App.Settings.XboxCn2Ip = XboxCn2Ip.Trim();
            App.Settings.XboxAppIp = XboxAppIp.Trim();
            App.Settings.PsIp = PsIp.Trim();
            App.Settings.NsIp = NsIp.Trim();
            App.Settings.EaIp = EaIp.Trim();
            App.Settings.BattleIp = BattleIp.Trim();
            App.Settings.EpicIp = EpicIp.Trim();
            App.Settings.UbisoftIp = UbisoftIp.Trim();
            App.Settings.IsXboxGameDownloadLinksShown = IsXboxGameDownloadLinksShown;
            App.Settings.IsLocalUploadEnabled = IsLocalUploadEnabled;
            App.Settings.LocalUploadPath = LocalUploadPath.Equals(Path.Combine(AppContext.BaseDirectory, "Upload")) ? string.Empty : LocalUploadPath;
            App.Settings.ListeningIp = SelectedListeningIp!.Key;
            App.Settings.IsDnsServiceEnabled = IsDnsServiceEnabled;
            App.Settings.IsHttpServiceEnabled = IsHttpServiceEnabled;
            App.Settings.IsSystemSleepPrevented = IsSystemSleepPrevented;
            App.Settings.IsSetLocalDnsEnabled = IsSetLocalDnsEnabled;
            App.Settings.IsDoHEnabled = IsDoHEnabled;
            App.Settings.IsIPv6DomainFilterEnabled = IsIPv6DomainFilterEnabled;
            App.Settings.IsLocalProxyEnabled = IsLocalProxyEnabled;
            App.Settings.LocalIp = SelectedAdapter!.Ip;
            SettingsManager.Save(App.Settings);

            IsListening = true;
            _listeningCts = new CancellationTokenSource();

            if (OperatingSystem.IsWindows())
            {
                FirewallHelper.EnsureFirewallRule(nameof(XboxDownload));
                await PortConflictHelper.CheckAndHandlePortConflictAsync(IsDnsServiceEnabled, IsHttpServiceEnabled);
            }

            if (IsSystemSleepPrevented) SystemSleepHelper.PreventSleep(false);

            await TcpConnectionListener.GenerateServerCertificate();

            string errDnsMessage = string.Empty, errTcpMessage = string.Empty;
            var tasks = new List<Task>();
            if (IsDnsServiceEnabled)
            {
                tasks.Add(Task.Run(async () =>
                {
                    errDnsMessage = await DnsConnectionListener.StartAsync();
                }, ListeningToken));
            }
            if (IsHttpServiceEnabled)
            {
                tasks.Add(Task.Run(() =>
                {
                    errTcpMessage = TcpConnectionListener.Start();
                }, ListeningToken));
            }
            await Task.WhenAll(tasks);

            if (IsListeningFailed)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Service.Service.ServiceEnableFailedDialogTitle"),
                    string.Format(
                        ResourceHelper.GetString("Service.Service.ServiceEnableFailedDialogMessage"),
                        (errDnsMessage + Environment.NewLine + Environment.NewLine + errTcpMessage).Trim()),
                    Icon.Error);
            }

            await HostsHelper.ApplySystemHostsAsync(true);
        }
        if (IsSetLocalDnsEnabled) CommandHelper.FlushDns();
    }

    private readonly ConcurrentDictionary<string, string> _backupIps = new();

    [RelayCommand]
    private async Task FastestAkamaiIpAsync()
    {
        if (!IsListening) return;
        if (IsFastestAkamaiIp)
        {
            var filePath = PathHelper.GetResourceFilePath("IP.AkamaiV2.txt");
            var fi = new FileInfo(filePath);
            if (!fi.Exists || fi.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7))
            {
                await UpdateService.DownloadIpAsync(fi);
            }

            // ReSharper disable once MethodSupportsCancellation
            var content = fi.Exists ? await File.ReadAllTextAsync(fi.FullName) : string.Empty;
            var matches = RegexHelper.ExtractIpv4AndLocation().Matches(content);
            var items = matches.Where(m => m.Success).Select(m => new IpItem
            {
                Ip = m.Groups["IP"].Value,
                Location = m.Groups["Location"].Value
            }).ToList();

            if (items.Count == 0)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Service.Service.FastestAkamaiIpFailedDialogTitle"),
                    ResourceHelper.GetString("Service.Service.FastestAkamaiIpFailedDialogMessage"),
                    Icon.Error);
                return;
            }

            if (App.Settings.Culture != "zh-Hans")
            {
                await Ioc.Default.GetRequiredService<SpeedTestViewModel>().TranslationLocation(items);
            }

            _backupIps.Clear();
            _backupIps.TryAdd("XboxGlobalIp", XboxGlobalIp);
            _backupIps.TryAdd("XboxCn1Ip", XboxCn1Ip);
            _backupIps.TryAdd("XboxCn2Ip", XboxCn2Ip);
            _backupIps.TryAdd("XboxAppIp", XboxAppIp);
            _backupIps.TryAdd("PsIp", PsIp);
            _backupIps.TryAdd("NsIp", NsIp);
            _backupIps.TryAdd("EaIp", EaIp);
            _backupIps.TryAdd("BattleIp", BattleIp);
            _backupIps.TryAdd("EpicIp", EpicIp);
            _backupIps.TryAdd("UbisoftIp", UbisoftIp);

            var bestIp = await SpeedTestService.FindFastestOrBestAkamaiIpAsync(items, ListeningToken);
            if (IsListening)
            {
                if (bestIp != null)
                {
                    XboxGlobalIp = XboxCn1Ip = XboxCn2Ip = XboxAppIp = PsIp = NsIp = EaIp = BattleIp = EpicIp = UbisoftIp = bestIp.Ip;
                    DnsConnectionListener.ApplyAkamaiIpOverride(bestIp.Ip);
                    await DnsConnectionListener.LoadHostAndAkamaiMapAsync(bestIp.Ip);
                    await HostsHelper.ApplySystemHostsAsync(true);
                    if (IsLogging)
                        AddLog(ResourceHelper.GetString("Service.Service.NoticeTitle"), $"{ResourceHelper.GetString("Service.Service.FastestAkamaiIp")} -> {bestIp.Ip} ({bestIp.Location})", "System");
                }
                else
                {
                    IsFastestAkamaiIp = false;
                    if (IsLogging)
                        AddLog(ResourceHelper.GetString("Service.Service.NoticeTitle"), $"{ResourceHelper.GetString("Service.Service.FastestAkamaiIp")} {ResourceHelper.GetString("Service.Service.FastestAkamaiIpNetworkFailed")}", "System");
                }
            }
        }
        else
        {
            XboxGlobalIp = _backupIps.GetValueOrDefault("XboxGlobalIp") ?? "";
            XboxCn1Ip = _backupIps.GetValueOrDefault("XboxCn1Ip") ?? "";
            XboxCn2Ip = _backupIps.GetValueOrDefault("XboxCn2Ip") ?? "";
            XboxAppIp = _backupIps.GetValueOrDefault("XboxAppIp") ?? "";
            PsIp = _backupIps.GetValueOrDefault("PsIp") ?? "";
            NsIp = _backupIps.GetValueOrDefault("NsIp") ?? "";
            EaIp = _backupIps.GetValueOrDefault("EaIp") ?? "";
            BattleIp = _backupIps.GetValueOrDefault("BattleIp") ?? "";
            EpicIp = _backupIps.GetValueOrDefault("EpicIp") ?? "";
            UbisoftIp = _backupIps.GetValueOrDefault("UbisoftIp") ?? "";
            DnsConnectionListener.ApplyAkamaiIpOverride();
            await DnsConnectionListener.LoadHostAndAkamaiMapAsync();
            await HostsHelper.ApplySystemHostsAsync(true);
        }
        if (IsSetLocalDnsEnabled) CommandHelper.FlushDns();
    }

    #endregion

    #region Settings

    [ObservableProperty]
    private string
        _xboxGlobalDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("XboxGlobal")),
        _xboxCn1DlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("XboxCn1")),
        _xboxCn2DlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("XboxCn2")),
        _xboxAppDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("XboxApp")),
        _psDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Ps")),
        _nsDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Ns")),
        _eaDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Ea")),
        _battleDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Battle")),
        _epicDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Epic")) +
                       Environment.NewLine +
                       string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("EpicCn")),
        _ubisoftDlIpTip = string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("Ubisoft")) +
                          Environment.NewLine +
                          string.Join(Environment.NewLine, DnsMappingGenerator.GenerateHostList("UbisoftCn"));

    [ObservableProperty]
    private string _dnsIp = App.Settings.DnsIp, _xboxGlobalIp = App.Settings.XboxGlobalIp, _xboxCn1Ip = App.Settings.XboxCn1Ip, _xboxCn2Ip = App.Settings.XboxCn2Ip, _xboxAppIp = App.Settings.XboxAppIp,
        _psIp = App.Settings.PsIp, _nsIp = App.Settings.NsIp, _eaIp = App.Settings.EaIp, _battleIp = App.Settings.BattleIp, _epicIp = App.Settings.EpicIp, _ubisoftIp = App.Settings.UbisoftIp,
        _localUploadPath = App.Settings.LocalUploadPath;

    [ObservableProperty]
    private bool _isXboxGameDownloadLinksShown = App.Settings.IsXboxGameDownloadLinksShown, _isLocalUploadEnabled = App.Settings.IsLocalUploadEnabled;

    partial void OnIsXboxGameDownloadLinksShownChanged(bool value)
    {
        if (value)
            IsHttpServiceEnabled = true;
        else
            IsLocalUploadEnabled = false;
    }

    partial void OnIsLocalUploadEnabledChanged(bool value)
    {
        if (!value) return;
        IsXboxGameDownloadLinksShown = IsHttpServiceEnabled = true;
    }

    private Bitmap? _statusImage;
    public Bitmap? StatusImage
    {
        get => _statusImage;
        set => SetProperty(ref _statusImage, value);
    }

    private readonly ConcurrentDictionary<string, Bitmap> _imageCache = new();

    private void ToggleImage()
    {
        var assetName = !IsListening
            ? "xbox1.png"
            : !IsListeningFailed ? "xbox2.png" : "xbox3.png";

        var bitmap = _imageCache.GetOrAdd(assetName, key =>
        {
            var uri = new Uri($"avares://{nameof(XboxDownload)}/Assets/{key}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        });

        StatusImage = bitmap;
    }

    #endregion

    #region Logs

    [ObservableProperty]
    private bool _isLogging = App.Settings.IsLogging;

    partial void OnIsLoggingChanged(bool value)
    {
        App.Settings.IsLogging = value;
        SettingsManager.Save(App.Settings);
    }

    public ObservableCollection<ServiceModels> Logs { get; } = [];

    public void AddLog(string method, string content, string ip)
    {
        if (!IsLogging) return;

        var item = new ServiceModels(method, content, ip);
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Insert(0, item);
            if (Logs.Count > 3_000)
                Logs.RemoveAt(Logs.Count - 1);
        });
    }

    [RelayCommand]
    private static async Task CopyContentAsync(ServiceModels? log)
    {
        if (log == null)
            return;

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(log.Content);
        }
    }

    [RelayCommand]
    private async Task ExportAllLogsAsync()
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null)
            return;

        var options = new FilePickerSaveOptions
        {
            Title = ResourceHelper.GetString("Service.Logs.SaveLogFile"),
            FileTypeChoices =
            [
                new FilePickerFileType(ResourceHelper.GetString("Service.Logs.TxtFile"))
                {
                    Patterns = ["*.txt"]
                }
            ],
            SuggestedFileName = "logs.txt"
        };

        var result = await topLevel.StorageProvider.SaveFilePickerAsync(options);

        if (result == null)
            return;

        try
        {
            await using var stream = await result.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);

            var copiedLogs = new ObservableCollection<ServiceModels>(Logs);
            foreach (var log in copiedLogs)
            {
                await writer.WriteLineAsync($"{log.RequestMethod} | {log.Content} | {log.ClientIp} | {log.TimestampFormatted}");
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Service.Logs.ExportLogFailed"),
                ResourceHelper.GetString("Service.Logs.ExportLogFailedMsg") + Environment.NewLine + Environment.NewLine + ex.Message,
                Icon.Error);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }

    #endregion

    #region Footer

    public ObservableCollection<AdapterInfo> AdapterList { get; } = [];

    [ObservableProperty] private AdapterInfo? _selectedAdapter;

    [ObservableProperty]
    private string _traffic = string.Empty, _adapterInfo = string.Empty;

    partial void OnSelectedAdapterChanged(AdapterInfo? oldValue, AdapterInfo? newValue)
    {
        if (ReferenceEquals(oldValue, newValue) || SelectedAdapter == null) return;

        Traffic = $" ↑ {UnitConverter.ConvertBps(0)} ↓ {UnitConverter.ConvertBps(0)}";
        OldUp = SelectedAdapter.Adapter.GetIPStatistics().BytesSent;
        OldDown = SelectedAdapter.Adapter.GetIPStatistics().BytesReceived;

        UpdateAdapterInfo();
    }

    private void UpdateAdapterInfo()
    {
        if (SelectedAdapter == null) return;

        AdapterInfo = string.Format(ResourceHelper.GetString("Service.Bottom.AdapterInfo"),
            SelectedAdapter.Adapter.Name,
            SelectedAdapter.Adapter.Description,
            SelectedAdapter.Adapter.NetworkInterfaceType,
            UnitConverter.ConvertBps(SelectedAdapter.Adapter.Speed)); // Retrieving accurate adapter speed is not supported on Linux/macOS platforms

        if (!OperatingSystem.IsWindows())
        {
            AdapterInfo = AdapterInfo[..AdapterInfo.LastIndexOf('\n')];
        }
    }

    private long OldUp { get; set; }
    private long OldDown { get; set; }

    private void OnTick(object? sender, EventArgs e)
    {
        if (SelectedAdapter?.Adapter == null) return;

        var nowUp = SelectedAdapter.Adapter.GetIPStatistics().BytesSent;
        var nowDown = SelectedAdapter.Adapter.GetIPStatistics().BytesReceived;

        if (OldUp > 0 || OldDown > 0)
        {
            var up = nowUp - OldUp;
            var down = nowDown - OldDown;

            Traffic = $" ↑ {UnitConverter.ConvertBps(up * 8)} ↓ {UnitConverter.ConvertBps(down * 8)}";
        }

        OldUp = nowUp;
        OldDown = nowDown;
    }

    #endregion
}