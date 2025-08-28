using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;

namespace XboxDownload.ViewModels.Dialog;

public partial class EditHostsDialogViewModel : ObservableObject
{
    public EditHostsDialogViewModel()
    {
        _ = ReadHostsAsync();
    }

    public Action? CloseDialog { get; init; }

    [ObservableProperty]
    private string _content = string.Empty;

    [RelayCommand]
    private async Task SaveHostsAsync()
    {
        try
        {
            await File.WriteAllTextAsync(PathHelper.SystemHostsPath, Content.Trim() + Environment.NewLine);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowInfoDialogAsync(
                ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileFailed"),
                ResourceHelper.GetString("SpeedTest.MenuItem.WriteToSystemHostsFileFailedMsg") + Environment.NewLine + Environment.NewLine + ex.Message,
                Icon.Error);
        }
        CloseDialog?.Invoke();
    }

    [RelayCommand]
    private async Task ReadHostsAsync()
    {
        Content = await File.ReadAllTextAsync(PathHelper.SystemHostsPath);
    }
}