using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class StartupSettingsDialog : Window
{
    public StartupSettingsDialog()
    {
        InitializeComponent();
        
        var vm = new StartupSettingsDialogViewModel()
        {
            CloseDialog = () => Close(null)
        };
        
        DataContext = vm;
    }
}