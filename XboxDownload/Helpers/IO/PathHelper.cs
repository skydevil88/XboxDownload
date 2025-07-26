using System;
using System.IO;

namespace XboxDownload.Helpers.IO;

public static class PathHelper
{
    private static string _localFolder = string.Empty;

    private static string LocalFolder
    {
        get
        {
            if (!string.IsNullOrEmpty(_localFolder))
                return _localFolder;

            _localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(XboxDownload));

            if (!Directory.Exists(_localFolder))
                Directory.CreateDirectory(_localFolder);
            
            return _localFolder;
        }
    }

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
}