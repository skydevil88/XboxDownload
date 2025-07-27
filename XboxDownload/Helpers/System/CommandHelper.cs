using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace XboxDownload.Helpers.System;

public static class CommandHelper
{
    public static async Task RunCommandAsync(string fileName, string arguments, bool useShellExecute = false)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = useShellExecute,
            CreateNoWindow = true
        };
        process.Start();
        await process.WaitForExitAsync();
    }

    public static async Task<List<string>> RunCommandWithOutputAsync(string fileName, string arguments)
    {
        var output = new List<string>();
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
                output.Add(line.Trim());
        }

        await process.WaitForExitAsync();
        return output;
    }
    
    public static void FlushDns()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _ = RunCommandAsync("ipconfig", "/flushdns");
            }
            else if (OperatingSystem.IsMacOS())
            {
                _ = RunCommandAsync("/bin/bash", "-c \"sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                _ = RunCommandAsync("/bin/bash", "-c \"sudo systemd-resolve --flush-caches\"");
            }
        }
        catch
        {
            // ignored
        }
    }
}