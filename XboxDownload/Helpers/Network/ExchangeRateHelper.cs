using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace XboxDownload.Helpers.Network;

public static class ExchangeRateHelper
{
    public static async Task<bool> TryGetExchangeRatesAsync(string currency, ConcurrentDictionary<string, decimal> exchangeRates)
    {
        ArgumentNullException.ThrowIfNull(exchangeRates);

        var url = $"https://latest.currency-api.pages.dev/v1/currencies/{currency.ToLowerInvariant()}.min.json";
        var responseString = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000);

        if (string.IsNullOrEmpty(responseString))
        {
            url = $"https://testingcf.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{currency.ToLowerInvariant()}.min.json";
            responseString = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000);
        }

        if (string.IsNullOrEmpty(responseString))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (!root.TryGetProperty(currency.ToLowerInvariant(), out var currencyNode))
                return false;

            exchangeRates.Clear();
            foreach (var property in currencyNode.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number &&
                    property.Value.TryGetDecimal(out var rate))
                {
                    exchangeRates[property.Name.ToUpperInvariant()] = rate;
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("JSON error: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("General error: " + ex.Message);
        }

        return false;
    }

    public static string GetSystemCurrencyCode()
    {
        var region = new RegionInfo(CultureInfo.CurrentCulture.Name);
        return region.ISOCurrencySymbol.ToUpperInvariant();
    }
}