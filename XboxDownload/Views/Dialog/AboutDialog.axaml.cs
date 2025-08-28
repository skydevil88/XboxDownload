using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        DataContext = new AboutDialogViewModel();
    }
}