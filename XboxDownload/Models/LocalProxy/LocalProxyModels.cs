using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.LocalProxy;

public class ProxyModels(string display, string[] rule)
{
    public string Display { get; set; } = display;
    public bool IsChecked { get; set; }
    public string[] Rule { get; init; } = rule;
}

public partial class DohModels : ObservableObject
{
    public string Id { get; }
    public string Name { get; set; }

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                CheckedChanged?.Invoke(this, value);
            }
        }
    }

    [ObservableProperty]
    private bool _isEnabled;

    public DohModels(string id, string name, bool isChecked = false, bool isEnabled = true)
    {
        Id = id;
        Name = name;
        IsChecked = isChecked;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// 当 IsChecked 变化时触发
    /// </summary>
    public event EventHandler<bool>? CheckedChanged;
}