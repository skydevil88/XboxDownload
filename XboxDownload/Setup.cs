using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using XboxDownload.Helpers.Network;
using XboxDownload.Services;

namespace XboxDownload;

public static class Setup
{
    public static IServiceProvider ConfigureServices()
    {
        var userAgent = $"{nameof(XboxDownload)}/" +
            $"{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version} " +
            $"(+{UpdateService.Project})";

        var services = new ServiceCollection();

        services.AddHttpClient(HttpClientNames.Default, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }).ConfigurePrimaryHttpMessageHandler(() => CreateBaseHandler());

        services.AddHttpClient(HttpClientNames.XboxDownload, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            client.DefaultRequestHeaders.Add("X-Organization", nameof(XboxDownload));
            client.DefaultRequestHeaders.Add("X-Author", "Devil");
        }).ConfigurePrimaryHttpMessageHandler(() => CreateBaseHandler());

        services.AddHttpClient(HttpClientNames.SpeedTest, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            client.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
        }).ConfigurePrimaryHttpMessageHandler(() => CreateBaseHandler(isSpeedTest: true));

        services.AddSingleton<HttpClientHelper>();

        return services.BuildServiceProvider();
    }

    private static SocketsHttpHandler CreateBaseHandler(bool isSpeedTest = false) => new()
    {
        UseCookies = false,
        EnableMultipleHttp2Connections = true,
        AutomaticDecompression = isSpeedTest ? DecompressionMethods.None : DecompressionMethods.All,
        PooledConnectionLifetime = isSpeedTest ? TimeSpan.Zero : TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = isSpeedTest ? TimeSpan.Zero :TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = isSpeedTest ? 50 : 20,
        AllowAutoRedirect = !isSpeedTest,
        UseProxy = !isSpeedTest
    };
}