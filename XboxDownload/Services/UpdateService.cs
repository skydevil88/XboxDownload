using CommunityToolkit.Mvvm.DependencyInjection;
using System;
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
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Network;
using XboxDownload.Helpers.Resources;
using XboxDownload.Helpers.System;
using XboxDownload.Helpers.UI;
using XboxDownload.ViewModels;

namespace XboxDownload.Services;

public static partial class UpdateService
{
    public const string Website = "https://xbox.skydevil.xyz";
    public const string Project = "https://github.com/skydevil88/XboxDownload";
    public static readonly string[] Proxies1 = ["https://gh-proxy.com/", "https://ghproxy.net/"];
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

        var latestVersion = new Version(versionText);
        var currentVersion = new Version(Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version!);
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
                          OperatingSystem.IsMacOS() ? "macos" :
                          OperatingSystem.IsLinux() ? "linux" : "unknown";

        var archLabel = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => "unknown"
        };

        var fileName = $"{nameof(XboxDownload)}-{systemLabel}-{archLabel}.zip";

        var fastestUrl = await HttpClientHelper.GetFastestProxyAsync([.. Proxies1, .. Proxies2],
            $"{Project}/releases/download/{tagName}/{fileName}",
            new Dictionary<string, string> { { "Range", "bytes=0-10239" } },
            6000);

        if (fastestUrl != null)
        {
            using var response = await HttpClientHelper.SendRequestAsync(fastestUrl, timeout: 180000, token: CancellationToken.None);
            if (response is { IsSuccessStatusCode: true } && response.Content.Headers.ContentDisposition?.FileName?.Equals(fileName) == true)
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
                        var saveFilepath = Path.Combine(tempDirectory, fileName);
                        await using (var fs = new FileStream(saveFilepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            await fs.WriteAsync(buffer, CancellationToken.None);
                            await fs.FlushAsync(CancellationToken.None);
                            fs.Close();
                        }

                        await ZipFile.ExtractToDirectoryAsync(saveFilepath, tempDirectory, overwriteFiles: true, CancellationToken.None);

                        var extractDir = new DirectoryInfo(Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(fileName)));
                        if (extractDir.Exists)
                        {
                            var mainWindowVm = Ioc.Default.GetRequiredService<MainWindowViewModel>();
                            await mainWindowVm.OnShutdownAsync();

                            var installContext = GetUpdateInstallContext();

                            if (OperatingSystem.IsWindows())
                            {
                                var scriptPath = Path.Combine(tempDirectory, "update.cmd");

                                var script = $@"
@echo off
chcp 65001 >nul
timeout /t 3 /nobreak >nul
robocopy ""{extractDir.FullName}"" ""{installContext.AppDir}"" /e /is /it /r:5 /w:2 >nul 2>&1
if %ERRORLEVEL% GEQ 8 exit /b %ERRORLEVEL%
start """" /d ""{installContext.AppDir}"" ""{installContext.AppPath}""
rd /s /q ""{tempDirectory}""
";

                                await File.WriteAllTextAsync(scriptPath, script, CancellationToken.None);
                                _ = CommandHelper.RunCommandAsync("cmd.exe", $"/c \"{scriptPath}\"");
                            }
                            else
                            {
                                var scriptPath = Path.Combine(tempDirectory, "update.sh");

                                var script = $@"
#!/bin/bash
sleep 3

if [[ ""$(uname)"" == ""Darwin"" ]]; then
    APP_BUNDLE=""{installContext.MacosAppBundlePath}""
    APP_EXEC=""$APP_BUNDLE/Contents/MacOS/XboxDownloadLauncher""
    APP_BIN=""$APP_BUNDLE/Contents/MacOS/XboxDownload""
    RUN_SCRIPT=""{installContext.InstallDir}/run_xboxdownload.command""

    if ! rm -rf -- ""$APP_BUNDLE""; then
        exit 1
    fi
    if ! cp -Rf ""{extractDir.FullName}""/. ""{installContext.InstallDir}""; then
        exit 1
    fi

    xattr -dr com.apple.quarantine ""$APP_BUNDLE"" 2>/dev/null || true
    xattr -dr com.apple.quarantine ""$RUN_SCRIPT"" 2>/dev/null || true
    chmod +x ""$APP_EXEC"" 2>/dev/null || true
    chmod +x ""$APP_BIN"" 2>/dev/null || true
    chmod +x ""$RUN_SCRIPT"" 2>/dev/null || true

    if [[ -n ""${{SUDO_USER:-}}"" ]]; then
        REAL_GROUP=""$(id -gn ""$SUDO_USER"" 2>/dev/null || echo staff)""
        chown ""$SUDO_USER:$REAL_GROUP"" ""{installContext.InstallDir}"" 2>/dev/null || true
        chown -R ""$SUDO_USER:$REAL_GROUP"" ""$APP_BUNDLE"" ""$RUN_SCRIPT"" ""{installContext.InstallDir}/README.md"" 2>/dev/null || true
    fi

    open ""$APP_BUNDLE"" &
else
    if ! cp -Rf ""{extractDir.FullName}""/. ""{installContext.InstallDir}""; then
        exit 1
    fi
    chmod +x ""{installContext.AppPath}"" 2>/dev/null || true
    chmod +x ""{installContext.InstallDir}/run_xboxdownload.sh"" 2>/dev/null || true
    nohup ""{installContext.AppPath}"" >/dev/null 2>&1 </dev/null &
fi

rm -rf -- ""{tempDirectory}""
exit 0
";

                                await File.WriteAllTextAsync(scriptPath, script.Replace("\r\n", "\n"), CancellationToken.None);
                                await CommandHelper.RunCommandAsync("chmod", $"+x \"{scriptPath}\"");
                                _ = CommandHelper.RunCommandAsync("/usr/bin/env", $"bash \"{scriptPath}\"");
                            }

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

    private readonly record struct UpdateInstallContext(
        string? AppPath,
        string? AppDir,
        string? InstallDir,
        string? MacosAppBundlePath);

    private static UpdateInstallContext GetUpdateInstallContext()
    {
        var appPath = Process.GetCurrentProcess().MainModule?.FileName;
        var appDir = Path.GetDirectoryName(appPath);
        var installDir = appDir;
        var macosAppBundlePath = appDir != null ? Path.Combine(appDir, $"{nameof(XboxDownload)}.app") : null;

        if (OperatingSystem.IsMacOS() && appDir != null)
        {
            var macosDir = new DirectoryInfo(appDir);
            var contentsDir = macosDir.Name == "MacOS" ? macosDir.Parent : null;
            var appBundleDir = contentsDir?.Name == "Contents" && contentsDir.Parent?.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase) == true
                ? contentsDir.Parent
                : null;

            if (appBundleDir != null)
            {
                installDir = appBundleDir.Parent?.FullName ?? appDir;
                macosAppBundlePath = appBundleDir.FullName;
            }
            else
            {
                installDir = appDir;
                macosAppBundlePath = Path.Combine(installDir, $"{nameof(XboxDownload)}.app");
            }
        }

        return new UpdateInstallContext(appPath, appDir, installDir, macosAppBundlePath);
    }

    public static async Task DownloadIpAsync(FileInfo fi, string keyword = "")
    {
        var url = $"{Project.Replace("https://github.com", "https://raw.githubusercontent.com")}/refs/heads/master/IP/{fi.Name}";
        if (string.IsNullOrEmpty(keyword)) keyword = fi.Name[3..^4];
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
            var responseString = await HttpClientHelper.GetStringContentAsync($"{Project.Replace("https://github.com", "https://testingcf.jsdelivr.net/gh")}/IP/{fi.Name}", token: CancellationToken.None);
            if (responseString.StartsWith(keyword))
            {
                await SaveToFileAsync(fi, responseString);
            }
        }
    }

    private static async Task SaveToFileAsync(FileInfo fi, string content)
    {
        if (!Directory.Exists(fi.DirectoryName))
        {
            Directory.CreateDirectory(fi.DirectoryName!);

            if (!OperatingSystem.IsWindows())
                await PathHelper.FixOwnershipAsync(fi.DirectoryName!, true);
        }
        await File.WriteAllTextAsync(fi.FullName, content);
        fi.Refresh();

        if (!OperatingSystem.IsWindows())
            await PathHelper.FixOwnershipAsync(fi.FullName);
    }

    public static async Task<string> FetchAppDownloadUrlAsync(string product, CancellationToken token = default)
    {
        var products = product.Split('|');
        if (products.Length != 3) return string.Empty;

        var wuCategoryId = products[0];
        var json = await HttpClientHelper.GetStringContentAsync(
            $"Game/GetAppPackage?WuCategoryId={wuCategoryId}",
            name: HttpClientNames.XboxDownload,
            token: token);

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
