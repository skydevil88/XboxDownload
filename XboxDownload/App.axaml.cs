using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using XboxDownload.Models;
using XboxDownload.Services;
using XboxDownload.ViewModels;
using XboxDownload.Views;

namespace XboxDownload;

public partial class App : Application
{
    public static TrayIconService TrayIconService { get; } = new();
    public static AppSettings Settings { get; private set; } = new();
    public static IServiceProvider? Services { get; private set; }

    // Windows wake-up message ID (assigned in Program)
    public static uint ShowWindowMessageId;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const int GWLP_WNDPROC = -4;
    private WndProcDelegate? _newWndProc;
    private IntPtr _oldWndProc;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Settings = SettingsManager.Load();
        LoadLanguage();
    }
    
    private void LoadLanguage()
    {
        var culture = Settings.Culture;

        if (culture is not ("en-US" or "zh-Hans" or "zh-Hant"))
        {
            culture = CultureInfo.CurrentUICulture.Name switch
            {
                "zh" or "zh-CN" or "zh-SG" or "zh-Hans" or "zh-Hans-CN" or "zh-Hans-SG" => "zh-Hans",
                "zh-HK" or "zh-MO" or "zh-TW" or "zh-Hant" or "zh-Hant-HK" or "zh-Hant-MO" or "zh-Hant-TW" => "zh-Hant",
                _ => "en-US"
            };
            if (string.IsNullOrEmpty(Settings.DohServerId))
            {
                Settings.DohServerId = culture == "zh-Hans" ? "AlibabaCloud" : "Google";
            }
            Settings.Culture = culture;
            SettingsManager.Save(Settings);
        }

        var langDict = new ResourceInclude(new Uri($"resm:Styles?assembly={nameof(XboxDownload)}"))
        {
            Source = new Uri($"avares://{nameof(XboxDownload)}/Resources/Languages/{culture}.axaml")
        };
        Resources.MergedDictionaries.Add(langDict);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = Setup.ConfigureServices();
        
        Ioc.Default.ConfigureServices(new ServiceCollection()
            .AddSingleton<TrayIconService>()
            .AddSingleton<MainWindowViewModel>()
            .AddSingleton<ServiceViewModel>()
            .AddSingleton<SpeedTestViewModel>()
            .AddSingleton<HostViewModel>()
            .AddSingleton<CdnViewModel>()
            .AddSingleton<StorageViewModel>()
            .AddSingleton<StoreViewModel>()
            .AddSingleton<ToolsViewModel>()
            .BuildServiceProvider());
        
        if (OperatingSystem.IsWindows())
        {
            var trayService = Ioc.Default.GetRequiredService<TrayIconService>();
            trayService.Initialize();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Set application theme
            var themeVariant = Settings.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            RequestedThemeVariant = themeVariant;

            var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainWindowVm };

            // Windows message hook
            if (OperatingSystem.IsWindows())
            {
                var handle = desktop.MainWindow.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    _newWndProc = WndProc;
                    _oldWndProc = GetWindowLongPtr(handle, GWLP_WNDPROC);
                    SetWindowLongPtr(handle, GWLP_WNDPROC, _newWndProc);
                }
            }

            // Launch on system startup
            if (desktop.Args?.Contains("Startup") == true)
            {
                desktop.MainWindow.ShowInTaskbar = false;
                desktop.MainWindow.WindowState = WindowState.Minimized;
                Ioc.Default.GetRequiredService<ServiceViewModel>().ToggleListeningCommand.Execute(null);
                Dispatcher.UIThread.Post(() => desktop.MainWindow.Hide(), DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var win = desktop.MainWindow;
        if (win == null) return;
        win.Show();
        win.WindowState = WindowState.Normal;
        win.Activate();
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != ShowWindowMessageId) return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        Dispatcher.UIThread.Post(ShowMainWindow);
        return IntPtr.Zero;
    }
}
