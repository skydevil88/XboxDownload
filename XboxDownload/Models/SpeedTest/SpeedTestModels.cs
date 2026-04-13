using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.SpeedTest;

public partial class LocationFilter(string key, string[] keywords, string display, bool isVisible = false) : ObservableObject
{
    public string Key { get; } = key;
    public string[] Keywords { get; } = keywords;

    [ObservableProperty]
    public partial string Display { get; set; } = display;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = isVisible;
}

public partial class ImportOption(string key, string target, string display, string hint) : ObservableObject
{
    public string Key { get; } = key;
    public string Target { get; } = target;

    [ObservableProperty]
    public partial string Display { get; set; } = display;

    [ObservableProperty]
    public partial string Hint { get; set; } = hint;
}

public partial class SpeedTestFile(string key, string target, string display, string url) : ObservableObject
{
    public string Key { get; } = key;
    public string Target { get; } = target;

    [ObservableProperty]
    public partial string Display { get; set; } = display;

    public string Url { get; } = url;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }
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
