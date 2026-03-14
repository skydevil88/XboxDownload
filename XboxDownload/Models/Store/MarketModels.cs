using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Store;

public partial class Market : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string Region =>
        string.IsNullOrEmpty(Code)
            ? string.Empty
            : MyRegex().Replace(Code, string.Empty);

    [GeneratedRegex(@"^.*-")]
    private static partial Regex MyRegex();
}