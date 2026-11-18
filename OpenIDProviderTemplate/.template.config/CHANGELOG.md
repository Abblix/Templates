# Changelog: abblix-oidc-server-gettingstarted

What changed in the project that the OpenIDProviderTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template; a version that left the template untouched says so.

## 3.0.0

### Upgrading a project created from 2.x

- The short name changed. In 2.x, `dotnet new abblix-oidc-server` created this in-memory provider. That name now creates the production-shaped provider on ASP.NET Core Identity and EF Core, together with the aliases `oidc-server`, `oauth2-server` and `openidconnect`; this template is `abblix-oidc-server-gettingstarted`.
- The project targets .NET 11 and references Abblix.Oidc.Server.Mvc 3.0.0, up from .NET 10 and 2.2.
- The provider declares its issuer as one tenant: `AddOidcServices` adds a `TenantDefinition` to `options.Tenants`, and `LoginUri` and `SigningKeys` are set on it instead of on the options themselves.
- The issuer is now fixed in `Program.cs` as `https://localhost:<port>`, the port the project is served on. In 2.x no issuer was set, so it came from the host each request arrived on. Before deploying, set `Issuer` to the provider's public address, or clients reject its tokens.
- A consent provider is registered with `AddConsentGrantedForEveryClient()`, which grants every client everything it asks for. That suits only clients of your own; to ask the end user, register your own `IUserConsentsProvider` instead.
- `app.MapOidcControllers()` maps the protocol endpoints, because the library's controllers carry no routes of their own.
- `AuthController.Login` finds the authorization endpoint with `IOidcEndpointResolver.Resolve(OidcEndpoints.Authorize)` instead of `IUriResolver.Content(Path.Authorize)`.
- `TestUserStorage.GetUserInfoAsync` takes the `TenantDefinition` the request reached as its first parameter.
- Clients are read from the `Oidc:Clients` configuration section instead of a commented example in `Program.cs`. Only `appsettings.Development.json` declares one, so a host started in any other environment has no client. A section the binder cannot read, or a client with no `ClientId`, stops startup with a message naming the section.

### Added

- `--https-port` sets the HTTPS port the provider is served on and names in its issuer. When it is omitted or 0, a free port between 7000 and 7300 is picked, so two projects created one after another no longer share a port. In 2.x every project used 5001.
- `--user` and `--password` seed a development sample user into the in-memory store; without `--user` the store starts empty. The password defaults to `Passw0rd!` and is stored and compared in plaintext.
- `requests.http`, to send from Visual Studio, JetBrains Rider or VS Code against the running provider: the discovery document, the published keys, an access token from the client credentials grant and introspection of that token. Its address follows the project's port.
- `console_client` in `appsettings.Development.json`, for the client credentials grant only, with the secret `secret` published with the template. Because that secret is public, the client gets no token for a registered API.
- Token introspection (RFC 7662) is switched on with `AddIntrospection()` and advertised in the discovery document.
- `dotnet publish` no longer copies `appsettings.Development.json`, which holds the demo client's published secret.
- Build warnings (CS1030) mark what to replace before production: the in-memory user store, the plaintext password check, the signing key generated at every startup and the in-memory distributed cache. Each warning names the fix and links the matching guide.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `OpenIDProvider` as the project name.

### Changed

- The browser tab shows the Abblix icon, and the footer links to the Abblix OIDC Server repository on GitHub instead of the home page.
- The README is rewritten for 3.0. It also asks deployers to set `AllowedHosts` to the issuer's host name instead of `*`, since the provider builds its endpoint addresses from the host a request arrives with.

### Removed

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
