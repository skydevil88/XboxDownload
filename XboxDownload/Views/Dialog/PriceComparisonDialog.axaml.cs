using System;
using Avalonia.Controls;
using Avalonia.Threading;
using XboxDownload.ViewModels;
using XboxDownload.ViewModels.Dialog;


namespace XboxDownload.Views.Dialog;

public partial class PriceComparisonDialog : Window
{
    public PriceComparisonDialog()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }
    
    public PriceComparisonDialog(StoreViewModel storeViewModel)
        : this()
    {
        var vm = new PriceComparisonDialogViewModel(storeViewModel);
        
        DataContext = vm;
    }
    
        
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PriceComparisonDialogViewModel vm)
        {
            vm.RequestFocus += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    CurrencyTextBox.Focus();
                    CurrencyTextBox.SelectAll();
                });
            };
        }
    }
}