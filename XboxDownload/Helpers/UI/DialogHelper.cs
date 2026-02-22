using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using System.Threading.Tasks;

namespace XboxDownload.Helpers.UI;

public static class DialogHelper
{
    public static async Task ShowInfoDialogAsync(string title, string message, Icon icon = Icon.None)
    {
        var mainWindow = GetMainWindow();

        var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            ContentTitle = title,
            ContentMessage = message,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 300,
            MaxWidth = 600,
            MaxHeight = 550,
            Icon = icon
        });

        if (mainWindow is not null)
            await msgBox.ShowWindowDialogAsync(mainWindow);
        else
            await msgBox.ShowAsync();
    }

    public static async Task<bool> ShowConfirmDialogAsync(string title, string message, Icon icon = Icon.None, bool defaultYes = true)
    {
        var mainWindow = GetMainWindow();

        var msgBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = title,
            ContentMessage = message,
            Icon = icon,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 300,
            MaxWidth = 600,
            MaxHeight = 550,
            ButtonDefinitions =
            [
                new ButtonDefinition { Name = "Yes", IsCancel = false, IsDefault = defaultYes },
                new ButtonDefinition { Name = "No", IsCancel = true, IsDefault = !defaultYes }
            ],
        });

        var result = mainWindow is not null
            ? await msgBox.ShowWindowDialogAsync(mainWindow)
            : await msgBox.ShowAsync();

        return string.Equals(result, "Yes", StringComparison.OrdinalIgnoreCase);
    }

    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }
}