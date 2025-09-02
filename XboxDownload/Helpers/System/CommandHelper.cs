using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace XboxDownload.Helpers.System;

public static class CommandHelper
{
    public static async Task<int> RunCommandAsync(string fileName, string arguments, bool useShellExecute = false)
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

    public static async Task<List<string>> RunCommandWithOutputAsync(string fileName, string arguments, int timeoutMs = 30000)
    {
        var output = new List<string>();
        var errorOutput = new List<string>();

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

        // Asynchronously read stdout
        var outputTask = ReadStreamAsync(process.StandardOutput, output);
        // Asynchronously read stderr
        var errorTask = ReadStreamAsync(process.StandardError, errorOutput);

        // Wait for the process to exit or timeout
        var exitTask = process.WaitForExitAsync();
        var allTasks = Task.WhenAll(outputTask, errorTask, exitTask);

        if (await Task.WhenAny(allTasks, Task.Delay(timeoutMs)) != allTasks)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                // ignored
            }
        }

        return output;
    }

    private static async Task ReadStreamAsync(StreamReader reader, List<string> outputList)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                outputList.Add(line.Trim());
        }
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