using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XboxDownload.Helpers.Network;

namespace XboxDownload.Models.Store;

public partial class StoreSearchResult : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProductId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Bitmap? IconBitmap { get; set; }

    public async Task LoadIconFromUrlAsync(string? url)
    {
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return;

        using var response = await HttpClientHelper.SendRequestAsync(url);
        if (response is { IsSuccessStatusCode: true })
        {
            var buffer = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(buffer);
            IconBitmap = new Bitmap(stream);
        }
    }
}
