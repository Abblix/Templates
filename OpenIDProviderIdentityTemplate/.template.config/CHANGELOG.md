# Changelog: abblix-oidc-server

What changed in the project that the OpenIDProviderIdentityTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template.

## 3.0.0

First release of this template.

- The short name `abblix-oidc-server` and its aliases `oidc-server`, `oauth2-server` and `openidconnect` now create this provider. In 2.x they created the in-memory provider, which is now `abblix-oidc-server-gettingstarted`; a project created from that one is described in its own changelog.
- An OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 3.0.0 for .NET 11, serving one issuer declared as a tenant. The issuer is fixed in `Program.cs` as `https://localhost:<port>`; before deploying, set `Issuer` to the provider's public address, or clients reject its tokens. In 2.x, `abblix-oidc-server` set no issuer and took it from the host each request arrived on.
- Users are kept by ASP.NET Core Identity on EF Core 11 with SQLite, which stores passwords as salted hashes.
- Client registrations and the signing key are stored in SQLite too, so a restart keeps them and tokens issued before it still validate. The signing key is protected with ASP.NET Core Data Protection, whose key ring is a local folder; a build warning says what a real deployment uses instead.
- Clients are read from the `Oidc:Clients` configuration section, and only `appsettings.Development.json` declares any: `test_client` for the authorization code flow with PKCE and redirect addresses on `https://localhost:5002`, and `console_client` for the client credentials grant only, which gets no token for the registered `weather` API. Both use the secret `secret`, published with the template. `dotnet publish` does not copy that file.
- Configured clients are seeded into `oidc.db` at startup. The server's own client checks judge them first, and a client they refuse stops startup with a message naming `Oidc:Clients`. A seeded row stays whatever environment later runs on the database, and a later edit to its configuration is not applied: the host logs a warning naming the client and the row to delete. A build warning says to delete the demo clients' rows, or start from a database never run in Development, before a real deployment.
- `requests.http`, to send from Visual Studio, JetBrains Rider or VS Code against the running provider: the discovery document, the published keys, an access token for `console_client` and introspection of that token. Token introspection (RFC 7662) is switched on with `AddIntrospection()` and advertised in the discovery document.
- `--https-port` sets the HTTPS port the provider is served on and names in its issuer. When it is omitted or 0, a free port between 7000 and 7300 is picked.
- `--user` seeds a development sample user into the Identity store, and `--password` sets its password. Without `--password`, a random password is generated and printed once on the console at first run, which then waits for Enter on an interactive console so the password can be copied. Without `--user`, no user is seeded.
- Building the project needs Node.js and npm. The build bundles Bootstrap into `wwwroot/lib` with Vite, again whenever the client sources are newer than the bundle, and installs the npm packages first when `ClientAssets/node_modules` is missing. A new project has neither, so its first build always runs both. The 2.x provider needed no Node.js.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `OpenIDProvider` as the project name.
