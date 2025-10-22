using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClarityDesk.Infrastructure.Authentication;

/// <summary>
/// LINE OAuth 2.0 Authentication Handler
/// </summary>
public class LineAuthenticationHandler : OAuthHandler<OAuthOptions>
{
    public LineAuthenticationHandler(
        IOptionsMonitor<OAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// 建立 Authentication Ticket (取得 LINE User Profile 並建立 Claims)
    /// </summary>
    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        ClaimsIdentity identity,
        AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        // 使用 Access Token 取得 LINE User Profile
        var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"無法取得 LINE 使用者資料: {response.StatusCode}");
        }

        var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // 提取使用者資料
        var userId = user.RootElement.GetProperty("userId").GetString();
        var displayName = user.RootElement.GetProperty("displayName").GetString();
        var pictureUrl = user.RootElement.TryGetProperty("pictureUrl", out var pic) ? pic.GetString() : null;

        // 建立 Claims
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId ?? ""));
        identity.AddClaim(new Claim(ClaimTypes.Name, displayName ?? ""));
        if (!string.IsNullOrEmpty(pictureUrl))
        {
            identity.AddClaim(new Claim("picture", pictureUrl));
        }

        // 建立 Principal 與 Ticket
        var principal = new ClaimsPrincipal(identity);
        var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, user.RootElement);

        await Events.CreatingTicket(context);

        return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
    }
}
