using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.SpeedTest;

public partial class LocationFilter : ObservableObject
{
    public string Key { get; }
    public string[] Keywords { get; }

    [ObservableProperty]
    public partial string Display { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

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
    public partial string Display { get; set; }

    [ObservableProperty]
    public partial string Hint { get; set; }

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
    public partial string Display { get; set; }

    public string Url { get; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

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
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string Ip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? Ttl { get; set; }

    [ObservableProperty]
    public partial long? RoundtripTime { get; set; }

    [ObservableProperty]
    public partial double? Speed { get; set; }

    [ObservableProperty]
    public partial bool IsFilterMatched { get; set; }
}
