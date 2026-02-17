using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class StorageView : UserControl
{
    public StorageView()
    {
        InitializeComponent();
    }
    
    private async void FormatHardDriveAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;

            var dialog = new Dialog.HardDriveDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
}