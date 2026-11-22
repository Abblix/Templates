# abblix-oidc-server

`abblix-oidc-server` creates an OpenID Provider on ASP.NET Core and Abblix OIDC Server 3.0, as a Minimal API host or, with `--host mvc`, an MVC host. Users are kept by ASP.NET Core Identity on EF Core with SQLite, and the issuer with its clients, the signing key and dynamic client registrations are stored in SQLite as well, so a restart keeps them. Start a real project from this template. For a smaller provider that keeps everything in memory and is meant for learning, see [abblix-oidc-server-gettingstarted](abblix-oidc-server-gettingstarted.md).

The template lives in the [OpenIDProviderIdentityTemplate](../OpenIDProviderIdentityTemplate) folder. What changed between versions is in its [changelog](../OpenIDProviderIdentityTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.
- [Node.js](https://nodejs.org/) with npm. The build bundles Bootstrap into `wwwroot/lib` with Vite and first installs the npm packages with `npm ci`, exactly as `ClientAssets/package-lock.json` records them, whenever that lock is newer than the last install, so the first build of a new project runs both. Without Node.js the build stops with an error that says so.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-oidc-server -n MyProvider
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder.

A project with the main options set:

```shell
dotnet new abblix-oidc-server -n MyProvider --https-port 7100 --user alice@example.com --password "Str0ng-Passw0rd"
```

The same provider as an MVC host:

```shell
dotnet new abblix-oidc-server -n MyProvider --host mvc
```

The short names `oidc-server`, `oauth2-server` and `openidconnect` create the same project.

## Options

`dotnet new abblix-oidc-server -h` lists every option of the installed version, with its type, default and description. The main ones:

- `--host` picks how the host serves the OpenID endpoints and its own pages. `minimal`, the default, maps them as Minimal API endpoints and references Abblix.Oidc.Server.MinimalApi; the login page and the other pages are rendered by `MinimalApiPages.cs`. `mvc` serves the endpoints as MVC controllers from Abblix.Oidc.Server.Mvc, and the pages as controllers with Razor views. Everything else is the same in both: the settings, the database with its clients, Identity, the stores, the demo client and the sign-out pages. Both login pages check their antiforgery token in the handler, so a form submitted from another site is refused and an expired form is shown again.
- `--https-port` sets the HTTPS port the provider is served on, as in `--https-port 7100`. The port is written into the issuer in `OidcStore/InitialData.cs`, the launch profile and `requests.http`. When the option is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created.
- `--user` seeds a development user into the Identity store at the first run in Development, as in `--user alice@example.com`, and `demo@example.com` when it is omitted. The value is both the user's email address and login, so it has to be an email address made of the characters Identity accepts in a user name: Latin letters, digits and `-._@+`. Any other value stops the first run with Identity's error, such as `Email 'dev' is invalid.` for `--user dev`. The user gets the name John Doe, returned for the `profile` scope. With `--user ""`, the store starts with no user.
- `--client-secret` sets the secret of the demo clients `test_client` and `console_client`. When it is omitted, a random one is generated as the project is created, so every project has its own. It is written into `OidcStore/InitialData.cs` and `requests.http`, and sent as is in `requests.http`, so use letters and digits only.
- `--password` sets the password of that user, as in `--password "Str0ng-Passw0rd"`. It has to meet Identity's default password rules: at least six characters, with an uppercase letter, a lowercase letter, a digit and a symbol; a password that breaks them stops the first run with an error listing the rules it breaks. With `--user ""` it has no effect.

Without `--password`, the first run generates a password and prints it once on the console:

```
Seeded alice@example.com with a one-time generated password: ...
Copy it now, then press Enter to start the server...
```

On an interactive console the provider waits for Enter, so the password can be copied before the startup log scrolls it away. When input is redirected, as in a container or a CI job, it starts right away.

The user is created only in Development, and only when no user with that email address exists yet. It stays in `users.db` afterwards, so a `users.db` that ever ran in Development holds it in every environment. Identity keeps only a salted hash of the password, in `users.db`.

## Creating a project from an IDE

Once the package is installed, the template is available in the IDE's new-project dialog as "Abblix OpenID Provider (ASP.NET Identity + EF Core)". Its `ide.host.json` marks the host style, the HTTPS port, the sample user and its password as visible, which is the setting Visual Studio reads to show an option in that dialog (per the `ide.host.json` schema).

## What the project contains

```
MyProvider/
├── ClientAssets/                  npm project that bundles Bootstrap with Vite
├── Controllers/                   MVC only: AuthController.cs, the login page, and HomeController.cs
├── Models/                        MVC only
├── OidcStore/
│   ├── DatabaseKeysProvider.cs    signing keys stored in SQLite
│   ├── DurableClientStore.cs      dynamic client registrations stored in SQLite
│   ├── DurableTenantStore.cs      the tenants and the clients they declare, read from SQLite
│   ├── InitialData.cs             the rows a new oidc.db starts with: the tenant and the demo clients
│   ├── OidcStoreDbContext.cs
│   └── SigningKeyOpening.cs       creates a tenant's signing key before it is first served
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── Views/                         MVC only
├── wwwroot/
├── .gitignore                     keeps the databases, the key ring and the build output out of a commit
├── AppDbContext.cs                the Identity tables
├── DemoClient.cs                  the demo client's pages, served in Development
├── EndSessionPages.cs             the sign-out confirmation and signed-out pages
├── IdentityUserInfoProvider.cs    the user's claims for ID tokens and UserInfo
├── MinimalApiPages.cs             Minimal API only: the home, error and login pages
├── MyProvider.csproj
├── Pages.cs                       the frame of the pages rendered as plain HTML
├── Program.cs                     Identity, the stores and the pipeline
├── appsettings.json               connection strings, password hashing work factor
├── appsettings.Development.json   logging in Development
└── requests.http                  requests to send against the running provider
```

## Tenants and clients

The provider serves the tenants in `oidc.db`, each an issuer with the clients it declares. A new database starts with the rows of `OidcStore/InitialData.cs`: one tenant, `main`, whose issuer is `https://localhost:<port>`, with its login page and the `weather` API on `https://localhost:5004`, and two demo clients that share the secret the project was created with:

- `test_client`, for the authorization code flow with PKCE and refresh tokens. Its first redirect address is the demo client's page in this project, `https://localhost:<port>/demo/callback`; the second is on `https://localhost:5002`, for a client application of your own run there.
- `console_client`, for the client credentials grant only. Its tokens are addressed to the provider itself: their audience is the issuer.

`EnsureCreated` writes these rows only when it creates the database, so an edit to `InitialData.cs` reaches a database created after it and never one that exists. In an existing database, a tenant is a row of the `Tenants` table and a client it declares is a row of `TenantClients`, holding the client's settings as one JSON document in `Payload`. Add, change or delete those rows with any SQLite tool, such as `sqlite3 oidc.db`. The provider reads both tables again on a timer, every `OidcOptions.RefreshTenantsEvery`, so a change is served within that period, without a restart. A tenant added this way gets its signing key before it is first served.

The provider checks every tenant it reads, with its clients, before serving it. A tenant that fails a check, such as a client whose secret hash has the wrong length, is left out and logged as an error naming the tenant, the client and the reason, for example `The tenants main are left out of the tenants served, and requests to them are answered as to no tenant: Tenant 'main': Client 'console_client' has a Sha512Hash of 6 bytes, where 64 are expected.` The provider keeps running: requests to that tenant are answered 404 until the row is fixed, and the next reading serves it again. The other tenants are served meanwhile.

Clients that register themselves through Dynamic Client Registration, which the template does not switch on, are kept apart in the `Clients` table. A client a tenant declares is served ahead of a registration under the same id.

## Running the project

```shell
cd MyProvider
dotnet run
```

`dotnet run` uses the launch profile, so the provider runs in the Development environment at `https://localhost:<port>`, the port chosen at creation. The discovery document is at `https://localhost:<port>/.well-known/openid-configuration`. Trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`, so browsers and clients accept the address.

The first run creates `users.db` and `oidc.db` in the project folder with the `--user` account, the tenant and the demo clients, and generates the signing key. The signing key is encrypted with ASP.NET Core Data Protection, whose key ring is kept in the `dataprotection-keys` folder. Later runs reuse all of them.

To sign in, open `https://localhost:<port>/` in a browser and choose "Sign in with the demo client". The provider shows its login page; sign in with the `--user` email and its password. The browser lands on the demo client's page, which shows the authorization code, its PKCE verifier and the token request that redeems them, and a button that signs out through the end-session endpoint: the provider asks to confirm, then sends the browser back to the demo client.

With the provider running, open `requests.http` in Visual Studio, JetBrains Rider or VS Code with the REST Client extension and send its requests in turn: the discovery document, the published keys, an access token for `console_client` and the introspection of that token, then the exchange of a code from the demo client, the user's claims and a refresh.

## Before deploying

The build prints warnings (CS1030) for what a real deployment replaces: the local Data Protection key ring, the signing key kept in the database, the client store holding secret hashes, the demo clients and, when the sample user is created without `--password`, the printed password. Each warning names the fix. Besides them:

- Set the `Issuer` of the tenant to the provider's public address, the one clients validate its tokens against: in `InitialData.cs` before the production database is created, or in the `Tenants` table of one that exists.
- Set `AllowedHosts` in `appsettings.json` to the issuer's host name instead of `*`.
- Remove `test_client` and `console_client` from `DemoClients` in `InitialData.cs` before the production database is created, or delete their rows from `TenantClients` in one that exists. They redirect to `localhost`, and their secret is in the project's source.
- Delete the sample user from `users.db`, or start from a `users.db` that never ran in Development: its login is the template's default or the one given to `dotnet new`.
