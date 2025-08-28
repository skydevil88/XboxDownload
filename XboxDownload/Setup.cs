using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using XboxDownload.Helpers.Network;

namespace XboxDownload;

public static class Setup
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";

        services.AddHttpClient("Default", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        services.AddHttpClient("XboxDownload", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"XboxDownload/{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}");
            client.DefaultRequestHeaders.Add("X-Organization", "XboxDownload");
            client.DefaultRequestHeaders.Add("X-Author", "Devil");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        services.AddHttpClient("NoCache", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });

        services.AddSingleton<HttpClientHelper>();

        return services.BuildServiceProvider();
    }
}