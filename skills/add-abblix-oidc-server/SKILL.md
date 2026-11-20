---
name: add-abblix-oidc-server
description: Adds Abblix OIDC Server 3.0 to an existing ASP.NET Core application so that the application itself becomes an OpenID Connect provider, issuing tokens to its own client applications. Use when asked to "make this app an OpenID Connect provider", "add an OIDC server", "add Abblix OIDC Server", "issue ID tokens from this app" or "let other apps sign in with this app's users". Not for signing users in through an external provider (that is an OpenID Connect client, Microsoft.AspNetCore.Authentication.OpenIdConnect), and not for starting a new project, which the abblix-oidc-server template does.
license: MIT
compatibility: ASP.NET Core on .NET 11 with Abblix OIDC Server 3.0 (NuGet packages Abblix.OIDC.Server.MVC or Abblix.OIDC.Server.MinimalAPI)
---

# Add Abblix OIDC Server to an existing ASP.NET Core application

The result: the application serves the OpenID Connect endpoints (`/.well-known/openid-configuration`,
`/connect/authorize`, `/connect/token` and the rest) for one issuer, signs users in through a login page of
its own, knows one client, and signs tokens with a key that is still the same after a restart. The steps follow
the `abblix-oidc-server` and `abblix-oidc-server-gettingstarted` templates of
[Abblix.Templates](https://github.com/Abblix/Templates), applied to an application that already exists.

## Rules that hold for the whole task

- Write only the API names this skill gives, or names you have read in the installed package. Version 3.0
  renamed and moved much of what 2.x had (`OidcOptions` no longer holds clients or keys, the registries take a
  `TenantDefinition`), so a name recalled from older samples compiles against nothing or against the wrong thing.
- The library targets .NET 11 only. If the project's `TargetFramework` is lower, say so and stop until the user
  agrees to raise it: raising a framework is their decision, it affects every other package of the project.
- Install exactly one adapter. Both together make the application refuse to start, since both map the same paths.
- The server does not start without a consent decision (step 2). Without `app.UseCors()` after routing
  (step 3), or without `IMemoryCache` (step 2), discovery and the token endpoint answer 500 while the
  authorization endpoint may still work, so a working sign-in redirect rules out neither cause.
- The server writes its session into the cookie scheme named `Cookies` and nowhere else. If the application's
  own sign-in uses that scheme too, stop at step 0 (see there): the two overwrite each other.
- Keep everything the application already does. Add registrations and middleware beside the existing ones; never
  remove or reorder the host's own middleware except where a step says where a call must go.
- Tell the user before you finish that the library is distributed under a source-available license, free of
  charge below the thresholds on its [pricing page](https://www.abblix.com/en/oidc-server/pricing) and for
  development and test environments, with one production issuer per free deployment.

## Step 0: read the application

Before editing, find out and note:

- `TargetFramework` in the project file (must be `net11.0`).
- The hosting model. `AddControllers`, `AddControllersWithViews` or `AddMvc` in `Program.cs` means MVC; only
  `app.MapGet`/`MapPost` and friends means Minimal API. Note whether views are registered
  (`AddControllersWithViews`, `AddMvc`, `AddRazorPages`): the MVC login page of step 4 needs them, and an
  application with `AddControllers` alone takes the Minimal API login endpoints instead, which work beside
  controllers.
- What authentication is already registered: `AddAuthentication`, `AddCookie`, `AddIdentity`,
  `AddDefaultIdentity`. If the application signs its own users in on the scheme named `Cookies` (an
  `AddCookie()` without a name, or `CookieAuthenticationDefaults.AuthenticationScheme`), stop and tell the user:
  the server's sign-in replaces that cookie's principal with its own (the application's roles and claims are
  gone), and the application's next sign-in ends the server's session. One of the two has to move to another
  scheme name, and which one is the user's decision. ASP.NET Core Identity's own cookie
  (`Identity.Application`) does not collide.
- Where users and their passwords live (ASP.NET Core Identity, a table of the application's own, an external
  directory). The server keeps no users; it asks the application for them.
- Whether `IDistributedCache` is registered (`AddStackExchangeRedisCache`, `AddDistributedSqlServerCache`,
  `AddDistributedMemoryCache`).
- Routes the application already serves under `/connect/` or `/.well-known/`, which would collide with the
  server's defaults.
- The address the application is served on in production. That address becomes the issuer, and every token
  carries it.

## Step 1: add the package

MVC applications:

```shell
dotnet add package Abblix.OIDC.Server.MVC --version 3.0.0
```

Minimal API applications:

```shell
dotnet add package Abblix.OIDC.Server.MinimalAPI --version 3.0.0
```

Each pulls in `Abblix.OIDC.Server`, `Abblix.OIDC`, `Abblix.JWT`, `Abblix.DependencyInjection` and
`Abblix.Utils`. Use the newest 3.x the feed offers if it is later than 3.0.0.

## Step 2: register the services

Add to `Program.cs`, before `builder.Build()`:

```csharp
using Abblix.Oidc.Server.Features.Consents;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Abblix.Oidc.Server.Features.UserInfo;
using Abblix.Oidc.Server.Mvc;          // a Minimal API application uses Abblix.Oidc.Server.MinimalApi instead

// The consent decision: without one the server does not start.
// This one grants every client what it asks for, which suits only clients the application's owner controls.
// To ask the end user, register an IUserConsentsProvider of your own instead.
builder.Services.AddConsentGrantedForEveryClient();

// One tenant: 3.0 always serves tenants, and an application with one issuer declares one.
builder.Services.AddOidcServices(options => options.Tenants.Add(new TenantDefinition
{
    Id = "main",

    // The address the application is served on, written into every token it issues
    Issuer = new Uri(builder.Configuration["Oidc:Issuer"]
        ?? throw new InvalidOperationException("Set Oidc:Issuer to the address the app is served on")),

    // Where the server sends a user who has to sign in (step 4)
    LoginUri = new Uri("/Auth/Login", UriKind.Relative),

    // Both variables are declared above this call: signingKey in step 6, firstClient in step 5
    SigningKeys = [signingKey],
    Clients = [firstClient],
}));

// The server keeps its session in a cookie of the scheme named "Cookies" (see step 0 if the application
// already uses that scheme for its own sign-in)
builder.Services.AddAuthentication().AddCookie();

// Authorization codes, request URIs and the like live here between requests.
// Several instances need a shared cache (Redis, SQL Server); skip this line if one is registered already.
builder.Services.AddDistributedMemoryCache();

// The server needs IMemoryCache and, in 3.0.0, does not register it. Views bring one, AddControllers alone
// and Minimal API do not; the call does nothing where one is registered already.
builder.Services.AddMemoryCache();

// The users: the server asks this service for the claims of a signed-in user
builder.Services.AddScoped<IUserInfoProvider, AppUserInfoProvider>();
```

Put the issuer in `appsettings.json` (and the development address in `appsettings.Development.json`):

```json
"Oidc": { "Issuer": "https://localhost:5001" }
```

The issuer must be the exact scheme, host and port clients reach, with no trailing slash. An issuer with a path
(`https://example.com/identity`) serves every endpoint under that path and also needs `app.UseMultiTenancy()`
(`using Abblix.Oidc.Server.AspNetCore.MultiTenancy;`) before `app.UseRouting()`; one without a path does not.

`AppUserInfoProvider` implements one method. It returns `null` only when the user does not exist, and leaves
out any claim it does not hold:

```csharp
using System.Text.Json.Nodes;
using Abblix.Oidc.Server.Features.MultiTenancy;
using Abblix.Oidc.Server.Features.UserAuthentication;
using Abblix.Oidc.Server.Features.UserInfo;

public sealed class AppUserInfoProvider(/* the application's user store */) : IUserInfoProvider
{
    public async Task<JsonObject?> GetUserInfoAsync(
        TenantDefinition tenant, AuthSession authSession, IEnumerable<string> requestedClaims)
    {
        // authSession.Subject is the user id the login page put into the session (step 4)
        // An asynchronous lookup in the store, such as UserManager.FindByIdAsync. A synchronous store drops
        // async from the method and returns Task.FromResult<JsonObject?>(...) instead.
        var user = await /* find the user by authSession.Subject */;
        if (user is null)
            return null;

        var claims = new JsonObject();
        foreach (var claim in requestedClaims)
        {
            switch (claim)
            {
                // A claim the user has no value for is left out, never written as null
                case "email" when user.Email is { } email: claims[claim] = email; break;
                case "email_verified": claims[claim] = user.EmailConfirmed; break;
                case "name" when user.DisplayName is { } name: claims[claim] = name; break;
            }
        }
        return claims;
    }
}
```

An application on ASP.NET Core Identity can start from the template's
[IdentityUserInfoProvider.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/IdentityUserInfoProvider.cs),
replacing its namespace with the application's and `IdentityUser` with the application's user type (an
application on `AddDefaultIdentity<ApplicationUser>` has no `UserManager<IdentityUser>`). Without any `IUserInfoProvider` the server falls back to one that knows no user: no ID token is issued
and the userinfo endpoint answers `invalid_token`.

## Step 3: map the endpoints

In the request pipeline, after `builder.Build()`:

```csharp
app.UseRouting();   // keep the application's own call if it has one
app.UseCors();      // after routing, before the endpoints: without it discovery and token answer 500

app.MapOidcControllers();   // MVC
// app.MapOidcEndpoints();  // Minimal API
```

The library's controllers carry no routes of their own, so `MapControllers()` and the default
`{controller}/{action}` route never reach them; `MapOidcControllers()` is what maps them. The server registers
its own CORS policy, so the application adds no `AddCors` for it; an application that already calls
`app.UseCors()` keeps its one call in that position. The server needs neither `UseAuthentication()` nor
`UseAuthorization()`: keep them if the application has them, do not add them if it does not (`UseAuthorization`
without `AddAuthorization` stops the application at startup).

If the application already serves something under `/connect/` or `/.well-known/`, move single endpoints with
`builder.Services.Configure<OidcRouteOptions>(routes => routes.Token = "/oauth2/token")`
(`using Abblix.Oidc.Server.AspNetCore;`), or move the whole server under a path by putting the path into the
issuer (`https://example.com/oidc`, with `app.UseMultiTenancy()` as in step 2) and mapping with no prefix. Do not
use the `prefix` argument of `MapOidcControllers` or `MapOidcEndpoints` for this: discovery then sits under the
prefix while its issuer does not carry it, and a client that checks the issuer against the address it fetched the
document from, as OpenID Connect Discovery requires, refuses it.

## Step 4: hand the user from the login page back to the server

When a user has to sign in, the server redirects to `LoginUri` with a `request_uri` query parameter. The login
page checks the credentials against the application's own user store, starts the server's session and sends the
browser back to the authorization endpoint with the same `request_uri`. Signing the user in with the
application's existing cookie (for example `SignInManager.PasswordSignInAsync`) is not enough: the server reads
only the session that `IAuthSessionService.SignInAsync` writes.

The MVC controller and view, and the Minimal API endpoints, are in
[references/login-page.md](references/login-page.md). A Minimal API application needs `app.UseAntiforgery()`
after `app.UseRouting()` for its login form: without it a post without the antiforgery token is accepted.

An application that already has a login page can add the hand-off at the end of its successful sign-in instead
of adding a second page (point `LoginUri` at it and carry `request_uri` through its form), but only when its own
cookie is a scheme other than `Cookies`, such as Identity's `Identity.Application` (step 0).

## Step 5: the first client

A web application signing in with the authorization code flow and PKCE:

```csharp
using System.Security.Cryptography;
using System.Text;
using Abblix.Oidc;
using Abblix.Oidc.Server.Features.ClientInformation;

var firstClient = new ClientInfo("my_web_app")
{
    // Only the SHA-512 hash of the secret is kept; the client sends the secret itself
    ClientSecrets = [new ClientSecret { Sha512Hash = SHA512.HashData(Encoding.UTF8.GetBytes(clientSecret)) }],
    TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretPost,
    AllowedGrantTypes = [GrantTypes.AuthorizationCode],
    PkceRequired = true,
    RedirectUris = [new Uri("https://localhost:5002/signin-oidc", UriKind.Absolute)],
    PostLogoutRedirectUris = [new Uri("https://localhost:5002/signout-callback-oidc", UriKind.Absolute)],
};
```

Ask the user for the client's redirect URIs; the ones above are what the ASP.NET Core OpenID Connect handler of a
client on `https://localhost:5002` uses. Read `clientSecret` from configuration or a secret store, never from
source. A browser or mobile application, which cannot keep a secret, sets
`TokenEndpointAuthMethod = ClientAuthenticationMethods.None` and no `ClientSecrets`. A client gets refresh tokens
only when `OfflineAccess` says so (`OfflineAccess.Allowed` or `OfflineAccess.ConsentRequired`); it is disabled by
default. Under `AddConsentGrantedForEveryClient()`, `ConsentRequired` is no safeguard: a client that sends
`prompt=consent` gets the refresh token, since every consent is granted.

Clients declared in `Clients` suit a fixed set the application owns. For clients registered at run time, the
`abblix-oidc-server` template keeps them in a database through `ITenantClientRegistrationStore`, see its
[DurableClientStore.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/OidcStore/DurableClientStore.cs).

## Step 6: a signing key that outlives a restart

`JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature)`, which the library's quick start uses, makes a new key at
every start: every token issued before a restart then fails validation, and two instances sign with different
keys. Use it only for a first local run, and tell the user. For anything else pick one of these, in this order of
preference for an application with no key store yet.

A certificate the deployment supplies, read at startup. The key id is the certificate's thumbprint, so it stays
the same across restarts and instances:

```csharp
using System.Security.Cryptography.X509Certificates;
using Abblix.Jwt;

var signingKey = X509Certificate2
    .CreateFromPemFile(
        builder.Configuration["Oidc:SigningCertificate:CertificatePath"]
            ?? throw new InvalidOperationException("Set Oidc:SigningCertificate:CertificatePath"),
        builder.Configuration["Oidc:SigningCertificate:KeyPath"]
            ?? throw new InvalidOperationException("Set Oidc:SigningCertificate:KeyPath"))
    .ToJsonWebKey(includePrivateKeys: true);
```

A PKCS#12 file works the same way through `X509CertificateLoader.LoadPkcs12FromFile(path, password)`. The private
key file is a secret: keep it outside the repository, mount it from the deployment's secret store, and put only
the paths in configuration. For development, one can be made with OpenSSL:

```shell
openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes -subj "/CN=OIDC signing" \
  -addext "keyUsage=critical,digitalSignature" -keyout signing.key.pem -out signing.cert.pem
```

Keys kept in the application's database, encrypted with ASP.NET Core Data Protection, as the
`abblix-oidc-server` template does: an `IAuthServiceKeysProvider` registered after `AddOidcServices`, which then
replaces the server's own, and the tenant declares no `SigningKeys`. Its
[DatabaseKeysProvider.cs](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/OidcStore/DatabaseKeysProvider.cs)
and the seeding in its `Program.cs` can be copied.

Keys held by HashiCorp Vault, OpenBao or Azure Key Vault, through the `Abblix.JWT.Vault` or `Abblix.JWT.Azure`
package, so the private key never enters the process: see
[External key custodians](https://github.com/Abblix/Oidc.Server/blob/master/EXTERNAL_KEYS.md).

## Step 7: prove it works

Build, start the application on its issuer address, and read the discovery document:

```shell
dotnet build
curl -k https://localhost:5001/.well-known/openid-configuration
curl -k https://localhost:5001/.well-known/jwks
```

Success: the first answers 200 with `"issuer"` equal to `Oidc:Issuer` and the endpoint addresses under it, the
second lists the signing key. Over plain HTTP the endpoints answer a redirect, which `curl` without `-L` reports
as an empty success, so test the HTTPS address. Restart the application and read the key set again: the `kid`
must not change. Then sign in once from the client, or open
`/connect/authorize?client_id=my_web_app&response_type=code&scope=openid&redirect_uri=<a registered redirect URI>&code_challenge=<S256 challenge>&code_challenge_method=S256`
in a browser: it must land on the login page, and after signing in, on the redirect URI with a `code`.

Before the application goes to production, set `AllowedHosts` in `appsettings.json` to the issuer's host name:
the server builds the addresses in the discovery document from the host a request arrives with.

## Troubleshooting

- 500 from discovery and the token endpoint, the log saying the endpoint contains CORS metadata but no
  middleware supporting CORS was found: `app.UseCors()` is missing or sits before `app.UseRouting()`.
- 500 from discovery and the token endpoint, `Unable to resolve service for type
  'Microsoft.Extensions.Caching.Memory.IMemoryCache'`: add `builder.Services.AddMemoryCache()` (step 2).
- 500 from the login page, `No service for type
  'Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory' has been registered`: the MVC login page
  was added to an application without views; use the Minimal API login endpoints (step 0).
- The application stops at startup naming `IUserConsentsProvider`: no consent decision is registered (step 2).
- The application stops at startup saying both OIDC transport adapters are registered: remove one package and
  its `AddOidcServices` namespace.
- `InvalidOperationException: Scheme already exists: Cookies`: the application already registers that scheme.
  Remove the added `AddCookie()`, and check step 0: if the application signs its own users in on it, stop.
- 404 from `/.well-known/openid-configuration`: `MapOidcControllers()` or `MapOidcEndpoints()` is missing. With
  an issuer that has a path, the document is under that path.
- The application does not start ("Hosting failed to start"), saying "The pipeline does not call
  UseMultiTenancy()": the issuer has a path and the pipeline lacks `app.UseMultiTenancy()`; call it before
  `app.UseRouting()` and `app.UseAuthentication()` (step 2).
- After signing in, the browser returns to the login page: the login page signed the user in with the
  application's own cookie but did not call `IAuthSessionService.SignInAsync`, did not pass `request_uri` on, or
  the application's own sign-in shares the `Cookies` scheme and wrote over the server's session (step 0).
- `UseMultiTenancy` is not found: add `using Abblix.Oidc.Server.AspNetCore.MultiTenancy;`.
- A token issued before a restart no longer validates: the signing key is generated at startup (step 6).
- No ID token and userinfo answers `invalid_token`: no `IUserInfoProvider` is registered, or it returns `null`
  for the subject the login page set.
