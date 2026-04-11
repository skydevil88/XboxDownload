using System;
using CommunityToolkit.Mvvm.ComponentModel;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.Models.Storage;

public partial class StorageMappingEntry(int index, string deviceId, string model, string serialNumber, long size, byte[] bootSignatureBytes, int partitions = 0, string driveLetter = "") : ObservableObject
{
    [ObservableProperty]
    public partial int Index { get; set; } = index;

    [ObservableProperty]
    public partial string DeviceId { get; set; } = deviceId;

    [ObservableProperty]
    public partial string Model { get; set; } = model;

    [ObservableProperty]
    public partial string SerialNumber { get; set; } = serialNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatSize))]
    public partial long Size { get; set; } = size;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Mode))]
    public partial byte[] BootSignatureBytes { get; set; } = bootSignatureBytes;

    [ObservableProperty]
    public partial int Partitions { get; set; } = partitions;

    [ObservableProperty]
    public partial string DriveLetter { get; set; } = driveLetter;

    public string FormatSize => UnitConverter.ConvertBytes(Size);

    public string Mode => BootSignatureBytes.SequenceEqual(MbrHelper.XboxMode)
        ? ResourceHelper.GetString("Storage.XboxMode")
        : BootSignatureBytes.SequenceEqual(MbrHelper.PcMode)
            ? ResourceHelper.GetString("Storage.PcMode")
            : "Unknown";
}
