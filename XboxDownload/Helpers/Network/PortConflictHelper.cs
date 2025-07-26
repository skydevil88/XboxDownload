using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;

namespace XboxDownload.Helpers.Network;

public static partial class PortConflictHelper
{
    /// <summary>
    /// Checks for port conflicts on well-known ports (53, 80, 443) by parsing the output of "netstat -aon".
    /// If conflicting processes are found, prompts the user whether to forcibly terminate them.
    /// </summary>
    /// <param name="isDnsServiceEnabled">Whether DNS service is enabled (port 53 check enabled)</param>
    /// <param name="isHttpServiceEnabled">Whether HTTP service is enabled (ports 80 and 443 check enabled)</param>
    [SupportedOSPlatform("windows")]
    public static async Task CheckAndHandlePortConflictAsync(bool isDnsServiceEnabled, bool isHttpServiceEnabled)
    {
        if (!OperatingSystem.IsWindows() || (!isDnsServiceEnabled && !isHttpServiceEnabled))
            return;
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo("netstat", "-aon")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            p.Start();
            var resultInfo = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync();

            var matches = NetstatPortRegex().Matches(resultInfo);
            var conflictPids = new ConcurrentDictionary<int, string>();
            var sb = new StringBuilder();

            foreach (Match match in matches)
            {
                var port = int.Parse(match.Groups["port"].Value);
                var protocol = match.Groups["protocol"].Value;
                var status = match.Groups["status"].Value.Trim();
                var pid = int.Parse(match.Groups["pid"].Value);
                var ip = match.Groups["ip"].Value;

                // Only handle TCP LISTENING or any UDP entry
                if ((protocol == "TCP" && status != "LISTENING") || pid == 0)
                    continue;

                // Check if configured services are enabled and matched port is in use
                if ((port != 53 || !isDnsServiceEnabled) &&
                    (port is not (80 or 443) || !isHttpServiceEnabled)) continue;
                if (conflictPids.ContainsKey(pid)) continue;
                if (pid == 4)
                {
                    conflictPids.TryAdd(pid, "System Service");
                }
                else
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        var filename = proc.MainModule?.FileName ?? "Unknown";
                        conflictPids.TryAdd(pid, filename);
                    }
                    catch
                    {
                        conflictPids.TryAdd(pid, "Unknown");
                    }
                }

                sb.AppendLine($"{protocol}\t{ip}:{port}\tPID: {pid}\t{conflictPids[pid]}");
            }

            if (!conflictPids.IsEmpty)
            {
                var shouldKill = await DialogHelper.ShowConfirmDialogAsync(
                    ResourceHelper.GetString("Service.Service.PortConflict"),
                    string.Format(ResourceHelper.GetString("Service.Service.PortConflictMessage"), sb),
                    Icon.Question);

                if (shouldKill)
                {
                    foreach (var pid in conflictPids.Keys)
                    {
                        if (pid == 4)
                        {
                            // Attempt to stop known system services (PID 4)
                            foreach (var svcName in new[]
                                     {
                                         "MsDepSvc",      // Web Deployment Agent Service
                                         "PeerDistSvc",   // BranchCache
                                         "ReportServer",  // SQL Server Reporting Services
                                         "SyncShareSvc",  // Sync Share Service
                                         "W3SVC",         // World Wide Web Publishing Service (IIS)
                                         "WAS",           // Windows Process Activation Service (IIS dependency)
                                     })
                            {
                                try
                                {
                                    var svc = new ServiceController(svcName);
                                    if (svc.Status != ServiceControllerStatus.Running) continue;
                                    svc.Stop();
                                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[Service Stop Error] {svcName}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Process.GetProcessById(pid).Kill();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[Kill Error] PID {pid}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Port Conflict Check Failed] {ex.Message}");
        }
    }

    [GeneratedRegex(@"(?<protocol>TCP|UDP)\s+(?<ip>[^\s]+):(?<port>80|443|53)\s+[^\s]+\s+(?<status>[^\s]+\s+)?(?<pid>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex NetstatPortRegex();
}