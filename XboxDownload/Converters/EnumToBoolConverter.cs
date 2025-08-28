using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace XboxDownload.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool and true ? Enum.Parse(targetType, parameter?.ToString() ?? string.Empty) : AvaloniaProperty.UnsetValue;
    }
}