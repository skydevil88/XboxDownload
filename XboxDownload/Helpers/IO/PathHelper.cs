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

    public static string GetResourceFilePath(string fileName) => Path.Combine(AppContext.BaseDirectory, "Resource", fileName);


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
}