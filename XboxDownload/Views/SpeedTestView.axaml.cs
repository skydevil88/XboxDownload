using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DynamicData.Binding;
using XboxDownload.ViewModels;

namespace XboxDownload.Views;

public partial class SpeedTestView : UserControl
{
    public SpeedTestView()
    {
        InitializeComponent();
        
        //DataContext = new SpeedTestViewModel();

        DataContextChanged += OnDataContextChanged;

        // 双击行事件监听
        MyDataGrid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (e.ClickCount != 2) return;
            if (DataContext is SpeedTestViewModel { SelectedItem: not null } vm)
            {
                await vm.RunSingleSpeedTestCommand.ExecuteAsync(vm.SelectedItem);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not SpeedTestViewModel vm) return;
        vm.ClearSortRequested = ClearSortArrows;

        // 监听 IsSortingEnabled 变化
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.IsSortingEnabled))
            {
                _isSortingEnabled = vm.IsSortingEnabled;
            }
        };

        _isSortingEnabled = vm.IsSortingEnabled;
    }

    private SortDirection _currentDirection = SortDirection.Ascending;
    private DataGridColumn? _lastSortedColumn;
    private bool _isSortingEnabled = true;

    private void MyDataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        if (!_isSortingEnabled)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;

        if (DataContext is not SpeedTestViewModel vm)
            return;

        var column = e.Column;
        var sortProperty = column.SortMemberPath;
        if (string.IsNullOrEmpty(sortProperty))
            return;

        if (_lastSortedColumn == column)
        {
            _currentDirection = _currentDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
        else
        {
            _currentDirection = SortDirection.Ascending;
            _lastSortedColumn = column;
        }

        vm.ApplySort(sortProperty, _currentDirection);
        UpdateSortArrow(column, _currentDirection);
    }

    private void UpdateSortArrow(DataGridColumn sortedColumn, SortDirection direction)
    {
        foreach (var col in MyDataGrid.Columns)
        {
            var header = col.Header?.ToString() ?? "";
            header = header.TrimEnd(' ', '↑', '↓');

            if (col == sortedColumn)
            {
                var arrow = direction == SortDirection.Ascending ? " ↑" : " ↓";
                col.Header = header + arrow;
            }
            else
            {
                col.Header = header;
            }
        }
    }

    private void ClearSortArrows()
    {
        foreach (var col in MyDataGrid.Columns)
        {
            var header = col.Header?.ToString() ?? "";
            col.Header = header.TrimEnd(' ', '↑', '↓');
        }

        _lastSortedColumn = null;
    }

    private void OnLocationQueryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is SpeedTestViewModel vm && vm.FilterByLocationCommand.CanExecute(null))
        {
            vm.FilterByLocationCommand.Execute(null);
        }
    }
    
    private async void ShowEditHostsDialogAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (VisualRoot is not Window window) return;
            var dialog = new Dialog.EditHostsDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in show dialog: {ex}");
        }
    }
}