using Avalonia.Controls;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class EditHostsDialog : Window
{
    public EditHostsDialog()
    {
        InitializeComponent();
        
        var vm = new EditHostsDialogViewModel()
        {
            CloseDialog = () => Close(null)
        };
        DataContext = vm;
    }
}