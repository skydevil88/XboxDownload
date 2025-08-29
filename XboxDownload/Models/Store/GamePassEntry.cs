using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Store;

public partial class GamePassEntry : ObservableObject
{
    public string SigId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;
}