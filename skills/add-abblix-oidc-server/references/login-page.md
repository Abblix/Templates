# The login page

The server redirects a user who has to sign in to the tenant's `LoginUri` with a `request_uri` query parameter.
The page checks the credentials against the application's user store, starts the server's session with
`IAuthSessionService.SignInAsync`, and redirects to the authorization endpoint with the same `request_uri`.
The subject put into `AuthSession` is the user id that `IUserInfoProvider` later receives as
`authSession.Subject`, so use the store's stable user id, never the email address.

Replace `UserStore.Authenticate` below with the application's own credential check. On ASP.NET Core Identity
that is `UserManager.FindByEmailAsync` followed by `SignInManager.CheckPasswordSignInAsync(user, password,
lockoutOnFailure: true)`, which checks the password and the lockout without issuing Identity's cookie, as the
`abblix-oidc-server` template does in its
[AuthController.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/Controllers/AuthController.cs).

## MVC

`Controllers/AuthController.cs`:

```csharp
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Features.UserAuthentication;
using Abblix.Oidc.Server.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UriBuilder = Abblix.Utils.UriBuilder;

public class AuthController : Controller
{
    [HttpGet]
    public IActionResult Login([FromQuery(Name = "request_uri")] string requestUri)
        => View(model: requestUri);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromServices] UserStore users,
        [FromServices] IAuthSessionService authSessions,
        [FromServices] ISessionIdGenerator sessionIds,
        [FromServices] IOidcEndpointResolver endpoints,
        [FromServices] TimeProvider clock,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm(Name = "request_uri")] string requestUri)
    {
        if (users.Authenticate(email, password) is not { } subject)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model: requestUri);
        }

        await authSessions.SignInAsync(new AuthSession(
            subject,
            sessionIds.GenerateSessionId(),
            clock.GetUtcNow(),
            CookieAuthenticationDefaults.AuthenticationScheme));

        // Every endpoint is enabled by default, so the resolver has an address for the authorization endpoint
        var authorize = new UriBuilder(endpoints.Resolve(OidcEndpoints.Authorize)!)
            { Query = { [AuthorizationRequest.Parameters.RequestUri] = requestUri } };
        return Redirect(authorize);
    }
}
```

`Views/Auth/Login.cshtml`:

```cshtml
@model string
<form method="post" asp-action="Login">
    <div asp-validation-summary="ModelOnly"></div>
    <input type="hidden" name="request_uri" value="@Model" />
    <input name="email" type="email" autocomplete="username" required />
    <input name="password" type="password" autocomplete="current-password" required />
    <button type="submit">Sign in</button>
</form>
```

The form tag helper adds the antiforgery token that `[ValidateAntiForgeryToken]` checks; it needs
`@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` in `Views/_ViewImports.cshtml`, which a project made from
the MVC template already has.

## Minimal API

At the top of `Program.cs`:

```csharp
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Features.UserAuthentication;
using Abblix.Oidc.Server.Model;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UriBuilder = Abblix.Utils.UriBuilder;
```

Before `builder.Build()`:

```csharp
builder.Services.AddAntiforgery();
```

After `builder.Build()`. The antiforgery middleware goes after `app.UseRouting()` (and after
`app.UseAuthentication()` if the application has it): placed anywhere else, or left out, it lets a post without
the token through.

```csharp
app.UseAntiforgery();

app.MapGet("/Auth/Login", (HttpContext http, IAntiforgery antiforgery, [FromQuery(Name = "request_uri")] string requestUri) =>
{
    var token = antiforgery.GetAndStoreTokens(http);
    var html = $"""
        <form method="post" action="/Auth/Login">
          <input type="hidden" name="{token.FormFieldName}" value="{token.RequestToken}">
          <input type="hidden" name="request_uri" value="{System.Net.WebUtility.HtmlEncode(requestUri)}">
          <input name="email" type="email" autocomplete="username" required>
          <input name="password" type="password" autocomplete="current-password" required>
          <button type="submit">Sign in</button>
        </form>
        """;
    return Results.Content(html, "text/html");
});

app.MapPost("/Auth/Login", async (
    [FromForm] string email,
    [FromForm] string password,
    [FromForm(Name = "request_uri")] string requestUri,
    UserStore users,
    IAuthSessionService authSessions,
    ISessionIdGenerator sessionIds,
    IOidcEndpointResolver endpoints,
    TimeProvider clock) =>
{
    if (users.Authenticate(email, password) is not { } subject)
        return Results.Redirect($"/Auth/Login?request_uri={Uri.EscapeDataString(requestUri)}");

    await authSessions.SignInAsync(new AuthSession(
        subject,
        sessionIds.GenerateSessionId(),
        clock.GetUtcNow(),
        CookieAuthenticationDefaults.AuthenticationScheme));

    var authorize = new UriBuilder(endpoints.Resolve(OidcEndpoints.Authorize)!)
        { Query = { [AuthorizationRequest.Parameters.RequestUri] = requestUri } };
    return Results.Redirect(authorize);
});
```

The page above is the smallest that works; an application with Razor Pages or its own front end renders the same
form there. Whatever renders it must HTML-encode `request_uri` and carry the antiforgery token.

## What the session may carry

`AuthSession` takes optional properties, set with an object initializer: `AuthenticationMethodReferences` (how
the user signed in, for example `["pwd"]`, released as the `amr` claim), `AuthContextClassRef`, `Email` and
`EmailVerified` (the address used to sign in; the server does not put it into an ID token itself, the
`IUserInfoProvider` receives the session and decides what it releases), and `AdditionalClaims`, a `JsonObject`
kept in the session cookie.
