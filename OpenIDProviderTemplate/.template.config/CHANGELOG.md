# Changelog: abblix-oidc-server-gettingstarted

What changed in the project that the OpenIDProviderTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template; a version that left the template untouched says so.

## 3.1.0

- The project references Abblix OIDC Server 3.1.0, up from 3.0.0.
- `Program.cs` no longer calls `AddDistributedMemoryCache()`, nor `AddMemoryCache()` in the Minimal API host: 3.1 falls back to in-process caches of its own when the host registers none. The build warning on the cache stays where a distributed cache would be registered and says what the in-process one loses.

## 3.0.0

### Upgrading a project created from 2.x

- The short name changed. In 2.x, `dotnet new abblix-oidc-server` created this in-memory provider. That name now creates the production-shaped provider on ASP.NET Core Identity and EF Core, and so do the short names `oidc-server`, `oauth2-server` and `openidconnect`; this template is `abblix-oidc-server-gettingstarted`.
- The project targets .NET 11 and references Abblix.Oidc.Server.Mvc 3.0.0, up from .NET 10 and 2.2.
- The provider declares its issuer as one tenant: `AddOidcServices` adds a `TenantDefinition` to `options.Tenants`, and `LoginUri` and `SigningKeys` are set on it instead of on the options themselves.
- The issuer is now fixed in `Program.cs` as `https://localhost:<port>`, the port the project is served on. In 2.x no issuer was set, so it came from the host each request arrived on. Before deploying, set `Issuer` to the provider's public address, or clients reject its tokens.
- A consent provider is registered with `AddConsentGrantedForEveryClient()`, which grants every client everything it asks for. That suits only clients of your own; to ask the end user, register your own `IUserConsentsProvider` instead.
- `app.MapOidcControllers()` maps the protocol endpoints, because the library's controllers carry no routes of their own.
- `AuthController.Login` finds the authorization endpoint with `IOidcEndpointResolver.Resolve(OidcEndpoints.Authorize)` instead of `IUriResolver.Content(Path.Authorize)`.
- `TestUserStorage.GetUserInfoAsync` takes the `TenantDefinition` the request reached as its first parameter.
- Clients are read from the `Oidc:Clients` configuration section instead of a commented example in `Program.cs`. Only `appsettings.Development.json` declares any, so a host started in any other environment has no client. A section the binder cannot read, or a client with no `ClientId`, stops startup with a message naming the section.

### Added

- `--host` picks the host style. `minimal`, the default, maps the OpenID endpoints with `app.MapOidcEndpoints()` from Abblix.Oidc.Server.MinimalApi and serves the home, error and login pages from `MinimalApiPages.cs`, with no MVC controllers or Razor views. `mvc` keeps the 2.x shape: Abblix.Oidc.Server.Mvc, `app.MapOidcControllers()`, and the pages as controllers with views. A project created from 2.x is an MVC host; pass `--host mvc` to compare it with a new one.
- The demo client's pages, served in Development from `DemoClient.cs`: the home page starts a sign-in as `test_client` with a fresh state and PKCE verifier, and after the login page the browser lands on a page of the project that shows the code, the verifier and the token request, with a button to sign out. `test_client` redirects to those pages, and it may use refresh tokens for the `offline_access` scope.
- `EndSessionPages.cs` answers the end-session endpoint with a page asking the user to confirm a sign-out that carries no ID token, and a page saying they are signed out when no client asked to be sent back.
- A `.gitignore` for the project.
- `--https-port` sets the HTTPS port the provider is served on and names in its issuer. When it is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created. In 2.x every project used 5001.
- `--user` and `--password` set the development sample user of the in-memory store, `demo@example.com` by default, so a new project has a user to sign in with; `--user ""` starts the store empty. The store holds the user only in Development. Without `--password`, a password is generated at every start and printed on the console. It is stored and compared in plaintext.
- `requests.http`, to send from Visual Studio, JetBrains Rider or VS Code against the running provider: the discovery document, the published keys, an access token from the client credentials grant and introspection of that token, then the exchange of a code from the demo client for tokens, the user's claims and a refresh. Its address follows the project's port.
- Two demo clients in `appsettings.Development.json`, both with the secret `secret` published with the template. `test_client` is for the authorization code flow with PKCE. `console_client` is for the client credentials grant only; because its secret is public, it gets no token for a registered API.
- Token introspection (RFC 7662) is switched on with `AddIntrospection()` and advertised in the discovery document.
- `dotnet publish` no longer copies `appsettings.Development.json`, which holds the demo clients' published secret.
- Build warnings (CS1030) mark what to replace before production: the in-memory user store, the plaintext password check, the signing key generated at every startup and the in-memory distributed cache. Each warning names the fix and links the matching guide.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `OpenIDProvider` as the project name. `ide.host.json` marks `--host`, `--https-port`, `--user` and `--password` as options for that dialog to show.

### Fixed

- The login form checks its antiforgery token in the handler. A form submitted from another site is refused, so it can no longer sign the browser in with the other site's account, and a form opened in another tab before a sign-in is shown again rather than refused with a bare 400.
- A sign-in on the login page opened on its own, with no authorization request to resume, goes to the home page.

### Changed

- The login page asks for an email or login in a plain text field, so a `--user` value that is not an email address signs in too. 2.x took only an email address there.
- The template's display name, shown by `dotnet new list` and in the Visual Studio dialog, is now "Abblix OpenID Provider - Getting Started (in-memory)", where 2.x showed "ASP.NET Core OpenID Provider".
- The browser tab shows the Abblix icon, and the footer links to the Abblix OIDC Server repository on GitHub instead of the home page.
- The README is rewritten for 3.0. It also asks deployers to set `AllowedHosts` to the issuer's host name instead of `*`, since the provider builds its endpoint addresses from the host a request arrives with.

### Removed

- `Views/Shared/_Layout.cshtml.css`, which no page loaded.
- The license header that `AuthController.cs` carried over from the library source. It forbade modifying or redistributing the file, which contradicted the MIT license of the template.

## 2.0.1 - 2026-02-19

- No change to this template.

## 2.0.0 - 2026-02-19

- The project targets .NET 10 and references Abblix.Oidc.Server.Mvc 2.2, up from .NET 9 and 1.6.0.
- After a successful login, `AuthController` builds the authorization endpoint address with `Abblix.Utils.UriBuilder` and `IUriResolver` instead of concatenating a string with `System.Web.HttpUtility`, so the redirect stays correct behind a reverse proxy.
- `TestUserStorage.GetUserInfoAsync` takes the `AuthSession` instead of the subject.
- The signing key is created with `PublicKeyUsages.Signature` instead of `JsonWebKeyUseNames.Sig`.

## 1.2.0 - 2025-08-14

- The project targets .NET 9 and references Abblix.Oidc.Server.Mvc 1.6.0, up from .NET 8 and 1.1.0.

## 1.0.2 - 2024-08-23

- No change to this template.

## 1.0.1 - 2024-08-22

- README corrections only.

## 1.0.0 - 2024-08-22

- First release: an OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 1.1.0 for .NET 8, with a login page, an in-memory user store and a signing key generated at startup.
