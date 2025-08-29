using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;

namespace XboxDownload.ViewModels.Dialog;

public partial class StartupSettingsDialogViewModel : ObservableObject
{
    [ObservableProperty] private bool _isRunAtStartup;

    public StartupSettingsDialogViewModel()
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Tasks", nameof(XboxDownload))))
        {
            IsRunAtStartup = true;
        }
    }

    public Action? CloseDialog { get; init; }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (IsRunAtStartup)
        {
            var taskPath = Path.Combine(Path.GetTempPath(), $"{nameof(XboxDownload)}Task.xml");

            try
            {
                await using var stream = AssetLoader.Open(new Uri($"avares://{nameof(XboxDownload)}/Resources/Task.xml"));
                using var reader = new StreamReader(stream, Encoding.Unicode); // UTF-16 LE BOM

                var xml = string.Format(await reader.ReadToEndAsync(), Environment.ProcessPath);
                await File.WriteAllTextAsync(taskPath, xml, Encoding.Unicode);

                var cmd = $"schtasks /create /xml \"{taskPath}\" /tn \"{nameof(XboxDownload)}\" /f";
                await CommandHelper.RunCommandAsync("cmd.exe", $"/c \"{cmd}\"");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Startup.StartupFailedTitle"),
                    string.Format(ResourceHelper.GetString("Startup.StartupFailedMessage"), ex.Message),
                    Icon.Error);
                return;
            }
            finally
            {
                if (File.Exists(taskPath))
                    File.Delete(taskPath);
            }
        }
        else
        {
            const string cmd = $"schtasks /delete /tn \"{nameof(XboxDownload)}\" /f & schtasks /delete /tn \"{nameof(XboxDownload)}\" /f";
            try
            {
                await CommandHelper.RunCommandAsync("cmd.exe", $"/c \"{cmd}\"");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Startup.StartupFailedTitle"),
                    string.Format(ResourceHelper.GetString("Startup.StartupFailedMessage"), ex.Message),
                    Icon.Error);
                return;
            }
        }

        CloseDialog?.Invoke();
    }
}