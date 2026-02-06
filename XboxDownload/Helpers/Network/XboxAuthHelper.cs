using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace XboxDownload.Helpers.Network;

public static class XboxAuthHelper
{
    private static readonly string ClientId =
        "b3900558-4f9d-43ef-9db5-cfc7cb01874e";

    private static readonly string Authority =
        "https://login.microsoftonline.com/consumers";

    private static readonly string[] Scopes =
        ["XboxLive.signin"];

    public static async Task<string> GetXbl3TokenAsync(
        bool interactive = false,
        CancellationToken cancellationToken = default)
    {
        var app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithDefaultRedirectUri()
            .Build();

        AuthenticationResult result;

        try
        {
            var accounts = await app.GetAccountsAsync();

            if (accounts.Any() && !interactive)
            {
                result = await app
                    .AcquireTokenSilent(Scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
            }
            else
            {
                result = await app
                    .AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
            }
        }
        catch (MsalClientException ex)
            when (ex.ErrorCode == "authentication_canceled")
        {
            throw new OperationCanceledException(
                "The user canceled the Microsoft sign-in process.");
        }

        string msaAccessToken = result.AccessToken;

        using var http = new HttpClient();

        // 1️、Xbox Live User Token
        var xboxResp = await http.PostAsync(
            "https://user.auth.xboxlive.com/user/authenticate",
            JsonContent(new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = $"d={msaAccessToken}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            }),
            cancellationToken);

        if (!xboxResp.IsSuccessStatusCode)
            throw new Exception("Failed to authenticate Xbox Live user.");

        var xboxJson = JsonDocument.Parse(
            await xboxResp.Content.ReadAsStringAsync(cancellationToken));

        if (!xboxJson.RootElement.TryGetProperty("Token", out var xboxTokenProp))
            throw new Exception("Xbox Live user token was not returned.");

        string xboxUserToken = xboxTokenProp.GetString()!;

        // 2️、XSTS Token
        var xstsResp = await http.PostAsync(
            "https://xsts.auth.xboxlive.com/xsts/authorize",
            JsonContent(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xboxUserToken }
                },
                RelyingParty = "http://update.xboxlive.com",
                TokenType = "JWT"
            }),
            cancellationToken);

        var xstsText = await xstsResp.Content.ReadAsStringAsync(cancellationToken);
        var xstsJson = JsonDocument.Parse(xstsText);

        // Important: XSTS errors do NOT include a Token field
        if (!xstsJson.RootElement.TryGetProperty("Token", out var tokenProp))
        {
            if (xstsJson.RootElement.TryGetProperty("XErr", out var xerr))
                throw new Exception($"XSTS authorization was denied. XErr={xerr.GetInt32()}");

            throw new Exception("XSTS response did not contain a token.");
        }

        string xstsToken = tokenProp.GetString()!;

        var uhs = xstsJson.RootElement
            .GetProperty("DisplayClaims")
            .GetProperty("xui")[0]
            .GetProperty("uhs")
            .GetString();

        return $"XBL3.0 x={uhs};{xstsToken}";
    }

    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}
