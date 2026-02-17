using System;
using CommunityToolkit.Mvvm.ComponentModel;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.Models.Storage;

public partial class StorageMappingEntry : ObservableObject
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _deviceId;

    [ObservableProperty]
    private string _model;

    [ObservableProperty]
    private string _serialNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatSize))]
    private long _size;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Mode))]
    private byte[] _bootSignatureBytes;
    
    [ObservableProperty]
    private string _driveLetter;

    public string FormatSize => UnitConverter.ConvertBytes(Size);

    public string Mode => BootSignatureBytes.SequenceEqual(MbrHelper.XboxMode)
        ? ResourceHelper.GetString("Storage.XboxMode")
        : BootSignatureBytes.SequenceEqual(MbrHelper.PcMode)
            ? ResourceHelper.GetString("Storage.PcMode")
            : "Unknown";

    public StorageMappingEntry(int index, string deviceId, string model, string serialNumber, long size, byte[] bootSignatureBytes, string driveLetter = "")
    {
        Index = index;
        DeviceId = deviceId;
        Model = model;
        SerialNumber = serialNumber;
        Size = size;
        BootSignatureBytes = bootSignatureBytes;
        DriveLetter = driveLetter;
    }
}