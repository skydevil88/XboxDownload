using Avalonia.Controls;
using XboxDownload.ViewModels;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class ImportHostDialog : Window
{
    public ImportHostDialog()
    {
        InitializeComponent();
    }

    public ImportHostDialog(HostViewModel hostViewModel) : this()
    {
        var vm = new ImportHostDialogViewModel(hostViewModel)
        {
            CloseDialog = () => Close(null)
        };
        DataContext = vm;
    }
}