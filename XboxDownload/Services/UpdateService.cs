﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MsBox.Avalonia.Enums;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.ViewModels;

namespace XboxDownload.Services;

public static partial class UpdateService
{
    public const string Website = "https://xbox.skydevil.xyz";
    private const string Project = "https://github.com/skydevil88/XboxDownload";
    private static readonly string[] Proxies1 = ["https://gh-proxy.com/", "https://ghproxy.net/"];
    private static readonly string[] Proxies2 = ["https://pxy1.skydevil.xyz/", "https://pxy2.skydevil.xyz/", ""];
    
    public static async Task Start(bool autoupdate = false)
    {
        App.Settings.NextUpdate = DateTime.UtcNow.AddDays(7);
        SettingsManager.Save(App.Settings);

        string tagName = "", versionText = "";
        using var cts = new CancellationTokenSource();

        var tasks = Proxies2.Select(proxy =>
            HttpClientHelper.SendRequestAsync($"{proxy}{Project}/releases/latest", method: "HEAD", token: cts.Token)
        ).ToList();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            var task = await completedTask;
            if (task is not { IsSuccessStatusCode: true }) continue;
            await cts.CancelAsync();

            var finalUrl = task.RequestMessage?.RequestUri?.ToString();
            if (!string.IsNullOrEmpty(finalUrl))
            {
                var match = GitHubTagRegex().Match(finalUrl);
                if (match.Success)
                {
                    tagName = match.Groups["tag_name"].Value;
                    versionText = match.Groups["version"].Value;
                }
            }
            break;
        }

        if (string.IsNullOrEmpty(tagName))
        {
            if (!autoupdate)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Update.UpdateFailedTitle"),
                    ResourceHelper.GetString("Update.UpdateFailedMessage"),
                    Icon.Error);
            }
            return;
        }
        
        var currentVersion = new Version(Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version!);
        
        var latestVersion = new Version(versionText);
        
        if (latestVersion > currentVersion)
        {
            var confirm = await DialogHelper.ShowConfirmDialogAsync(
                ResourceHelper.GetString("Update.Title"),
                string.Format(ResourceHelper.GetString("Update.UpdateNow"), latestVersion),
                Icon.Question);

            if (!confirm) return;
        }
        else
        {
            if (!autoupdate)
            {
                await DialogHelper.ShowInfoDialogAsync(
                    ResourceHelper.GetString("Update.Title"),
                    string.Format(ResourceHelper.GetString("Update.AlreadyUpToDate"), currentVersion),
                    Icon.Success);
            }
            return;
        }
        
        var serviceVm = Ioc.Default.GetRequiredService<ServiceViewModel>();
        if (serviceVm.IsLogging) 
            serviceVm.AddLog(ResourceHelper.GetString("Update.Title"), ResourceHelper.GetString("Update.Downloading"), "System");
        
        var systemLabel = OperatingSystem.IsWindows() ? "windows" :
                          OperatingSystem.IsLinux() ? "linux" :
                          OperatingSystem.IsMacOS() ? "macos" : "unknown";
        
        var archLabel = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        var fileName = $"{nameof(XboxDownload)}-{systemLabel}-{archLabel}.zip";
        
        var fastestUrl = await HttpClientHelper.GetFastestProxy(Proxies1.Concat(Proxies2).ToArray(),
            $"{Project}/releases/download/{tagName}/{fileName}",
            new Dictionary<string, string> { { "Range", "bytes=0-10239" } },
            6000);
        if (fastestUrl != null)
        {
            using var response = await HttpClientHelper.SendRequestAsync(fastestUrl, timeout: 180000, token: CancellationToken.None);
            if (response is { IsSuccessStatusCode: true } &&
                response.Content.Headers.ContentType?.MediaType?.Equals("application/octet-stream") == true &&
                response.Content.Headers.ContentDisposition?.FileName?.StartsWith(nameof(XboxDownload), StringComparison.OrdinalIgnoreCase) == true)
            {
                var tempDirectory = Path.Combine(Path.GetTempPath(), nameof(XboxDownload));
                try
                {
                    if (Directory.Exists(tempDirectory))
                        Directory.Delete(tempDirectory, recursive: true);
                    Directory.CreateDirectory(tempDirectory);
                    
                    var buffer = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
                    if (buffer.Length > 0)
                    {
                        var saveFilepath = Path.Combine(tempDirectory, $"{nameof(XboxDownload)}.zip");
                        await using (FileStream fs = new(saveFilepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            await fs.WriteAsync(buffer, CancellationToken.None);
                            await fs.FlushAsync(CancellationToken.None);
                            fs.Close();
                        }
                        
                        try
                        {
                            ZipFile.ExtractToDirectory(saveFilepath, tempDirectory, overwriteFiles: true);
                        }
                        catch
                        {
                            // ignored
                        }

                        var dirs = new DirectoryInfo(tempDirectory).GetDirectories();
                        if (dirs.Length == 1)
                        {
                            if (serviceVm.IsListening) 
                                await serviceVm.ToggleListeningAsync();
                            
                            var exePath = OperatingSystem.IsWindows() ? Process.GetCurrentProcess().MainModule?.FileName : Environment.GetCommandLineArgs()[0];
                            
                            var cmd = $"chcp 65001\r\nchoice /t 3 /d y /n >nul\r\nxcopy \"{dirs[0]}\" \"{Path.GetDirectoryName(exePath)}\" /s /e /y\r\n\"{exePath}\"\r\nrd /s/q \"{tempDirectory}\"";
                            var cmdPath = Path.Combine(tempDirectory, "update.cmd");
                            await File.WriteAllTextAsync(cmdPath, cmd, CancellationToken.None);
                            _ = CommandHelper.RunCommandAsync("cmd.exe", $"/c \"{cmdPath}\"");
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    if (Directory.Exists(tempDirectory))
                        Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        await DialogHelper.ShowInfoDialogAsync(
            ResourceHelper.GetString("Update.UpdateFailedTitle"),
            ResourceHelper.GetString("Update.DownloadFailed"),
            Icon.Error);
    }

    
    [GeneratedRegex(@"/releases/tag/(?<tag_name>[^\d]*(?<version>\d+(\.\d+){2,3}))$", RegexOptions.Compiled)]
    private static partial Regex GitHubTagRegex();
    
    public static async Task DownloadIpAsync(FileInfo fi, string keyword = "")
    {
        string url = $"{Project.Replace("https://github.com", "https://raw.githubusercontent.com")}/refs/heads/master/IP/{fi.Name}";
        if(string.IsNullOrEmpty(keyword)) keyword = fi.Name[3..^4];
        using var cts = new CancellationTokenSource();
        
        var tasks = Proxies2.Select(async proxy => await HttpClientHelper.GetStringContentAsync(proxy + url, token: cts.Token, timeout: 6000)).ToList();
        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            var responseString = await completedTask;
            if (!responseString.StartsWith(keyword)) continue;
            await cts.CancelAsync();
            await SaveToFileAsync(fi, responseString);
        }
        
        if (!cts.IsCancellationRequested)
        {
            var responseString = await HttpClientHelper.GetStringContentAsync($"{Project.Replace("https://github.com", "https://testingcf.jsdelivr.net/gh")}/IP/{fi.Name}", token: cts.Token);
            if (responseString.StartsWith(keyword))
            {
                await SaveToFileAsync(fi, responseString);
            }
        }
    }
    
    private static async Task SaveToFileAsync(FileInfo fi, string content)
    {
        if (!Directory.Exists(fi.DirectoryName))
            Directory.CreateDirectory(fi.DirectoryName!);
        await File.WriteAllTextAsync(fi.FullName, content);
        fi.Refresh();
    }
    
    public static async Task<string> FetchAppDownloadUrlAsync(string product, CancellationToken token = default)
    {
        var products = product.Split('|');
        if (products.Length != 3) return string.Empty;

        var wuCategoryId = products[0];
        var json = await HttpClientHelper.GetStringContentAsync(
            Website + "/Game/GetAppPackage?WuCategoryId=" + wuCategoryId,
            name: "XboxDownload",
            token: token
        );

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array &&
                dataArray.GetArrayLength() > 0)
            {
                var firstItem = dataArray[0];
                if (firstItem.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                {
                    return urlProp.GetString() ?? string.Empty;
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine("JSON parse error: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error extracting URL: " + ex.Message);
        }

        return string.Empty;
    }
}
