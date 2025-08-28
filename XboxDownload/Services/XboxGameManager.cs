using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.IO;
using XboxDownload.Helpers.Utilities;

namespace XboxDownload.Services;

public static class XboxGameManager
{
    private static readonly string XboxGameFilePath = PathHelper.GetResourceFilePath("XboxGame.json");

    public static readonly ConcurrentDictionary<string, Product> Dictionary = new();

    public static void Load()
    {
        if (!File.Exists(XboxGameFilePath)) return;

        try
        {
            var json = File.ReadAllText(XboxGameFilePath);
            var xboxGame = JsonSerializer.Deserialize<XboxGame>(json);
            if (xboxGame?.Serialize is not { IsEmpty: false }) return;

            foreach (var kvp in xboxGame.Serialize)
            {
                Dictionary[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // ignored
        }
    }

    private static CancellationTokenSource? _saveCancellationTokenSource;

    private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(6);

    public static async Task SaveAsync()
    {
        try
        {
            // ReSharper disable once MethodHasAsyncOverload
            _saveCancellationTokenSource?.Cancel();

            _saveCancellationTokenSource = new CancellationTokenSource();
            var token = _saveCancellationTokenSource.Token;

            await Task.Delay(SaveDelay, token);
            token.ThrowIfCancellationRequested();

            var xboxGame = new XboxGame
            {
                Serialize = Dictionary
            };
            var json = JsonSerializer.Serialize(xboxGame, JsonHelper.Indented);

            var directoryPath = Path.GetDirectoryName(XboxGameFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath!);
            }

            await File.WriteAllTextAsync(XboxGameFilePath, json, token);
        }
        catch (TaskCanceledException)
        {
        }
        catch
        {
            // ignored
        }
    }

    private class XboxGame
    {
        public ConcurrentDictionary<string, Product>? Serialize { get; init; }
    }

    public class Product
    {
        public Version Version { get; set; } = new();
        public long FileSize { get; set; }
        public string Url { get; set; } = "";
    }
}


internal static class XboxPackage
{
    public class Game
    {
        public string Code { get; init; } = "";
        public GameData? Data { get; init; } = new();
    }

    public class GameData
    {
        public long Size { get; init; }
        public string Url { get; init; } = "";
    }

    public class App
    {
        public string Code { get; init; } = "";
        public List<AppData> Data { get; init; } = [];
    }

    public class AppData
    {
        public string Name { get; init; } = "";
        public long Size { get; set; }
        public string Url { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
