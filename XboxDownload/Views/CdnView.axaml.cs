using System;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class CdnView : UserControl
{
    public CdnView()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CdnViewModel vm)
        {
            vm.RequestFocus += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    CdnIpAddressInput.Focus();
                    CdnIpAddressInput.SelectAll();
                });
            };
        }
    }
}