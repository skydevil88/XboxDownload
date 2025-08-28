using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Host;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class HostMappingEntry : ObservableObject
{
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _hostName;
    [ObservableProperty] private string _ip;
    [ObservableProperty] private string _note;

    public HostMappingEntry(bool isEnabled, string hostName, string ip, string note)
    {
        IsEnabled = isEnabled;
        HostName = hostName;
        Ip = ip;
        Note = note;
    }
}