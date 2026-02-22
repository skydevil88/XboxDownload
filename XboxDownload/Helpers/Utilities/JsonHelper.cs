using System.Text.Json;

namespace XboxDownload.Helpers.Utilities;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true
    };

    public static readonly JsonSerializerOptions PropertyNameInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}