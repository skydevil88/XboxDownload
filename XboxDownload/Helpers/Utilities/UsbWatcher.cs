using System;
using System.Management;
using System.Runtime.Versioning;

namespace XboxDownload.Helpers.Utilities;

public class UsbWatcher : IDisposable
{
    public event Action<string>? UsbInserted;
    public event Action<string>? UsbRemoved;

    private ManagementEventWatcher? _insertWatcher;
    private ManagementEventWatcher? _removeWatcher;

    [SupportedOSPlatform("windows")]
    public void Start()
    {
        // 监听插入
        var insertQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
        _insertWatcher = new ManagementEventWatcher(insertQuery);
        _insertWatcher.EventArrived += (_, e) =>
        {
            var path = GetDriveName(e);
            if (path != null)
            {
                UsbInserted?.Invoke(path);
            }
        };
        _insertWatcher.Start();

        // 监听移除
        var removeQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3");
        _removeWatcher = new ManagementEventWatcher(removeQuery);
        _removeWatcher.EventArrived += (_, e) =>
        {
            var path = GetDriveName(e);
            if (path != null)
            {
                UsbRemoved?.Invoke(path);
            }
        };
        _removeWatcher.Start();
    }

    [SupportedOSPlatform("windows")]
    private static string? GetDriveName(EventArrivedEventArgs e)
    {
        try
        {
            return e.NewEvent.Properties["DriveName"]?.Value?.ToString();
        }
        catch
        {
            return null;
        }
    }
    
    [SupportedOSPlatform("windows")]
    public void Dispose()
    {
        _insertWatcher?.Stop();
        _insertWatcher?.Dispose();

        _removeWatcher?.Stop();
        _removeWatcher?.Dispose();
    }
}