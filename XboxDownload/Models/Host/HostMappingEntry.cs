using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Host;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class HostMappingEntry : ObservableObject
{
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial string HostName { get; set; }
    [ObservableProperty] public partial string Ip { get; set; }
    [ObservableProperty] public partial string Note { get; set; }

    public HostMappingEntry(bool isEnabled, string hostName, string ip, string note)
    {
        IsEnabled = isEnabled;
        HostName = hostName;
        Ip = ip;
        Note = note;
    }
}
