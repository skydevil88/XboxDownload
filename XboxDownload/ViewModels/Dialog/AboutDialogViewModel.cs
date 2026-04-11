using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Services;

namespace XboxDownload.ViewModels.Dialog;

public partial class AboutDialogViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Version { get; set; } = string.Format(ResourceHelper.GetString("About.Version"),
        Assembly.GetEntryAssembly()?
        .GetCustomAttribute<AssemblyFileVersionAttribute>()?
        .Version);

    [ObservableProperty]
    public partial bool IsChineseUser { get; set; } = App.Settings.Culture == "zh-Hans";

    public static string Project => UpdateService.Project;

    [RelayCommand]
    private static async Task OpenUrlAsync()
    {
        await HttpClientHelper.OpenUrlAsync(Project);
    }

    [RelayCommand]
    private static async Task CopyAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            return;

        await ClipboardHelper.SetTextAsync(provider, "TT9CzksU5KuXkkYaox2ifvF5tbGaQRmSZw");
    }
}
