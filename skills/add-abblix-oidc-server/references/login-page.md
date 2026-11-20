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
[AuthController.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/Controllers/AuthController.cs)
(MVC) and [MinimalApiPages.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/MinimalApiPages.cs)
(Minimal API).

Both variants below check the antiforgery token inside the handler with `IAntiforgery.IsRequestValidAsync`
rather than with `[ValidateAntiForgeryToken]` or the antiforgery middleware. Either of those answers a form
without a valid token with a bare 400, and that is what a user gets who opened the login page in a second tab
before signing in in the first: the sign-in changed the token, so the older form no longer matches. Checked in
the handler, a forged form is still refused, and an expired one is shown again with a fresh token.

## MVC

This variant needs views (`AddControllersWithViews` or `AddMvc`). An application with `AddControllers` alone
takes the Minimal API variant below, which works beside its controllers.

`Controllers/AuthController.cs`:

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

public class AuthController : Controller
{
    [HttpGet]
    public IActionResult Login([FromQuery(Name = "request_uri")] string? requestUri)
        => View(model: requestUri);

    [HttpPost]
    public async Task<IActionResult> Login(
        [FromServices] IAntiforgery antiforgery,
        [FromServices] UserStore users,
        [FromServices] IAuthSessionService authSessions,
        [FromServices] ISessionIdGenerator sessionIds,
        [FromServices] IOidcEndpointResolver endpoints,
        [FromServices] TimeProvider clock,
        [FromForm] string? email,
        [FromForm] string? password,
        [FromForm(Name = "request_uri")] string? requestUri)
    {
        // A page on another site could submit this form with the attacker's own credentials: the victim's browser
        // would be signed in as the attacker, and whatever the victim then authorizes or enters lands in the
        // attacker's account. The antiforgery token the form carries is what such a page cannot supply. A form
        // opened in another tab before signing in carries a token that no longer matches, so it is shown again.
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
        {
            ModelState.AddModelError("", "The sign-in form has expired, please sign in again");
            return View(model: requestUri);
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
            users.Authenticate(email, password) is not { } subject)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model: requestUri);
        }

        await authSessions.SignInAsync(new AuthSession(
            subject,
            sessionIds.GenerateSessionId(),
            clock.GetUtcNow(),
            CookieAuthenticationDefaults.AuthenticationScheme));

        // A visit to the login page on its own carries no authorization request to resume
        if (string.IsNullOrEmpty(requestUri))
            return Redirect("/");

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

The form tag helper adds the antiforgery token the handler checks, and a view returned again carries a fresh
one; it needs
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

After `builder.Build()`. The post endpoint checks the token itself and says so with `.DisableAntiforgery()`: a
Minimal API endpoint that binds a form otherwise expects the antiforgery middleware and fails without it.

```csharp
app.MapGet("/Auth/Login", (HttpContext http, IAntiforgery antiforgery, [FromQuery(Name = "request_uri")] string? requestUri) =>
    LoginPage(http, antiforgery, requestUri, error: null));

app.MapPost("/Auth/Login", async (
    [FromForm] string? email,
    [FromForm] string? password,
    [FromForm(Name = "request_uri")] string? requestUri,
    HttpContext http,
    IAntiforgery antiforgery,
    UserStore users,
    IAuthSessionService authSessions,
    ISessionIdGenerator sessionIds,
    IOidcEndpointResolver endpoints,
    TimeProvider clock) =>
{
    // A page on another site could submit this form with the attacker's own credentials: the victim's browser
    // would be signed in as the attacker, and whatever the victim then authorizes or enters lands in the
    // attacker's account. The antiforgery token the form carries is what such a page cannot supply. A form
    // opened in another tab before signing in carries a token that no longer matches, so it is shown again.
    if (!await antiforgery.IsRequestValidAsync(http))
        return LoginPage(http, antiforgery, requestUri, "The sign-in form has expired, please sign in again");

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
        users.Authenticate(email, password) is not { } subject)
        return LoginPage(http, antiforgery, requestUri, "Invalid email or password");

    await authSessions.SignInAsync(new AuthSession(
        subject,
        sessionIds.GenerateSessionId(),
        clock.GetUtcNow(),
        CookieAuthenticationDefaults.AuthenticationScheme));

    // A visit to the login page on its own carries no authorization request to resume
    if (string.IsNullOrEmpty(requestUri))
        return Results.Redirect($"{http.Request.PathBase}/");

    var authorize = new UriBuilder(endpoints.Resolve(OidcEndpoints.Authorize)!)
        { Query = { [AuthorizationRequest.Parameters.RequestUri] = requestUri } };
    return Results.Redirect(authorize);
})
// The handler checks the antiforgery token itself, so an expired form gets the login page again rather than the
// bare 400 the automatic check answers
.DisableAntiforgery();

// The form with a fresh antiforgery token, and the error of the previous attempt when there was one
static IResult LoginPage(HttpContext http, IAntiforgery antiforgery, string? requestUri, string? error)
{
    var token = antiforgery.GetAndStoreTokens(http);
    Func<string?, string?> encode = System.Net.WebUtility.HtmlEncode;

    // The path base carries the issuer's path when it has one, so the form posts inside the tenant
    var action = encode($"{http.Request.PathBase}/Auth/Login");
    var message = error is null ? "" : $"<p>{encode(error)}</p>";
    var html = $"""
        <form method="post" action="{action}">
          {message}
          <input type="hidden" name="{encode(token.FormFieldName)}" value="{encode(token.RequestToken)}">
          <input type="hidden" name="request_uri" value="{encode(requestUri ?? "")}">
          <input name="email" type="email" autocomplete="username" required>
          <input name="password" type="password" autocomplete="current-password" required>
          <button type="submit">Sign in</button>
        </form>
        """;
    return Results.Content(html, "text/html");
}
```

The page above is the smallest that works; an application with Razor Pages or its own front end renders the same
form there. Whatever renders it must HTML-encode `request_uri` and carry a fresh antiforgery token.

## What the session may carry

`AuthSession` takes optional properties, set with an object initializer: `AuthenticationMethodReferences` (how
the user signed in, for example `["pwd"]`, released as the `amr` claim), `AuthContextClassRef`, `Email` and
`EmailVerified` (the address used to sign in; the server does not put it into an ID token itself, the
`IUserInfoProvider` receives the session and decides what it releases), and `AdditionalClaims`, a `JsonObject`
kept in the session cookie.
