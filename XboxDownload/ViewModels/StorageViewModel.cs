using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Storage;

namespace XboxDownload.ViewModels;

public partial class StorageViewModel : ObservableObject
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    private readonly ConcurrentDictionary<string, MbrCacheEntry> _mbrMap = new();

    private static readonly string MbrFilePath = PathHelper.GetLocalFilePath("MbrBackup.json");

    public StorageViewModel()
    {
        _ = LoadMbrMapAsync(MbrFilePath);
    }

    public ObservableCollection<StorageMappingEntry> StorageMappings { get; } = [];

    [ObservableProperty]
    private StorageMappingEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isEnablePcMode, _isEnableXboxMode;

    partial void OnSelectedEntryChanged(StorageMappingEntry? value)
    {
        IsEnablePcMode = IsEnableXboxMode = false;
        if (value == null) return;

        if (IsRepair)
        {
            if (value.BootSignature == "0000")
                IsEnableXboxMode = true;
        }
        else
        {
            switch (value.BootSignature)
            {
                case "99CC":
                    IsEnablePcMode = true;
                    break;
                case "55AA":
                    IsEnableXboxMode = true;
                    break;
            }
        }
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task ConvertStorageAsync(string parameter)
    {
        if (SelectedEntry == null) return;

        var mbrBytes = MbrHelper.ReadMbr(SelectedEntry.DeviceId);
        if (mbrBytes.Length == 0) return;

        var mbrHex = await Task.Run(() => MbrHelper.ByteToHex(mbrBytes));

        if (IsRepair)
        {
            if (mbrHex == SelectedEntry.MbrHex)
            {
                var key = $"{SelectedEntry.Model}_{SelectedEntry.SerialNumber}_{SelectedEntry.Size}";
                if (_mbrMap.TryGetValue(key, out var kv))
                {
                    if (MbrHelper.WriteMbr(SelectedEntry.DeviceId, MbrHelper.HexToByte(kv.MbrHex)))
                    {
                        SelectedEntry.BootSignature = "99CC";
                        SelectedEntry = null;
                        await DialogHelper.ShowInfoDialogAsync(
                            ResourceHelper.GetString("Storage.SwitchToXboxMode"),
                            ResourceHelper.GetString("Storage.SuccessfullySwitchedToXboxMode"),
                            Icon.Success);
                    }
                }
            }
        }
        else
        {
            if (mbrHex[..1020] == SelectedEntry.MbrHex[..1020])
            {
                var bootSignature = mbrHex[1020..1024];
                switch (parameter)
                {
                    case "PC" when SelectedEntry.BootSignature == "99CC" && bootSignature == "99CC":
                        {
                            var newMbr = string.Concat(
                                SelectedEntry.MbrHex.AsSpan(0, 1020),
                                "55AA",
                                SelectedEntry.MbrHex.AsSpan(1024)
                            );
                            if (MbrHelper.WriteMbr(SelectedEntry.DeviceId, MbrHelper.HexToByte(newMbr)))
                            {
                                if (OperatingSystem.IsWindows())
                                {
                                    try
                                    {
                                        await CommandHelper.RunCommandAsync("powershell",
                                            $"Update-Disk -Number {SelectedEntry.Index}");
                                    }
                                    catch
                                    {
                                        // ignored
                                    }
                                }
                                SelectedEntry.BootSignature = "55AA";
                                SelectedEntry = null;
                                await DialogHelper.ShowInfoDialogAsync(
                                    ResourceHelper.GetString("Storage.SwitchToPcMode"),
                                    ResourceHelper.GetString("Storage.SuccessfullySwitchedToPcMode"),
                                    Icon.Success);
                            }
                            break;
                        }
                    case "Xbox" when SelectedEntry.BootSignature == "55AA" && bootSignature == "55AA":
                        {
                            var newMbr = string.Concat(
                                SelectedEntry.MbrHex.AsSpan(0, 1020),
                                "99CC",
                                SelectedEntry.MbrHex.AsSpan(1024)
                            );
                            if (MbrHelper.WriteMbr(SelectedEntry.DeviceId, MbrHelper.HexToByte(newMbr)))
                            {
                                SelectedEntry.BootSignature = "99CC";
                                SelectedEntry = null;
                                await DialogHelper.ShowInfoDialogAsync(
                                    ResourceHelper.GetString("Storage.SwitchToXboxMode"),
                                    ResourceHelper.GetString("Storage.SuccessfullySwitchedToXboxMode"),
                                    Icon.Success);
                            }
                            break;
                        }
                }
            }
        }
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task ScanAsync()
    {
        StorageMappings.Clear();
        SelectedEntry = null;

        var isSave = false;
        var entries = await Task.Run(() =>
        {
            var result = new List<StorageMappingEntry>();
            var mc = new ManagementClass("Win32_DiskDrive");
            var moc = mc.GetInstances();
            foreach (var mo in moc)
            {
                var deviceId = mo.Properties["DeviceID"].Value?.ToString();
                if (string.IsNullOrEmpty(deviceId)) continue;

                var mbrBytes = MbrHelper.ReadMbr(deviceId);
                if (mbrBytes.Length == 0) continue;

                var mbrHex = MbrHelper.ByteToHex(mbrBytes);
                var index = Convert.ToInt32(mo["Index"]);
                var model = mo.Properties["Model"].Value?.ToString()?.Trim() ?? string.Empty;
                var serialNumber = mo.Properties["SerialNumber"].Value?.ToString()?.Trim() ?? string.Empty;
                var size = Convert.ToInt64(mo.Properties["Size"].Value);
                var bootSignature = mbrHex[1020..1024];
                var key = $"{model}_{serialNumber}_{size}";

                if (IsRepair)
                {
                    if (mbrHex[..892] == MbrHelper.Mbr || !_mbrMap.TryGetValue(key, out _)) continue;

                    var entry = new StorageMappingEntry(index, deviceId, model, serialNumber, size, mbrHex, "0000");
                    result.Add(entry);
                }
                else
                {
                    if (mbrHex[..892] != MbrHelper.Mbr) continue;

                    if (bootSignature == "99CC")
                    {
                        if (_mbrMap.TryGetValue(key, out var kv))
                        {
                            if (mbrHex == kv.MbrHex)
                            {
                                kv.Timestamp = DateTime.UtcNow;
                                isSave = true;
                            }
                            else if ((DateTime.UtcNow - kv.Timestamp).TotalHours <= 72)
                            {
                                mbrHex = kv.MbrHex;
                            }
                            else
                            {
                                // 保留旧值，但修改其 key，防止被覆盖
                                string newKey;
                                do
                                {
                                    newKey = $"{key}.Backup_{Guid.NewGuid().ToString("N")[..8]}";
                                } while (_mbrMap.ContainsKey(newKey));
                                _mbrMap[newKey] = kv;

                                // 替换当前 key 对应的新 MBR 数据
                                _mbrMap[key] = new MbrCacheEntry
                                {
                                    MbrHex = mbrHex,
                                    Timestamp = DateTime.UtcNow
                                };

                                isSave = true;
                            }
                        }
                        else
                        {
                            _mbrMap[key] = new MbrCacheEntry
                            {
                                MbrHex = mbrHex,
                                Timestamp = DateTime.UtcNow
                            };
                            isSave = true;
                        }
                    }
                    var entry = new StorageMappingEntry(index, deviceId, model, serialNumber, size, mbrHex, bootSignature);
                    result.Add(entry);
                }
            }
            return result;
        });

        if (isSave)
        {
            var json = JsonSerializer.Serialize(_mbrMap, JsonHelper.Indented);
            await File.WriteAllTextAsync(MbrFilePath, json);

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(MbrFilePath);
        }

        StorageMappings.AddRange(entries);
    }

    private async Task LoadMbrMapAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, MbrCacheEntry>>(json);
            if (dict != null)
            {
                foreach (var kv in dict.Where(kv => kv.Value.MbrHex.Length == 4096 && (DateTime.UtcNow - kv.Value.Timestamp).TotalDays <= 365))
                {
                    _mbrMap.TryAdd(kv.Key, kv.Value);
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    partial void OnIsRepairChanged(bool value)
    {
        ScanCommand.Execute(null);
        _ = value;
    }

    [RelayCommand]
    private static void OpenUrl()
    {
        HttpClientHelper.OpenUrl(App.Settings.Culture == "zh-Hans"
            ? "https://www.bilibili.com/video/BV1CN4y197Js?t=130"
            : "https://www.youtube.com/watch?v=3F499kh_jfk&t=130");
    }

    [ObservableProperty]
    private string _downloadUrl = string.Empty, _filePath = string.Empty, _fileTimeCreated = string.Empty, _driveSize = string.Empty, _contentId = string.Empty, _productId = string.Empty, _productId2 = string.Empty, _buildId = string.Empty, _packageVersion = string.Empty;

    [ObservableProperty]
    private bool _isProductId2, _isCopyContentId, _isRename, _isRepair;

    [RelayCommand]
    private async Task CopyContentIdAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            return;

        await provider.SetTextAsync(ContentId.ToUpperInvariant());
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        var filename = Path.GetFileName(FilePath);
        var rename = ContentId.ToUpperInvariant();
        var confirm = await DialogHelper.ShowConfirmDialogAsync(
            ResourceHelper.GetString("Storage.RenameLocalFile"),
            string.Format(ResourceHelper.GetString("Storage.RenameConfirm"), filename, rename),
            Icon.Question);
        if (!confirm) return;

        FileInfo fi = new(FilePath);
        try
        {
            var newFilePath = Path.Combine(Path.GetDirectoryName(FilePath)!, rename);
            fi.MoveTo(newFilePath);
            FilePath = newFilePath;
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Storage.RenameLocalFile"),
                string.Format(ResourceHelper.GetString("Storage.RenameFileFailed"), ex.Message),
                Icon.Error);
        }
        IsRename = false;
    }

    public event Action? RequestFocus;

    [RelayCommand]
    private async Task ProcessDownloadAsync()
    {
        DownloadUrl = DownloadUrl.Trim();
        if (string.IsNullOrEmpty(DownloadUrl)) return;

        if (!Uri.IsWellFormedUriString(DownloadUrl, UriKind.Absolute))
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Storage.InvalidDownloadLinkTitle"),
                ResourceHelper.GetString("Storage.InvalidDownloadLinkMessage"),
                Icon.Error);
            RequestFocus?.Invoke();
            return;
        }

        FilePath = FileTimeCreated = DriveSize = ContentId = ProductId = ProductId2 = BuildId = PackageVersion = string.Empty;
        IsProductId2 = IsRename = false;
        Dictionary<string, string> headers = new() { { "Range", "bytes=0-4095" } };
        using var response = await HttpClientHelper.SendRequestAsync(DownloadUrl, headers: headers);
        if (response is { IsSuccessStatusCode: true })
        {
            var buffer = await response.Content.ReadAsByteArrayAsync();
            await ParseXboxPackageAsync(buffer);
        }
        else
        {
            var msg = response != null ? string.Format(ResourceHelper.GetString("Storage.DownloadErrorMessage"), response.ReasonPhrase) : ResourceHelper.GetString("Storage.DownloadFailed");
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Storage.DownloadFailed"),
                msg,
                Icon.Error);
        }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null)
            return;

        var options = new FilePickerOpenOptions
        {
            Title = ResourceHelper.GetString("Storage.OpenXboxPackage")
        };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            FilePath = files[0].Path.LocalPath;

            var buffer = new byte[4096];
            await using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytesRead = fs.Read(buffer, 0, buffer.Length);
            if (bytesRead < buffer.Length)
            {
                Array.Resize(ref buffer, bytesRead);
            }

            DownloadUrl = FileTimeCreated = DriveSize = ContentId = ProductId = ProductId2 = BuildId = PackageVersion = string.Empty;
            IsProductId2 = IsRename = false;
            await ParseXboxPackageAsync(buffer);
        }
    }

    private async Task ParseXboxPackageAsync(byte[] buffer)
    {
        if (buffer.Length >= 4096)
        {
            using var ms = new MemoryStream(buffer);
            using var br = new BinaryReader(ms);

            br.BaseStream.Position = 0x200;
            var magic = Encoding.ASCII.GetString(br.ReadBytes(8));
            if (magic == "msft-xvd")
            {
                br.BaseStream.Position = 0x210;
                FileTimeCreated = DateTime.FromFileTime(BitConverter.ToInt64(br.ReadBytes(8), 0))
                    .ToString(CultureInfo.CurrentCulture);

                br.BaseStream.Position = 0x218;
                DriveSize = UnitConverter.ConvertBytes(BitConverter.ToInt64(br.ReadBytes(8), 0));

                br.BaseStream.Position = 0x220;
                ContentId = new Guid(br.ReadBytes(0x10)).ToString();

                br.BaseStream.Position = 0x39C;
                var productIdBytes = br.ReadBytes(16);
                ProductId = new Guid(productIdBytes).ToString();
                ProductId2 = TryExtractProductId2(productIdBytes);
                IsProductId2 = !string.IsNullOrEmpty(ProductId2);

                br.BaseStream.Position = 0x3AC;
                BuildId = new Guid(br.ReadBytes(0x10)).ToString();

                br.BaseStream.Position = 0x3BC;
                var v1 = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                var v2 = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                var v3 = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                var v4 = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                PackageVersion = $"{v4}.{v3}.{v2}.{v1}";

                IsCopyContentId = true;
                IsRename = !string.IsNullOrEmpty(FilePath) &&
                           !Path.GetFileName(FilePath).Equals(ContentId, StringComparison.OrdinalIgnoreCase) &&
                           !FilePath.EndsWith(".msixvc", StringComparison.OrdinalIgnoreCase);
                return;
            }
        }

        await DialogHelper.ShowInfoDialogAsync(
            ResourceHelper.GetString("Storage.InvalidXboxPackageTitle"),
            ResourceHelper.GetString("Storage.InvalidXboxPackageMessage"),
            Icon.Error);
    }

    private static string TryExtractProductId2(byte[] productIdBytes)
    {
        var part1 = Encoding.ASCII.GetString(productIdBytes, 0, 7);
        var part2 = Encoding.ASCII.GetString(productIdBytes, 9, 5);
        var candidate = part1 + part2;

        return MyRegex().IsMatch(candidate)
            ? candidate
            : string.Empty;
    }

    [GeneratedRegex(@"^[A-Z0-9]{12}$", RegexOptions.IgnoreCase)]
    private static partial Regex MyRegex();

    [RelayCommand]
    private static async Task QueryContentId(string productId)
    {
        var storeVm = Ioc.Default.GetRequiredService<StoreViewModel>();
        storeVm.QueryUrl = $"https://www.microsoft.com/store/productid/{productId}";

        var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        mainWindowVm.SelectedTabIndex = 5;

        await storeVm.QueryCommand.ExecuteAsync(null);
    }
}
