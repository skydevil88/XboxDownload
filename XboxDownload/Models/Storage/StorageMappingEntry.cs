using CommunityToolkit.Mvvm.ComponentModel;
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
    private string _mbrHex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Mode))]
    private string _bootSignature;

    public string FormatSize => UnitConverter.ConvertBytes(Size);

    public string Mode => BootSignature switch
    {
        "99CC" => ResourceHelper.GetString("Storage.XboxMode"),
        "55AA" => ResourceHelper.GetString("Storage.PcMode"),
        "0000" => ResourceHelper.GetString("Storage.RepairMode"),
        _ => "Unknown"
    };

    public StorageMappingEntry(int index, string deviceId, string model, string serialNumber, long size, string mbrHex, string bootSignature)
    {
        Index = index;
        DeviceId = deviceId;
        Model = model;
        SerialNumber = serialNumber;
        Size = size;
        MbrHex = mbrHex;
        BootSignature = bootSignature;
    }
}