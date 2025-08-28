using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.UI;
using XboxDownload.Services;

namespace XboxDownload.Helpers.IO
{
    public static partial class HostsHelper
    {
        /// <summary>
        /// Asynchronously reads the content of the system Hosts file.
        /// </summary>
        private static async Task<string> ReadHostsContentAsync()
        {
            try
            {
                return await File.ReadAllTextAsync(PathHelper.SystemHostsPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HostsHelper] Read error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously writes content to the system Hosts file.
        /// </summary>
        /// <param name="content">The content to write.</param>
        private static async Task WriteHostsContentAsync(string content)
        {
            if (OperatingSystem.IsWindows())
            {
                FileInfo fi = new(PathHelper.SystemHostsPath);
                try
                {
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        fi.Attributes &= ~FileAttributes.ReadOnly;
                    await using var fs = fi.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    await using StreamWriter sw = new(fs);
                    await sw.WriteAsync(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HostsHelper] Write error: {ex.Message}");
                    throw;
                }
            }
            else
            {
                try
                {
                    await File.WriteAllTextAsync(PathHelper.SystemHostsPath, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HostsHelper] Write error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Applies DNS, redirect, and blacklist rules to the system hosts file,
        /// according to current application settings and listening states.
        /// </summary>
        /// <param name="isListening">Whether the application is currently listening (actively serving)</param>
        public static async Task ApplySystemHostsAsync(bool isListening = false)
        {
            try
            {
                var content = await ReadHostsContentAsync();

                if (isListening)
                {
                    content = RemoveAppSectionRegex().Replace(content, "").Trim();

                    var sb = new StringBuilder();
                    if (App.Settings.IsDnsServiceEnabled && App.Settings.IsSetLocalDnsEnabled)
                    {
                        var lines = content.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();

                            if (trimmedLine.StartsWith('#'))
                                continue;

                            var match = ParseHostsLineRegex().Match(trimmedLine);
                            if (!match.Success)
                                continue;

                            var matchedIp = match.Groups[1].Value;
                            var hostname = match.Groups[2].Value.ToLowerInvariant();

                            var ip = string.Empty;
                            if (DnsConnectionListener.Ipv6ServiceMap.TryGetValue(hostname, out var v6))
                            {
                                if (v6 is { Count: > 0 })
                                {
                                    var bytes = v6[0].Data!;
                                    ip = new IPAddress(bytes).ToString();
                                }
                                else if (DnsConnectionListener.Ipv4ServiceMap.TryGetValue(hostname, out var v4))
                                {
                                    if (v4 is { Count: > 0 })
                                    {
                                        var bytes = v4[0].Data!;
                                        ip = new IPAddress(bytes).ToString();
                                    }
                                    else ip = "0.0.0.0";
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(ip) && !string.Equals(matchedIp, ip))
                                sb.AppendLine($"{ip} {hostname}");
                        }
                    }
                    else if (App.Settings.IsDnsServiceEnabled)
                    {
                        foreach (var (hostname, v6) in DnsConnectionListener.Ipv6ServiceMap)
                        {
                            var ip = string.Empty;
                            if (v6.Count > 0)
                            {
                                var bytes = v6[0].Data!;
                                ip = new IPAddress(bytes).ToString();
                            }
                            else if (DnsConnectionListener.Ipv4ServiceMap.TryGetValue(hostname, out var v4))
                            {
                                if (v4 is { Count: > 0 })
                                {
                                    var bytes = v4[0].Data!;
                                    ip = new IPAddress(bytes).ToString();
                                }
                                else ip = "0.0.0.0";
                            }
                            if (!string.IsNullOrWhiteSpace(ip))
                                sb.AppendLine($"{ip} {hostname}");
                        }
                    }
                    else if (App.Settings.IsHttpServiceEnabled)
                    {
                        if (App.Settings.IsXboxGameDownloadLinksShown)
                        {
                            foreach (var hostsMap in DnsMappingGenerator.HostsMap)
                            {
                                if (App.Settings.Culture == "zh-Hans")
                                {
                                    if (hostsMap.Value != "XboxGlobal" &&
                                        (hostsMap.Value is not ("XboxCn1" or "XboxCn2") || hostsMap.Key.Contains('2')))
                                        continue;
                                    var hostname = hostsMap.Key;
                                    sb.AppendLine($"{App.Settings.LocalIp} {hostname}");
                                }
                                else
                                {
                                    if ((hostsMap.Value is not ("XboxGlobal" or "XboxCn1" or "XboxCn2")) ||
                                        hostsMap.Key.Contains('2')) continue;
                                    var hostname = hostsMap.Key;
                                    sb.AppendLine($"{App.Settings.LocalIp} {hostname}");
                                }
                            }
                        }
                    }

                    if (sb.Length > 0)
                    {
                        var newHosts = $"# --- Begin {nameof(XboxDownload)} Section ---{Environment.NewLine}{sb}# --- End {nameof(XboxDownload)} Section ---{Environment.NewLine}{content}{Environment.NewLine}";
                        await WriteHostsContentAsync(newHosts);
                    }
                }
                else
                {
                    var newHosts = RemoveAppSectionRegex().Replace(content, "").Trim();
                    if (!string.Equals(content.Trim(), newHosts))
                    {
                        await WriteHostsContentAsync(newHosts + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                if (isListening)
                {
                    await DialogHelper.ShowInfoDialogAsync(
                        ResourceHelper.GetString("Service.Service.HostsFileErrorTitle"),
                        ResourceHelper.GetString("Service.Service.HostsFileErrorMessage") + Environment.NewLine + Environment.NewLine + ex.Message,
                        Icon.Error);
                }
            }
        }

        #region Regex

        /// <summary>
        /// Matches a hosts line like: 127.0.0.1 example.com
        /// Group 1 = IP, Group 2 = hostname
        /// </summary>
        [GeneratedRegex(@"^\s*([^#\s]+)\s+([^\s#]+)", RegexOptions.Compiled)]
        private static partial Regex ParseHostsLineRegex();

        /// <summary>
        /// Matches and removes the section injected by XboxDownload:
        /// </summary>
        [GeneratedRegex($@"# --- Begin {nameof(XboxDownload)} Section ---[\s\S]*?# --- End {nameof(XboxDownload)} Section ---|# Added by XboxDownload[\s\S]*?# End of XboxDownload", RegexOptions.Multiline)]
        public static partial Regex RemoveAppSectionRegex();

        #endregion

    }
}