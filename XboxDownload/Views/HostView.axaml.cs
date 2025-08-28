using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class HostView : UserControl
{
    public HostView()
    {
        InitializeComponent();
    }
    
    private async void ShowResolveDomainDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;

            // Ensure DataContext is HostViewModel
            if (DataContext is HostViewModel hostViewModel)
            {
                // Assuming ServiceViewModel is accessible from HostViewModel
                var serviceViewModel = Ioc.Default.GetRequiredService<ServiceViewModel>(); // Or retrieve from another place if needed

                var dialog = new Dialog.ResolveDomainDialog(serviceViewModel, hostViewModel);
                await dialog.ShowDialog(window);  // Only call this once
            }
            else
            {
                Console.WriteLine("Current DataContext is not HostViewModel");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
    
    private async void ShowImportHostDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            
            if (DataContext is HostViewModel hostViewModel)
            {
                var dialog = new Dialog.ImportHostDialog(hostViewModel);
                await dialog.ShowDialog(window);
            }
            else
            {
                Console.WriteLine("Current DataContext is not HostViewModel");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
}