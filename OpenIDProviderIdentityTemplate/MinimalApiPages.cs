using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Features.UserAuthentication;
using Abblix.Oidc.Server.Model;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using UriBuilder = Abblix.Utils.UriBuilder;

namespace AspNetIdentitySample;

/// <summary>
/// The host's own pages on Minimal API, the counterpart of the MVC host's controllers and views: the home page, the
/// error page and the login page the server sends a user to.
/// </summary>
public static class MinimalApiPages
{
    /// <summary>Where the server sends a user to sign in; the tenant's LoginUri names it.</summary>
    public const string LoginPath = "/Auth/Login";

    /// <summary>Where the exception handler sends a failed request outside Development.</summary>
    public const string ErrorPath = "/Error";

    public static void MapHostPages(this WebApplication app)
    {
        var demoClient = app.Environment.IsDevelopment()
            ? $"""<p><a class="btn btn-primary" href="{DemoClient.StartPath}">Sign in with the demo client</a></p>"""
            : string.Empty;

        app.MapGet("/", () => Html(Pages.Layout("Home", $"""
            <div class="container-sm">
                <h1 class="display-4 m-5 text-center">Welcome to Abblix OIDC Server</h1>
                <p>This provider is powered by the Abblix OIDC Server library, which implements the OpenID Connect and
                OAuth 2.0 protocols on the server side.</p>
                {demoClient}
                <ul>
                    <li>Explore the <a href="https://github.com/Abblix/Oidc.Server">repository on GitHub</a></li>
                    <li>Read the <a href="https://docs.abblix.com/">documentation</a></li>
                </ul>
            </div>
            """)));

        app.Map(ErrorPath, () => Html(Pages.Card("Error", "<p>An error occurred while processing your request.</p>")));

        app.MapGet(LoginPath, ([FromQuery(Name = "request_uri")] string? requestUri, IAntiforgery antiforgery, HttpContext context) =>
            Html(LoginPage(antiforgery.GetAndStoreTokens(context), requestUri, error: null)));

        app.MapPost(LoginPath, async (
                [FromForm] string? email,
                [FromForm] string? password,
                [FromForm] string? requestUri,
                IAntiforgery antiforgery,
                HttpContext context,
                IAuthSessionService authService,
                IOidcEndpointResolver endpointResolver,
                ISessionIdGenerator sessionIdGenerator,
                TimeProvider clock,
                UserManager<IdentityUser> userManager,
                SignInManager<IdentityUser> signIn) =>
            {
                // A page on another site could submit this form with the attacker's own credentials: the victim's
                // browser would be signed in as the attacker, and whatever the victim then authorizes or enters lands
                // in the attacker's account. The antiforgery token the form carries is what such a page cannot supply.
                // A form opened in another tab before signing in carries a token that no longer matches, so it is shown again.
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    return Html(LoginPage(antiforgery.GetAndStoreTokens(context), requestUri,
                        "The sign-in form has expired, please sign in again"));
                }

                var user = string.IsNullOrEmpty(email) ? null : await userManager.FindByEmailAsync(email);

                // verifies the hash and enforces lockout, but issues no cookie: the session stays the library's
                var result = user is not null && !string.IsNullOrEmpty(password)
                    ? await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)
                    : SignInResult.Failed;

                if (!result.Succeeded)
                    return Html(LoginPage(antiforgery.GetAndStoreTokens(context), requestUri, "Invalid username or password"));

                // If authentication is successful, create a new authentication session
                var authSession = new AuthSession(
                    user!.Id,                                        // the subject the claims adapter resolves later
                    sessionIdGenerator.GenerateSessionId(),
                    clock.GetUtcNow(),
                    CookieAuthenticationDefaults.AuthenticationScheme)
                {
                    Email = user.Email,
                    EmailVerified = user.EmailConfirmed,
                    AuthenticationMethodReferences = ["pwd"],        // lands in the amr claim

                    // snapshot the security stamp: the cookie's OnValidatePrincipal compares it on every
                    // request, so a password change or UpdateSecurityStampAsync ends this session
                    AdditionalClaims = new JsonObject
                    {
                        ["security_stamp"] = await userManager.GetSecurityStampAsync(user),
                    },
                };

                // Sign in the user using the authentication service
                await authService.SignInAsync(authSession);

                // A visit to the login page on its own carries no authorization request to resume
                if (string.IsNullOrEmpty(requestUri))
                    return Results.Redirect("/");

                // Redirect the user to the authorization endpoint URL, recovering the OIDC flow. The resolver answers
                // the address as mapped, a route prefix or a path base included; every endpoint is enabled here, as the
                // defaults leave them, so it always has an address to return.
                var authorizeUrl = new UriBuilder(endpointResolver.Resolve(OidcEndpoints.Authorize)!)
                    { Query = { [AuthorizationRequest.Parameters.RequestUri] = requestUri } };
                return Results.Redirect(authorizeUrl);
            })
            // The handler checks the antiforgery token itself, so an expired form gets the login page again
            // rather than the bare 400 the automatic check answers
            .DisableAntiforgery();
    }

    private static string LoginPage(AntiforgeryTokenSet tokens, string? requestUri, string? error)
    {
        var errorBlock = error == null ? string.Empty : $"""<div class="text-danger mb-3">{Pages.Encode(error)}</div>""";
        return Pages.Card("Sign in", $"""
            <form method="post" action="{LoginPath}">
                <input type="hidden" name="{Pages.Encode(tokens.FormFieldName)}" value="{Pages.Encode(tokens.RequestToken)}" />
                <input type="hidden" name="requestUri" value="{Pages.Encode(requestUri)}" />
                <div class="form-group mb-3">
                    <label for="email" class="form-label">Email</label>
                    <input type="email" class="form-control" id="email" name="email" required autocomplete="email" autofocus />
                </div>
                <div class="form-group mb-3">
                    <label for="password" class="form-label">Password</label>
                    <input type="password" class="form-control" id="password" name="password" required autocomplete="current-password" />
                </div>
                {errorBlock}
                <div class="d-grid mt-4">
                    <button type="submit" class="btn btn-primary">Login</button>
                </div>
            </form>
            """);
    }

    private static IResult Html(string page) => Results.Content(page, "text/html");
}
