using System;
using System.Globalization;
using Avalonia.Data.Converters;
using XboxDownload.Models;

namespace XboxDownload.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum e)
            return e.GetDescription();
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/*
<UserControl.Resources>
<converters:EnumDescriptionConverter x:Key="EnumDescriptionConverter" />
</UserControl.Resources>

<TextBlock Text="{Binding SelectedPlatformType, Converter={StaticResource EnumDescriptionConverter}}" />
*/