using System;
using System.ComponentModel;
using System.Reflection;

namespace XboxDownload.Models;

public enum PlatformType
{
    [Description("Xbox One")]
    XboxOne = 0,

    [Description("Xbox Series X|S")]
    XboxSeries = 1,

    [Description("Windows PC")]
    WindowsPc = 9
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        if (fi == null) return value.ToString();
        var attr = fi.GetCustomAttribute<DescriptionAttribute>();
        return attr != null ? attr.Description : value.ToString();
    }
}