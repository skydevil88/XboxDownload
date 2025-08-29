using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XboxDownload.ViewModels;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class ResolveDomainDialog : Window
{
    private readonly Dictionary<string, TextBox> _focusMap;
    
    public ResolveDomainDialog()
    {
        InitializeComponent();
        _focusMap = new Dictionary<string, TextBox>(); 
    }
    
    public ResolveDomainDialog(ServiceViewModel serviceViewModel, HostViewModel hostViewModel)
        : this()
    {
        DataContextChanged += OnDataContextChanged;
        
        var vm = new ResolveDomainDialogViewModel(serviceViewModel, hostViewModel)
        {
            CloseDialog = () => Close(null)
        };
        
        MyDataGrid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        
        _focusMap = new Dictionary<string, TextBox>
        {
            ["Host"] = Host,
            ["Ip"] = Ip
        };
        
        DataContext = vm;
    }

    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (e.ClickCount != 2) return;
            if (DataContext is ResolveDomainDialogViewModel { SelectedItem: not null } vm)
            {
                vm.SetIpForHostCommand.Execute(vm.SelectedItem);
            }
        }
        catch
        {
            // ignored
        }
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ResolveDomainDialogViewModel vm)
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
    
    private async void Host_KeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key != Key.Enter || DataContext is not ResolveDomainDialogViewModel vm) return;
            await vm.QueryAsync();
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"QueryAsync failed: {ex}");
        }
    }
}