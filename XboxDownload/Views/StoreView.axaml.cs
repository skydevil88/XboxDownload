using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XboxDownload.Models.Store;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class StoreView : UserControl
{
    public StoreView()
    {
        InitializeComponent();

        this.GetObservable(DataContextProperty).Subscribe(dc =>
        {
            if (dc is StoreViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != nameof(vm.IsSuggestsOpen)) return;

                    if (vm.IsSuggestsOpen)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            FlyoutBase.ShowAttachedFlyout(SearchBox);
                            Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
                        });
                    }
                    else if (FlyoutBase.GetAttachedFlyout(SearchBox) is Flyout f)
                    {
                        f.Hide();
                        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
                    }
                };
            }
        });

        SearchBox.AddHandler(
            PointerPressedEvent,
            SearchBox_PointerPressed,
            RoutingStrategies.Bubble,
            handledEventsToo: true
        );

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StoreViewModel vm)
        {
            vm.RequestFocus += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    QueryUrl.Focus();
                    QueryUrl.SelectAll();
                });
            };
        }
    }

    private void QueryUrl_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Dispatcher.UIThread.Post(() =>
        {
            QueryButton.Focus();
        }, DispatcherPriority.Input);
        QueryButton.Command?.Execute(null);
        e.Handled = true;
    }

    private void ResultListBox_OnTapped(object? sender, RoutedEventArgs e)
    {
        if (ResultListBox.SelectedItem is not StoreSearchResult selected ||
            DataContext is not StoreViewModel vm) return;
        if (FlyoutBase.GetAttachedFlyout(SearchBox) is Flyout f)
        {
            f.Hide();
            Dispatcher.UIThread.Post(() =>
            {
                QueryButton.Focus();
            }, DispatcherPriority.Input);
        }
        //vm.QueryUrl = $"https://www.microsoft.com/store/productid/{selected.ProductId}";
        vm.QueryUrl = $"https://apps.microsoft.com/detail/{selected.ProductId}";
        QueryButton.Command?.Execute(null);
    }

    private void ResultListBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (ResultListBox.SelectedItem is not StoreSearchResult selected ||
            DataContext is not StoreViewModel vm) return;
        if (FlyoutBase.GetAttachedFlyout(SearchBox) is Flyout f)
        {
            f.Hide();
            Dispatcher.UIThread.Post(() =>
            {
                QueryButton.Focus();
            }, DispatcherPriority.Input);
        }
        //vm.QueryUrl = $"https://www.microsoft.com/store/productid/{selected.ProductId}";
        vm.QueryUrl = $"https://apps.microsoft.com/detail/{selected.ProductId}";
        QueryButton.Command?.Execute(null);
        e.Handled = true;
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down || DataContext is StoreViewModel { SearchResults.Count: 0 }) return;

        if (ResultListBox.ItemCount == 0)
        {
            FlyoutBase.ShowAttachedFlyout(SearchBox);
        }

        if (!(ResultListBox?.ItemCount > 0)) return;
        ResultListBox.SelectedIndex = 0;

        Dispatcher.UIThread.Post(() =>
        {
            var item = ResultListBox.ContainerFromIndex(0);
            item?.Focus();
        }, DispatcherPriority.Input);

        e.Handled = true;
    }

    private void SearchBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not StoreViewModel { SearchResults.Count: > 0 } vm) return;
        var flyout = FlyoutBase.GetAttachedFlyout(SearchBox);
        if (flyout is not Flyout f) return;
        f.ShowAt(SearchBox);
        vm.IsSuggestsOpen = true;
    }

    private void SearchBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager?.GetFocusedElement();

        if (focused is Control c &&
            (SearchBox == c ||
             SearchBox.IsVisualAncestorOf(c) ||
             (ResultListBox != null && ResultListBox.IsVisualAncestorOf(c))))
        {
            return;
        }

        if (FlyoutBase.GetAttachedFlyout(SearchBox) is Flyout f)
        {
            f.Hide();
        }

        if (DataContext is StoreViewModel vm)
        {
            vm.IsSuggestsOpen = false;
        }
    }

    private int? _selectedXgp1;
    private void OnGamePass1SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StoreViewModel vm || vm.SelectedGamePass1 is null) return;
        if (string.IsNullOrEmpty(vm.SelectedGamePass1.ProductId)) return;

        var selectedIndex = (sender as ComboBox)?.SelectedIndex;
        if (selectedIndex == _selectedXgp1) return;
        _selectedXgp1 = selectedIndex;
        
        Dispatcher.UIThread.Post(() =>
        {
            Xgp1.Focus();
        }, DispatcherPriority.Input);
        
        //vm.QueryUrl = $"https://www.microsoft.com/store/productid/{vm.SelectedGamePass1.ProductId}";
        vm.QueryUrl = $"https://apps.microsoft.com/detail/{vm.SelectedGamePass1.ProductId}";
        QueryButton.Command?.Execute(null);
    }

    private int? _selectedXgp2;
    private void OnGamePass2SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StoreViewModel vm || vm.SelectedGamePass2 is null) return;
        if (string.IsNullOrEmpty(vm.SelectedGamePass2.ProductId)) return;
        
        var selectedIndex = (sender as ComboBox)?.SelectedIndex;
        if (selectedIndex == _selectedXgp2) return;
        _selectedXgp2 = selectedIndex;
        
        Dispatcher.UIThread.Post(() =>
        {
            Xgp2.Focus();
        }, DispatcherPriority.Input);
        
        //vm.QueryUrl = $"https://www.microsoft.com/store/productid/{vm.SelectedGamePass2.ProductId}";
        vm.QueryUrl = $"https://apps.microsoft.com/detail/{vm.SelectedGamePass2.ProductId}";
        QueryButton.Command?.Execute(null);
    }
    
    private void OnProductSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not StoreViewModel { BundledLoaded: true } vm) return;
        
        var selectedItem = (sender as ComboBox)?.SelectedItem as Bundled;
        if (selectedItem == vm.SelectedBundled) return;
        vm.SelectedBundled = selectedItem;

        _ = vm.StoreParseAsync(vm.SelectedBundledIndex);
    }
    
    private async void ShowPriceComparisonAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;

            // Ensure DataContext is HostViewModel
            if (DataContext is StoreViewModel storeViewModel)
            {
                var dialog = new Dialog.PriceComparisonDialog(storeViewModel);
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
}