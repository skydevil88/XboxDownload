using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxDownload.Models;
using XboxDownload.Services;
using XboxDownload.ViewModels;
using XboxDownload.Views;

namespace XboxDownload;

public partial class App : Application
{
    public static TrayIconService TrayIconService { get; private set; } = new();
    
    public static AppSettings Settings { get; private set; } = new();
    
    public static IServiceProvider? Services { get; private set; }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        Settings = SettingsManager.Load();

        LoadLanguage();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        TrayIconService.Initialize();
    
        // Initialize service container
        Services = Setup.ConfigureServices(); 

        // Register global services
        Ioc.Default.ConfigureServices(new ServiceCollection()
            .AddSingleton<MainWindowViewModel>()
            .AddSingleton<ServiceViewModel>()
            .AddSingleton<SpeedTestViewModel>()
            .AddSingleton<HostViewModel>()
            .AddSingleton<CdnViewModel>()
            .AddSingleton<StorageViewModel>()
            .AddSingleton<StoreViewModel>()
            .AddSingleton<ToolsViewModel>()
            .BuildServiceProvider());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Set the application theme
            var themeVariant = Settings.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            RequestedThemeVariant = themeVariant;

            DisableAvaloniaDataAnnotationValidation();

            // Get the MainWindowViewModel instance via dependency injection
            var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowVm
            };
        
            if (desktop.Args?.Contains("Startup") == true)
            {
                desktop.MainWindow.ShowInTaskbar = false; 
                desktop.MainWindow.WindowState = WindowState.Minimized;
                // Start the listening service
                Ioc.Default.GetRequiredService<ServiceViewModel>()
                    .ToggleListeningCommand.Execute(null);
                Dispatcher.UIThread.Post(() => { desktop.MainWindow.Hide(); }, DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    private void LoadLanguage()
    {
        var culture = Settings.Culture;
        if (culture is not ("en-US" or "zh-Hans"))
        {
            culture = CultureInfo.CurrentUICulture.Name switch
            {
                "zh" or "zh-CN" or "zh-Hans" or "zh-Hans-CN" or "zh-SG" or "zh-Hans-SG" => "zh-Hans",
                _ => "en-US"
            };
            
            Settings.Culture = culture;
            SettingsManager.Save(Settings);
        }

        var langDict = new ResourceInclude(new Uri($"resm:Styles?assembly={nameof(XboxDownload)}"))
        {
            Source = new Uri($"avares://{nameof(XboxDownload)}/Resources/Languages/{culture}.axaml")
        };

        var dictionaries = Resources.MergedDictionaries;

        dictionaries.Add(langDict);
    }
}