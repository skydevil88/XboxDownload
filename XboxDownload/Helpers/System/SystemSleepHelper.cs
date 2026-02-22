using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XboxDownload.Helpers.System;

public static class SystemSleepHelper
{
    public static void PreventSleep(bool keepDisplayOn = true)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsPreventSleep(keepDisplayOn);
        }
        else if (OperatingSystem.IsMacOS())
        {
            MacPreventSleep(keepDisplayOn);
        }
        else if (OperatingSystem.IsLinux())
        {
            LinuxPreventSleep(keepDisplayOn);
        }
    }

    public static void RestoreSleep()
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsRestoreSleep();
        }
        else if (OperatingSystem.IsMacOS())
        {
            StopProcess(ref _caffeinateProcess);
        }
        else if (OperatingSystem.IsLinux())
        {
            StopProcess(ref _inhibitProcess);
        }
    }

    // --- Windows Implementation ---
    [DllImport("kernel32")]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;
    private const uint EsDisplayRequired = 0x00000002;

    private static void WindowsPreventSleep(bool keepDisplayOn)
    {
        var flags = EsContinuous | EsSystemRequired;
        if (keepDisplayOn) flags |= EsDisplayRequired;
        SetThreadExecutionState(flags);
    }

    private static void WindowsRestoreSleep()
    {
        SetThreadExecutionState(EsContinuous);
    }

    // --- macOS Implementation ---
    private static Process? _caffeinateProcess;

    private static void MacPreventSleep(bool keepDisplayOn)
    {
        // -d  Prevent display sleep
        // -i  Prevent idle sleep (system sleep)
        var args = keepDisplayOn ? "-dims" : "-ims";

        _caffeinateProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "caffeinate",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        _caffeinateProcess.Start();
    }

    // --- Linux Implementation ---
    private static Process? _inhibitProcess;

    private static void LinuxPreventSleep(bool keepDisplayOn)
    {
        var args = keepDisplayOn
            ? "--what=idle:handle-lid-switch:sleep:shutdown --mode=block --why=\"Prevent sleep and display off\" sleep infinity"
            : "--what=idle --mode=block --why=\"Prevent sleep only\" sleep infinity";

        _inhibitProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "systemd-inhibit",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        _inhibitProcess.Start();
    }

    // --- macOS & Linux Process Cleanup ---
    private static void StopProcess(ref Process? process)
    {
        try
        {
            if (process is { HasExited: false })
            {
                process.Kill();
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            process = null;
        }
    }
}
