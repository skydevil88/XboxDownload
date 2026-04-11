using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace XboxDownload.Helpers.UI;

public static class ClipboardHelper
{
    public static Task SetTextAsync(IClipboard clipboard, string text)
    {
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(text));
        return clipboard.SetDataAsync(data);
    }
}
