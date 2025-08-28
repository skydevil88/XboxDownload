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
    private static NativeMenuItem? _showItem, _exitItem;

    public void Initialize()
    {
        if (_trayIcon != null) return;

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri($"avares://{nameof(XboxDownload)}/Assets/xbox.ico"))),
            Menu = BuildTrayMenu(),
            IsVisible = true
        };
        UpdateToolTip();

        // macOS 下 Clicked 事件不会触发（由系统托盘行为决定）
        if (!OperatingSystem.IsMacOS())
        {
            _trayIcon.Clicked += (_, _) => ShowMainWindow();
        }
    }

    private static NativeMenu BuildTrayMenu()
    {
        var menu = new NativeMenu();

        _showItem = new NativeMenuItem();
        _showItem.Click += (_, _) => ShowMainWindow();

        _exitItem = new NativeMenuItem();
        _exitItem.Click += (_, _) => _ = ExitAsync();

        menu.Items.Add(_showItem);
        menu.Items.Add(_exitItem);

        return menu;
    }

    public void UpdateToolTip()
    {
        if (_trayIcon != null) _trayIcon.ToolTipText = ResourceHelper.GetString($"App.Title");
        if (_showItem != null) _showItem.Header = ResourceHelper.GetString($"Menu.Show");
        if (_exitItem != null) _exitItem.Header = ResourceHelper.GetString($"Menu.Exit");
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null) return;

        var window = desktop.MainWindow;

        // 恢复最小化窗口
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (!desktop.MainWindow.ShowInTaskbar)
        {
            desktop.MainWindow.ShowInTaskbar = true;
        }

        // 确保窗口可见
        if (!window.IsVisible)
        {
            window.Show();
        }

        // 激活窗口
        if (!window.IsActive)
        {
            window.Activate();
        }

        // 仅在 Windows 上使用 Topmost 技巧
        if (!OperatingSystem.IsWindows()) return;
        window.Topmost = true;
        window.Topmost = false;
    }

    private static async Task ExitAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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
        }
    }
}