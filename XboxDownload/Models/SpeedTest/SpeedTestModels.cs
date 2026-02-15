using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.SpeedTest;

public partial class LocationFilter : ObservableObject
{
    public string Key { get; }
    public string[] Keywords { get; }

    [ObservableProperty]
    private string _display;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible;

    public LocationFilter(string key, string[] keywords, string display, bool isVisible = false)
    {
        Key = key;
        Keywords = keywords;
        Display = display;
        IsVisible = isVisible;
    }
}

public partial class ImportOption : ObservableObject
{
    public string Key { get; }
    public string Target { get; }

    [ObservableProperty]
    private string _display;

    [ObservableProperty]
    private string _hint;

    public ImportOption(string key, string target, string display, string hint)
    {
        Key = key;
        Target = target;
        Display = display;
        Hint = hint;
    }
}

public partial class SpeedTestFile : ObservableObject
{
    public string Key { get; }
    public string Target { get; }

    [ObservableProperty]
    private string _display;

    public string Url { get; }

    [ObservableProperty]
    private bool _isVisible;

    public SpeedTestFile(string key, string target, string display, string url)
    {
        Key = key;
        Target = target;
        Display = display;
        Url = url;
    }
}

public partial class IpItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private bool _isRedirect;

    [ObservableProperty]
    private int? _ttl;

    [ObservableProperty]
    private long? _roundtripTime;

    [ObservableProperty]
    private double? _speed;

    [ObservableProperty]
    private bool _isFilterMatched;
}