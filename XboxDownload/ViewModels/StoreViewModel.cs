using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models;
using XboxDownload.Models.Store;
using XboxDownload.Services;

namespace XboxDownload.ViewModels;

public partial class StoreViewModel : ObservableObject
{
    [ObservableProperty]
    private Bitmap? _boxArt, _cachedBoxArt;

    public static bool IsWindows => OperatingSystem.IsWindows();

    public StoreViewModel()
    {
        ReloadMarketList();
        XboxGameManager.Load();

        var uri = new Uri($"avares://{nameof(XboxDownload)}/Assets/Store/BoxArt.jpg");
        using var stream = AssetLoader.Open(uri);
        BoxArt = CachedBoxArt = new Bitmap(stream);
    }

    public void LanguageChanged()
    {
        ReloadMarketList();

        var gamePassInfoArray = new[,]
        {
            { ResourceHelper.GetString("Store.MostPopularConsoleGames"), "eab7757c-ff70-45af-bfa6-79d3cfb2bf81" },
            { ResourceHelper.GetString("Store.MostPopularPcGames"), "a884932a-f02b-40c8-a903-a008c23b1df1" },
            { ResourceHelper.GetString("Store.RecentlyAddedConsoleGames"), "f13cf6b4-57e6-4459-89df-6aec18cf0538" },
            { ResourceHelper.GetString("Store.RecentlyAddedPcGames"), "163cdff5-442e-4957-97f5-1050a3546511" }
        };
        for (var i = 0; i < gamePassInfoArray.GetLength(0); i++)
        {
            var xgp = i <= 1
                ? GamePass1Mappings.FirstOrDefault(m => m.SigId == gamePassInfoArray[i, 1])
                : GamePass2Mappings.FirstOrDefault(m => m.SigId == gamePassInfoArray[i, 1]);
            if (xgp == null) continue;

            var match = GetNumberRegex().Match(xgp.Title);
            if (!match.Success) continue;
            var number = match.Groups[1].Value;
            xgp.Title = string.Format(gamePassInfoArray[i, 0], number);
        }

        if (BundledMappings.Count > 1)
        {
            BundledMappings[0].Title = string.Format(ResourceHelper.GetString("Store.InThisBundle"), BundledMappings.Count - 1);
        }
    }

    [GeneratedRegex(@"\((\d+)\)")]
    private static partial Regex GetNumberRegex();

    public ObservableCollection<GamePassEntry> GamePass1Mappings { get; } = [];
    public ObservableCollection<GamePassEntry> GamePass2Mappings { get; } = [];

    [ObservableProperty]
    private GamePassEntry? _selectedGamePass1, _selectedGamePass2;

    [ObservableProperty]
    private DateTime _nextXgpUpdated = DateTime.MinValue;

    public ObservableCollection<Market> Markets { get; } = [];

    [ObservableProperty]
    private Market? _selectedMarket;

    partial void OnSelectedMarketChanged(Market? value)
    {
        if (value == null || App.Settings.StoreRegion == value.Language) return;
        App.Settings.StoreRegion = value.Language;
        SettingsManager.Save(App.Settings);

        NextXgpUpdated = DateTime.MinValue;
        ReloadGamePass();
    }

    public ObservableCollection<StoreSearchResult> SearchResults { get; } = [];

    private void ReloadMarketList()
    {
        var sorted = MarketBuilder.BuildMarket()
            .OrderBy(m => m.Region, StringComparer.CurrentCultureIgnoreCase);

        Markets.Clear();
        foreach (var item in sorted)
            Markets.Add(item);

        var storeLanguage = App.Settings.StoreRegion;
        if (string.IsNullOrEmpty(storeLanguage))
        {
            var raw = CultureInfo.CurrentUICulture.Name;
            storeLanguage = raw switch
            {
                "zh" or "zh-CN" or "zh-Hans" or "zh-Hans-CN" or "zh-Hant-TW" => "zh-TW",
                "zh-Hant-HK" or "zh-MO" or "zh-Hant-MO" => "zh-HK",
                "zh-SG" or "zh-Hans-SG" => "en-SG",
                _ => raw
            };
        }
        SelectedMarket = Markets.FirstOrDefault(m => m.Language == storeLanguage)
                         ?? Markets.FirstOrDefault(m => m.Language.StartsWith(storeLanguage[..2]))
                         ?? Markets.FirstOrDefault(m => m.Language == "en-US");
    }

    private CancellationTokenSource? _xgpToken;

    public void ReloadGamePass()
    {
        if (NextXgpUpdated >= DateTime.UtcNow) return;

        NextXgpUpdated = DateTime.UtcNow.AddMinutes(1);
        _xgpToken?.Cancel();
        _xgpToken = new CancellationTokenSource();
        var token = _xgpToken.Token;
        _ = ReloadGamePassListAsync(token);
    }

    private async Task ReloadGamePassListAsync(CancellationToken token)
    {
        var isAllSucceeded = true;

        GamePass1Mappings.Clear();
        GamePass2Mappings.Clear();
        GamePass1Mappings.Add(new GamePassEntry { Title = ResourceHelper.GetString("Store.LoadingMostPopularGames") });
        GamePass2Mappings.Add(new GamePassEntry { Title = ResourceHelper.GetString("Store.LoadingRecentlyAddedGames") });
        SelectedGamePass1 = GamePass1Mappings[0];
        SelectedGamePass2 = GamePass2Mappings[0];

        var gamePassInfoArray = new[,]
        {
            { ResourceHelper.GetString("Store.MostPopularConsoleGames"), "eab7757c-ff70-45af-bfa6-79d3cfb2bf81" },
            { ResourceHelper.GetString("Store.MostPopularPcGames"), "a884932a-f02b-40c8-a903-a008c23b1df1" },
            { ResourceHelper.GetString("Store.RecentlyAddedConsoleGames"), "f13cf6b4-57e6-4459-89df-6aec18cf0538" },
            { ResourceHelper.GetString("Store.RecentlyAddedPcGames"), "163cdff5-442e-4957-97f5-1050a3546511" }
        };

        ConcurrentDictionary<string, List<string>> gamePassDictionary = new();

        var tasks = Enumerable.Range(0, gamePassInfoArray.GetLength(0))
            .Select(async i =>
            {
                var sigId = gamePassInfoArray[i, 1];

                var url = $"https://catalog.gamepass.com/sigls/v2?id={sigId}&market={SelectedMarket?.Code}";
                var responseString = await HttpClientHelper.GetStringContentAsync(url, token: token);
                try
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in root.EnumerateArray())
                        {
                            if (!element.TryGetProperty("id", out JsonElement idElement)) continue;
                            var productId = idElement.GetString();
                            if (string.IsNullOrEmpty(productId)) continue;

                            var list = gamePassDictionary.GetOrAdd(sigId, _ => []);
                            list.Add(productId);
                        }
                    }
                }
                catch
                {
                    isAllSucceeded = false;
                }
            });

        await Task.WhenAll(tasks);

        if (token.IsCancellationRequested) return;

        var allProductIds = gamePassDictionary.Values
            .SelectMany(list => list)
            .Distinct().ToArray();

        if (allProductIds.Length > 0)
        {
            var url = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={string.Join(',', allProductIds)}&market=US&languages={SelectedMarket?.Language}&MS-CV=DGU1mcuYo0WMMp+F.1";
            var responseString = await HttpClientHelper.GetStringContentAsync(url, token: token);
            if (token.IsCancellationRequested) return;

            ConcurrentDictionary<string, string> gameDictionary = new();
            try
            {
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("Products", out var products) && products.ValueKind == JsonValueKind.Array)
                {
                    foreach (var product in products.EnumerateArray())
                    {
                        var productId = product.GetProperty("ProductId").GetString();

                        var title = product
                            .GetProperty("LocalizedProperties")[0]
                            .GetProperty("ProductTitle")
                            .GetString();

                        if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(title)) continue;
                        gameDictionary[productId] = title;
                    }
                }
            }
            catch
            {
                isAllSucceeded = false;
            }

            GamePass1Mappings.Clear();
            GamePass2Mappings.Clear();

            for (var i = 0; i < gamePassInfoArray.GetLength(0); i++)
            {
                var title = gamePassInfoArray[i, 0];
                var sigId = gamePassInfoArray[i, 1];

                if (i <= 1)
                {
                    if (gamePassDictionary.TryGetValue(sigId, out var list))
                    {
                        GamePass1Mappings.Add(new GamePassEntry { Title = string.Format(title, list.Count), SigId = sigId });
                        var no = 0;
                        foreach (var productId in list)
                        {
                            no++;
                            if (gameDictionary.TryGetValue(productId, out var value))
                            {
                                GamePass1Mappings.Add(new GamePassEntry { Title = $"{no}. {value}", ProductId = productId });
                            }
                        }
                    }
                    else
                    {
                        isAllSucceeded = false;
                        GamePass1Mappings.Add(new GamePassEntry { Title = string.Format(title, 0) });
                    }
                }
                else
                {
                    if (gamePassDictionary.TryGetValue(sigId, out var list))
                    {
                        GamePass2Mappings.Add(new GamePassEntry { Title = string.Format(title, list.Count), SigId = sigId });
                        var no = 0;
                        foreach (var productId in list)
                        {
                            no++;
                            if (gameDictionary.TryGetValue(productId, out var value))
                            {
                                GamePass2Mappings.Add(new GamePassEntry { Title = $"{no}. {value}", ProductId = productId });
                            }
                        }
                    }
                    else
                    {
                        isAllSucceeded = false;
                        GamePass2Mappings.Add(new GamePassEntry { Title = string.Format(title, 0) });
                    }
                }
            }

            if (GamePass1Mappings.Any()) SelectedGamePass1 = GamePass1Mappings[0];
            if (GamePass2Mappings.Any()) SelectedGamePass2 = GamePass2Mappings[0];
        }
        else
        {
            isAllSucceeded = false;
            GamePass1Mappings.Clear();
            GamePass2Mappings.Clear();
            GamePass1Mappings.Add(new GamePassEntry { Title = ResourceHelper.GetString("Store.FailedToLoadPopularGames") });
            GamePass2Mappings.Add(new GamePassEntry { Title = ResourceHelper.GetString("Store.FailedToLoadNewGames") });
            SelectedGamePass1 = GamePass1Mappings[0];
            SelectedGamePass2 = GamePass2Mappings[0];
        }

        NextXgpUpdated = isAllSucceeded ? DateTime.UtcNow.AddHours(12) : DateTime.MinValue;
    }

    [ObservableProperty]
    private bool _isSuggestsOpen;

    private string _query = string.Empty;

    private CancellationTokenSource? _delayToken;

    partial void OnSearchTextChanged(string value)
    {
        var query = value.Trim();
        if (_query == query) return;

        _delayToken?.Cancel();
        SearchResults.Clear();
        IsSuggestsOpen = false;
        _query = query;
        if (string.IsNullOrEmpty(value)) return;

        _delayToken = new CancellationTokenSource();
        Task.Delay(300, _delayToken.Token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                var token = _delayToken.Token;
                await PerformSearchAsync(_query, token);
            }
        });
    }

    private async Task PerformSearchAsync(string query, CancellationToken token)
    {
        var requestUrl = "https://www.microsoft.com/msstoreapiprod/api/autosuggest?market=" + SelectedMarket?.Language + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Microsoft-Terms,Iris-Products,xSearch-Products&filter=+ClientType:StoreWeb&counts=5,1,5&query=" + HttpUtility.UrlEncode(query);
        var responseString = await HttpClientHelper.GetStringContentAsync(requestUrl, token: token);
        if (string.IsNullOrWhiteSpace(responseString)) return;

        try
        {
            var document = JsonDocument.Parse(responseString);
            var resultSets = document.RootElement.GetProperty("ResultSets");
            foreach (var suggest in resultSets.EnumerateArray().Select(resultSet => resultSet.GetProperty("Suggests")).SelectMany(suggests => suggests.EnumerateArray()))
            {
                string? title = null, imageUrl = null, bigCatalogId = null;
                if (suggest.TryGetProperty("Title", out var titleProperty))
                    title = titleProperty.GetString();
                if (suggest.TryGetProperty("ImageUrl", out var imageUrlProperty))
                    imageUrl = imageUrlProperty.GetString();
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    if (imageUrl.StartsWith("//"))
                        imageUrl = "https:" + imageUrl;
                    var queryIndex = imageUrl.IndexOf('?');
                    if (queryIndex != -1)
                        imageUrl = imageUrl[..queryIndex];
                    imageUrl += "?w=32&h=32";
                }
                if (suggest.TryGetProperty("Metas", out var metasProperty))
                {
                    bigCatalogId =
                        (from meta in metasProperty.EnumerateArray()
                         where meta.GetProperty("Key").GetString() == "BigCatalogId"
                         select meta.GetProperty("Value").GetString()).FirstOrDefault();
                }

                if (title == null || bigCatalogId == null) continue;
                var resultItem = new StoreSearchResult { Title = title, ProductId = bigCatalogId };
                _ = resultItem.LoadIconFromUrlAsync(imageUrl);
                SearchResults.Add(resultItem);
            }
        }
        catch
        {
            // ignored
        }

        IsSuggestsOpen = SearchResults.Any();
    }

    public event Action? RequestFocus;

    [ObservableProperty]
    private string _queryUrl = string.Empty, _searchText = string.Empty;

    private Market? _currentProductMarket;

    [RelayCommand]
    private async Task QueryAsync()
    {
        if (string.IsNullOrWhiteSpace(QueryUrl) || IsLoding) return;
        IsLoding = true;

        var result = RegexHelper.ExtractProductIdRegex().Match(QueryUrl);
        var productId = result.Success ? result.Groups["productId"].Value.ToUpperInvariant() : string.Empty;
        if (string.IsNullOrEmpty(productId))
        {
            IsLoding = false;
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Store.InvalidUrlOrProductIdTitle"),
                ResourceHelper.GetString("Store.InvalidUrlOrProductIdMessage"),
                Icon.Error);
            RequestFocus?.Invoke();
            return;
        }

        GameData = null;
        BoxArt = CachedBoxArt;
        Title = PublisherAndDeveloper = Category = OriginalReleaseDate = Description = GameLanguages = Price = null;
        BundledMappings.Clear();
        SelectedBundledIndex = -1;
        BundledLoaded = ProductLoaded = IsEnablePriceComparison = false;
        SelectedBundled = null;
        PlatformDownloadInfo.Clear();

        _currentProductMarket = SelectedMarket;
        var url = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={productId}&market={_currentProductMarket?.Code}&languages={_currentProductMarket?.Language},neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
        var responseString = await HttpClientHelper.GetStringContentAsync(url);
        if (string.IsNullOrEmpty(responseString.Trim()))
        {
            IsLoding = false;
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Store.CannotConnectServerTitle"),
                ResourceHelper.GetString("Store.CannotConnectServerMessage"),
                Icon.Error);
            return;
        }

        try
        {
            GameData = JsonSerializer.Deserialize<Game>(responseString, JsonHelper.PropertyNameInsensitive);
        }
        catch (Exception ex)
        {
            IsLoding = false;
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Store.DataAnalysisFailedTitle"),
                string.Format(ResourceHelper.GetString("Store.DataAnalysisFailedMessage"), ex.Message),
                Icon.Error);
            return;
        }

        if (GameData is { Products.Count: >= 1 })
        {
            await StoreParseAsync(0);
        }

        IsLoding = false;
    }

    public Game? GameData;

    [ObservableProperty]
    private string? _title, _publisherAndDeveloper, _category, _originalReleaseDate, _description, _gameLanguages, _price;

    public ObservableCollection<Bundled> BundledMappings { get; } = [];

    [ObservableProperty]
    private int _selectedBundledIndex = -1;

    [ObservableProperty]
    private bool _isLoding, _bundledLoaded, _productLoaded, _isEnablePriceComparison;

    [ObservableProperty]
    private Bundled? _selectedBundled;

    public ObservableCollection<PlatformDownloadItem> PlatformDownloadInfo { get; } = [];

    [ObservableProperty]
    private PlatformDownloadItem? _selectedPlatformDownloadItem = new();

    public bool IsShowUpdateMenu => !(OperatingSystem.IsLinux() && Program.UnixUserIsRoot()) &&
        ((string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url) && string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Display) ||
        SelectedPlatformDownloadItem!.Outdated) && (SelectedPlatformDownloadItem?.Platform == PlatformType.XboxOne || SelectedPlatformDownloadItem?.Platform == PlatformType.WindowsPc));
    public bool IsShowContextMenu => !string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url) || IsShowUpdateMenu;
    public bool IsShowGameGlobalMenu =>
        SelectedPlatformDownloadItem!.Category == "Game" && !string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url);
    public bool IsShowGameCnMenu =>
        SelectedPlatformDownloadItem!.Category == "Game" && !string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url)
        && App.Settings.Culture == "zh-Hans";
    public bool IsShowAzureMenu =>
        SelectedPlatformDownloadItem!.Category == "Game" && !string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url)
        && !SelectedPlatformDownloadItem!.Url.Contains("/public/");
    public bool IsShowAllAppMenu =>
        SelectedPlatformDownloadItem!.Category == "App"
        && SelectedPlatformDownloadItem!.Platform == PlatformType.WindowsPc
        && !PlatformDownloadInfo.Any(x => string.IsNullOrEmpty(x.Key));

    public DateTime NextUpdatedRates = DateTime.MinValue;
    public readonly ConcurrentDictionary<string, decimal> ExchangeRates = new();

    [ObservableProperty]
    private string _useCurrencyCode = !string.IsNullOrEmpty(App.Settings.CurrencyCode) ? App.Settings.CurrencyCode : ExchangeRateHelper.GetSystemCurrencyCode();

    partial void OnUseCurrencyCodeChanged(string value)
    {
        _ = value;
        ExchangeRates.Clear();
    }

    public async Task StoreParseAsync(int index)
    {
        if (GameData == null || GameData.Products.Count - 1 < index) return;

        Title = PublisherAndDeveloper = Category = OriginalReleaseDate = Description = GameLanguages = Price = null;
        IsEnablePriceComparison = false;
        PlatformDownloadInfo.Clear();

        var product = GameData.Products[index];
        var localizedPropertie = product.LocalizedProperties;
        if (localizedPropertie is { Count: >= 1 })
        {
            Title = localizedPropertie[0].ProductTitle.Trim();
            PublisherAndDeveloper = $"{localizedPropertie[0].PublisherName.Trim()} / {localizedPropertie[0].DeveloperName.Trim()}";
            var properties = product.Properties;
            Category = properties.Category;
            var marketProperties = product.MarketProperties;
            if (marketProperties is [{ OriginalReleaseDate.Year: < 3000 }, ..])
                OriginalReleaseDate = marketProperties[0].OriginalReleaseDate.ToLocalTime().ToString("d");
            Description = localizedPropertie[0].ProductDescription;

            var tasks = new List<Task>();
            List<string> bundledId = [];
            var displaySkuAvailabilities = product.DisplaySkuAvailabilities.FirstOrDefault(displaySkuAvailabilities => displaySkuAvailabilities.Sku.SkuType == "full");
            if (displaySkuAvailabilities != null)
            {
                var packages = displaySkuAvailabilities.Sku.Properties.Packages;
                if (packages is { Count: > 0 })
                {
                    var wuCategoryId = packages[0].FulfillmentData.WuCategoryId.ToLowerInvariant();
                    GameLanguages = string.Join(", ", packages[0].Languages);

                    List<PlatformDownloadItem> platformDownloadList = [];
                    foreach (var package in packages)
                    {
                        var platformDependencies = package.PlatformDependencies;
                        if (platformDependencies.Count >= 1)
                        {
                            var contentId = package.ContentId.ToLowerInvariant();
                            switch (platformDependencies[0].PlatformName)
                            {
                                case "Windows.Xbox":
                                    switch (package.PackageRank)
                                    {
                                        case 50000:
                                            {
                                                var key = contentId + "_x";
                                                var platformDownload = new PlatformDownloadItem
                                                {
                                                    Platform = PlatformType.XboxOne,
                                                    Key = key,
                                                    ContentId = contentId,
                                                    Category = "Game",
                                                    Market = _currentProductMarket!.Region,
                                                    FileSize = package.MaxDownloadSizeInBytes,
                                                    LatestSize = package.MaxDownloadSizeInBytes
                                                };
                                                if (XboxGameManager.Dictionary.TryGetValue(key, out var xboxGame))
                                                {
                                                    platformDownload.Url = xboxGame.Url;
                                                    platformDownload.Display = Path.GetFileName(xboxGame.Url);
                                                    if (platformDownload.LatestSize != xboxGame.FileSize)
                                                    {
                                                        platformDownload.Outdated = true;
                                                        platformDownload.FileSize = xboxGame.FileSize;
                                                        platformDownload.Display += $" ({ResourceHelper.GetString("Store.UpdateAvailable")})";
                                                        tasks.Add(Task.Run(async () =>
                                                        {
                                                            await GetGamePackageAsync(platformDownload, contentId);
                                                        }));
                                                    }
                                                }
                                                else
                                                {
                                                    tasks.Add(Task.Run(async () =>
                                                    {
                                                        await GetGamePackageAsync(platformDownload, contentId);
                                                    }));
                                                }
                                                platformDownloadList.Add(platformDownload);
                                            }
                                            break;
                                        case 51000:
                                            {
                                                var key = contentId + "_xs";
                                                var platformDownload = new PlatformDownloadItem
                                                {
                                                    Platform = PlatformType.XboxSeries,
                                                    Key = key,
                                                    ContentId = contentId,
                                                    Category = "Game",
                                                    Market = _currentProductMarket!.Region,
                                                    FileSize = package.MaxDownloadSizeInBytes,
                                                    LatestSize = package.MaxDownloadSizeInBytes
                                                };
                                                if (XboxGameManager.Dictionary.TryGetValue(key, out var xboxGame))
                                                {
                                                    platformDownload.Url = xboxGame.Url;
                                                    platformDownload.Display = Path.GetFileName(xboxGame.Url);
                                                    if (platformDownload.LatestSize != xboxGame.FileSize)
                                                    {
                                                        platformDownload.Outdated = true;
                                                        platformDownload.FileSize = xboxGame.FileSize;
                                                        platformDownload.Display += $" ({ResourceHelper.GetString("Store.UpdateAvailable")})";
                                                        tasks.Add(Task.Run(async () =>
                                                        {
                                                            await GetGamePackageAsync(platformDownload, contentId);
                                                        }));
                                                    }
                                                }
                                                else
                                                {
                                                    tasks.Add(Task.Run(async () =>
                                                    {
                                                        await GetGamePackageAsync(platformDownload, contentId);
                                                    }));
                                                }

                                                platformDownloadList.Add(platformDownload);
                                            }
                                            break;
                                        default:
                                            {
                                                var version = RegexHelper.GetVersion().Match(package.PackageFullName).Value;
                                                var filename = package.PackageFullName + "." + package.PackageFormat;
                                                var key = filename.Replace(version, "").ToLowerInvariant();
                                                var platformDownload = platformDownloadList.FirstOrDefault(x => x.Category == "App" && x.Key == key);
                                                if (platformDownload == null)
                                                {
                                                    platformDownload = new PlatformDownloadItem
                                                    {
                                                        Platform = PlatformType.XboxOne,
                                                        Key = key,
                                                        ContentId = contentId,
                                                        Category = "App",
                                                        Market = _currentProductMarket!.Region,
                                                        FileSize = package.MaxDownloadSizeInBytes,
                                                        WuCategoryId = wuCategoryId,
                                                        AppVersion = version,
                                                        FileName = filename
                                                    };
                                                    platformDownloadList.Add(platformDownload);
                                                }
                                                else
                                                {
                                                    if (new Version(version) > new Version(platformDownload.AppVersion))
                                                    {
                                                        platformDownload.FileSize = package.MaxDownloadSizeInBytes;
                                                        platformDownload.AppVersion = version;
                                                        platformDownload.FileName = filename;
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                    break;
                                case "Windows.Desktop":
                                case "Windows.Universal":
                                    switch (package.PackageFormat.ToLowerInvariant())
                                    {
                                        case "msixvc":
                                            {
                                                var key = contentId;
                                                var platformDownload = new PlatformDownloadItem
                                                {
                                                    Platform = PlatformType.WindowsPc,
                                                    Key = key,
                                                    ContentId = contentId,
                                                    Category = "Game",
                                                    Market = _currentProductMarket!.Region,
                                                    FileSize = package.MaxDownloadSizeInBytes,
                                                    LatestSize = package.MaxDownloadSizeInBytes
                                                };
                                                if (XboxGameManager.Dictionary.TryGetValue(key, out var xboxGame))
                                                {
                                                    platformDownload.Url = xboxGame.Url;
                                                    platformDownload.Display = Path.GetFileName(xboxGame.Url);
                                                    if (platformDownload.LatestSize != xboxGame.FileSize)
                                                    {
                                                        platformDownload.Outdated = true;
                                                        platformDownload.FileSize = xboxGame.FileSize;
                                                        platformDownload.Display += $" ({ResourceHelper.GetString("Store.UpdateAvailable")})";
                                                        tasks.Add(Task.Run(async () =>
                                                        {
                                                            await GetGamePackageAsync(platformDownload, contentId);
                                                        }));
                                                    }
                                                }
                                                else
                                                {
                                                    tasks.Add(Task.Run(async () =>
                                                    {
                                                        await GetGamePackageAsync(platformDownload, contentId);
                                                    }));
                                                }
                                                platformDownloadList.Add(platformDownload);
                                            }
                                            break;
                                        case "appx":
                                        case "appxbundle":
                                        case "eappx":
                                        case "eappxbundle":
                                        case "msix":
                                        case "msixbundle":
                                            {
                                                var version = RegexHelper.GetVersion().Match(package.PackageFullName).Value;
                                                var filename = package.PackageFullName + "." + package.PackageFormat;
                                                var key = filename.Replace(version, "").ToLowerInvariant();
                                                var platformDownload = platformDownloadList.FirstOrDefault(x => x.Category == "App" && x.Key == key);
                                                if (platformDownload == null)
                                                {
                                                    platformDownload = new PlatformDownloadItem
                                                    {
                                                        Platform = PlatformType.WindowsPc,
                                                        Key = key,
                                                        ContentId = contentId,
                                                        Category = "App",
                                                        Market = _currentProductMarket!.Region,
                                                        FileSize = package.MaxDownloadSizeInBytes,
                                                        WuCategoryId = wuCategoryId,
                                                        AppVersion = version,
                                                        FileName = filename
                                                    };
                                                    platformDownloadList.Add(platformDownload);
                                                }
                                                else
                                                {
                                                    if (new Version(version) > new Version(platformDownload.AppVersion))
                                                    {
                                                        platformDownload.FileSize = package.MaxDownloadSizeInBytes;
                                                        platformDownload.AppVersion = version;
                                                        platformDownload.FileName = filename;
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                    if (platformDownloadList.Count > 0)
                    {
                        List<PlatformDownloadItem> app = [];
                        foreach (var platformDownload in platformDownloadList.Where(x => x.Category == "App"))
                        {
                            if (_xboxAppPackage.TryGetValue(platformDownload.FileName.ToLowerInvariant(), out var appData) && (DateTime.Now - appData.Date).TotalSeconds <= 300)
                            {
                                var result = ExpireLinkRegex().Match(appData.Url);
                                var display = result.Success
                                    ? $"{platformDownload.FileName} ({string.Format(ResourceHelper.GetString("Store.Expires"), DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime())})"
                                    : platformDownload.FileName;

                                platformDownload.Url = appData.Url;
                                platformDownload.Display = display;
                                platformDownload.FileSize = appData.Size;
                            }
                            else app.Add(platformDownload);
                        }

                        if (app.Count >= 1)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                await GetAppPackageAsync(wuCategoryId, app);
                            }));
                        }

                        PlatformDownloadInfo.AddRange(platformDownloadList.OrderBy(p => p.Platform));
                    }
                }

                var msrp = displaySkuAvailabilities.Availabilities[0].OrderManagementData.Price.MSRP;
                if (msrp > 0)
                {
                    IsEnablePriceComparison = true;
                    var currencyCode = displaySkuAvailabilities.Availabilities[0].OrderManagementData.Price.CurrencyCode.ToUpperInvariant();

                    var listPrice1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
                    var listPrice2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
                    var listPrice = listPrice2 > 0 && listPrice2 < listPrice1 ? listPrice2 : listPrice1;

                    Price = $"{currencyCode}: {listPrice1:N2}";
                    if (listPrice < msrp)
                        Price += $" ({string.Format(ResourceHelper.GetString("Store.Discounted"), msrp.ToString("N2"), ((1 - listPrice / msrp) * 100).ToString("N0"))})";

                    if (currencyCode != UseCurrencyCode)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            if (ExchangeRates.IsEmpty || NextUpdatedRates <= DateTime.UtcNow)
                            {
                                if (await ExchangeRateHelper.TryGetExchangeRatesAsync(UseCurrencyCode, ExchangeRates))
                                {
                                    NextUpdatedRates = DateTime.UtcNow.AddHours(12);
                                }
                            }
                            if (ExchangeRates.TryGetValue(currencyCode, out var value))
                            {
                                Price += $", {UseCurrencyCode}: {listPrice / value:N2}, {ResourceHelper.GetString("Store.ExchangeRate")}: {(1 / value):N8}";
                            }
                        }));
                    }
                }

                if (displaySkuAvailabilities.Sku.Properties.BundledSkus is { Count: >= 1 })
                {
                    bundledId.AddRange(displaySkuAvailabilities.Sku.Properties.BundledSkus.Select(bundledSkus => bundledSkus.BigId));
                }
            }

            var imageUri = localizedPropertie[0].Images.Where(x => x.ImagePurpose == "BoxArt").Select(x => x.Uri).FirstOrDefault()
                           ?? localizedPropertie[0].Images.Where(x => x.Width == x.Height).OrderByDescending(x => x.Width).Select(x => x.Uri).FirstOrDefault();
            if (!string.IsNullOrEmpty(imageUri))
            {
                if (imageUri.StartsWith("//")) imageUri = "https:" + imageUri;
            }

            ProductLoaded = true;

            if (!string.IsNullOrEmpty(imageUri) && Uri.IsWellFormedUriString(imageUri, UriKind.Absolute))
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var response = await HttpClientHelper.SendRequestAsync(imageUri + "?w=177&h=177");
                    if (response is { IsSuccessStatusCode: true })
                    {
                        var buffer = await response.Content.ReadAsByteArrayAsync();
                        using var stream = new MemoryStream(buffer);
                        BoxArt = new Bitmap(stream);
                    }
                }));
            }

            if (bundledId.Count >= 1 && GameData.Products.Count == 1)
            {
                SelectedBundled = new Bundled { Title = string.Format(ResourceHelper.GetString("Store.InThisBundle"), bundledId.Count), ProductId = product.ProductId };
                BundledMappings.Add(SelectedBundled);
                SelectedBundledIndex = 0;
                BundledLoaded = true;

                tasks.Add(Task.Run(async () =>
                {
                    var url = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={string.Join(',', bundledId.ToArray())}&market={_currentProductMarket?.Code}&languages={_currentProductMarket?.Language},neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
                    var responseString = await HttpClientHelper.GetStringContentAsync(url);
                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        Game? gameData = null;
                        try
                        {
                            gameData = JsonSerializer.Deserialize<Game>(responseString, JsonHelper.PropertyNameInsensitive);
                        }
                        catch
                        {
                            // ignored
                        }
                        if (gameData is { Products.Count: >= 1 })
                        {
                            GameData.Products.AddRange(gameData.Products);
                            foreach (var item in gameData.Products)
                            {
                                BundledMappings.Add(new Bundled { Title = item.LocalizedProperties[0].ProductTitle, ProductId = item.ProductId });
                            }
                        }
                    }

                }));
            }

            await Task.WhenAll(tasks);
        }
    }

    private readonly ConcurrentDictionary<string, DateTime> _platformPackageFetchTimes = new();

    private async Task GetGamePackageAsync(PlatformDownloadItem platformDownload, string contentId)
    {
        if (!_platformPackageFetchTimes.TryGetValue(platformDownload.Key, out var value) || DateTime.Compare(value, DateTime.Now) < 0)
        {
            _platformPackageFetchTimes[platformDownload.Key] = DateTime.Now.AddMinutes(3);

            var responseString = await HttpClientHelper.GetStringContentAsync(
                $"{UpdateService.Website}/Game/GetGamePackage?contentId={contentId}&platform={(int)platformDownload.Platform}",
                name: nameof(XboxDownload)
            );

            XboxPackage.Game? game = null;
            if (!string.IsNullOrWhiteSpace(responseString))
            {
                try
                {
                    game = JsonSerializer.Deserialize<XboxPackage.Game>(responseString,
                        JsonHelper.PropertyNameInsensitive);
                }
                catch
                {
                    // ignored
                }
            }

            if (game is { Code: "200", Data: not null })
            {
                var version = new Version(RegexHelper.GetVersion().Match(game.Data.Url).Value);
                if (XboxGameManager.Dictionary.TryGetValue(platformDownload.Key, out var xboxGame))
                {
                    if (version > xboxGame.Version)
                    {
                        xboxGame.Version = version;
                        xboxGame.FileSize = game.Data.Size;
                        xboxGame.Url = game.Data.Url;
                        _ = XboxGameManager.SaveAsync();
                    }
                }
                else
                {
                    xboxGame = new XboxGameManager.Product
                    {
                        Version = version,
                        FileSize = game.Data.Size,
                        Url = game.Data.Url
                    };
                    XboxGameManager.Dictionary.TryAdd(platformDownload.Key, xboxGame);
                    _ = XboxGameManager.SaveAsync();
                }

                platformDownload.Url = xboxGame.Url;
                platformDownload.Display = Path.GetFileName(xboxGame.Url);
                if (platformDownload.LatestSize != xboxGame.FileSize)
                {
                    platformDownload.Outdated = true;
                    platformDownload.FileSize = xboxGame.FileSize;
                    platformDownload.Display += $" ({ResourceHelper.GetString("Store.UpdateAvailable")})";
                }
                else platformDownload.Outdated = false;
            }
        }

        if ((string.IsNullOrEmpty(platformDownload.Url) || platformDownload.Outdated)
            && platformDownload.Platform is PlatformType.XboxOne or PlatformType.WindowsPc
            && !string.IsNullOrEmpty(App.Settings.Authorization))
        {
            const string host = "packagespc.xboxlive.com";
            var ipAddresses = App.Settings.IsDoHEnabled
                ? await DnsHelper.ResolveDohAsync(host, DnsHelper.CurrentDoH)
                : await DnsHelper.ResolveDnsAsync(host, Ioc.Default.GetRequiredService<ServiceViewModel>().DnsIp);
            if (ipAddresses?.Count > 0)
            {
                var headers = new Dictionary<string, string>() { { "Host", host }, { "Authorization", App.Settings.Authorization } };
                using var response = await HttpClientHelper.SendRequestAsync($"https://{ipAddresses[0].ToString()}/GetBasePackage/{contentId}", headers: headers);
                if (response is { IsSuccessStatusCode: true })
                {
                    var json = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var packageFiles = root.GetProperty("PackageFiles").EnumerateArray();
                        var matchedPackageFile = packageFiles.FirstOrDefault(x =>
                        {
                            var url = x.GetProperty("RelativeUrl").GetString();
                            var ext = Path.GetExtension(url)?.ToLowerInvariant();
                            return ext != ".phf" && ext != ".xsp";
                        });
                        if (matchedPackageFile.ValueKind != JsonValueKind.Undefined)
                        {
                            var gameUrl = matchedPackageFile.GetProperty("CdnRootPaths")[0].GetString()!.Replace(".xboxlive.cn", ".xboxlive.com") + matchedPackageFile.GetProperty("RelativeUrl").GetString();
                            var version = new Version(RegexHelper.GetVersion().Match(gameUrl).Value);
                            var fileSize = matchedPackageFile.GetProperty("FileSize").GetInt64();

                            platformDownload.Outdated = false;
                            platformDownload.FileSize = fileSize;
                            platformDownload.Url = gameUrl;
                            platformDownload.Display = Path.GetFileName(gameUrl);

                            var update = false;
                            if (XboxGameManager.Dictionary.TryGetValue(platformDownload.Key, out var xboxGame))
                            {
                                if (version > xboxGame.Version)
                                {
                                    xboxGame.Version = version;
                                    xboxGame.FileSize = fileSize;
                                    xboxGame.Url = gameUrl;
                                    update = true;
                                }
                            }
                            else
                            {
                                xboxGame = new XboxGameManager.Product
                                {
                                    Version = version,
                                    FileSize = fileSize,
                                    Url = gameUrl
                                };
                                update = true;
                            }
                            if (update)
                            {
                                XboxGameManager.Dictionary.AddOrUpdate(platformDownload.Key, xboxGame, (_, _) => xboxGame);
                                _ = XboxGameManager.SaveAsync();
                                _ = HttpClientHelper.GetStringContentAsync(UpdateService.Website + "/Game/AddGameUrl?url=" + HttpUtility.UrlEncode(xboxGame.Url), method: "PUT", name: "XboxDownload");
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized || response?.StatusCode == HttpStatusCode.Forbidden || response?.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    App.Settings.Authorization = string.Empty;
                    SettingsManager.Save(App.Settings);
                }
            }
        }

        if (string.IsNullOrEmpty(platformDownload.Url))
            platformDownload.Display = string.Empty;
    }

    private readonly ConcurrentDictionary<string, XboxPackage.AppData> _xboxAppPackage = new();

    private async Task GetAppPackageAsync(string wuCategoryId, List<PlatformDownloadItem> platformDownloadList)
    {
        var responseString = await HttpClientHelper.GetStringContentAsync(
            $"{UpdateService.Website}/Game/GetAppPackage?WuCategoryId={wuCategoryId}",
            name: nameof(XboxDownload)
        );

        XboxPackage.App? app = null;
        if (!string.IsNullOrWhiteSpace(responseString))
        {
            try
            {
                app = JsonSerializer.Deserialize<XboxPackage.App>(responseString,
                    JsonHelper.PropertyNameInsensitive);
            }
            catch
            {
                // ignored
            }
        }

        if (app is { Code: "200" })
        {
            foreach (var item in app.Data)
            {
                if (string.IsNullOrEmpty(item.Url)) continue;
                XboxPackage.AppData appData = new()
                {
                    Name = item.Name,
                    Size = item.Size,
                    Url = item.Url,
                    Date = DateTime.Now
                };
                _xboxAppPackage[item.Name.ToLowerInvariant()] = appData;
            }
        }

        foreach (var platformDownload in platformDownloadList)
        {
            var data = app?.Data.FirstOrDefault(x => RegexHelper.GetVersion().Replace(x.Name, "").Equals(platformDownload.Key, StringComparison.CurrentCultureIgnoreCase));
            if (data != null)
            {
                var result = ExpireLinkRegex().Match(data.Url);
                var display = result.Success
                    ? $"{data.Name} ({string.Format(ResourceHelper.GetString("Store.Expires"), DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(result.Groups[1].Value) * 1000).DateTime.ToLocalTime())})"
                    : data.Name;

                platformDownload.Url = data.Url;
                platformDownload.Display = display;
                platformDownload.FileSize = data.Size;
            }
            else platformDownload.Display = string.Empty;
        }
    }

    [RelayCommand]
    private async Task GetAllAppPackageAsync()
    {
        if (string.IsNullOrEmpty(SelectedPlatformDownloadItem?.WuCategoryId)) return;

        var tmp = new PlatformDownloadItem();
        PlatformDownloadInfo.Add(tmp);

        var responseString = await HttpClientHelper.GetStringContentAsync(
            $"{UpdateService.Website}/Game/GetAppPackage2?WuCategoryId={SelectedPlatformDownloadItem.WuCategoryId}",
            name: nameof(XboxDownload)
        );

        if (!PlatformDownloadInfo.Contains(tmp)) return;
        PlatformDownloadInfo.Remove(tmp);

        XboxPackage.App? app = null;
        if (!string.IsNullOrWhiteSpace(responseString))
        {
            try
            {
                app = JsonSerializer.Deserialize<XboxPackage.App>(responseString,
                    JsonHelper.PropertyNameInsensitive);
            }
            catch
            {
                // ignored
            }
        }

        if (app is { Code: "200" })
        {
            List<PlatformDownloadItem> platformDownloadList = [];
            foreach (var item in app.Data)
            {
                if (string.IsNullOrEmpty(item.Url)) continue;
                var platformDownload = PlatformDownloadInfo.FirstOrDefault(x => x.Display.StartsWith(item.Name, StringComparison.OrdinalIgnoreCase));
                if (platformDownload != null)
                {
                    platformDownload.Url = item.Url;
                    if (!_xboxAppPackage.TryGetValue(item.Name.ToLowerInvariant(), out var appData)) continue;
                    appData.Size = item.Size;
                    appData.Url = item.Url;
                    appData.Date = DateTime.Now;
                }
                else
                {
                    platformDownloadList.Add(new PlatformDownloadItem
                    {
                        Category = "App",
                        Display = item.Name,
                        FileSize = item.Size,
                        Url = item.Url
                    });
                }
            }
            if (platformDownloadList.Count > 0)
                PlatformDownloadInfo.AddRange(platformDownloadList.OrderBy(p => p.Display));
        }
    }

    [RelayCommand]
    private void OpenProduct(string parameter)
    {
        var productId = !string.IsNullOrEmpty(SelectedBundled?.ProductId) ? SelectedBundled.ProductId : GameData?.Products[0].ProductId;
        if (string.IsNullOrEmpty(productId)) return;

        var url = parameter switch
        {
            "Store" => $"ms-windows-store://pdp/?productid={productId}",
            "Xbox" => $"msxbox://game/?productId={productId}",
            _ => $"https://www.xbox.com/{_currentProductMarket?.Language}/games/store/_/{productId}"
        };
        HttpClientHelper.OpenUrl(url);
    }

    [RelayCommand]
    private static void QuickInstallation()
    {
        var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        mainWindowVm.SelectedTabIndex = 6;

        var toolsVm = Ioc.Default.GetRequiredService<ToolsViewModel>();
        toolsVm.FocusText();
    }

    [RelayCommand]
    private void OpenWebStore()
    {
        var productId = !string.IsNullOrEmpty(SelectedBundled?.ProductId) ? SelectedBundled.ProductId : GameData?.Products[0].ProductId;
        var url = string.IsNullOrEmpty(productId) ? UpdateService.Website : $"{UpdateService.Website}?{productId}";
        HttpClientHelper.OpenUrl(url);
    }

    [RelayCommand]
    private static void OpenConsoleGuide()
    {
        HttpClientHelper.OpenUrl(App.Settings.Culture == "zh-Hans"
            ? "https://www.bilibili.com/video/BV1CN4y197Js"
            : "https://www.youtube.com/watch?v=3F499kh_jfk");
    }

    [RelayCommand]
    private async Task CopyUrlAsync(string parameter)
    {
        if (string.IsNullOrEmpty(SelectedPlatformDownloadItem?.Url)) return;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            return;

        var url = SelectedPlatformDownloadItem.Url;
        switch (parameter)
        {
            case "Cn":
                {
                    url = url.Replace(".xboxlive.com", ".xboxlive.cn");
                    break;
                }
            case "Azure":
                {
                    var result = UrlPatternRegex().Match(url);
                    if (result.Success)
                        url = "http://xbasset" + result.Groups[1].Value.Replace("Z", "0") + ".blob.core.windows.net/" + result.Groups[2].Value;
                    break;
                }
        }
        await provider.SetTextAsync(url);
    }

    [RelayCommand]
    private async Task UpdateGameLinkAsync()
    {
        var platformDownload = SelectedPlatformDownloadItem;

        if (string.IsNullOrEmpty(App.Settings.Authorization))
        {
            string xbl = string.Empty;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                xbl = await XboxAuthHelper.GetXbl3TokenAsync(
                   interactive: false,
                   cancellationToken: cts.Token);
            }
            catch
            {
                // ignored
            }
            if (string.IsNullOrEmpty(xbl)) return;

            App.Settings.Authorization = xbl;
            SettingsManager.Save(App.Settings);
        }
        platformDownload?.Display = ResourceHelper.GetString("Store.FetchingDownloadLink");
        await GetGamePackageAsync(platformDownload!, platformDownload!.ContentId);
    }
    

    [GeneratedRegex(@"P1=(\d+)")]
    private static partial Regex ExpireLinkRegex();

    [GeneratedRegex(@"https?://[^\.]+\.xboxlive\.com/(\d{1,2}|Z)/(.+)")]
    private static partial Regex UrlPatternRegex();
}