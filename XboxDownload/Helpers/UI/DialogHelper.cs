using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

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

    public static async Task<bool> ShowConfirmDialogAsync(string title, string message, Icon icon = Icon.None)
    {
        var mainWindow = GetMainWindow();

        var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.YesNo,
            ContentTitle = title,
            ContentMessage = message,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 300,
            MaxWidth = 600,
            MaxHeight = 550,
            Icon = icon
        });

        var result = mainWindow is not null
            ? await msgBox.ShowWindowDialogAsync(mainWindow)
            : await msgBox.ShowAsync();

        return result == ButtonResult.Yes;
    }


    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }
}