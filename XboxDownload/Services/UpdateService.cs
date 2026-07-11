using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    private const long MaxUpdatePackageBytes = 512L * 1024L * 1024L;
    private const long MultiThreadDownloadThresholdBytes = 32L * 1024L * 1024L;
    private const long MultiThreadDownloadPartBytes = 8L * 1024L * 1024L;
    private const int MultiThreadDownloadConcurrency = 4;

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
            var tempDirectory = Path.Combine(Path.GetTempPath(), nameof(XboxDownload));
            try
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);

                Directory.CreateDirectory(tempDirectory);

                var saveFilepath = Path.Combine(tempDirectory, fileName);
                var downloadResult = await ShowUpdateDownloadProgressAsync(fastestUrl, saveFilepath);
                if (downloadResult == UpdateDownloadResult.Cancelled)
                    return;

                if (downloadResult == UpdateDownloadResult.Success)
                {
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
                            await File.WriteAllTextAsync(
                                scriptPath,
                                CreateWindowsUpdateScript(extractDir.FullName, tempDirectory, installContext),
                                CancellationToken.None);
                            _ = CommandHelper.RunCommandAsync("cmd.exe", $"/c \"{scriptPath}\"");
                        }
                        else
                        {
                            var scriptPath = Path.Combine(tempDirectory, "update.sh");
                            await File.WriteAllTextAsync(
                                scriptPath,
                                CreateUnixUpdateScript(extractDir.FullName, tempDirectory, installContext),
                                CancellationToken.None);
                            await CommandHelper.RunCommandAsync("chmod", $"+x \"{scriptPath}\"");
                            StartDetachedUnixUpdateScript(scriptPath);
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

        await DialogHelper.ShowInfoDialogAsync(
            ResourceHelper.GetString("Update.UpdateFailedTitle"),
            ResourceHelper.GetString("Update.DownloadFailed"),
            Icon.Error);
    }

    [GeneratedRegex(@"/releases/tag/(?<tag_name>[^\d]*(?<version>\d+(\.\d+){2,3}))$", RegexOptions.Compiled)]
    private static partial Regex GitHubTagRegex();

    private sealed record UpdateDownloadProgress(string Message, long DownloadedBytes, long TotalBytes, bool IsDownloading)
    {
        public static UpdateDownloadProgress Stage(string message) => new(message, 0, 0, false);

        public static UpdateDownloadProgress Downloading(long downloadedBytes, long totalBytes) =>
            new(string.Empty, downloadedBytes, totalBytes, true);
    }

    private enum UpdateDownloadResult
    {
        Success,
        Cancelled,
        Failed
    }

    private readonly record struct DownloadPart(int Index, long From, long To)
    {
        public long Length => To - From + 1;
    }

    private sealed class UpdateDownloadDialogState(
        CancellationTokenSource cancellation,
        Button cancelButton) : IDisposable
    {
        public bool CanClose { get; set; }

        public void CancelDownload()
        {
            if (cancellation.IsCancellationRequested)
                return;

            cancellation.Cancel();
            cancelButton.IsEnabled = false;
        }

        public void Dispose()
        {
            cancellation.Dispose();
        }
    }

    private static async Task<UpdateDownloadResult> ShowUpdateDownloadProgressAsync(string url, string saveFilepath)
    {
        var downloadCancellation = new CancellationTokenSource();
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var downloadingText = ResourceHelper.GetString("Update.Downloading");
        var processingText = ResourceHelper.GetString("Update.Progress.Processing");
        var unknownSizeText = ResourceHelper.GetString("Update.Progress.UnknownSize");
        var statusText = new TextBlock
        {
            Text = downloadingText,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        };
        var detailText = new TextBlock
        {
            Text = "",
            FontSize = 12,
            Foreground = Brushes.Gray
        };
        var percentText = new TextBlock
        {
            Text = ResourceHelper.GetString("Update.Progress.Preparing"),
            FontSize = 12,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 8,
            IsIndeterminate = true,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var cancelButton = new Button
        {
            Content = ResourceHelper.GetString("Update.Progress.Cancel"),
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var dialog = new Window
        {
            Title = ResourceHelper.GetString("Update.Title"),
            Width = 420,
            Height = 210,
            MinWidth = 420,
            MinHeight = 210,
            CanResize = false,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(18),
                Child = new Grid
                {
                    RowDefinitions =
                    [
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(new GridLength(1, GridUnitType.Star)),
                        new RowDefinition(GridLength.Auto)
                    ],
                    RowSpacing = 14,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = ResourceHelper.GetString("Update.Title"),
                            FontSize = 18,
                            FontWeight = FontWeight.Bold
                        },
                        new StackPanel
                        {
                            [Grid.RowProperty] = 1,
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                statusText,
                                new Grid
                                {
                                    Margin = new Thickness(0, 8, 0, 0),
                                    ColumnDefinitions =
                                    [
                                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                                        new ColumnDefinition(GridLength.Auto)
                                    ],
                                    Children =
                                    {
                                        detailText,
                                        percentText
                                    }
                                },
                                progressBar
                            }
                        },
                        new StackPanel
                        {
                            [Grid.RowProperty] = 2,
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children = { cancelButton }
                        }
                    }
                }
            }
        };
        Grid.SetColumn(percentText, 1);

        var dialogState = new UpdateDownloadDialogState(downloadCancellation, cancelButton);
        dialog.Tag = dialogState;
        cancelButton.Tag = dialogState;

        dialog.Closing += static (sender, args) =>
        {
            if (sender is not Window { Tag: UpdateDownloadDialogState dialogState })
                return;

            if (dialogState.CanClose) return;
            args.Cancel = true;
            dialogState.CancelDownload();
        };
        cancelButton.Click += static (sender, _) =>
        {
            if (sender is Button { Tag: UpdateDownloadDialogState dialogState })
                dialogState.CancelDownload();
        };

        var progress = new Progress<UpdateDownloadProgress>(state =>
        {
            if (downloadCancellation.IsCancellationRequested)
                return;

            if (state.IsDownloading)
            {
                statusText.Text = downloadingText;
                if (state.TotalBytes > 0)
                {
                    var percent = Math.Clamp(state.DownloadedBytes * 100d / state.TotalBytes, 0, 100);
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = percent;
                    detailText.Text = $"{FormatUpdateSize(state.DownloadedBytes)} / {FormatUpdateSize(state.TotalBytes)}";
                    percentText.Text = $"{percent:0}%";
                }
                else
                {
                    progressBar.IsIndeterminate = true;
                    detailText.Text = $"{FormatUpdateSize(state.DownloadedBytes)} / {unknownSizeText}";
                    percentText.Text = processingText;
                }

                return;
            }

            statusText.Text = state.Message;
            progressBar.IsIndeterminate = true;
            detailText.Text = "";
            percentText.Text = processingText;
        });

        var downloadTask = DownloadUpdatePackageAsync(url, saveFilepath, progress, downloadCancellation.Token);
        if (owner is null)
            dialog.Show();
        else
            dialog.Show(owner);

        try
        {
            return await downloadTask;
        }
        finally
        {
            dialogState.CanClose = true;
            dialog.Close();
            dialogState.Dispose();
        }
    }

    private static async Task<UpdateDownloadResult> DownloadUpdatePackageAsync(
        string url,
        string saveFilepath,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(UpdateDownloadProgress.Stage(ResourceHelper.GetString("Update.Progress.Connecting")));
            var rangeInfo = await GetRangeDownloadInfoAsync(url, cancellationToken);
            if (rangeInfo is { SupportsRange: true, TotalBytes: >= MultiThreadDownloadThresholdBytes })
            {
                try
                {
                    await DownloadUpdatePackageInPartsAsync(url, saveFilepath, rangeInfo.TotalBytes, progress, cancellationToken);
                    return UpdateDownloadResult.Success;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
                                           ex is IOException or InvalidDataException or HttpRequestException or TaskCanceledException)
                {
                    TryDeleteFile(saveFilepath);
                }
            }

            await DownloadUpdatePackageSingleThreadAsync(url, saveFilepath, rangeInfo.TotalBytes, progress, cancellationToken);
            return UpdateDownloadResult.Success;
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(saveFilepath);
            return UpdateDownloadResult.Cancelled;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            TryDeleteFile(saveFilepath);
            return cancellationToken.IsCancellationRequested
                ? UpdateDownloadResult.Cancelled
                : UpdateDownloadResult.Failed;
        }
    }

    private static async Task<(bool SupportsRange, long TotalBytes)> GetRangeDownloadInfoAsync(string url, CancellationToken cancellationToken)
    {
        using var rangeResponse = await HttpClientHelper.SendRequestAsync(
            url,
            headers: new Dictionary<string, string> { { "Range", "bytes=0-0" } },
            timeout: 30000,
            name: HttpClientNames.XboxDownload,
            token: cancellationToken);

        if (rangeResponse?.StatusCode == HttpStatusCode.PartialContent &&
            rangeResponse.Content.Headers.ContentRange?.Length is { } rangeLength and > 0 and <= MaxUpdatePackageBytes)
        {
            return (true, rangeLength);
        }

        using var response = await HttpClientHelper.SendRequestAsync(
            url,
            timeout: 30000,
            name: HttpClientNames.XboxDownload,
            token: cancellationToken);
        var contentLength = response?.Content.Headers.ContentLength ?? 0;
        return (false, contentLength is > 0 and <= MaxUpdatePackageBytes ? contentLength : 0);
    }

    private static async Task DownloadUpdatePackageSingleThreadAsync(
        string url,
        string saveFilepath,
        long expectedBytes,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClientHelper.SendRequestAsync(
            url,
            timeout: 180000,
            name: HttpClientNames.XboxDownload,
            token: cancellationToken);
        if (response is not { IsSuccessStatusCode: true })
            throw new HttpRequestException("Update package download failed.");

        var totalBytes = expectedBytes > 0 ? expectedBytes : response.Content.Headers.ContentLength ?? 0;
        if (totalBytes > MaxUpdatePackageBytes)
            throw new InvalidDataException("Update package exceeded the allowed size.");

        progress?.Report(UpdateDownloadProgress.Downloading(0, totalBytes));
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(saveFilepath);
        await CopyToFileWithProgressAsync(source, target, totalBytes, progress, cancellationToken);
    }

    private static async Task DownloadUpdatePackageInPartsAsync(
        string url,
        string saveFilepath,
        long totalBytes,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (totalBytes <= 0 || totalBytes > MaxUpdatePackageBytes)
            throw new InvalidDataException("Update package size is invalid.");

        var partDirectory = $"{saveFilepath}.parts";
        Directory.CreateDirectory(partDirectory);
        try
        {
            var parts = CreateDownloadParts(totalBytes);
            var downloadedBytesByPart = new long[parts.Count];
            progress?.Report(UpdateDownloadProgress.Downloading(0, totalBytes));

            await Parallel.ForEachAsync(
                parts,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MultiThreadDownloadConcurrency,
                    CancellationToken = cancellationToken
                },
                async (part, ct) =>
                {
                    await DownloadUpdatePartAsync(
                        url,
                        part,
                        Path.Combine(partDirectory, $"{part.Index}.part"),
                        totalBytes,
                        downloadedBytesByPart,
                        progress,
                        ct);
                });

            await using var target = File.Create(saveFilepath);
            foreach (var part in parts)
            {
                var partPath = Path.Combine(partDirectory, $"{part.Index}.part");
                await using var source = File.OpenRead(partPath);
                if (source.Length != part.Length)
                    throw new InvalidDataException("Update package part size does not match metadata.");

                await source.CopyToAsync(target, cancellationToken);
            }
        }
        finally
        {
            TryDeleteDirectory(partDirectory);
        }
    }

    private static async Task DownloadUpdatePartAsync(
        string url,
        DownloadPart part,
        string partPath,
        long totalBytes,
        long[] downloadedBytesByPart,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClientHelper.SendRequestAsync(
            url,
            headers: new Dictionary<string, string> { { "Range", $"bytes={part.From}-{part.To}" } },
            timeout: 180000,
            name: HttpClientNames.XboxDownload,
            token: cancellationToken);
        if (response?.StatusCode != HttpStatusCode.PartialContent)
            throw new InvalidDataException("Server does not support range download.");

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(partPath);
        await CopyPartWithProgressAsync(source, target, part, totalBytes, downloadedBytesByPart, progress, cancellationToken);
    }

    private static List<DownloadPart> CreateDownloadParts(long totalBytes)
    {
        var partCount = (int)Math.Min(
            MultiThreadDownloadConcurrency,
            Math.Max(1, (long)Math.Ceiling(totalBytes / (double)MultiThreadDownloadPartBytes)));
        var partSize = (long)Math.Ceiling(totalBytes / (double)partCount);
        var parts = new List<DownloadPart>(partCount);
        for (var index = 0; index < partCount; index++)
        {
            var start = index * partSize;
            if (start >= totalBytes)
                break;

            var end = Math.Min(totalBytes - 1, start + partSize - 1);
            parts.Add(new DownloadPart(index, start, end));
        }

        return parts;
    }

    private static async Task CopyToFileWithProgressAsync(
        Stream source,
        Stream target,
        long totalBytes,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long downloadedBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            downloadedBytes += read;
            if (downloadedBytes > MaxUpdatePackageBytes || totalBytes > 0 && downloadedBytes > totalBytes)
                throw new InvalidDataException("Update package exceeded the allowed size.");

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            progress?.Report(UpdateDownloadProgress.Downloading(downloadedBytes, totalBytes));
        }

        if (totalBytes > 0 && downloadedBytes != totalBytes)
            throw new InvalidDataException("Update package size does not match metadata.");
    }

    private static async Task CopyPartWithProgressAsync(
        Stream source,
        Stream target,
        DownloadPart part,
        long totalBytes,
        long[] downloadedBytesByPart,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long partBytes = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            partBytes += read;
            if (partBytes > part.Length)
                throw new InvalidDataException("Update package part exceeded the expected size.");

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            Interlocked.Exchange(ref downloadedBytesByPart[part.Index], partBytes);
            var downloadedBytes = downloadedBytesByPart.Sum();
            if (downloadedBytes > totalBytes || downloadedBytes > MaxUpdatePackageBytes)
                throw new InvalidDataException("Update package exceeded the allowed size.");

            progress?.Report(UpdateDownloadProgress.Downloading(downloadedBytes, totalBytes));
        }

        if (partBytes != part.Length)
            throw new InvalidDataException("Update package part size does not match metadata.");
    }

    private static string FormatUpdateSize(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignored
        }
    }

    private readonly record struct UpdateInstallContext(
        string? AppPath,
        string? AppDir,
        string? InstallDir,
        string? MacosAppBundlePath);

    private static string CreateWindowsUpdateScript(
        string extractDirectory,
        string tempDirectory,
        UpdateInstallContext installContext)
    {
        return $@"
@echo off
chcp 65001 >nul
timeout /t 3 /nobreak >nul
robocopy ""{extractDirectory}"" ""{installContext.AppDir}"" /e /is /it /r:5 /w:2 >nul 2>&1
if %ERRORLEVEL% GEQ 8 goto cleanup
start """" /d ""{installContext.AppDir}"" ""{installContext.AppPath}""
:cleanup
rd /s /q ""{tempDirectory}""
";
    }

    private static string CreateUnixUpdateScript(
        string extractDirectory,
        string tempDirectory,
        UpdateInstallContext installContext)
    {
        var script = $@"
#!/bin/bash
trap 'rm -rf -- ""{tempDirectory}""' EXIT
sleep 3

if [[ ""$(uname)"" == ""Darwin"" ]]; then
    APP_BUNDLE=""{installContext.MacosAppBundlePath}""
    APP_EXEC=""$APP_BUNDLE/Contents/MacOS/XboxDownloadLauncher""
    APP_BIN=""$APP_BUNDLE/Contents/MacOS/XboxDownload""
    RESOURCE_DIR=""$APP_BUNDLE/Contents/MacOS/Resource""
    PRESERVED_RESOURCE_DIR=""{tempDirectory}/preserved-resource""
    RUN_SCRIPT=""{installContext.InstallDir}/run_xboxdownload.command""
    ROOT_LAUNCH=false

    if [[ -d ""$RESOURCE_DIR"" ]]; then
        rm -rf -- ""$PRESERVED_RESOURCE_DIR""
        if ! cp -Rp ""$RESOURCE_DIR"" ""$PRESERVED_RESOURCE_DIR""; then
            exit 1
        fi
    fi

    if ! rm -rf -- ""$APP_BUNDLE""; then
        exit 1
    fi
    if ! cp -Rf ""{extractDirectory}""/. ""{installContext.InstallDir}""; then
        exit 1
    fi
    if [[ -d ""$PRESERVED_RESOURCE_DIR"" ]]; then
        rm -rf -- ""$RESOURCE_DIR""
        mkdir -p ""$(dirname ""$RESOURCE_DIR"")""
        if ! cp -Rp ""$PRESERVED_RESOURCE_DIR"" ""$RESOURCE_DIR""; then
            exit 1
        fi
    fi

    xattr -dr com.apple.quarantine ""$APP_BUNDLE"" 2>/dev/null || true
    xattr -dr com.apple.quarantine ""$RUN_SCRIPT"" 2>/dev/null || true
    chmod +x ""$APP_EXEC"" 2>/dev/null || true
    chmod +x ""$APP_BIN"" 2>/dev/null || true
    chmod +x ""$RUN_SCRIPT"" 2>/dev/null || true

    if [[ -n ""${{SUDO_USER:-}}"" ]]; then
        ROOT_LAUNCH=true
        REAL_GROUP=""$(id -gn ""$SUDO_USER"" 2>/dev/null || echo staff)""
        chown ""$SUDO_USER:$REAL_GROUP"" ""{installContext.InstallDir}"" 2>/dev/null || true
        chown -R ""$SUDO_USER:$REAL_GROUP"" ""$APP_BUNDLE"" ""$RUN_SCRIPT"" ""{installContext.InstallDir}/README.md"" 2>/dev/null || true
    fi

    if [[ ""$ROOT_LAUNCH"" == true ]]; then
        nohup ""$APP_EXEC"" >/dev/null 2>&1 </dev/null &
    else
        open ""$APP_BUNDLE"" &
    fi
else
    if ! cp -Rf ""{extractDirectory}""/. ""{installContext.InstallDir}""; then
        exit 1
    fi
    chmod +x ""{installContext.AppPath}"" 2>/dev/null || true
    chmod +x ""{installContext.InstallDir}/run_xboxdownload.sh"" 2>/dev/null || true
    nohup ""{installContext.AppPath}"" >/dev/null 2>&1 </dev/null &
fi

exit 0
";
        return script.Replace("\r\n", "\n");
    }

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

    private static void StartDetachedUnixUpdateScript(string scriptPath)
    {
        try
        {
            var command = $"nohup /bin/bash {QuoteShellArgument(scriptPath)} >/dev/null 2>&1 < /dev/null &";
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(command);
            process.Start();
        }
        catch
        {
            // ignored
        }
    }

    private static string QuoteShellArgument(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'")}'";
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
