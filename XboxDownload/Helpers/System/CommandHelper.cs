using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    
    public static async Task<int> RunCommandAsync2(string fileName, string arguments, bool useShellExecute = false)
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
        return process.ExitCode;
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
                _ = RunCommandAsync("dscacheutil", "-flushcache");
            }
            else if (OperatingSystem.IsLinux())
            {
                if (File.Exists("/usr/bin/resolvectl"))
                {
                    _ = RunCommandAsync("resolvectl", "flush-caches");
                }
                else if (File.Exists("/usr/bin/systemd-resolve"))
                {
                    _ = RunCommandAsync("systemd-resolve", "--flush-caches");
                }
                else
                {
                    // 退而求其次，尝试重启 nscd 或 dnsmasq
                    _ = RunCommandAsync("systemctl", "restart nscd");
                    _ = RunCommandAsync("systemctl", "restart dnsmasq");
                }
            }
        }
        catch
        {
            // ignored
        }
    }
}