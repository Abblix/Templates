using System.Security.Cryptography;
using System.Text;
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features.ClientInformation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using UriBuilder = Abblix.Utils.UriBuilder;

namespace AspNetIdentitySample;

/// <summary>
/// The relying party of the demo client test_client, served by this host in Development so a browser can walk the
/// whole sign-in on a new project: the authorization request with PKCE, the login page, the code arriving at the
/// redirect URI, and the sign-out back to the client. A real client is a separate application; this one only shows
/// the code it receives and the token request that redeems it.
/// </summary>
public static class DemoClient
{
    /// <summary>The client appsettings.Development.json declares for the authorization code flow.</summary>
    public const string ClientId = "test_client";

    /// <summary>Where the home page sends a browser to start the sign-in.</summary>
    public const string StartPath = "/demo/start";

    private const string FlowCookie = "demo_client_flow";
    private const string Scope = "openid profile email offline_access";

    /// <summary>
    /// Maps the demo client's pages when the configuration declares test_client, which only
    /// appsettings.Development.json does: without it the authorization request would be refused.
    /// </summary>
    public static void MapDemoClient(this WebApplication app, ClientInfo[] clients)
    {
        var client = Array.Find(clients, candidate => candidate.ClientId == ClientId);
        if (client is not { RedirectUris.Length: > 0, PostLogoutRedirectUris.Length: > 0 })
            return;

        // The addresses come from the client's own registration, so the pages cannot drift from what the
        // provider accepts for it
        var redirectUri = client.RedirectUris[0];
        var postLogoutRedirectUri = client.PostLogoutRedirectUris[0];

        app.MapGet(StartPath, (HttpContext context, IDataProtectionProvider protection, IOidcEndpointResolver endpoints) =>
        {
            // A fresh state and PKCE verifier per sign-in, kept in a protected cookie until the code comes back
            var state = RandomValue();
            var codeVerifier = RandomValue();
            var codeChallenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

            context.Response.Cookies.Append(FlowCookie, Protector(protection).Protect($"{state} {codeVerifier}"),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/demo",
                    MaxAge = TimeSpan.FromMinutes(10),
                });

            var authorize = new UriBuilder(endpoints.Resolve(OidcEndpoints.Authorize)!)
            {
                Query =
                {
                    ["client_id"] = ClientId,
                    ["response_type"] = "code",
                    ["scope"] = Scope,
                    ["redirect_uri"] = redirectUri.OriginalString,
                    ["state"] = state,
                    ["code_challenge"] = codeChallenge,
                    ["code_challenge_method"] = "S256",
                },
            };
            return Results.Redirect(authorize);
        });

        app.MapGet(redirectUri.AbsolutePath, (
            HttpContext context,
            IDataProtectionProvider protection,
            IOidcEndpointResolver endpoints,
            string? code,
            string? state,
            string? error,
            string? error_description) =>
        {
            var flow = ReadFlow(context, protection);
            context.Response.Cookies.Delete(FlowCookie, new CookieOptions { Path = "/demo" });

            if (flow is not { } started || state != started.State)
                return Html(Pages.Card("Sign-in not started here", $"""
                    <p>This answer does not belong to a sign-in this browser started, or it came after ten minutes.</p>
                    <a class="btn btn-primary" href="{StartPath}">Sign in again</a>
                    """));

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                return Html(Pages.Card("Sign-in refused", $"""
                    <p>The provider answered <code>{Pages.Encode(error)}</code>: {Pages.Encode(error_description)}</p>
                    <a class="btn btn-primary" href="{StartPath}">Sign in again</a>
                    """));

            var token = endpoints.Resolve(OidcEndpoints.Token)!.OriginalString;
            var command = $"curl -k {token} -d grant_type=authorization_code -d code={code} " +
                          $"-d redirect_uri={Uri.EscapeDataString(redirectUri.OriginalString)} -d code_verifier={started.CodeVerifier} " +
                          $"-d client_id={ClientId} -d client_secret=secret";

            return Html(Pages.Card("Signed in", $"""
                <p>The provider sent {ClientId} an authorization code. A client redeems it at the token endpoint,
                within a minute, for an ID token, an access token and a refresh token.</p>
                <dl>
                    <dt>code</dt><dd style="word-break: break-all"><code id="code">{Pages.Encode(code)}</code></dd>
                    <dt>code_verifier</dt><dd style="word-break: break-all"><code id="code_verifier">{Pages.Encode(started.CodeVerifier)}</code></dd>
                </dl>
                <p>The same token request from a terminal:</p>
                <pre class="bg-light p-2" style="white-space: pre-wrap; word-break: break-all"><code>{Pages.Encode(command)}</code></pre>
                {SignOutForm(endpoints, postLogoutRedirectUri)}
                """));
        });

        app.MapGet(postLogoutRedirectUri.AbsolutePath, () =>
            Html(Pages.Card("Signed out", $"""
                <p>The provider ended the session and sent the browser back to {ClientId}.</p>
                <a class="btn btn-primary" href="{StartPath}">Sign in again</a>
                """)));
    }

    // RP-Initiated Logout sent as a form, so the provider asks the user to confirm it (no ID token is held here)
    private static string SignOutForm(IOidcEndpointResolver endpoints, Uri postLogoutRedirectUri) => $"""
        <form method="post" action="{Pages.Encode(endpoints.Resolve(OidcEndpoints.EndSession)!.OriginalString)}">
            <input type="hidden" name="client_id" value="{ClientId}" />
            <input type="hidden" name="post_logout_redirect_uri" value="{Pages.Encode(postLogoutRedirectUri.OriginalString)}" />
            <button type="submit" class="btn btn-outline-secondary">Sign out</button>
        </form>
        """;

    private static (string State, string CodeVerifier)? ReadFlow(HttpContext context, IDataProtectionProvider protection)
    {
        if (!context.Request.Cookies.TryGetValue(FlowCookie, out var cookie))
            return null;

        try
        {
            var parts = Protector(protection).Unprotect(cookie).Split(' ');
            return parts is [var state, var codeVerifier] ? (state, codeVerifier) : null;
        }
        catch (CryptographicException)
        {
            // A cookie protected with a key this host no longer holds, such as one from before a restart
            return null;
        }
    }

    private static IDataProtector Protector(IDataProtectionProvider protection)
        => protection.CreateProtector(nameof(DemoClient));

    private static string RandomValue() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static IResult Html(string page) => Results.Content(page, "text/html");
}
