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

    private static DataGridCell? ResolveCurrentCell(object? sender, KeyEventArgs e, DataGridCell? fallback)
    {
        var grid = sender as DataGrid ?? (e.Source as Control)?.FindAncestorOfType<DataGrid>();
        var current = e.Source as DataGridCell ?? (e.Source as Control)?.FindAncestorOfType<DataGridCell>();
        if (current is not null && ReferenceEquals(current.FindAncestorOfType<DataGrid>(), grid)) return current;
        if (grid is not null && ResolveFocusedCell(grid) is { } focused) return focused;
        return fallback is not null && ReferenceEquals(fallback.FindAncestorOfType<DataGrid>(), grid) ? fallback : null;
    }

    private static DataGridCell? ResolveFocusedCell(DataGrid grid)
    {
        var focused = TopLevel.GetTopLevel(grid)?.FocusManager?.GetFocusedElement() as Control;
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

    private static void GridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            Selections.GetOrCreateValue(grid).Cell = e.Cell;
        }
    }

    private static async void GridOnKeyDown(object? sender, KeyEventArgs e)
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
        selection.Cell = ResolveCurrentCell(grid, e, selection.Cell);
        if (selection.Cell is null)
        {
            return;
        }

        e.Handled = true;
        try
        {
            await CopyAsync(grid, selection.Cell);
        }
        catch
        {
            // Clipboard access can fail when the OS denies or temporarily owns it.
        }
    }

    private sealed class CellSelection
    {
        public DataGridCell? Cell { get; set; }
    }
}
