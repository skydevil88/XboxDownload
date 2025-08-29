using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Store;

public partial class Market : ObservableObject
{
    [ObservableProperty]
    private string _region = string.Empty;

    public string Language { get; init; } = string.Empty;

    public string Code =>
        string.IsNullOrEmpty(Language) ? string.Empty :
        Language.Split('-').Length > 1 ? Language.Split('-')[1] : string.Empty;
}