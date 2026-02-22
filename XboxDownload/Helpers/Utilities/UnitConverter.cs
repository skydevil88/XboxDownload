using System;

namespace XboxDownload.Helpers.Utilities;

public static class UnitConverter
{
    private static readonly string[] ByteUnits = ["Bytes", "KB", "MB", "GB", "TB", "PB"];
    private static readonly string[] BitUnits = ["bps", "Kbps", "Mbps", "Gbps", "Tbps", "Pbps"];
    private const double Log1024 = 6.931471805599453;  // Math.Log(1024)
    private const double Log1000 = 6.907755278982137;  // Math.Log(1000)
    private const int BinaryBase = 1024;
    private const int DecimalBase = 1000;

    public static string ConvertBytes(long bytes)
    {
        return bytes switch
        {
            <= 0 => "0 Bytes",
            1 => "1 Byte",
            _ => FormatValue(bytes, ByteUnits, Log1024, BinaryBase)
        };
    }

    public static string ConvertBps(long bps)
    {
        return bps switch
        {
            <= 0 => "0 bps",
            _ => FormatValue(bps, BitUnits, Log1000, DecimalBase)
        };
    }

    private static string FormatValue(long value, string[] units, double logBase, int baseUnit)
    {
        var order = Math.Min(
            (int)(Math.Log(value) / logBase),
            units.Length - 1);

        if (value < baseUnit) order = 0;

        var converted = value / Math.Pow(baseUnit, order);

        return order > 0
            ? $"{converted:N2} {units[order]}"
            : $"{value:N0} {units[0]}";
    }
}