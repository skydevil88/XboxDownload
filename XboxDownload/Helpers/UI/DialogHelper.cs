using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using MsBox.Avalonia.Enums;

namespace XboxDownload.Helpers.UI;

public static class DialogHelper
{
    public static async Task ShowInfoDialogAsync(string title, string message, Icon icon = Icon.None)
    {
        var dialog = new DialogWindow(title, message, icon,
        [
            new DialogButton("OK", true, true, true)
        ]);

        await ShowDialogAsync(dialog);
    }

    public static async Task<bool> ShowConfirmDialogAsync(string title, string message, Icon icon = Icon.None, bool defaultYes = true)
    {
        var dialog = new DialogWindow(title, message, icon,
        [
            new DialogButton("Yes", defaultYes, false, true),
            new DialogButton("No", !defaultYes, true, false)
        ]);

        return await ShowDialogAsync(dialog);
    }

    private static async Task<bool> ShowDialogAsync(DialogWindow dialog)
    {
        var mainWindow = GetMainWindow();

        if (mainWindow is not null)
            return await dialog.ShowOwnedAsync(mainWindow);

        return await dialog.ShowStandaloneAsync();
    }

    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    private sealed record DialogButton(string Text, bool IsDefault, bool IsCancel, bool Result);

    private sealed class DialogWindow : Window
    {
        private readonly TaskCompletionSource<bool> _resultSource = new();
        private bool _resultSet;

        public DialogWindow(string title, string message, Icon icon, IReadOnlyList<DialogButton> buttons)
        {
            Title = title;
            MinWidth = 320;
            MaxWidth = 640;
            MaxHeight = 560;
            CanResize = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Closed += (_, _) =>
            {
                if (!_resultSet)
                    _resultSource.TrySetResult(false);
            };

            Content = new Border
            {
                Padding = new Thickness(20),
                Child = new Grid
                {
                    RowDefinitions =
                    [
                        new RowDefinition(new GridLength(1, GridUnitType.Star)),
                        new RowDefinition(GridLength.Auto)
                    ],
                    RowSpacing = 16,
                    Children =
                    {
                        BuildMessageBlock(message, icon),
                        BuildButtons(buttons)
                    }
                }
            };
        }

        public async Task<bool> ShowOwnedAsync(Window owner)
        {
            await ShowDialog(owner);
            return await _resultSource.Task;
        }

        public async Task<bool> ShowStandaloneAsync()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Show();
            return await _resultSource.Task;
        }

        private static Grid BuildMessageBlock(string message, Icon icon)
        {
            var (marker, background, foreground) = GetIconStyle(icon);

            var iconBadge = new Border
            {
                Width = 40,
                Height = 40,
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(20),
                Background = background,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0),
                Child = new TextBlock
                {
                    Text = marker,
                    Foreground = foreground,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold
                }
            };
            Grid.SetColumn(iconBadge, 0);

            var messageBlock = new SelectableTextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                MaxWidth = 540
            };

            var messageScrollViewer = new ScrollViewer
            {
                MaxHeight = 360,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = messageBlock
            };
            Grid.SetColumn(messageScrollViewer, 1);

            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star))
                ],
                ColumnSpacing = 14,
                Children =
                {
                    iconBadge,
                    messageScrollViewer
                }
            };

            Grid.SetRow(grid, 0);
            return grid;
        }

        private StackPanel BuildButtons(IReadOnlyList<DialogButton> buttons)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetRow(panel, 1);

            foreach (var button in buttons)
            {
                var control = new Button
                {
                    Content = button.Text,
                    MinWidth = 88,
                    IsDefault = button.IsDefault,
                    IsCancel = button.IsCancel,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };

                control.Click += (_, _) => CloseWithResult(button.Result);
                panel.Children.Add(control);
            }

            return panel;
        }

        private void CloseWithResult(bool result)
        {
            if (_resultSet)
                return;

            _resultSet = true;
            _resultSource.TrySetResult(result);
            Close();
        }

        private static (string Marker, IBrush Background, IBrush Foreground) GetIconStyle(Icon icon)
        {
            return icon switch
            {
                MsBox.Avalonia.Enums.Icon.Error => ("X", Brush.Parse("#7F1D1D"), Brush.Parse("#FEE2E2")),
                MsBox.Avalonia.Enums.Icon.Success => ("OK", Brush.Parse("#14532D"), Brush.Parse("#DCFCE7")),
                MsBox.Avalonia.Enums.Icon.Question => ("?", Brush.Parse("#1E3A8A"), Brush.Parse("#DBEAFE")),
                MsBox.Avalonia.Enums.Icon.Info => ("i", Brush.Parse("#0C4A6E"), Brush.Parse("#E0F2FE")),
                MsBox.Avalonia.Enums.Icon.Warning => ("!", Brush.Parse("#78350F"), Brush.Parse("#FEF3C7")),
                _ => ("", Brush.Parse("#374151"), Brush.Parse("#F3F4F6"))
            };
        }
    }
}
