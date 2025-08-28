using System;

namespace XboxDownload.Models.Dns;

public static class Extension
{
    public static int GetBits(this byte b, int start, int length)
    {
        if (start < 0 || length <= 0 || start + length > 8)
            throw new ArgumentOutOfRangeException(nameof(start), $"Invalid start ({start}) or length ({length})");

        return (b >> (8 - start - length)) & ((1 << length) - 1);
    }

    public static byte SetBits(this byte b, int data, int start, int length)
    {
        if (start < 0 || length <= 0 || start + length > 8)
            throw new ArgumentOutOfRangeException(nameof(start), $"Invalid start ({start}) or length ({length})");

        var mask = ((1 << length) - 1) << (8 - start - length);
        b = (byte)(b & ~mask); // Clear the original area
        b |= (byte)((data << (8 - start - length)) & mask); // Set new value
        return b;
    }
}
