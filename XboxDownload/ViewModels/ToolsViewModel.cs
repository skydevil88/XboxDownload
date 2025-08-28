using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models;

namespace XboxDownload.ViewModels;

public partial class ToolsViewModel : ObservableObject, IDisposable
{
    private UsbWatcher? _usbWatcher;

    [ObservableProperty]
    private bool _isChineseUsers = App.Settings.Culture == "zh-Hans";

    public ObservableCollection<DeviceMappingEntry> DrivePaths { get; } = [];

    [ObservableProperty]
    private DeviceMappingEntry? _selectedDrivePath;

    public void LanguageChanged()
    {
        OnSelectedDrivePathChanged(SelectedDrivePath);

        IsChineseUsers = App.Settings.Culture == "zh-Hans";
        if (IsChineseUsers)
        {
            EnableUsbWatcherIfNeeded();
        }
        else
        {
            Dispose();
        }
    }

    partial void OnSelectedDrivePathChanged(DeviceMappingEntry? value)
    {
        _ = value;

        if (SelectedDrivePath == null)
        {
            AppInstallPath = string.Format(ResourceHelper.GetString("Tools.Install.AppDirectory"), "");
            GameInstallPath = string.Format(ResourceHelper.GetString("Tools.Install.GameDirectory"), "");
            return;
        }

        AppInstallPath = string.Format(ResourceHelper.GetString("Tools.Install.AppDirectory"), SelectedDrivePath.StorePath);
        if (SelectedDrivePath.IsOffline) AppInstallPath += $" ({ResourceHelper.GetString("Tools.Install.Offline")})";

        string gamesPath;
        if (File.Exists(SelectedDrivePath!.DirectoryRoot + "\\.GamingRoot"))
        {
            try
            {
                using FileStream fs = new(SelectedDrivePath.DirectoryRoot + "\\.GamingRoot", FileMode.Open, FileAccess.Read, FileShare.Read);
                using BinaryReader br = new(fs);
                if (MbrHelper.ByteToHex(br.ReadBytes(0x8)) == "5247425801000000")
                {
                    gamesPath = SelectedDrivePath.DirectoryRoot + Encoding.GetEncoding("UTF-16").GetString(br.ReadBytes((int)fs.Length - 0x8)).Trim('\0');
                    if (!Directory.Exists(gamesPath))
                    {
                        gamesPath += $" ({ResourceHelper.GetString("Tools.Install.FolderNotFound")})";
                    }
                }
                else
                {
                    gamesPath = SelectedDrivePath.DirectoryRoot + $" ({ResourceHelper.GetString("Tools.Install.FolderUnknown")})";
                }
            }
            catch (Exception ex)
            {
                gamesPath = SelectedDrivePath!.DirectoryRoot + $" ({ex.Message})";
            }
        }
        else
        {
            gamesPath = SelectedDrivePath.DirectoryRoot + $" ({ResourceHelper.GetString("Tools.Install.FolderUnknown")})";
        }

        GameInstallPath = string.Format(ResourceHelper.GetString("Tools.Install.GameDirectory"), gamesPath);
    }

    [ObservableProperty]
    private string _filePath = string.Empty, _appInstallPath = string.Empty, _gameInstallPath = string.Empty;

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
            Title = ResourceHelper.GetString("Tools.Install.SelectInstallationPackage")
        };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            FilePath = files[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task AppxInstallAsync()
    {
        if (SelectedDrivePath == null || !OperatingSystem.IsWindows()) return;
        
        if (string.IsNullOrEmpty(FilePath))
        {
            FocusText();
            return;
        }
        
        var service = ServiceController.GetServices().SingleOrDefault(s => s.ServiceName == "GamingServices");
        if (service is not { Status: ServiceControllerStatus.Running })
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Tools.Install.InstallationFailedTitle"),
                ResourceHelper.GetString("Tools.Install.InstallationFailedMessage1"),
                Icon.Error);
            return;
        }

        var cmd = $"-noexit \"Add-AppxPackage -Path '{FilePath}' -Volume '{SelectedDrivePath.DirectoryRoot}'\necho '{ResourceHelper.GetString("Tools.Install.InstallationComplete")}'\"";
        FilePath = string.Empty;
        try
        {
            await CommandHelper.RunCommandAsync("powershell", cmd, true);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Tools.Install.InstallationFailedTitle"),
                string.Format(ResourceHelper.GetString("Tools.Install.InstallationFailedMessage1"), ex.Message),
                Icon.Error);
        }
    }

    [RelayCommand]
    public async Task RefreshDrives()
    {
        DrivePaths.Clear();

        var entries = await Task.Run(() =>
        {
            var outputString = "";
            try
            {
                using Process p = new();
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("Get-AppxVolume");
                p.StandardInput.Close();
                outputString = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch
            {
                // ignored
            }

            var results = new ConcurrentDictionary<string, (string StorePath, bool IsOffline)>();

            var match = Regex.Match(outputString, @"(?<Name>\\\\\?\\Volume\{\w{8}-\w{4}-\w{4}-\w{4}-\w{12}\})\s+(?<PackageStorePath>.+)\s+(?<IsOffline>True|False)\s+(?<IsSystemVolume>True|False)");
            while (match.Success)
            {
                var storePath = match.Groups["PackageStorePath"].Value.Trim();
                var directoryRoot = Directory.GetDirectoryRoot(storePath);
                var isOffline = match.Groups["IsOffline"].Value == "True";
                results[directoryRoot] = (storePath, isOffline);
                match = match.NextMatch();
            }

            var result = new List<DeviceMappingEntry>();
            foreach (var drive in Array.FindAll(DriveInfo.GetDrives(), d => d is { DriveType: DriveType.Fixed, IsReady: true, DriveFormat: "NTFS" }))
            {
                var directoryRoot = drive.RootDirectory.FullName;
                if (results.TryGetValue(directoryRoot, out var value))
                {
                    var entry = new DeviceMappingEntry(directoryRoot)
                    {
                        StorePath = value.StorePath,
                        IsOffline = value.IsOffline
                    };

                    result.Add(entry);
                }
                else
                {
                    var entry = new DeviceMappingEntry(directoryRoot);
                    result.Add(entry);
                }
            }

            return result;
        });

        DrivePaths.AddRange(entries);
        if (DrivePaths.Count > 0)
            SelectedDrivePath = DrivePaths[0];
    }

    public partial class DeviceMappingEntry : ObservableObject
    {
        [ObservableProperty]
        private string _directoryRoot;

        [ObservableProperty]
        private string _storePath;

        [ObservableProperty]
        private bool _isOffline;

        public DeviceMappingEntry(string directoryRoot, string storePath = "", bool isOffline = false)
        {
            DirectoryRoot = directoryRoot;
            StorePath = storePath;
            IsOffline = isOffline;
        }
    }

    public event Action? RequestFocus;

    [RelayCommand]
    public void FocusText()
    {
        RequestFocus?.Invoke();
    }




    public void EnableUsbWatcherIfNeeded()
    {
        if (!OperatingSystem.IsWindows() || !IsChineseUsers || _usbWatcher != null) return;

        _usbWatcher = new UsbWatcher();
        _usbWatcher.UsbInserted += OnUsbInserted;
        _usbWatcher.UsbRemoved += OnUsbRemoved;
        _usbWatcher.Start();

        RefreshUsbDrives();
    }

    private void OnUsbInserted(string path)
    {
        if (!path.EndsWith('\\')) path += "\\";
        if (!UsbDrivePaths.Contains(path))
            UsbDrivePaths.Add(path);

        if (string.IsNullOrEmpty(SelectedUsbDrivePath))
            SelectedUsbDrivePath = path;
    }

    private void OnUsbRemoved(string path)
    {
        if (!path.EndsWith('\\')) path += "\\";
        UsbDrivePaths.Remove(path);

        if (SelectedUsbDrivePath == path)
            SelectedUsbDrivePath = UsbDrivePaths.FirstOrDefault();
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows() || _usbWatcher == null) return;

        _usbWatcher.UsbInserted -= OnUsbInserted;
        _usbWatcher.UsbRemoved -= OnUsbRemoved;
        _usbWatcher.Dispose();
        _usbWatcher = null;

        GC.SuppressFinalize(this);
    }

    [ObservableProperty]
    private PlatformType _selectedPlatformType = PlatformType.XboxSeries;

    public ObservableCollection<string> UsbDrivePaths { get; } = [];

    [ObservableProperty]
    private string? _selectedUsbDrivePath, _usbDriveStatus = string.Empty;

    partial void OnSelectedUsbDrivePathChanged(string? value)
    {
        if (!string.IsNullOrEmpty(SelectedUsbDrivePath))
        {
            DriveInfo driveInfo = new(SelectedUsbDrivePath);
            if (driveInfo.DriveType != DriveType.Removable) return;
            if (driveInfo is { IsReady: true, DriveFormat: "NTFS" })
            {
                if (File.Exists(SelectedUsbDrivePath + "$ConsoleGen8Lock"))
                    UsbDriveStatus = $"当前U盘状态：{PlatformType.XboxOne.GetDescription()} 回国";
                else if (File.Exists(SelectedUsbDrivePath + "$ConsoleGen8"))
                    UsbDriveStatus = $"当前U盘状态：{PlatformType.XboxOne.GetDescription()} 出国";
                else if (File.Exists(SelectedUsbDrivePath + "$ConsoleGen9Lock"))
                    UsbDriveStatus = $"当前U盘状态：{PlatformType.XboxSeries.GetDescription()} 回国";
                else if (File.Exists(SelectedUsbDrivePath + "$ConsoleGen9"))
                    UsbDriveStatus = $"当前U盘状态：{PlatformType.XboxSeries.GetDescription()} 出国";
                else
                    UsbDriveStatus = "当前U盘状态：未转换";
            }
            else
            {
                UsbDriveStatus = "当前U盘状态：不是NTFS格式";
            }
        }
        else
        {
            UsbDriveStatus = string.Empty;
        }
        _ = value;
    }

    [RelayCommand]
    private void RefreshUsbDrives()
    {
        if (!OperatingSystem.IsWindows())
            return;

        UsbDrivePaths.Clear();

        var usbPaths = DriveInfo.GetDrives()
            .Where(d => d is { DriveType: DriveType.Removable, IsReady: true })
            .Select(d => d.RootDirectory.FullName);

        UsbDrivePaths.AddRange(usbPaths);

        if (UsbDrivePaths.Count > 0)
            SelectedUsbDrivePath = UsbDrivePaths[0];
    }

    [RelayCommand]
    private async Task ConsoleRegionAsync(string unlock)
    {
        if (string.IsNullOrEmpty(SelectedUsbDrivePath)) return;

        var driveInfo = new DriveInfo(SelectedUsbDrivePath);
        if (driveInfo.DriveType != DriveType.Removable) return;

        var drive = driveInfo.RootDirectory.FullName;

        UsbDriveStatus = "当前U盘状态：制作中，请稍候..";
        if (!driveInfo.IsReady || driveInfo.DriveFormat != "NTFS")
        {
            var size = UnitConverter.ConvertBytes(Convert.ToInt64(driveInfo.TotalSize));
            string title, message, cmd;
            if (driveInfo is { IsReady: true, DriveFormat: "FAT32" })
            {
                title = "转换U盘格式";
                message = "当前U盘格式 " + driveInfo.DriveFormat + "，是否把U盘转换为 NTFS 格式？\n\n注意，如果U盘有重要数据请先备份!\n\n当前U盘位置： " + drive + "，容量：" + size;
                cmd = $"/c convert {drive.TrimEnd('\\')} /fs:ntfs /x";
            }
            else
            {
                title = "格式化U盘";
                message = "当前U盘格式 " + (driveInfo.IsReady ? driveInfo.DriveFormat : "RAW") + "，是否把U盘格式化为 NTFS？\n\n警告，格式化将删除U盘中的所有文件!\n\n当前U盘位置： " + drive + "，容量：" + (driveInfo.IsReady ? size : "未知");
                cmd = $"/c echo.|format {drive.TrimEnd('\\')} /fs:ntfs /q /y";
            }
            if (await DialogHelper.ShowConfirmDialogAsync(title, message, Icon.Question))
            {
                try
                {
                    await CommandHelper.RunCommandAsync("cmd.exe", cmd);
                }
                catch
                {
                    // ignored
                }
            }
        }

        if (driveInfo is { IsReady: true, DriveFormat: "NTFS" })
        {
            string[] files = ["$ConsoleGen8", "$ConsoleGen8Lock", "$ConsoleGen9", "$ConsoleGen9Lock"];
            foreach (var file in files)
            {
                if (File.Exists(SelectedUsbDrivePath + file))
                    File.Delete(SelectedUsbDrivePath + file);
            }

            var isUnlock = unlock == "True";
            if (SelectedPlatformType == PlatformType.XboxOne)
            {
                await using (File.Create(drive + (isUnlock ? "$ConsoleGen8" : "$ConsoleGen8Lock"))) { }
            }
            else if (SelectedPlatformType == PlatformType.XboxSeries)
            {
                await using (File.Create(drive + (isUnlock ? "$ConsoleGen9" : "$ConsoleGen9Lock"))) { }
            }

            if (RegexHelper.NonAsciiRegex().IsMatch(driveInfo.VolumeLabel)) //卷标含有非英文字符
            {
                if (OperatingSystem.IsWindows())
                    driveInfo.VolumeLabel = "";
            }
        }
        else
        {
            await DialogHelper.ShowInfoDialogAsync(
                "Error",
                "U盘不是NTFS格式，请重新格式化NTFS格式后再转换。",
                Icon.Error);
        }
        OnSelectedUsbDrivePathChanged(SelectedUsbDrivePath);
    }
}