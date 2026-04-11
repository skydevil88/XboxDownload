using System;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Services;

public class AdapterInfo(string ip, NetworkInterface adapter)
{
    public string Ip { get; } = ip;
    public NetworkInterface Adapter { get; } = adapter;
}

public partial class ListeningIpOption : ObservableObject
{
    public string Key { get; }

    [ObservableProperty]
    public partial string Display { get; set; }

    public ListeningIpOption(string key, string display)
    {
        Key = key;
        Display = display;
    }
}

public partial class ServiceModels : ObservableObject
{
    public ServiceModels(string method, string content, string ip)
    {
        RequestMethod = method;
        Content = content;
        ClientIp = ip;
        Timestamp = DateTime.Now;
    }

    [ObservableProperty]
    public partial string RequestMethod { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; }

    [ObservableProperty]
    public partial string ClientIp { get; set; }

    private DateTime Timestamp { get; }

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}

public partial class DohServerOption : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    public string Url { get; }

    [ObservableProperty]
    public partial string Ip { get; set; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    [ObservableProperty]
    public partial bool IsProxyDisabled { get; set; }

    public DohServerOption(string id, string name, string url, string ip = "", bool isChecked = false, bool isProxyDisabled = false)
    {
        Id = id;
        Name = name;
        Url = url;
        Ip = ip;
        IsChecked = isChecked;
        IsProxyDisabled = isProxyDisabled;
    }

    public override string ToString() => Name;
}
