using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using XboxDownload.Helpers.Resources;
using XboxDownload.ViewModels;

namespace XboxDownload.Services;

public class TrayIconService : IDisposable
{
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _showItem, _exitItem;

    public void Initialize()
    {
        if (_trayIcon != null) return;

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(
                new Uri($"avares://{nameof(XboxDownload)}/Assets/xbox.ico"))),
            Menu = BuildTrayMenu(),
            IsVisible = true
        };

        UpdateToolTip();

        if (!OperatingSystem.IsMacOS())
        {
            _trayIcon.Clicked += (_, _) => ShowMainWindow();
        }
    }

    private NativeMenu BuildTrayMenu()
    {
        var menu = new NativeMenu();

        _showItem = new NativeMenuItem();
        _showItem.Click += (_, _) => ShowMainWindow();

        _exitItem = new NativeMenuItem();
        _exitItem.Click += async (_, _) => await ExitAsync();

        menu.Items.Add(_showItem);
        menu.Items.Add(_exitItem);

        return menu;
    }

    private void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow == null) return;

        var window = desktop.MainWindow;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.ShowInTaskbar = true;

        if (!window.IsVisible)
            window.Show();

        if (!window.IsActive)
            window.Activate();

        if (OperatingSystem.IsWindows())
        {
            window.Topmost = true;
            window.Topmost = false;
        }
    }

    private async Task ExitAsync()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                var serviceVm = mainVm.ServiceViewModel;
                if (serviceVm.IsListening)
                    await serviceVm.ToggleListeningAsync();

                mainVm.ToolsViewModel.Dispose();
            }

            Dispose();
            desktop.Shutdown();
        }
    }

    public void UpdateToolTip()
    {
        _trayIcon?.ToolTipText = ResourceHelper.GetString("App.Title");
        _showItem?.Header = ResourceHelper.GetString("Menu.Show");
        _exitItem?.Header = ResourceHelper.GetString("Menu.Exit");
    }

    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        
        GC.SuppressFinalize(this);
    }
}