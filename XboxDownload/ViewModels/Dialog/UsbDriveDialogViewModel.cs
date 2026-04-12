using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.ViewModels.Dialog;

public partial class UsbDriveDialogViewModel : ObservableObject
{
    public ObservableCollection<UsbDriveMappingEntry> UsbDriveMappings { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    public partial UsbDriveMappingEntry? SelectedEntry { get; set; }

    public bool IsEnabled => SelectedEntry != null;

    public UsbDriveDialogViewModel()
    {
        RefreshUsbDrivesCommand.Execute(null);
    }

    [ObservableProperty]
    public partial PartitionScheme DiskPartitionType { get; set; } = PartitionScheme.Mbr;

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private async Task RefreshUsbDrivesAsync()
    {
        if (!OperatingSystem.IsWindows())
            return;

        UsbDriveMappings.Clear();
        SelectedEntry = null;

        var entries = await Task.Run(() =>
        {
            var result = new List<UsbDriveMappingEntry>();
            using var mc = new ManagementClass("Win32_DiskDrive");
            using var moc = mc.GetInstances();
            foreach (var (mo, deviceId, interfaceType, mediaType) in from ManagementObject mo in moc
                                                                     let deviceId = mo.Properties["DeviceID"].Value?.ToString()
                                                                     let interfaceType = mo.Properties["InterfaceType"].Value?.ToString()
                                                                     let mediaType = mo.Properties["MediaType"].Value?.ToString()
                                                                     select (mo, deviceId, interfaceType, mediaType))
            {
                if (string.IsNullOrEmpty(deviceId) || interfaceType != "USB" || mediaType != "Removable Media")
                    continue;

                var index = Convert.ToInt32(mo.Properties["Index"].Value);
                var model = mo.Properties["Model"].Value?.ToString()?.Trim() ?? string.Empty;
                var serialNumber = mo.Properties["SerialNumber"].Value?.ToString()?.Trim() ?? string.Empty;
                var size = Convert.ToInt64(mo.Properties["Size"].Value);
                var partitions = Convert.ToInt32(mo.Properties["Partitions"].Value);
                var driveLetters = (from ManagementObject diskPartition in mo.GetRelated("Win32_DiskPartition")
                                    from ManagementBaseObject disk in diskPartition.GetRelated("Win32_LogicalDisk")
                                    select disk.Properties["Name"].Value.ToString()).ToList();

                var outputString = "";
                try
                {
                    using var p = new Process();
                    p.StartInfo.FileName = "DiskPart.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.StandardInput.WriteLine("list disk");
                    p.StandardInput.Close();
                    outputString = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                catch
                {
                    // ignored
                }

                var m = Regex.Match(outputString, @"\s" + index + ".{43}(?<Gpt>.)");
                var partitionScheme = m.Success ? m.Groups["Gpt"].Value == "*" ? "GPT" : "MBR" : "未知";
                var entry = new UsbDriveMappingEntry(index, model, interfaceType, serialNumber, size, partitionScheme, partitions, string.Join(',', driveLetters.ToArray()));

                result.Add(entry);
            }
            return result;
        });

        UsbDriveMappings.Clear();
        SelectedEntry = null;

        UsbDriveMappings.AddRange(entries);
    }

    [RelayCommand]
    private async Task RepartitionAsync()
    {
        if (SelectedEntry == null) return;

        var selectedEntry = SelectedEntry;

        var confirm = await DialogHelper.ShowConfirmDialogAsync(
            "重新分区",
            "确认要重新分区U盘吗？ \n\n⚠ 警告，此操作将删除U盘中的所有分区和文件!",
            Icon.Question, false);

        if (!confirm) return;

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

            //await p.StandardInput.WriteLineAsync("list disk");
            await p.StandardInput.WriteLineAsync("select disk " + selectedEntry.Index);
            await p.StandardInput.WriteLineAsync("clean");

            // 根据分区类型选择 MBR 或 GPT
            if (DiskPartitionType == PartitionScheme.Mbr)
                await p.StandardInput.WriteLineAsync("convert mbr");
            else if (DiskPartitionType == PartitionScheme.Gpt)
                await p.StandardInput.WriteLineAsync("convert gpt");

            await p.StandardInput.WriteLineAsync("create partition primary");
            //await p.StandardInput.WriteLineAsync("select partition 1");
            await p.StandardInput.WriteLineAsync("format fs=ntfs quick");

            // 如果没有驱动器盘符，自动分配
            if (string.IsNullOrEmpty(selectedEntry.DriveLetter))
            {
                await p.StandardInput.WriteLineAsync("assign");
            }

            // 将磁盘设置为在线
            await p.StandardInput.WriteLineAsync("online disk");
            // 清除只读属性
            await p.StandardInput.WriteLineAsync("attributes disk clear readonly");

            p.StandardInput.Close();

            // 等待命令执行完成
            await p.WaitForExitAsync();

            // 读取错误输出，捕获可能的错误信息
            var errorOutput = await p.StandardError.ReadToEndAsync();

            // 如果有错误输出，显示给用户
            if (!string.IsNullOrEmpty(errorOutput))
            {
                await DialogHelper.ShowInfoDialogAsync("Error", "磁盘操作错误：\n" + errorOutput, Icon.Error);
            }
            else
            {
                // 如果没有错误输出，可以显示成功消息
                await DialogHelper.ShowInfoDialogAsync("Success", "磁盘分区和格式化已成功完成。", Icon.Success);
            }
        }
        catch (Exception ex)
        {
            // 捕获并显示详细的异常信息
            await DialogHelper.ShowInfoDialogAsync(
                "Error",
                "重新分区失败，错误信息：\n" + ex.Message,
                Icon.Error);
        }

        RefreshUsbDrivesCommand.Execute(null);
    }

    public enum PartitionScheme
    {
        Gpt,
        Mbr
    }

    public partial class UsbDriveMappingEntry(int index, string model, string interfaceType, string serialNumber, long size, string partitionScheme, int partitions, string driveLetter) : ObservableObject
    {
        [ObservableProperty]
        public partial int Index { get; set; } = index;

        [ObservableProperty]
        public partial string Model { get; set; } = model;

        [ObservableProperty]
        public partial string InterfaceType { get; set; } = interfaceType;

        [ObservableProperty]
        public partial string SerialNumber { get; set; } = serialNumber;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormatSize))]
        public partial long Size { get; set; } = size;

        [ObservableProperty]
        public partial string PartitionScheme { get; set; } = partitionScheme;

        [ObservableProperty]
        public partial int Partitions { get; set; } = partitions;

        [ObservableProperty]
        public partial string DriveLetter { get; set; } = driveLetter;

        public string FormatSize => UnitConverter.ConvertBytes(Size);
    }
}
