using System;

namespace XboxDownload.Models.Storage;

public class MbrCacheEntry
{
    public string MbrHex { get; init; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}