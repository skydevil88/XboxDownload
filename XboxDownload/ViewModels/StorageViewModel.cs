using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
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

    public ObservableCollection<StorageMappingEntry> StorageMappings { get; } = [];

    [ObservableProperty]
    private StorageMappingEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isEnablePcMode, _isEnableXboxMode;

    partial void OnSelectedEntryChanged(StorageMappingEntry? value)
    {
        IsEnablePcMode = IsEnableXboxMode = false;
        if (value == null) return;
        
        if (value.BootSignatureBytes.SequenceEqual(MbrHelper.PcMode))
        {
            IsEnableXboxMode = true;
        }
        else if (value.BootSignatureBytes.SequenceEqual(MbrHelper.XboxMode))
        {
            IsEnablePcMode = true;
        }
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task ConvertStorageAsync(string parameter)
    {
        if (SelectedEntry == null) return;
        
        var mbrBytes = MbrHelper.ReadMbr(SelectedEntry.DeviceId);
        if (mbrBytes.Length != 512)
            return;
        
        var mbrDiskSignature = mbrBytes.AsSpan(0x1B8, 4);
        if (!mbrDiskSignature.SequenceEqual(MbrHelper.DiskSignatureBytes))
            return;
        
        var mbrBootSignature = mbrBytes.AsSpan(0x1FE, 2);
        
        switch (parameter)
        {
            case "Xbox" when mbrBootSignature.SequenceEqual(MbrHelper.PcMode) && SelectedEntry.BootSignatureBytes.SequenceEqual(MbrHelper.PcMode):
            {
                var mbrTail = mbrBytes.AsSpan(0x1FE, 2);
                MbrHelper.XboxMode.CopyTo(mbrTail);
                
                if (!MbrHelper.WriteMbr(SelectedEntry.DeviceId, mbrBytes)) return;
                
                SelectedEntry.BootSignatureBytes = MbrHelper.XboxMode.ToArray();
                SelectedEntry = null;
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Storage.SwitchToXboxMode"),
                    ResourceHelper.GetString("Storage.SuccessfullySwitchedToXboxMode"),
                    Icon.Success);
                break;
            }
            case "PC" when mbrBootSignature.SequenceEqual(MbrHelper.XboxMode) && SelectedEntry.BootSignatureBytes.SequenceEqual(MbrHelper.XboxMode):
            {
                var mbrTail = mbrBytes.AsSpan(0x1FE, 2);
                MbrHelper.PcMode.CopyTo(mbrTail);
                
                if (!MbrHelper.WriteMbr(SelectedEntry.DeviceId, mbrBytes)) return;
                
                try
                {
                    await CommandHelper.RunCommandAsync("PowerShell.exe",
                        $"Update-Disk -Number {SelectedEntry.Index}");
                }
                catch
                {
                    // ignored
                }
                
                SelectedEntry.BootSignatureBytes = MbrHelper.PcMode.ToArray();
                SelectedEntry = null;
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Storage.SwitchToPcMode"),
                    ResourceHelper.GetString("Storage.SuccessfullySwitchedToPcMode"),
                    Icon.Success);
                break;
            }
        }
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task ScanAsync()
    {
        StorageMappings.Clear();
        SelectedEntry = null;

        var entries = await Task.Run(() =>
        {
            var result = new List<StorageMappingEntry>();

            using var mc = new ManagementClass("Win32_DiskDrive");
            using var moc = mc.GetInstances();

            foreach (var mo in moc)
            {
                var deviceId = mo["DeviceID"]?.ToString();
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                // 读取 MBR
                var mbrBytes = MbrHelper.ReadMbr(deviceId);
                if (mbrBytes.Length != 512)
                    continue;
                
                // ===== 检查 Disk Signature（偏移 0x1B8） =====
                var mbrDiskSignature = mbrBytes.AsSpan(0x1B8, 4);
                if (!mbrDiskSignature.SequenceEqual(MbrHelper.DiskSignatureBytes))
                    continue;
                
                // 读取 MBR 尾部 2 个字节（偏移 0x1FE）
                var mbrBootSignature = mbrBytes.AsSpan(0x1FE, 2);
                if (!(mbrBootSignature.SequenceEqual(MbrHelper.XboxMode) || mbrBootSignature.SequenceEqual(MbrHelper.PcMode)))
                    continue;

                // 读取磁盘信息
                var index = Convert.ToInt32(mo["Index"] ?? -1);
                var model = mo["Model"]?.ToString()?.Trim() ?? string.Empty;
                var serialNumber = mo["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;
                var size = Convert.ToInt64(mo["Size"] ?? 0L);

                var entry = new StorageMappingEntry(
                    index,
                    deviceId,
                    model,
                    serialNumber,
                    size,
                    mbrBootSignature.ToArray()
                );

                result.Add(entry);
            }

            return result;
        });

        StorageMappings.AddRange(entries.OrderBy(e => e.Index));
    }

    [RelayCommand]
    private static async Task OpenUrl()
    {
        await HttpClientHelper.OpenUrlAsync(App.Settings.Culture == "zh-Hans"
            ? "https://www.bilibili.com/video/BV1CN4y197Js?t=130"
            : "https://www.youtube.com/watch?v=3F499kh_jfk&t=130");
    }

    [ObservableProperty]
    private string _downloadUrl = string.Empty, _filePath = string.Empty, _fileTimeCreated = string.Empty, _driveSize = string.Empty, _contentId = string.Empty, _productId = string.Empty, _productId2 = string.Empty, _buildId = string.Empty, _packageVersion = string.Empty;

    [ObservableProperty]
    private bool _isProductId2, _isCopyContentId, _isRename;

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
    
    [RelayCommand]
    private static async Task QueryContentId(string productId)
    {
        var storeVm = Ioc.Default.GetRequiredService<StoreViewModel>();
        storeVm.QueryUrl = $"https://apps.microsoft.com/detail/{productId}";

        var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        mainWindowVm.SelectedTabIndex = 5;

        await storeVm.QueryCommand.ExecuteAsync(null);
    }

    private static string TryExtractProductId2(byte[] productIdBytes)
    {
        var part1 = Encoding.ASCII.GetString(productIdBytes, 0, 7);
        var part2 = Encoding.ASCII.GetString(productIdBytes, 9, 5);
        var productId = part1 + part2;

        return MyRegex().IsMatch(productId)
            ? productId
            : string.Empty;
    }

    [GeneratedRegex(@"^[A-Z0-9]{12}$", RegexOptions.IgnoreCase)]
    private static partial Regex MyRegex();
}
