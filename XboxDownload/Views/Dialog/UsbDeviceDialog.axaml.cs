using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using XboxDownload.ViewModels;
using XboxDownload.ViewModels.Dialog;

namespace XboxDownload.Views.Dialog;

public partial class UsbDeviceDialog : Window
{
    public UsbDeviceDialog()
    {
        InitializeComponent();
        
        var vm = new UsbDeviceDialogViewModel();
        
        DataContext = vm;
    }
}