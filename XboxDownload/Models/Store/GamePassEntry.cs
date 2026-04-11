using CommunityToolkit.Mvvm.ComponentModel;

namespace XboxDownload.Models.Store;

public partial class GamePassEntry : ObservableObject
{
    public string ResourceKey { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;
}
