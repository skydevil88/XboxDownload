namespace XboxDownload.Models.LocalProxy;

public class ProxyModels(string display, string[] rule)
{
    public string Display { get; set; } = display;
    public bool IsChecked { get; set; }
    public string[] Rule { get; init; } = rule;
}

public class DohModels(string id, string name, bool isChecked)
{
    public string Id { get; } = id;
    public string Name { get; set; } = name;
    public bool IsChecked { get; set; } = isChecked;
}