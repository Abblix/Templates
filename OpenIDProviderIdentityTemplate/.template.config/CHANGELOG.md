# Changelog: abblix-oidc-server

What changed in the project that the OpenIDProviderIdentityTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template.

## 3.0.0

First release of this template.

- The short name `abblix-oidc-server` and its aliases `oidc-server`, `oauth2-server` and `openidconnect` now create this provider. In 2.x they created the in-memory provider, which is now `abblix-oidc-server-gettingstarted`; a project created from that one is described in its own changelog.
- An OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 3.0.0 for .NET 11, serving one issuer declared as a tenant at `https://localhost:<port>`.
- Users are kept by ASP.NET Core Identity on EF Core 11 with SQLite, which stores passwords as salted hashes.
- Client registrations and the signing key are stored in SQLite too, so a restart keeps them and tokens issued before it still validate. The signing key is protected with ASP.NET Core Data Protection, whose key ring is a local folder; a build warning says what a real deployment uses instead.
- A test client, `test_client`, is seeded on first run.
- `--https-port` sets the HTTPS port the provider is served on and names in its issuer. When it is omitted or 0, a free port between 7000 and 7300 is picked.
- `--user` seeds a development sample user into the Identity store, and `--password` sets its password. Without `--password`, a random password is generated and printed once on the console at first run. Without `--user`, no user is seeded.
- Bootstrap is bundled into `wwwroot/lib` by Vite during the build, which installs the npm packages itself when they are missing.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `OpenIDProvider` as the project name.
