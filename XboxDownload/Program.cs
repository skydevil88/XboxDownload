using Avalonia;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace XboxDownload;

sealed class Program
{
    private static Mutex? _mutex;
    
    [STAThread]
    public static void Main(string[] args)
    {
        const string mutexName = $"Global\\{nameof(XboxDownload)}_Mutex";

        _mutex = new Mutex(true, mutexName, out var createdNew);

        if (!createdNew)
        {
            if (OperatingSystem.IsWindows())
                BringExistingInstanceToFront();
            else
                Console.WriteLine("程序已在运行。");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        _mutex.ReleaseMutex();
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    
    private static void BringExistingInstanceToFront()
    {
        var current = Process.GetCurrentProcess();
        var others = Process.GetProcessesByName(current.ProcessName);

        foreach (var process in others)
        {
            if (process.Id == current.Id) continue;
            var hWnd = process.MainWindowHandle;
            if (hWnd == IntPtr.Zero) continue;
            ShowWindow(hWnd, 5); // SW_SHOW
            SetForegroundWindow(hWnd);
            break;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}