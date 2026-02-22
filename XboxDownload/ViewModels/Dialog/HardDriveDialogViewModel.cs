using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.Models.Storage;

namespace XboxDownload.ViewModels.Dialog;


public partial class HardDriveDialogViewModel : ObservableObject
{
    public ObservableCollection<StorageMappingEntry> StorageMappings { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    private StorageMappingEntry? _selectedEntry;

    public bool IsEnabled => SelectedEntry != null;

    public HardDriveDialogViewModel()
    {
        RefreshHardDrivesCommand.Execute(null);
    }

    [ObservableProperty]
    private MediaType _diskMediaType = MediaType.External;

    partial void OnDiskMediaTypeChanged(MediaType value)
    {
        _ = value;
        RefreshHardDrivesCommand.Execute(null);
    }

    private const long MinDiskSize = 256L * 1024 * 1024 * 1024;

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task RefreshHardDrivesAsync()
    {
        if (!OperatingSystem.IsWindows())
            return;

        StorageMappings.Clear();
        SelectedEntry = null;

        var entries = await Task.Run(() =>
        {
            var result = new List<StorageMappingEntry>();

            var systemDiskIndex = GetSystemDiskIndex();
            var driveMap = BuildDiskDriveLetterMap();

            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, MediaType, Size, Index, Model, SerialNumber, Partitions FROM Win32_DiskDrive");

            foreach (var mo in searcher.Get())
            {
                var deviceId = mo["DeviceID"]?.ToString();
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                var index = Convert.ToInt32(mo["Index"] ?? -1);
                if (index < 0 || index == systemDiskIndex)
                    continue;

                var size = Convert.ToInt64(mo["Size"] ?? 0L);
                if (size < MinDiskSize)
                    continue;

                if (DiskMediaType == MediaType.External)
                {
                    var mediaType = mo["MediaType"]?.ToString();
                    if (!string.Equals(mediaType, "External hard disk media", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var mbrBytes = MbrHelper.ReadMbr(deviceId);
                if (mbrBytes.Length != 512)
                    continue;

                var model = mo["Model"]?.ToString()?.Trim() ?? string.Empty;
                var serialNumber = mo["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;
                var bootSignature = mbrBytes.AsSpan(0x1FE, 2).ToArray();
                var partitions = Convert.ToInt32(mo["Partitions"] ?? 0);

                driveMap.TryGetValue(index, out var driveLetters);
                driveLetters ??= [];

                var entry = new StorageMappingEntry(
                    index,
                    deviceId,
                    model,
                    serialNumber,
                    size,
                    bootSignature,
                    partitions,
                    string.Join(',', driveLetters.ToArray())
                );

                result.Add(entry);
            }

            return result;
        });

        StorageMappings.AddRange(entries.OrderBy(e => e.Index));
    }


    [SupportedOSPlatform("windows")]
    private static int GetSystemDiskIndex()
    {
        try
        {
            var sysLetter = Environment.GetEnvironmentVariable("SystemDrive");
            if (string.IsNullOrWhiteSpace(sysLetter))
                return -1;

            using var partitionSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{sysLetter}'}} " +
                "WHERE AssocClass = Win32_LogicalDiskToPartition");

            foreach (var partition in partitionSearcher.Get())
            {
                using var diskSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} " +
                    "WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                foreach (var disk in diskSearcher.Get())
                {
                    return Convert.ToInt32(disk["Index"] ?? -1);
                }
            }
        }
        catch
        {
            // ignored
        }

        return -1;
    }

    [SupportedOSPlatform("windows")]
    private static Dictionary<int, List<string>> BuildDiskDriveLetterMap()
    {
        var map = new Dictionary<int, List<string>>();

        using var partitionSearcher = new ManagementObjectSearcher(
            "SELECT DiskIndex, DeviceID FROM Win32_DiskPartition");

        foreach (var partition in partitionSearcher.Get())
        {
            var diskIndex = Convert.ToInt32(partition["DiskIndex"] ?? -1);
            if (diskIndex < 0)
                continue;

            using var logicalSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} " +
                "WHERE AssocClass = Win32_LogicalDiskToPartition");

            foreach (var logical in logicalSearcher.Get())
            {
                var letter = logical["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(letter))
                    continue;

                if (!map.TryGetValue(diskIndex, out var list))
                {
                    list = [];
                    map[diskIndex] = list;
                }

                list.Add(letter);
            }
        }

        return map;
    }

    [RelayCommand]
    private async Task FormatHardDriveAsync()
    {
        if (SelectedEntry == null) return;

        var selectedEntry = SelectedEntry;

        var confirm = await DialogHelper.ShowConfirmDialogAsync(
            ResourceHelper.GetString("Storage.FormatHardDrive.DialogFormatHardDriveTitle"),
            ResourceHelper.GetString("Storage.FormatHardDrive.DialogFormatHardDriveMessage"),
            Icon.Question, false);

        if (!confirm) return;

        var success = false;
        try
        {
            using Process p = new();
            p.StartInfo.FileName = "DiskPart.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            await p.StandardInput.WriteLineAsync("select disk " + selectedEntry.Index);
            await p.StandardInput.WriteLineAsync("clean");
            await p.StandardInput.WriteLineAsync("convert gpt");
            await p.StandardInput.WriteLineAsync("create partition primary");
            await p.StandardInput.WriteLineAsync("format fs=ntfs quick");
            await p.StandardInput.WriteLineAsync("assign");
            p.StandardInput.Close();
            await p.WaitForExitAsync();
            var errorOutput = await p.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(errorOutput))
            {
                await DialogHelper.ShowInfoDialogAsync("Error",
                    string.Format(ResourceHelper.GetString("Storage.FormatHardDrive.DriveOperationError"), errorOutput),
                    Icon.Error);
            }
            else
            {
                success = true;

            }
        }
        catch (Exception ex)
        {
            // 捕获并显示详细的异常信息
            await DialogHelper.ShowInfoDialogAsync(
                "Error",
                string.Format(ResourceHelper.GetString("Storage.FormatHardDrive.FormatFailedMessage"), ex.Message),
                Icon.Error);
        }

        if (success)
        {
            try
            {
                await CommandHelper.RunCommandAsync("PowerShell.exe", $"Update-Disk -Number {selectedEntry.Index}");
            }
            catch
            {
                // ignored
            }

            var mbrBytes = MbrHelper.ReadMbr(selectedEntry.DeviceId);
            if (mbrBytes.Length == 512)
            {
                var mbrDiskSignature = mbrBytes.AsSpan(0x1B8, 4);
                MbrHelper.DiskSignatureBytes.CopyTo(mbrDiskSignature);
                var mbrTail = mbrBytes.AsSpan(0x1FE, 2);
                MbrHelper.XboxMode.CopyTo(mbrTail);
                MbrHelper.WriteMbr(selectedEntry.DeviceId, mbrBytes);
            }

            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Storage.FormatHardDrive.DialogFormatHardDriveTitle"),
                ResourceHelper.GetString("Storage.FormatHardDrive.FormatSuccessMessage"),
                Icon.Success);
        }

        RefreshHardDrivesCommand.Execute(null);
    }

    public enum MediaType
    {
        All,
        External
    }
}