using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.IO;
using XboxDownload.Helpers.Resources;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class ServiceView : UserControl
{
    private readonly Dictionary<string, TextBox> _focusMap;
    
    public ServiceView()
    {
        InitializeComponent();
        
        _focusMap = new Dictionary<string, TextBox>
        {
            ["DnsIp"] = DnsIpTextBox,
            ["XboxGlobalIp"] = XboxGlobalIpTextBox,
            ["XboxCn1Ip"] = XboxCn1IpTextBox,
            ["XboxCn2Ip"] = XboxCn2IpTextBox,
            ["XboxAppIp"] = XboxAppIpTextBox,
            ["PsIp"] = PsIpTextBox,
            ["NsIp"] = NsIpTextBox,
            ["EaIp"] = EaIpTextBox,
            ["BattleIp"] = BattleIpTextBox,
            ["EpicIp"] = EpicIpTextBox,
            ["UbisoftIp"] = UbisoftIpTextBox
        };

        DataContextChanged += OnDataContextChanged;
    }

    private async void ShowDohSettingsDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            if (DataContext is ServiceViewModel serviceVm)
            {
                var dialog = new Dialog.DohServerDialog(serviceVm);
                await dialog.ShowDialog(window);
            }
            else
            {
                Console.WriteLine("Current DataContext is not ServiceViewModel");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
    
    private async void ShowLocalProxyDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            if (DataContext is ServiceViewModel serviceVm)
            {
                var dialog = new Dialog.LocalProxyDialog(serviceVm);
                await dialog.ShowDialog(window);
            }
            else
            {
                Console.WriteLine("Current DataContext is not ServiceViewModel");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ServiceViewModel vm)
        {
            vm.RequestFocus += (targetName) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!_focusMap.TryGetValue(targetName, out var tb)) return;
                    tb.Focus();
                    tb.SelectAll();
                });
            };
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.GetVisualRoot() is not Window topWindow)
                return;

            var result = await topWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = ResourceHelper.GetString("Service.Settings.SelectFolder"),
                AllowMultiple = false
            });

            if (result.Count == 0)
                return;
            
            var path = result[0].Path.LocalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (DataContext is ServiceViewModel vm)
            {
                vm.LocalUploadPath = path;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in folder picking: {ex}");
        }
    }
}