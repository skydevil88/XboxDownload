using System;
using Microsoft.Identity.Client;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XboxDownload.Helpers.Security;

namespace XboxDownload.Helpers.Network;

public static class XboxAuthHelper
{
    private const string ClientId = "b3900558-4f9d-43ef-9db5-cfc7cb01874e";

    private const string Authority = "https://login.microsoftonline.com/consumers";

    private static readonly string[] Scopes = ["XboxLive.signin", "offline_access"];

    private static readonly IPublicClientApplication App =
    PublicClientApplicationBuilder
        .Create(ClientId)
        .WithAuthority(Authority)
        .WithDefaultRedirectUri()
        .Build();

    static XboxAuthHelper()
    {
        MsalTokenCacheHelper.EnableSerialization(App.UserTokenCache);
    }

    public static async Task<string> GetXbl3TokenAsync(bool interactive = false)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = cts.Token;

        AuthenticationResult result;

        try
        {
            var account = (await App.GetAccountsAsync()).FirstOrDefault();

            if (account != null)
            {
                result = await App
                    .AcquireTokenSilent(Scopes, account)
                    .ExecuteAsync(cancellationToken);
            }
            else if (interactive)
            {
                result = await App
                    .AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
            }
            else
            {
                return string.Empty;
            }
        }
        catch (MsalUiRequiredException)
        {
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }

        var msaAccessToken = result.AccessToken;

        // 1️、Xbox Live User Token
        var xboxText = await HttpClientHelper.GetStringContentAsync(
            "https://user.auth.xboxlive.com/user/authenticate",
            method: "POST",
            postData: JsonSerializer.Serialize(new
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
            contentType: "application/json",
            token: cancellationToken);

        using var xboxJson = JsonDocument.Parse(xboxText);

        if (!xboxJson.RootElement.TryGetProperty("Token", out var xboxTokenProp))
            return string.Empty;

        var xboxUserToken = xboxTokenProp.GetString();

        // 2️、XSTS Token
        var xstsText = await HttpClientHelper.GetStringContentAsync(
            "https://xsts.auth.xboxlive.com/xsts/authorize",
            method: "POST",
            postData: JsonSerializer.Serialize(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xboxUserToken }
                },
                RelyingParty = "http://update.xboxlive.com",
                TokenType = "JWT"
            }),
            contentType: "application/json",
            token: cancellationToken);

        using var xstsJson = JsonDocument.Parse(xstsText);

        // Important: XSTS errors do NOT include a Token field
        if (!xstsJson.RootElement.TryGetProperty("Token", out var tokenProp))
            return string.Empty;

        var xstsToken = tokenProp.GetString();

        var uhs = xstsJson.RootElement
            .GetProperty("DisplayClaims")
            .GetProperty("xui")[0]
            .GetProperty("uhs")
            .GetString();

        return $"XBL3.0 x={uhs};{xstsToken}";
    }
}
