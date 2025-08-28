using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class MainWindow : Window
{
    private bool _isSystemShutdown;
    
    public MainWindow()
    {
        InitializeComponent();
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownRequested += async (_, e) =>
            {
                _isSystemShutdown = true;
                
                e.Cancel = true;
                
                if (desktop.MainWindow?.DataContext is MainWindowViewModel mainVm)
                {
                    var serviceVm = mainVm.ServiceViewModel;
                    if (serviceVm.IsListening)
                    {
                        await serviceVm.ToggleListeningAsync();
                    }
                    
                    var toolsVm = mainVm.ToolsViewModel;
                    toolsVm.Dispose();
                }

                desktop.Shutdown();
            };
        }
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_isSystemShutdown || !OperatingSystem.IsWindows())
        {
            // Allow actual shutdown on system exit
            base.OnClosing(e);
            return;
        }

        // User clicked close button → hide the window instead of closing
        e.Cancel = true;
        Hide();
    }
    
    private async void ShowStartupSettingsDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            
            var dialog = new Dialog.StartupSettingsDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
    
    private async void ShowAboutDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            
            var dialog = new Dialog.AboutDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
}