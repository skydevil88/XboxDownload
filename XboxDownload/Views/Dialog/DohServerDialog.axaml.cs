using System;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxDownload.ViewModels;
using DohServerDialogViewModel = XboxDownload.ViewModels.Dialog.DohServerDialogViewModel;


namespace XboxDownload.Views.Dialog;

public partial class DohServerDialog : Window
{
    public DohServerDialog()
    {
        InitializeComponent();
    }
    
    public DohServerDialog(ServiceViewModel serviceViewModel) : this()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
        
        var vm = new DohServerDialogViewModel(serviceViewModel)
        {
            CloseDialog = () => Close(null)
        };
        
        DataContext = vm;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DohServerDialogViewModel vm)
        {
            vm.RequestProxyIpFocus = () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ProxyIp.Focus();
                    ProxyIp.SelectAll();
                });
            };
        }
    }
}