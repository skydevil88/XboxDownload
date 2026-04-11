using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XboxDownload.Helpers.System;

namespace XboxDownload.Helpers.IO;

public static class PathHelper
{
    private static string LocalFolder
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(field)) return field;

            var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
            var rootPath = GetRootPath(sudoUser);
            var targetPath = Path.Combine(rootPath, nameof(XboxDownload));

            if (Directory.Exists(targetPath)) return field = targetPath;

            Directory.CreateDirectory(targetPath);
            if (!OperatingSystem.IsWindows() && !string.IsNullOrEmpty(sudoUser))
            {
                _ = FixOwnershipAsync(targetPath, true);
            }

            return field = targetPath;

            static string GetRootPath(string? sudo)
            {
                if (OperatingSystem.IsWindows())
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var isMac = OperatingSystem.IsMacOS();
                var home = !string.IsNullOrEmpty(sudo)
                    ? (isMac ? $"/Users/{sudo}" : $"/home/{sudo}")
                    : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (isMac)
                    return Path.Combine(home, "Library", "Application Support");

                var xdg = string.IsNullOrEmpty(sudo) ? Environment.GetEnvironmentVariable("XDG_DATA_HOME") : null;
                return xdg ?? Path.Combine(home, ".local", "share");
            }
        }
    } = string.Empty;

    public static string GetLocalFilePath(string fileName) => Path.Combine(LocalFolder, fileName);

    public static readonly string SystemHostsPath = GetSystemHostsPath();

    private static string GetSystemHostsPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return "/etc/hosts";
        }

        throw new PlatformNotSupportedException("Unsupported OS for hosts path detection.");
    }

    public static string GetResourceFilePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Resource", fileName);


    /// <summary>
    /// Fixes file or directory ownership when running under sudo/root.
    /// Supports macOS and Linux. On macOS, the default group is "staff";
    /// on Linux, the user's primary group is detected via `id -gn`.
    /// </summary>
    /// <param name="path">File or directory path.</param>
    /// <param name="isDirectory">True if the path is a directory; false for a file.</param>
    public static async Task FixOwnershipAsync(string path, bool isDirectory = false)
    {
        // Determine the username (prefer the original user if running under sudo)
        var user = Environment.GetEnvironmentVariable("SUDO_USER")
                   ?? Environment.GetEnvironmentVariable("LOGNAME")
                   ?? Environment.UserName;

        string? group;

        if (OperatingSystem.IsMacOS())
        {
            // On macOS, the default group for normal users is "staff"
            group = "staff";
        }
        else if (OperatingSystem.IsLinux())
        {
            // On Linux, try to detect the user's primary group using `id -gn`
            var groupResult = await CommandHelper.RunCommandWithOutputAsync("id", $"-gn {user}");
            group = groupResult.FirstOrDefault()?.Trim();

            // Fallback to username if detection fails
            if (string.IsNullOrEmpty(group))
                group = user;
        }
        else
        {
            // Other OS: do nothing
            return;
        }

        if (isDirectory)
        {
            // Change ownership recursively for directories
            await CommandHelper.RunCommandAsync("chown", $"-R {user}:{group} \"{path}\"");
        }
        else
        {
            // Change ownership for a single file
            await CommandHelper.RunCommandAsync("chown", $"{user}:{group} \"{path}\"");
        }
    }
    
    private static string? _cachedDownloadsPath;

    /// <summary>
    /// Gets the Linux user's download directory path, resolving sudo users and XDG configurations.
    /// Only returns a path if the directory actually exists.
    /// </summary>
    /// <returns>The absolute path to the downloads folder, or null if not found.</returns>
    public static string? GetLinuxDownloadsPath()
    {
        // 1. Return cached result if available
        if (_cachedDownloadsPath != null) return _cachedDownloadsPath;

        // --- Part 1: Resolve Home Directory ---
        string? home = null;
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");

        if (!string.IsNullOrEmpty(sudoUser))
        {
            // If running via sudo, try to get the original user's home from /etc/passwd
            try
            {
                var entry = File.ReadLines("/etc/passwd")
                    .FirstOrDefault(l => l.StartsWith($"{sudoUser}:", StringComparison.Ordinal));
                if (entry != null)
                {
                    var parts = entry.Split(':');
                    if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5]))
                        home = parts[5];
                }
            }
            catch
            {
                /* fallback if file is inaccessible */
            }

            // Hardcoded guess if passwd parsing fails
            home ??= (sudoUser == "root" ? "/root" : $"/home/{sudoUser}");
        }
        else
        {
            // Priority: $HOME env > .NET API > fallback string concatenation
            home = Environment.GetEnvironmentVariable("HOME");
            home ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrEmpty(home))
            {
                var userName = Environment.UserName;
                home = userName == "root" ? "/root" : $"/home/{userName}";
            }
        }

        if (string.IsNullOrEmpty(home)) return null;

        // --- Part 2: Resolve Downloads Directory ---

        // 2a. Try XDG User Directories configuration (standard for Linux desktops)
        var xdgConfigPath = Path.Combine(home, ".config/user-dirs.dirs");
        if (File.Exists(xdgConfigPath))
        {
            try
            {
                foreach (var line in File.ReadLines(xdgConfigPath))
                {
                    var trimmed = line.Trim();
                    // Ensure it matches the key and is not a comment
                    if (trimmed.StartsWith("XDG_DOWNLOAD_DIR", StringComparison.Ordinal) && !trimmed.StartsWith('#'))
                    {
                        var parts = trimmed.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            // Remove potential quotes and resolve $HOME variable
                            var rawPath = parts[1].Trim(' ', '"').Replace("$HOME", home);
                            
                            // Handle relative paths or paths with dots
                            var fullPath = Path.GetFullPath(rawPath, home);
                            
                            if (Directory.Exists(fullPath))
                                return _cachedDownloadsPath = fullPath;
                        }
                    }
                }
            }
            catch
            {
                /* proceed to fallback detection */
            }
        }

        // 2b. Physical directory check with localized names
        // Essential for systems without xdg-user-dirs or customized installations
        string[] fallbacks =
        [
            "Downloads",        // English / German / Dutch
            "下载",             // Chinese (Simplified)
            "下載",             // Chinese (Traditional)
            "ダウンロード",      // Japanese
            "다운로드",          // Korean
            "Téléchargements", // French
            "Descargas",       // Spanish
            "Download",        // Italian / Portuguese
            "Загрузки",        // Russian
            "Indirilenler"     // Turkish
        ];

        foreach (var name in fallbacks)
        {
            var testPath = Path.Combine(home, name);
            if (Directory.Exists(testPath))
                return _cachedDownloadsPath = testPath;
        }

        // No existing directory found
        return null;
    }
}