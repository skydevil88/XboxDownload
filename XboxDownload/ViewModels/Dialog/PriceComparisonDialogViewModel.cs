using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models.Store;
using XboxDownload.Services;

namespace XboxDownload.ViewModels.Dialog;

public partial class PriceComparisonDialogViewModel : ObservableObject
{
    private readonly StoreViewModel _storeViewModel;

    [ObservableProperty]
    private string _title, _useCurrencyCode;

    private readonly string? _productId;

    public ObservableCollection<MarketMappingEntry> MarketMappings { get; } = [];

    [ObservableProperty]
    private MarketMappingEntry? _selectedMarket;

    public string ConvertedPrice => string.Format(ResourceHelper.GetString("Store.PriceComparison.ConvertedPrice"), UseCurrencyCode);

    public PriceComparisonDialogViewModel(StoreViewModel storeViewModel)
    {
        _storeViewModel = storeViewModel;
        Title = _storeViewModel.Title!;
        _useCurrencyCode = _storeViewModel.UseCurrencyCode;
        _productId = _storeViewModel.SelectedBundledIndex <= 0 ? _storeViewModel.GameData?.Products[0].ProductId : _storeViewModel.SelectedBundled?.ProductId;

        // 要排除的 Language（根据货币或不支持的地区）
        var excludedLanguages = new HashSet<string>
        {
            // 列支敦士登 - 瑞士法郎
            "de-LI", 

            // 欧元区国家（欧盟成员国，官方货币为欧元，语言代码）
            // 16个欧盟成员国语言代码
            "de-AT", // 奥地利
            "nl-BE", // 比利时
            "de-DE", // 德国
            "fr-FR", // 法国
            "fi-FI", // 芬兰
            "nl-NL", // 荷兰
            "pt-PT", // 葡萄牙
            "sk-SK", // 斯洛伐克
            "es-ES", // 西班牙
            "el-GR", // 希腊
            "it-IT", // 意大利
            "en-IE", // 爱尔兰
            "fr-LU", // 卢森堡（多语种，主要法语）
            "mt-MT", // 马耳他（官方语言之一为马耳他语）
            "en-CY", // 塞浦路斯（官方语言包括希腊语和土耳其语）
            "et-EE", // 爱沙尼亚 

            // 非欧盟使用欧元国家或地区语言代码
            "ca-AD", // 安道尔

            // 美元区（主要为小国家或属地）
            "ar-AE", "ru-RU",

            "fa-AF", "en-AG", "en-AI", "sq-AL", "hy-AM", "pt-AO", "en-AQ", "en-AS", "nl-AW","sv-AX", "az-AZ",
            "bs-BA", "en-BB", "fr-BF", "fr-BI", "fr-BJ", "fr-BL", "en-BM", "ms-BN", 
            
            // 不支持或无本地商店的地区
            "ar-LB",
        };

        // 合并市场并过滤
        var filteredMarkets = _storeViewModel.Markets
            .Union(MarketBuilder.BuildMarket2())
            .Where(m => !excludedLanguages.Contains(m.Language))
            .ToList();

        // 构造 MarketMappingEntry 列表
        var marketMappings = filteredMarkets
            .Select(m => new MarketMappingEntry(m.Region, m.Language))
            .ToList();

        // 添加额外的区域项
        marketMappings.Add(new MarketMappingEntry(ResourceHelper.GetString("Store.PriceComparison.Eurozone"), "de-DE"));

        // 最终排序并添加
        MarketMappings.AddRange(
            marketMappings.OrderBy(m => m.Market, StringComparer.CurrentCultureIgnoreCase));

    }

    [RelayCommand]
    private void UpdateSelection(string? parameter)
    {
        switch (parameter)
        {
            case "SelectAll":
                foreach (var option in MarketMappings)
                    option.IsSelect = true;
                break;

            case "InvertSelection":
                foreach (var option in MarketMappings)
                    option.IsSelect = !option.IsSelect;
                break;
        }
    }

    public event Action? RequestFocus;

    [RelayCommand]
    private async Task QueryAsync()
    {
        if (string.IsNullOrEmpty(_productId)) return;

        if (string.IsNullOrWhiteSpace(UseCurrencyCode))
        {
            UseCurrencyCode = _storeViewModel.UseCurrencyCode;
        }
        else
        {
            if (!string.Equals(UseCurrencyCode, _storeViewModel.UseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidCurrencyCode(UseCurrencyCode))
                {
                    await DialogHelper.ShowInfoDialogAsync(
                        ResourceHelper.GetString("Store.PriceComparison.InvalidCurrencyTitle"),
                        ResourceHelper.GetString("Store.PriceComparison.InvalidCurrencyMessage"),
                        Icon.Error);
                    RequestFocus?.Invoke();
                    return;
                }
                UseCurrencyCode = UseCurrencyCode.ToUpperInvariant();
                _storeViewModel.UseCurrencyCode = UseCurrencyCode;

                App.Settings.CurrencyCode = UseCurrencyCode;
                SettingsManager.Save(App.Settings);
            }
            else UseCurrencyCode = UseCurrencyCode.ToUpperInvariant();
        }


        if (_storeViewModel.ExchangeRates.IsEmpty)
        {
            if (await ExchangeRateHelper.TryGetExchangeRatesAsync(UseCurrencyCode, _storeViewModel.ExchangeRates))
            {
                _storeViewModel.NextUpdatedRates = DateTime.UtcNow.AddHours(12);
            }
        }

        if (_storeViewModel.ExchangeRates.IsEmpty)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("Store.PriceComparison.ExchangeRateFetchFailedTitle"),
                ResourceHelper.GetString("Store.PriceComparison.ExchangeRateFetchFailedMessage"),
                Icon.Error);
            return;
        }
        
        foreach (var market in MarketMappings)
        {
            market.ConvertPrices = null;
            market.ExchangeRates = null;
        }

        OnPropertyChanged(nameof(ConvertedPrice));
        var tasks = MarketMappings.Where(s => s.IsSelect)
            .Select(market => Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(market.Currency))
                {
                    var url = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={_productId}&market={market.Code}&languages=neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
                    var responseString = await HttpClientHelper.GetStringContentAsync(url);
                    if (string.IsNullOrEmpty(responseString.Trim())) return;

                    Game? gameData = null;
                    try
                    {
                        gameData = JsonSerializer.Deserialize<Game>(responseString, JsonHelper.PropertyNameInsensitive);
                    }
                    catch
                    {
                        // ignored
                    }

                    if (gameData?.Products.Count >= 1)
                    {
                        var product = gameData.Products[0];
                        var displaySkuAvailabilities = product.DisplaySkuAvailabilities.FirstOrDefault(displaySkuAvailabilities => displaySkuAvailabilities.Sku.SkuType == "full");
                        if (displaySkuAvailabilities != null)
                        {
                            var listPrice1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
                            var listPrice2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
                            var listPrice = listPrice2 > 0 && listPrice2 < listPrice1 ? listPrice2 : listPrice1;

                            if (listPrice > 0)
                            {
                                var currencyCode = displaySkuAvailabilities.Availabilities[0].OrderManagementData.Price.CurrencyCode.ToUpperInvariant();
                                market.Currency = currencyCode;
                                market.ListPrice = listPrice;
                                if (currencyCode != UseCurrencyCode)
                                {
                                    if (_storeViewModel.ExchangeRates.TryGetValue(currencyCode, out var value))
                                    {
                                        market.ConvertPrices = listPrice / value;
                                        market.ExchangeRates = 1 / value;
                                    }
                                }
                                else
                                {
                                    market.ConvertPrices = listPrice;
                                    market.ExchangeRates = 1;
                                }
                            }
                            else
                            {
                                market.Currency = ResourceHelper.GetString("Store.PriceComparison.Unavailable");
                            }
                        }
                    }
                }
                else
                {
                    var currencyCode = market.Currency;
                    var listPrice = market.ListPrice;
                    if (currencyCode != UseCurrencyCode)
                    {
                        if (_storeViewModel.ExchangeRates.TryGetValue(currencyCode, out var value))
                        {
                            market.ConvertPrices = listPrice / value;
                            market.ExchangeRates = 1 / value;
                        }
                    }
                    else
                    {
                        market.ConvertPrices = listPrice;
                        market.ExchangeRates = 1;
                    }
                }
            }))
            .ToList();

        await Task.WhenAll(tasks);

        var sorted = MarketMappings
            .Where(m => m.ConvertPrices.HasValue)
            .OrderBy(m => m.ConvertPrices.GetValueOrDefault())
            .Concat(MarketMappings.Where(m => !m.ConvertPrices.HasValue))
            .ToList();

        MarketMappings.Clear();
        MarketMappings.AddRange(sorted);
    }

    [RelayCommand]
    private void GetSystemCurrencyCode()
    {
        _storeViewModel.UseCurrencyCode = UseCurrencyCode = ExchangeRateHelper.GetSystemCurrencyCode();

        App.Settings.CurrencyCode = string.Empty;
        SettingsManager.Save(App.Settings);

        RequestFocus?.Invoke();
    }

    [RelayCommand]
    private void VisitWebsite()
    {
        var language = SelectedMarket?.Language;
        if (language == "de-DE")
        {
            var raw = CultureInfo.CurrentUICulture.Name;
            language = CultureInfo.CurrentUICulture.Name switch
            {
                "de-AT" or "nl-BE" or "de-DE" or "fr-FR" or "fi-FI" or "nl-NL" or "pt-PT" or "sk-SK" or "es-ES"
                    or "el-GR" or "it-IT" or "en-IE" or "fr-LU" or "mt-MT" or "en-CY" or "et-EE" or "ca-AD" => raw,
                _ => language
            };
        }
        //var url = $"https://www.microsoft.com/{language}/p/_/{_productId}";
        var url = $"https://www.xbox.com/{language}/games/store/_/{_productId}";
        HttpClientHelper.OpenUrl(url);
    }

    private static bool IsValidCurrencyCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            return false;

        code = code.ToUpperInvariant();

        // 遍历所有已知的区域（culture）来匹配货币代码
        var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture =>
            {
                try
                {
                    return new RegionInfo(culture.Name);
                }
                catch
                {
                    return null;
                }
            })
            .Where(region => region != null)
            .DistinctBy(region => region!.ISOCurrencySymbol); // 避免重复

        return regions.Any(r => r!.ISOCurrencySymbol.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}

public partial class MarketMappingEntry(string market, string language) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelect = true;

    public string Market { get; set; } = market;

    public string Language { get; set; } = language;


    [ObservableProperty]
    private string _currency = string.Empty;

    [ObservableProperty]
    private decimal? _listPrice;

    [ObservableProperty]
    private decimal? _convertPrices;

    [ObservableProperty]
    private decimal? _exchangeRates;

    public string Code =>
        string.IsNullOrEmpty(Language) ? string.Empty :
        Language.Split('-').Length > 1 ? Language.Split('-')[1] : string.Empty;
}
