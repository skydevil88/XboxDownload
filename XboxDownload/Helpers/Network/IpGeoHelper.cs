using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegexHelper = XboxDownload.Helpers.Utilities.RegexHelper;

namespace XboxDownload.Helpers.Network;

public static class IpGeoHelper
{
    /// <summary>
    /// Queries multiple external APIs concurrently to get the country code for the specified IP address.
    /// </summary>
    /// <param name="ip">The IP address to check. If null, the public IP of the current device will be used.</param>
    /// <returns>
    /// The country code (e.g., "CN") from the first successful API response,
    /// or null if all queries fail or return no valid data.
    /// </returns>
    public static async Task<string?> GetCountryFromMultipleApisAsync(string? ip = null)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = cts.Token;

        var tasks = new List<Task<string?>>
        {
            QueryIpInfo(ip, token: token),
            QueryIpApi(ip, token: token),
            QueryIpApiCo(ip, token: token)
        };

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            try
            {
                var result = await completed;
                if (string.IsNullOrEmpty(result)) continue;
                _ = cts.CancelAsync();
                return result;
            }
            catch
            {
                // Ignore
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the specified or current IP address belongs to mainland China.
    /// </summary>
    /// <param name="ip">The IP address to check. If null, the public IP of the current device will be used.</param>
    /// <param name="useInternationalApis">Whether to use international IP geolocation APIs.</param>
    /// <param name="useMainlandChinaApis">Whether to use mainland China-based IP geolocation APIs.</param>
    /// <returns>
    /// Returns <c>true</c> if the IP is determined to be located in mainland China; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<bool> IsMainlandChinaAsync(
        string? ip = null, bool useInternationalApis = true, bool useMainlandChinaApis = true)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = cts.Token;

        var tasks = new List<Task<string?>>();

        if (useInternationalApis)
        {
            tasks.Add(QueryIpInfo(ip, token: token));
            tasks.Add(QueryIpApi(ip, token: token));
            tasks.Add(QueryIpApiCo(ip, token: token));
        }

        if (useMainlandChinaApis)
        {
            tasks.Add(QueryBaiduAsync(ip, token: token));
            tasks.Add(QueryZxIncAsync(ip, token: token));
        }

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            try
            {
                var result = await completedTask;
                if (string.IsNullOrEmpty(result)) continue;

                await cts.CancelAsync(); // Cancel remaining tasks
                return string.Equals(result, "CN", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Ignore
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieves the IP location, supporting multiple API queries, and selects different APIs based on whether the user is a Simplified Chinese user.
    /// </summary>
    /// <param name="ip">The IP address to query. If null, it queries the current IP.</param>
    /// <param name="isSimplifiedChineseUser">Indicates whether the user is a Simplified Chinese user, which determines which API to use for the query.</param>
    /// <returns>Returns the IP location or null if no result is found or an error occurs.</returns>
    public static async Task<string?> GetIpLocationFromMultipleApisAsync(string? ip = null, bool isSimplifiedChineseUser = false)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = cts.Token;

        var tasks = new List<Task<string?>>();

        if (isSimplifiedChineseUser)
        {
            tasks.Add(QueryBaiduAsync(ip, false, token));
            tasks.Add(QueryZxIncAsync(ip, false, token));
        }
        else
        {
            tasks.Add(QueryIpInfo(ip, false, token));
            tasks.Add(QueryIpApi(ip, false, token));
        }

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            try
            {
                var result = await completed;
                if (string.IsNullOrEmpty(result)) continue;
                _ = cts.CancelAsync();
                return result;
            }
            catch
            {
                // Ignore
            }
        }

        return null;
    }

    private static async Task<string?> QueryIpInfo(string? ip, bool onlyCountry = true, CancellationToken token = default)
    {
        var url = string.IsNullOrWhiteSpace(ip)
            ? "https://ipinfo.io/json"
            : $"https://ipinfo.io/{ip}/json";

        var json = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000, token: token);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var countryValue = doc.RootElement.GetProperty("country").GetString();

            if (onlyCountry)
            {
                return countryValue;
            }

            var regionValue = doc.RootElement.GetProperty("region").GetString();
            var cityValue = doc.RootElement.GetProperty("city").GetString();
            return $"{countryValue}, {regionValue}, {cityValue}";
        }
        catch { return null; }
    }

    private static async Task<string?> QueryIpApi(string? ip, bool onlyCountry = true, CancellationToken token = default)
    {
        var url = string.IsNullOrWhiteSpace(ip)
            ? "http://ip-api.com/json/"
            : $"http://ip-api.com/json/{ip}";

        var json = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000, token: token);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var countryCode = doc.RootElement.GetProperty("countryCode").GetString();

            if (onlyCountry)
            {
                return doc.RootElement.GetProperty("country").GetString();
            }

            var regionName = doc.RootElement.GetProperty("regionName").GetString();
            var city = doc.RootElement.GetProperty("city").GetString();
            return $"{countryCode}, {regionName}, {city}";
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> QueryIpApiCo(string? ip, CancellationToken token = default)
    {
        var url = string.IsNullOrWhiteSpace(ip)
            ? "https://ipapi.co/country/"
            : $"https://ipapi.co/{ip}/country/";

        var responseString = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000, token: token
            , headers: new Dictionary<string, string> { { "referer", "https://ipapi.co/" } });
        return responseString is { Length: 2 } && responseString.All(char.IsLetter)
            ? responseString.ToUpperInvariant()
            : null;
    }

    private static async Task<string?> QueryBaiduAsync(string? ip, bool onlyCountry = true, CancellationToken token = default)
    {
        var url = string.IsNullOrWhiteSpace(ip)
            ? "https://qifu-api.baidubce.com/ip/local/geo/v1/district"
            : $"https://qifu-api.baidubce.com/ip/geo/v1/district?ip={ip}";

        var json = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000, token: token);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("code", out var codeProp) || codeProp.GetString() != "Success")
                return null;

            var data = root.GetProperty("data");
            var country = data.GetProperty("country").GetString();
            var prov = data.GetProperty("prov").GetString();

            if (onlyCountry)
            {
                if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(prov))
                    return null;

                if (country != "中国")
                    return "NOT_CN";

                if (prov.Contains("香港") || prov.Contains("澳门") || prov.Contains("台湾"))
                    return "NOT_CN";

                return "CN";
            }

            var city = data.GetProperty("city").GetString();
            return $"{country}, {prov}, {city}";
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> QueryZxIncAsync(string? ip, bool onlyCountry = true, CancellationToken token = default)
    {
        var url = string.IsNullOrWhiteSpace(ip)
            ? "https://ip.zxinc.org/api.php?type=json"
            : $"https://ip.zxinc.org/api.php?type=json&ip={ip}";

        var json = await HttpClientHelper.GetStringContentAsync(url, timeout: 5000, token: token);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetInt32() != 0)
                return null;

            if (onlyCountry)
            {
                var country = root.GetProperty("data").GetProperty("country").GetString();

                if (string.IsNullOrWhiteSpace(country))
                    return null;

                if (country.Contains("中国")
                    && !country.Contains("香港") && !country.Contains("澳门") && !country.Contains("台湾"))
                    return "CN";
                return "NOT_CN";
            }

            var location = RegexHelper.MultipleSpacesRegex().Replace(root.GetProperty("data").GetProperty("location").GetString()!, " ").Trim();
            return location;
        }
        catch
        {
            return null;
        }
    }
}