using Avalonia.Controls;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class HardDriveDialog : Window
{
    public HardDriveDialog()
    {
        InitializeComponent();
        
        var vm = new HardDriveDialogViewModel();
        
        DataContext = vm;
    }
}