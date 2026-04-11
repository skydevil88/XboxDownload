using System;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Services;

public class AdapterInfo(string ip, NetworkInterface adapter)
{
    public string Ip { get; } = ip;
    public NetworkInterface Adapter { get; } = adapter;
}

public partial class ListeningIpOption(string key, string display) : ObservableObject
{
    public string Key { get; } = key;

    [ObservableProperty]
    public partial string Display { get; set; } = display;
}

public partial class ServiceModels(string method, string content, string ip) : ObservableObject
{
    [ObservableProperty]
    public partial string RequestMethod { get; set; } = method;

    [ObservableProperty]
    public partial string Content { get; set; } = content;

    [ObservableProperty]
    public partial string ClientIp { get; set; } = ip;

    private DateTime Timestamp { get; } = DateTime.Now;

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}

public partial class DohServerOption(string id, string name, string url, string ip = "", bool isChecked = false, bool isProxyDisabled = false) : ObservableObject
{
    public string Id { get; } = id;

    [ObservableProperty]
    public partial string Name { get; set; } = name;

    public string Url { get; } = url;

    [ObservableProperty]
    public partial string Ip { get; set; } = ip;

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = isChecked;

    [ObservableProperty]
    public partial bool IsProxyDisabled { get; set; } = isProxyDisabled;

    public override string ToString() => Name;
}
