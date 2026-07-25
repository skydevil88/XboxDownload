using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace XboxDownload.Helpers.UI;

public sealed class DataGridClipboard : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DataGridClipboard, DataGrid, bool>("IsEnabled");

    private static readonly ConditionalWeakTable<DataGrid, CellSelection> Selections = new();

    static DataGridClipboard()
    {
        IsEnabledProperty.Changed.AddClassHandler<DataGrid>((grid, _) =>
        {
            if (grid.GetValue(IsEnabledProperty))
            {
                grid.ClipboardCopyMode = DataGridClipboardCopyMode.None;
                grid.CellPointerPressed -= GridOnCellPointerPressed;
                grid.KeyDown -= GridOnKeyDown;
                grid.CellPointerPressed += GridOnCellPointerPressed;
                grid.KeyDown += GridOnKeyDown;
            }
            else
            {
                grid.CellPointerPressed -= GridOnCellPointerPressed;
                grid.KeyDown -= GridOnKeyDown;
                Selections.Remove(grid);
            }
        });
    }

    public static void SetIsEnabled(AvaloniaObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(AvaloniaObject element) =>
        element.GetValue(IsEnabledProperty);

    private static bool IsCopyGesture(KeyEventArgs e) =>
        e.Key == Key.C && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;

    private static DataGridCell? ResolveCurrentCell(object? sender, KeyEventArgs e, CellSelection fallback)
    {
        var grid = sender as DataGrid ?? (e.Source as Control)?.FindAncestorOfType<DataGrid>();
        var current = e.Source as DataGridCell ?? (e.Source as Control)?.FindAncestorOfType<DataGridCell>();
        if (current is not null && ReferenceEquals(current.FindAncestorOfType<DataGrid>(), grid)) return current;
        if (grid is not null && ResolveFocusedCell(grid) is { } focused) return focused;
        if (ResolveGridCurrentCell(grid) is { } gridCurrent) return gridCurrent;
        return IsValidCachedCell(grid, fallback) ? fallback.Cell : null;
    }

    private static DataGridCell? ResolveFocusedCell(DataGrid grid)
    {
        var focused = TopLevel.GetTopLevel(grid)?.FocusManager.GetFocusedElement() as Control;
        var focusedCell = focused as DataGridCell ?? focused?.FindAncestorOfType<DataGridCell>();
        if (focusedCell is not null && ReferenceEquals(focusedCell.FindAncestorOfType<DataGrid>(), grid))
        {
            return focusedCell;
        }

        foreach (var descendant in grid.GetVisualDescendants())
        {
            if (descendant is DataGridCell { IsFocused: true } cell)
            {
                return cell;
            }
        }

        return null;
    }

    private static DataGridCell? ResolveGridCurrentCell(DataGrid? grid)
    {
        if (grid?.SelectedItem is null || grid.CurrentColumn is null)
        {
            return null;
        }

        foreach (var descendant in grid.GetVisualDescendants())
        {
            if (descendant is not DataGridRow row || !ReferenceEquals(row.DataContext, grid.SelectedItem))
            {
                continue;
            }

            foreach (var rowDescendant in row.GetVisualDescendants())
            {
                if (rowDescendant is DataGridCell cell &&
                    ReferenceEquals(DataGridColumn.GetColumnContainingElement(cell), grid.CurrentColumn))
                {
                    return cell;
                }
            }
        }

        return null;
    }

    private static bool IsValidCachedCell(DataGrid? grid, CellSelection selection)
    {
        if (grid is null ||
            selection.Cell is not { } cell ||
            selection.Column is null ||
            !ReferenceEquals(cell.FindAncestorOfType<DataGrid>(), grid) ||
            !ReferenceEquals(cell.DataContext, selection.RowItem))
        {
            return false;
        }

        return ReferenceEquals(DataGridColumn.GetColumnContainingElement(cell), selection.Column);
    }

    private static void StoreSelection(CellSelection selection, DataGridCell cell, DataGridColumn? column = null)
    {
        selection.Cell = cell;
        selection.RowItem = cell.DataContext;
        selection.Column = column ?? DataGridColumn.GetColumnContainingElement(cell);
    }

    private static string GetDisplayedText(DataGridCell cell)
    {
        TextBox? textBox = null;
        TextBlock? textBlock = null;
        CheckBox? checkBox = null;

        foreach (var descendant in cell.GetVisualDescendants())
        {
            switch (descendant)
            {
                case TextBox value when textBox is null:
                    textBox = value;
                    break;
                case TextBlock value when textBlock is null:
                    textBlock = value;
                    break;
                case CheckBox value when checkBox is null:
                    checkBox = value;
                    break;
            }
        }

        if (textBox?.Text is { } inputText) return inputText;
        if (textBlock?.Text is { } text) return text;

        var isChecked = checkBox?.IsChecked;
        return isChecked switch
        {
            true => "True",
            false => "False",
            _ => ""
        };
    }

    private static Task CopyAsync(Control owner, DataGridCell cell)
    {
        var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard;
        return clipboard is null
            ? Task.CompletedTask
            : ClipboardHelper.SetTextAsync(clipboard, GetDisplayedText(cell));
    }

    private static async Task CopyCellAsync(DataGrid grid, DataGridCell cell)
    {
        try
        {
            await CopyAsync(grid, cell);
        }
        catch
        {
            // Clipboard access can fail when the OS denies or temporarily owns it.
        }
    }

    private static void GridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            StoreSelection(Selections.GetOrCreateValue(grid), e.Cell, e.Column);
        }
    }

    private static void GridOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !IsCopyGesture(e) || sender is not DataGrid grid)
        {
            return;
        }

        if (e.Source is TextBox)
        {
            return;
        }

        var selection = Selections.GetOrCreateValue(grid);
        var cell = ResolveCurrentCell(grid, e, selection);
        if (cell is null)
        {
            return;
        }

        StoreSelection(selection, cell);
        e.Handled = true;
        _ = CopyCellAsync(grid, cell);
    }

    private sealed class CellSelection
    {
        public DataGridCell? Cell { get; set; }
        public object? RowItem { get; set; }
        public DataGridColumn? Column { get; set; }
    }
}
