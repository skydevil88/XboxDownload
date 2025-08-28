using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.Models.Store;

public class Game
{
    public List<Products> Products { get; init; } = [];
}

public class Products
{
    public DateTime LastModifiedDate { get; set; }
    public List<LocalizedProperties> LocalizedProperties { get; set; } = [];
    public List<MarketProperties> MarketProperties { get; set; } = [];
    public Properties Properties { get; set; } = new Properties();
    public List<DisplaySkuAvailabilities> DisplaySkuAvailabilities { get; set; } = [];
    public string ProductId { get; set; } = "";
}

public class LocalizedProperties
{
    public string DeveloperName { get; set; } = "";
    public string PublisherName { get; set; } = "";
    public EligibilityProperties EligibilityProperties { get; set; } = new EligibilityProperties();
    public List<Images> Images { get; set; } = new List<Images>();
    public string ProductDescription { get; set; } = "";
    public string ProductTitle { get; set; } = "";
    public string[] Markets { get; set; } = [];
}

public class MarketProperties
{
    public DateTime OriginalReleaseDate { get; set; }
}

public class EligibilityProperties
{
    public Affirmations[] Affirmations { get; set; } = [];
}

public class Affirmations
{
    public string Description { get; set; } = "";
}

public class Images
{
    public string ImagePurpose { get; set; } = "";
    public string Uri { get; set; } = "";
    public int Height { get; set; }
    public int Width { get; set; }
}

public class DisplaySkuAvailabilities
{
    public Sku Sku { get; set; } = new Sku();
    public List<Availabilities> Availabilities { get; set; } = [];
}

public class Sku
{
    public Properties Properties { get; set; } = new();
    public string SkuType { get; set; } = "";
}

public class Properties
{
    public string Category { get; set; } = "";
    public List<Packages> Packages { get; set; } = [];
    public List<BundledSkus> BundledSkus { get; set; } = [];

    //EA Play
    public string[] MerchandisingTags { get; set; } = [];
}

public class Packages
{
    public long MaxDownloadSizeInBytes { get; set; }
    public string[] Languages { get; set; } = [];
    public string PackageFormat { get; set; } = "";
    public string PackageFullName { get; set; } = "";
    public string ContentId { get; set; } = "";
    public int PackageRank { get; set; }
    public List<PlatformDependencies> PlatformDependencies { get; set; } = [];
    public List<PackageDownloadUris> PackageDownloadUris { get; set; } = [];
    public FulfillmentData FulfillmentData { get; set; } = new FulfillmentData();
}

public class BundledSkus
{
    public string BigId { get; set; } = "";
}

public class PlatformDependencies
{
    public string PlatformName { get; set; } = "";
}

public class PackageDownloadUris
{
    public string Uri { get; set; } = "";
}

public class FulfillmentData
{
    public string WuCategoryId { get; set; } = "";
}

public class Availabilities
{
    public Conditions Conditions { get; set; } = new();
    public OrderManagementData OrderManagementData { get; set; } = new();
    public Properties Properties { get; set; } = new();
}

public class Conditions
{
    public DateTime EndDate { get; set; }
    public DateTime StartDate { get; set; }
}
public class OrderManagementData
{
    public Price Price { get; set; } = new();
}

public class Price
{
    public string CurrencyCode { get; set; } = "";
    public decimal MSRP { get; set; }
    public decimal ListPrice { get; set; }
    public decimal WholesalePrice { get; set; }
}






public partial class Bundled : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    public string ProductId { get; init; } = "";
}

public partial class PlatformDownloadItem : ObservableObject
{
    public PlatformType Platform { get; init; }
    public string DisplayName => !string.IsNullOrEmpty(Key) ? Platform.GetDescription() : string.Empty;
    public string Key { get; init; } = "";
    public string Category { get; init; } = "";
    public string Market { get; init; } = "";
    public string WuCategoryId { get; init; } = "";
    public string AppVersion { get; set; } = "";
    public string FileName { get; set; } = "";
    public long LatestSize { get; init; }

    [ObservableProperty]
    private string _display = ResourceHelper.GetString("Store.FetchingDownloadLink");

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatSize))]
    private long _fileSize;
    public string FormatSize => FileSize > 0 ? UnitConverter.ConvertBytes(FileSize) : string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Foreground))]
    [NotifyPropertyChangedFor(nameof(TextDecorations))]
    private bool _outdated;

    public string Foreground => Application.Current?.ActualThemeVariant == ThemeVariant.Dark
        ? Outdated ? "#C2185B" : "White"
        : Outdated ? "Red" : "Black";

    public TextDecorationCollection TextDecorations => Outdated
        ? [new TextDecoration { Location = TextDecorationLocation.Strikethrough }]
        : [];

    public void NotifyThemeChanged()
    {
        OnPropertyChanged(nameof(Foreground));
    }
}

