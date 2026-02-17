using Avalonia.Controls;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class UsbDriveDialog : Window
{
    public UsbDriveDialog()
    {
        InitializeComponent();
        
        var vm = new UsbDriveDialogViewModel();
        
        DataContext = vm;
    }
}