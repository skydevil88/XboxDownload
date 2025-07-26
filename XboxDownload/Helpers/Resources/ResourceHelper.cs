using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace XboxDownload.Helpers.Resources;

public static class ResourceHelper
{
    /// <summary>
    /// Retrieves a localized string resource from the current Avalonia Application resources by key.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="fallback">
    /// A fallback string to return if the resource is not found or the value is null.
    /// If not specified, the key itself will be used as the fallback.
    /// </param>
    /// <returns>
    /// The string value of the resource if found; otherwise, returns the fallback or the key.
    /// </returns>
    public static string GetString(string key, string? fallback = null)
    {
        if (Application.Current?.TryFindResource(key, out var value) == true)
        {
            return value switch
            {
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? fallback ?? key
            };
        }

        return fallback ?? key;
    }
}