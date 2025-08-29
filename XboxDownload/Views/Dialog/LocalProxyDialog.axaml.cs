using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using XboxDownload.ViewModels;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class LocalProxyDialog : Window
{
    public LocalProxyDialog()
    {
        InitializeComponent();
    }
    
    public LocalProxyDialog(ServiceViewModel serviceVm) : this()
    {
        InitializeComponent();
        
        var vm = new LocalProxyDialogViewModel(serviceVm)
        {
            CloseDialog = () => Close(null)
        };
        
        DataContext = vm;
    }
    
    private async void ShowDohSettingsDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            var serviceVm = Ioc.Default.GetRequiredService<ServiceViewModel>();
            var dialog = new DohServerDialog(serviceVm);
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
}