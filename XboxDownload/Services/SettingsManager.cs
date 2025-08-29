using System;
using System.IO;
using System.Text.Json;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Utilities;
using XboxDownload.Models;

namespace XboxDownload.Services;

public static class SettingsManager
{
    private static readonly string SettingsFilePath = PathHelper.GetLocalFilePath("Settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings(); // fallback on error
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonHelper.Indented);
            File.WriteAllText(SettingsFilePath, json);

            if (!OperatingSystem.IsWindows())
                _ = PathHelper.FixOwnershipAsync(SettingsFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}