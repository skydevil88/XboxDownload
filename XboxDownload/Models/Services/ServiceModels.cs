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
    private string _display;

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
    private string _requestMethod;

    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private string _clientIp;

    private DateTime Timestamp { get; }

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss.fff");
}

public partial class DohServerOption : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    private string _name;

    public string Url { get; }

    [ObservableProperty]
    private string _ip;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isProxyDisabled;

    public DohServerOption(string id, string name, string url, string ip = "", bool isChecked = false, bool isProxyDisabled = false)
    {
        Id = id;
        _name = name;
        Url = url;
        Ip = ip;
        IsChecked = isChecked;
        IsProxyDisabled = isProxyDisabled;
    }

    public override string ToString() => Name;
}