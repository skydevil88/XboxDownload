using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Host;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class HostMappingEntry(bool isEnabled, string hostName, string ip, string note) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = isEnabled;

    [ObservableProperty]
    public partial string HostName { get; set; } = hostName;

    [ObservableProperty]
    public partial string Ip { get; set; } = ip;

    [ObservableProperty]
    public partial string Note { get; set; } = note;
}
