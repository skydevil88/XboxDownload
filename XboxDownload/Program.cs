using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace XboxDownload;

sealed class Program
{
    private const string MutexName = $"Global\\{nameof(XboxDownload)}_Mutex";
    private static string SocketPath => Path.Combine(Path.GetTempPath(), $"{nameof(XboxDownload)}.sock");

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            if (OperatingSystem.IsWindows())
            {
                var msg = RegisterWindowMessage("XboxDownload_ShowWindow");
                PostMessage((IntPtr)0xFFFF, msg, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                Console.WriteLine("This program is already running. Please avoid running multiple instances.");
            }
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            App.ShowWindowMessageId = RegisterWindowMessage("XboxDownload_ShowWindow");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static bool UnixUserIsRoot()
    {
        if (OperatingSystem.IsWindows()) return false;
        return getuid() == 0;
    }

    [DllImport("libc")]
    private static extern uint getuid();

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
