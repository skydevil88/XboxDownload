using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class ToolsView : UserControl
{
    public ToolsView()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ToolsViewModel vm)
        {
            vm.RequestFocus += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    FilePath.Focus();
                    FilePath.SelectAll();
                });
            };
        }
    }
    
    private async void ShowUsbDeviceAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;

            // Ensure DataContext is HostViewModel
            if (DataContext is ToolsViewModel toolsVm)
            {
                var dialog = new Dialog.UsbDeviceDialog();
                await dialog.ShowDialog(window);  // Only call this once
                toolsVm.RefreshUsbDrivesCommand.Execute(null);
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