using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.SpeedTest;
using XboxDownload.Services;

namespace XboxDownload.ViewModels;

public partial class SpeedTestViewModel : ViewModelBase
{
    private readonly string _translationPath = PathHelper.GetResourceFilePath("Translation.json");

    public List<int> TimeoutOptions { get; } = [3, 5, 10];

    [ObservableProperty]
    private int _selectedTimeout = 3;

    [ObservableProperty]
    private string _headerText, _searchLocation = App.Settings.SearchLocation;

    public ObservableCollection<LocationFilter> LocationFilters { get; } = new(SpeedTestDataBuilder.BuildLocationFilters());

    public ObservableCollection<ImportOption> ImportOptions { get; } = new(SpeedTestDataBuilder.BuildImportOptions());

    public ObservableCollection<SpeedTestFile> SpeedTestFiles { get; } = new(SpeedTestDataBuilder.BuildSpeedTestFiles());

    [ObservableProperty]
    private ImportOption? _selectedImportOption;

    public SpeedTestViewModel()
    {
        SelectedImportOption = ImportOptions.FirstOrDefault();
        HeaderText = SelectedImportOption!.Display;

        foreach (var option in LocationFilters)
        {
            option.IsVisible = App.Settings.Culture == "zh-Hans"
                ? option.Key is "ChinaTelecom" or "ChinaUnicom" or "ChinaMobile"
                : option.Key is not ("ChinaTelecom" or "ChinaUnicom" or "ChinaMobile");
        }

        foreach (var option in LocationFilters)
        {
            option.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(LocationFilter.IsSelected)) return;
                if (s is LocationFilter changedOption)
                {
                    ApplyLocationFilter(changedOption.Key);
                }
            };
        }

        WatermarkText = ImportOptions.FirstOrDefault()!.Hint;

        _sortSubject.Select(sortList => IpSource.Connect().Sort(GetMultiComparer(sortList))).Switch().Bind(out _ipItems).Subscribe();
    }

    [ObservableProperty]
    private bool _isSortingEnabled = true, _isImportEnabled = true, _isSpeedTest;

    private readonly ReadOnlyObservableCollection<IpItem> _ipItems;

    public ReadOnlyObservableCollection<IpItem> IpItems => _ipItems;

    partial void OnSelectedImportOptionChanged(ImportOption? value)
    {
        if (value == null || string.IsNullOrEmpty(value.Key)) return;

        LoadIpCommand.Execute(null);
    }

    [GeneratedRegex(
        @"(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*\((?<Location>[^\)]*)\)|^[^\d]+(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})(?<Location>[\u4e00-\u9fa5]+).+ms\d+|\s*(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s*$|(?<IP>\d{0,3}\.\d{0,3}\.\d{0,3}\.\d{0,3})\s+(?<Location>[^\s]+)\s+\<?\d+ms|(?<IP>([\da-fA-F]{1,4}:){3}([\da-fA-F]{0,4}:)+[\da-fA-F]{1,4})\s*\((?<Location>[^\)]*)\)",
        RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MatchIp();

    [ObservableProperty]
    private bool _isXboxCn1Visible, _isXboxCn2Visible, _isXboxAppVisible, _isXboxSeparatorVisible, _isPsVisible, _isPsSeparatorVisible, _isUbisoftVisible, _isAkamaiVisible, _isUbisoftSeparatorVisible;

    private string _selectedImportkey = string.Empty;
    [RelayCommand]
    private async Task LoadIpAsync()
    {
        if (string.IsNullOrEmpty(SelectedImportOption?.Target)) return;
        _selectedImportkey = SelectedImportOption.Key;

        CancelFetchAppDownloadUrl();
        ClearSort();
        IpSource.Clear();
        HeaderText = SelectedImportOption.Display;
        IsImportEnabled = false;
        TargetTestUrl = "";
        WatermarkText = SelectedImportOption.Hint;

        var filePath = PathHelper.GetResourceFilePath($"IP.{SelectedImportOption.Key}.txt");
        var fi = new FileInfo(filePath);
        if (!fi.Exists || fi.LastAccessTimeUtc < DateTime.UtcNow.AddHours(-24))
        {
            await UpdateService.DownloadIpAsync(fi);
        }
        var content = fi.Exists ? await File.ReadAllTextAsync(fi.FullName) : string.Empty;
        var items = MatchIp().Matches(content)
            .Where(m => m.Success)
            .Select(m => new IpItem
            {
                Ip = m.Groups["IP"].Value,
                Location = m.Groups["Location"].Value
            })
            .ToList();

        var isAkamai = SelectedImportOption.Key.StartsWith("Akamai");

        if (isAkamai && App.Settings.Culture != "zh-Hans" || App.Settings.Culture == "zh-Hant")
        {
            await TranslationLocation(items);
        }

        IpSource.AddRange(items);

        foreach (var option in LocationFilters)
        {
            option.IsVisible = isAkamai
                ? option.Key is not ("ChinaTelecom" or "ChinaUnicom" or "ChinaMobile")
                : option.Key is "ChinaTelecom" or "ChinaUnicom" or "ChinaMobile";
        }

        IsXboxCn1Visible = IsXboxCn2Visible = IsXboxAppVisible = IsXboxSeparatorVisible = IsPsVisible = IsPsSeparatorVisible = IsUbisoftVisible = IsUbisoftSeparatorVisible = IsAkamaiVisible = UploadAkamaiIpsVisible = false;
        switch (SelectedImportOption.Key)
        {
            case "XboxCn1":
                IsXboxCn1Visible = IsXboxSeparatorVisible = true;
                break;
            case "XboxCn2":
                IsXboxCn2Visible = IsXboxSeparatorVisible = true;
                break;
            case "XboxApp":
                IsXboxAppVisible = IsXboxSeparatorVisible = true;
                break;
            case "Ps":
                IsPsVisible = IsPsSeparatorVisible = true;
                break;
            case "Akamai":
            case "AkamaiV2":
            case "AkamaiV6":
                IsAkamaiVisible = IsXboxAppVisible = IsXboxSeparatorVisible = IsPsVisible = IsPsSeparatorVisible = IsUbisoftVisible = IsUbisoftSeparatorVisible = true;
                if (App.Settings.Culture == "zh-Hans") UploadAkamaiIpsVisible = true;
                break;
            case "UbisoftCn":
                IsUbisoftVisible = IsUbisoftSeparatorVisible = true;
                break;
        }

        ApplyLocationFilter();
        DownloadFileFilter(SelectedImportOption.Target);
        IsImportEnabled = true;
    }

    public async Task TranslationLocation(List<IpItem> items)
    {
        if (App.Settings.Culture == "zh-Hant")
        {
            foreach (var ip in items)
            {
                var location = ChineseConverter.SimplifiedToTraditional(ip.Location);
                if (!string.IsNullOrEmpty(location))
                    ip.Location = location;
            }
        }
        else
        {
            var translationFile = new FileInfo(_translationPath);
            if (!translationFile.Exists || translationFile.LastAccessTimeUtc < DateTime.UtcNow.AddDays(-7))
            {
                await UpdateService.DownloadIpAsync(translationFile, "{");
            }
            if (translationFile.Exists)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_translationPath);
                    var translation = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                    if (translation is { Count: > 0 })
                    {
                        foreach (var ip in items)
                        {
                            var location = translation?.GetValueOrDefault(ip.Location);
                            if (!string.IsNullOrEmpty(location))
                                ip.Location = location;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    [ObservableProperty]
    private IpItem? _selectedItem;

    [ObservableProperty]
    private bool _uploadAkamaiIpsVisible, _uploadAkamaiIpsEnabled = App.Settings.UploadAkamaiIpsEnabled;

    partial void OnUploadAkamaiIpsEnabledChanged(bool value)
    {
        App.Settings.UploadAkamaiIpsEnabled = value;
        SettingsManager.Save(App.Settings);
    }

    [RelayCommand]
    private void FilterByLocation()
    {
        SearchLocation = SearchLocation.Replace("　", " ").Trim();
        if (!string.Equals(App.Settings.SearchLocation, SearchLocation))
        {
            App.Settings.SearchLocation = SearchLocation;
            SettingsManager.Save(App.Settings);
        }

        var keywords = SearchLocation.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(k => k.Trim()).ToList();

        if (keywords.Count == 0)
        {
            return;
        }

        foreach (var item in IpItems)
        {
            var match = keywords.All(k => item.Location.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (match) item.IsSelected = true;
            item.IsFilterMatched = match;
        }

        if (!IpItems.Any(m => m.IsFilterMatched)) return;
        ClearSort();
        ApplySort("IsFilterMatched", SortDirection.Descending);
    }

    [RelayCommand]
    private void ToggleSelectAll(string isSelect)
    {
        var b = bool.Parse(isSelect);
        foreach (var item in LocationFilters)
        {
            item.IsSelected = b;
        }
        foreach (var ipItem in IpItems)
        {
            ipItem.IsSelected = b;
        }
    }

    [RelayCommand]
    private void ApplyLocationFilter(string? region = null)
    {
        // 全量刷新：region 为 null 或空
        if (string.IsNullOrEmpty(region))
        {
            var isOtherSelected = LocationFilters.FirstOrDefault(f => f.Key == "Other")?.IsSelected == true;
            var knownFilters = LocationFilters
                .Where(f => f is { IsVisible: true, IsSelected: true } && f.Key != "Other")
                .ToList();

            var knownKeywords = isOtherSelected
                ? LocationFilters.Where(f => f.IsVisible && f.Key != "Other").SelectMany(f => f.Keywords).ToList()
                : null;

            foreach (var ip in IpItems)
            {
                // 如果命中已选中的已知地区关键词
                ip.IsSelected = knownFilters.Any(f =>
                    f.Keywords.Any(kw => ip.Location.Contains(kw, StringComparison.OrdinalIgnoreCase)));

                // 如果是 Other 筛选器，并且没命中已知关键词，则设置为 true
                if (!ip.IsSelected && isOtherSelected && knownKeywords != null)
                {
                    ip.IsSelected = !knownKeywords.Any(kw => ip.Location.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }
            }
            return;
        }

        // 局部更新：只更新特定区域
        var filter = LocationFilters.FirstOrDefault(f => f.IsVisible && f.Key == region);
        if (filter == null) return;

        if (filter.Key == "Other")
        {
            var knownKeywords = LocationFilters
                .Where(f => f.IsVisible && f.Key != "Other")
                .SelectMany(f => f.Keywords)
                .ToList();

            foreach (var ip in IpItems)
            {
                var matchesKnown = knownKeywords.Any(kw => ip.Location.Contains(kw, StringComparison.OrdinalIgnoreCase));
                if (!matchesKnown)
                    ip.IsSelected = filter.IsSelected;
            }
        }
        else
        {
            foreach (var ip in IpItems)
            {
                if (filter.Keywords.Any(kw => ip.Location.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                {
                    ip.IsSelected = filter.IsSelected;
                }
            }
        }
    }

    private void DownloadFileFilter(string target)
    {
        foreach (var option in SpeedTestFiles)
        {
            option.IsVisible = option.Target == target;
        }
    }

    [ObservableProperty]
    private string _targetTestUrl = "", _watermarkText = "";

    [RelayCommand]
    private async Task FileClickedAsync(object? parameter)
    {
        if (parameter is not SpeedTestFile file) return;
        if (!string.IsNullOrEmpty(file.Url) && file.Url.StartsWith("http"))
        {
            TargetTestUrl = file.Url;
        }
        else
        {
            CancelFetchAppDownloadUrl();
            _fetchAppDownloadUrlCancellation = new CancellationTokenSource();
            var fetchToken = _fetchAppDownloadUrlCancellation.Token;
            TargetTestUrl = ResourceHelper.GetString("SpeedTest.GettingDownloadLink");
            TargetTestUrl = await UpdateService.FetchAppDownloadUrlAsync(file.Url, fetchToken);
        }
    }

    #region MenuItem

    [RelayCommand]
    private Task ExportDnsmasqAsync(Visual? visual) => ExportRulesAsync(visual, _selectedImportkey, SelectedItem?.Ip, "dnsmasq");

    [RelayCommand]
    private Task ExportHostsAsync(Visual? visual) => ExportRulesAsync(visual, _selectedImportkey, SelectedItem?.Ip, "hosts");

    private static async Task ExportRulesAsync(Visual? visual, string key, string? ip, string exportFormat)
    {
        if (visual == null || ip == null) return;

        var clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
        if (clipboard == null) return;

        var exportContent = DnsMappingGenerator.GenerateDnsMapping(key, ip, exportFormat, "Export");

        await clipboard.SetTextAsync(exportContent);

        var lines = exportContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var count = lines.Count(line => !line.StartsWith('#')); // 排除以 # 开头的行（忽略前导空格）
        
        var cnXboxDownloadHint  = App.Settings.Culture == "zh-Hans" && exportContent.Contains(".xboxlive.cn") ?
            $"{Environment.NewLine}提示：{Environment.NewLine}国内 Xbox 下载游戏可能会使用 .com 域名，{Environment.NewLine}此时需启用监听跳转方式才能加速下载。" :
            "";

        await DialogHelper.ShowInfoDialogAsync(
            ResourceHelper.GetString("SpeedTest.MenuItem.ExportRules"),
            string.Format(ResourceHelper.GetString("SpeedTest.MenuItem.RulesCopiedToClipboard"), count) + Environment.NewLine + Environment.NewLine + exportContent + cnXboxDownloadHint,
            Icon.Success);
    }

    [RelayCommand]
    private async Task CustomIpAsync(string parameter)
    {
        var ip = SelectedItem?.Ip;
        if (ip == null) return;

        var serviceVm = Ioc.Default.GetRequiredService<ServiceViewModel>();
        if (serviceVm.IsListening && parameter != "Akamai")
            await serviceVm.ToggleListeningAsync();

        var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        const int tabIndex = 0;
        switch (parameter)
        {
            case "XboxGlobal":
                serviceVm.XboxGlobalIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "XboxCn1":
                serviceVm.XboxCn1Ip = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "XboxCn2":
                serviceVm.XboxCn2Ip = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "XboxApp":
                serviceVm.XboxAppIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "XboxGlobalCnApps":
                serviceVm.XboxGlobalIp = ip;
                serviceVm.XboxCn1Ip = ip;
                serviceVm.XboxCn2Ip = ip;
                serviceVm.XboxAppIp = ip;
                serviceVm.FocusText("XboxGlobalIp");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Ps":
                serviceVm.PsIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Ns":
                serviceVm.NsIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Ea":
                serviceVm.EaIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Battle":
                serviceVm.BattleIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Epic":
                serviceVm.EpicIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Ubisoft":
                serviceVm.UbisoftIp = ip;
                serviceVm.FocusText($"{parameter}Ip");
                mainWindowVm.SelectedTabIndex = tabIndex;
                break;
            case "Akamai":
                var cdnVm = Ioc.Default.GetRequiredService<CdnViewModel>();
                cdnVm.AkamaiCdnIp = ip;
                cdnVm.FocusText();
                mainWindowVm.SelectedTabIndex = 3;
                break;
        }
    }

    [RelayCommand]
    private async Task CopyIpAsync(Visual? visual)
    {
        if (visual == null) return;

        var clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
        if (clipboard == null) return;

        var ip = SelectedItem?.Ip;
        if (string.IsNullOrEmpty(ip)) return;
        await clipboard.SetTextAsync(ip);
    }

    #endregion

    #region Host

    [RelayCommand]
    private async Task WriteHostsAsync()
    {
        var ip = SelectedItem?.Ip;
        if (string.IsNullOrEmpty(ip)) return;

        FileInfo fi = new(PathHelper.SystemHostsPath);

        try
        {
            string content;
            await using (var fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                using StreamReader sr = new(fs);
                content = await sr.ReadToEndAsync();
            }

            var patterns = DnsMappingGenerator.GenerateHostRegexPattern(_selectedImportkey);

            content = Regex.Replace(content, patterns, "");

            var dnsMapping = DnsMappingGenerator.GenerateDnsMapping(_selectedImportkey, ip, "hosts", "Write");

            content = content.Trim() + Environment.NewLine + dnsMapping;

            if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                fi.Attributes &= ~FileAttributes.ReadOnly;

            await using (var fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await using var sw = new StreamWriter(fs);
                await sw.WriteAsync(content);
            }

            var count = dnsMapping
                .Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                .Count(line => !string.IsNullOrWhiteSpace(line));

            var cnXboxDownloadHint  = App.Settings.Culture == "zh-Hans" && dnsMapping.Contains(".xboxlive.cn") ?
                $"{Environment.NewLine}提示：{Environment.NewLine}国内 Xbox 下载游戏可能会使用 .com 域名，{Environment.NewLine}此时需启用监听跳转方式才能加速下载。" :
                "";

            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileSuccessful"),
                string.Format(ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileSuccessfulMsg"), count) + Environment.NewLine + Environment.NewLine + dnsMapping + cnXboxDownloadHint,
                Icon.Success);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileFailed"),
                ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileFailedMsg") + Environment.NewLine + Environment.NewLine + ex.Message,
                Icon.Error);
        }
    }

    [RelayCommand]
    private static async Task ClearHostsAsync()
    {
        try
        {
            var content = await File.ReadAllTextAsync(PathHelper.SystemHostsPath);

            // 清理非正常退出残留内容
            if (!Ioc.Default.GetRequiredService<ServiceViewModel>().IsListening && HostsHelper.RemoveAppSectionRegex().IsMatch(content))
            {
                content = HostsHelper.RemoveAppSectionRegex().Replace(content, "").Trim();
                await File.WriteAllTextAsync(PathHelper.SystemHostsPath, content);
            }

            // 拆分为行
            var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            // 找出将被删除的行
            var removedLines = lines.Where(line => line.Contains($"# {nameof(XboxDownload)}")).ToList();

            if (removedLines.Count == 0)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("SpeedTest.Dialog.ConfirmCleanupHostsTitle"),
                    string.Format(ResourceHelper.GetString("SpeedTest.Dialog.NothingToCleanHosts"), nameof(XboxDownload)),
                    Icon.Info);
                return;
            }

            // 提示确认
            var preview = string.Join(Environment.NewLine, removedLines);
            var confirm = await DialogHelper.ShowConfirmDialogAsync(
                ResourceHelper.GetString("SpeedTest.Dialog.ConfirmCleanupHostsTitle", nameof(XboxDownload)),
                string.Format(ResourceHelper.GetString("SpeedTest.Dialog.ConfirmDeletionHosts"), removedLines.Count, nameof(XboxDownload)) + Environment.NewLine + Environment.NewLine + preview,
                Icon.Question);
            if (!confirm) return;

            // 保留其余内容
            var cleaned = string.Join(Environment.NewLine, lines.Where(line => !line.Contains($"# {nameof(XboxDownload)}")));

            // 写回 hosts 文件
            await File.WriteAllTextAsync(PathHelper.SystemHostsPath, cleaned);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("SpeedTest.Dialog.WriteToSystemHostsFileFailed"),
                ResourceHelper.GetString("SpeedTest.Dialog.WriteToSystemHostsFileFailedMsg") + Environment.NewLine + Environment.NewLine + ex.Message,
                Icon.Error);
        }
    }

    #endregion

    #region Sorting

    public Action? ClearSortRequested { get; set; }

    public readonly SourceList<IpItem> IpSource = new();

    private readonly BehaviorSubject<List<(string Property, SortDirection Direction)>> _sortSubject = new([("Location", SortDirection.Ascending)]);

    private static readonly Dictionary<string, Func<IpItem, IComparable?>> PropertySelector = new()
    {
        ["IsSelected"] = p => p.IsSelected,
        ["Ip"] = p => p.Ip,
        ["Location"] = p => p.Location,
        ["Ttl"] = p => p.Ttl ?? int.MinValue,
        ["RoundtripTime"] = p => p.RoundtripTime ?? int.MinValue,
        ["Speed"] = p => p.Speed ?? double.MinValue,
        ["IsFilterMatched"] = p => p.IsFilterMatched
    };

    // 自定义多字段比较器
    private static MultiKeyComparer<IpItem> GetMultiComparer(List<(string Property, SortDirection Direction)> sortList)
    {
        var keys = new List<(Func<IpItem, IComparable?> Selector, SortDirection Direction)>();

        foreach (var (property, direction) in sortList)
        {
            if (PropertySelector.TryGetValue(property, out var selector))
                keys.Add((selector, direction));
        }

        if (keys.Count == 0)
        {
            // 默认排序规则
            keys.Add((p => p.Location, SortDirection.Ascending));
        }

        return new MultiKeyComparer<IpItem>(keys);
    }

    // 多字段比较器实现
    private class MultiKeyComparer<T>(List<(Func<T, IComparable?> Selector, SortDirection Direction)> keys) : IComparer<T>
    {
        public int Compare(T? x, T? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            foreach (var (selector, direction) in keys)
            {
                var a = selector(x);
                var b = selector(y);

                var result = a switch
                {
                    null when b == null => 0,
                    null => -1,
                    _ => b == null ? 1 : Comparer<IComparable>.Default.Compare(a, b)
                };

                if (result != 0)
                    return direction == SortDirection.Ascending ? result : -result;
            }
            return 0;
        }
    }

    public void ApplySort(string propertyName, SortDirection direction, bool append = false)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            // 清空排序规则，使用默认
            _sortSubject.OnNext([("Location", SortDirection.Ascending)]);
            return;
        }

        var currentSortList = _sortSubject.Value.ToList();

        if (append)
        {
            // 追加或替换已有属性排序方向
            var index = currentSortList.FindIndex(s => s.Property == propertyName);
            if (index >= 0)
            {
                currentSortList[index] = (propertyName, direction);
            }
            else
            {
                currentSortList.Add((propertyName, direction));
            }
        }
        else
        {
            // 只使用当前一个排序条件
            currentSortList = [(propertyName, direction)];
        }

        _sortSubject.OnNext(currentSortList);
    }

    private void ClearSort()
    {
        ClearSortRequested?.Invoke(); // 通知 View 清除箭头
        _sortSubject.OnNext([("Location", SortDirection.Ascending)]);
    }

    #endregion

    #region SpeedTest

    /// <summary>
    /// 用于取消测速和下载链接获取的异步操作的令牌源。
    /// </summary>
    private CancellationTokenSource? _speedTestCancellation, _fetchAppDownloadUrlCancellation;

    /// <summary>
    /// 开始对所有选中的 IP 项进行测速操作。
    /// 测试开始后会禁用排序和导入功能，并清空排序状态。
    /// 每个选中的 IP 将依次执行 Ping 和下载速度测试，
    /// 所使用的 HTTP 请求头和 Range 长度将根据目标 URL 及导入类型自动调整。
    /// 测试完成后，如果在中国大陆环境中，将尝试上传优选 Akamai 节点。
    /// </summary>
    [RelayCommand]
    private async Task StartSpeedTestAsync()
    {
        if (IpItems.Count == 0 || !IpItems.Any(p => p.IsSelected)) return;

        StopSpeedTest();
        _speedTestCancellation = new CancellationTokenSource();
        var token = _speedTestCancellation.Token;

        IsSpeedTest = true;
        IsSortingEnabled = IsImportEnabled = false;
        ClearSortRequested?.Invoke();
        _sortSubject.OnNext([("IsSelected", SortDirection.Descending)]);

        var uri = await EnsureValidTargetTestUrlAsync();

        if (uri != null && !token.IsCancellationRequested)
        {
            
            var rangeTo = SelectedImportOption!.Key.StartsWith("Akamai") ? 31457279 : 52428799; //国外IP测试下载30M，国内IP测试下载50M
            var timeout = TimeSpan.FromSeconds(SelectedTimeout);
            var userAgent = uri.Host.EndsWith(".nintendo.net") ? "XboxDownload/Nintendo NX" : "XboxDownload";

            foreach (var item in IpItems.Where(p => p.IsSelected))
            {
                if (token.IsCancellationRequested)
                    break;

                await SpeedTestService.PingAndTestAsync(item, uri, rangeTo, timeout, userAgent, token);
            }

            _ = UploadPreferredAkamaiIpsIfInChinaAsync();
        }

        _speedTestCancellation?.Dispose();
        _speedTestCancellation = null;
        IsSortingEnabled = IsImportEnabled = true;
        IsSpeedTest = false;
    }

    /// <summary>
    /// 对单个 IP 项进行测速
    /// </summary>
    /// <param name="item">要进行测速的 IP 项。</param>
    [RelayCommand]
    private async Task RunSingleSpeedTestAsync(IpItem? item)
    {
        if (item == null || IsSpeedTest) return;

        StopSpeedTest();
        _speedTestCancellation = new CancellationTokenSource();
        var token = _speedTestCancellation.Token;

        var uri = await EnsureValidTargetTestUrlAsync();

        if (uri != null && !token.IsCancellationRequested)
        {
            var rangeTo = SelectedImportOption!.Key.StartsWith("Akamai") ? 31457279 : 52428799; //国外IP测试下载30M，国内IP测试下载50M
            var timeout = TimeSpan.FromSeconds(SelectedTimeout);
            var userAgent = uri.Host.EndsWith(".nintendo.net") ? "XboxDownload/Nintendo NX" : "XboxDownload";

            await SpeedTestService.PingAndTestAsync(item, uri, rangeTo, timeout, userAgent, token);

            _ = UploadPreferredAkamaiIpsIfInChinaAsync();
        }
    }

    /// <summary>
    /// 确保当前设置的测速目标链接为合法的下载 URL。
    /// 如果 <see cref="TargetTestUrl"/> 是有效的绝对地址则直接返回，
    /// 否则尝试从默认的测速文件条目中获取地址，
    /// 如为标识符则调用 <see cref="UpdateService.FetchAppDownloadUrlAsync"/> 生成下载地址。
    /// </summary>
    /// <returns>返回一个有效的下载 <see cref="Uri"/>，若无法获取则返回 <c>null</c>。</returns>
    private async Task<Uri?> EnsureValidTargetTestUrlAsync()
    {
        if (Uri.TryCreate(TargetTestUrl, UriKind.Absolute, out var baseUri))
            return baseUri;

        var url = SpeedTestFiles.FirstOrDefault(p => p.IsVisible)?.Url;
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            TargetTestUrl = url;
        }
        else
        {
            CancelFetchAppDownloadUrl();
            _fetchAppDownloadUrlCancellation = new CancellationTokenSource();
            var fetchToken = _fetchAppDownloadUrlCancellation.Token;

            TargetTestUrl = ResourceHelper.GetString("SpeedTest.GettingDownloadLink");
            TargetTestUrl = await UpdateService.FetchAppDownloadUrlAsync(url, fetchToken);
        }

        return Uri.TryCreate(TargetTestUrl, UriKind.Absolute, out baseUri) ? baseUri : null;
    }

    private readonly Dictionary<string, DateTime> _uploadedIpTimes = [];

    /// <summary>
    /// 上传在中国大陆区域测速良好的 Akamai 节点信息。
    /// 仅在启用上传功能、界面显示上传选项、当前语言为简体中文，
    /// 且 IP 项中存在指定亚洲地区（如香港、日本等）测速大于等于 20 的节点时才会执行上传。
    /// 上传前通过 API 判断是否处于中国大陆地区（排除港澳台）。
    /// </summary>
    private async Task UploadPreferredAkamaiIpsIfInChinaAsync()
    {
        // 若上传选项未显示、未启用上传功能，或当前语言非简体中文，则跳过上传逻辑
        if (!(UploadAkamaiIpsVisible && UploadAkamaiIpsEnabled && App.Settings.Culture == "zh-Hans"))
            return;

        // 优选区域：仅包含指定亚洲地区节点
        var preferredRegions = new[] { "香港", "台湾", "日本", "韩国", "新加坡" };
        var now = DateTime.UtcNow;

        // 过滤：测速>=20，优选区域，且48小时内未上传过
        var list = IpItems
            .Where(p =>
                p.Speed >= 20 &&
                preferredRegions.Any(r => p.Location.Contains(r)) &&
                (!_uploadedIpTimes.TryGetValue(p.Ip, out var lastTime) || (now - lastTime) > TimeSpan.FromHours(48))
            )
            .ToList();

        // 无满足条件的节点则直接返回
        if (list.Count == 0)
            return;

        // 构建 JSON 数组，用于上传
        var jsonArray = new JsonArray();
        foreach (var item in list)
        {
            jsonArray.Add(new JsonObject
            {
                ["ip"] = item.Ip,
                ["location"] = item.Location,
                ["speed"] = item.Speed
            });
            _uploadedIpTimes[item.Ip] = now;
        }

        // 请求 API 获取本地地理位置信息（用于判断是否在中国大陆）
        var result = await IpGeoHelper.GetCountryFromMultipleApisAsync();

        // 如果不在中国大陆，则跳过上传
        if (!string.Equals(result, "CN", StringComparison.OrdinalIgnoreCase))
            return;

        // 发送 POST 请求上传到服务端接口
        using var response = await HttpClientHelper.SendRequestAsync(
            UpdateService.Website + "/Akamai/Better",
            "POST",
            jsonArray.ToJsonString(),
            "application/json",
            name: HttpClientNames.XboxDownload);
        //Console.WriteLine(response?.StatusCode);

        var filePath = PathHelper.GetResourceFilePath("IP.AkamaiV2.txt");
        if (File.Exists(filePath)) File.SetLastWriteTime(filePath, DateTime.UtcNow.AddDays(-7));
    }

    private void CancelFetchAppDownloadUrl()
    {
        _fetchAppDownloadUrlCancellation?.Cancel();
        _fetchAppDownloadUrlCancellation?.Dispose();
        _fetchAppDownloadUrlCancellation = null;
    }

    [RelayCommand]
    private void StopSpeedTest()
    {
        CancelFetchAppDownloadUrl();

        _speedTestCancellation?.Cancel();
        _speedTestCancellation?.Dispose();
        _speedTestCancellation = null;
    }

    #endregion
}