# abblix-oidc-server

`abblix-oidc-server` creates an OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 3.0. Users are kept by ASP.NET Core Identity on EF Core with SQLite, and client registrations and the signing key are stored in SQLite as well, so a restart keeps them. Start a real project from this template. For a smaller provider that keeps everything in memory and is meant for learning, see [abblix-oidc-server-gettingstarted](abblix-oidc-server-gettingstarted.md).

The template lives in the [OpenIDProviderIdentityTemplate](../OpenIDProviderIdentityTemplate) folder. What changed between versions is in its [changelog](../OpenIDProviderIdentityTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.
- [Node.js](https://nodejs.org/) with npm. The build bundles Bootstrap into `wwwroot/lib` with Vite and installs the npm packages first when `ClientAssets/node_modules` is missing, so the first build of a new project runs both. Without Node.js the build stops with an error that says so.

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

The short names `oidc-server`, `oauth2-server` and `openidconnect` create the same project.

## Options

`dotnet new abblix-oidc-server -h` lists every option of the installed version, with its type, default and description. The main ones:

- `--https-port` sets the HTTPS port the provider is served on, as in `--https-port 7100`. The port is written into the issuer in `Program.cs`, the launch profile and `requests.http`. When the option is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created.
- `--user` seeds a development user into the Identity store at first run, as in `--user alice@example.com`. The value is both the user's email address and login, so it has to be an email address made of the characters Identity accepts in a user name: letters, digits and `-._@+`. Any other value stops the first run with Identity's error, such as `Email 'dev' is invalid.` for `--user dev`. The user gets the name John Doe, returned for the `profile` scope. Without `--user`, the store starts with no user.
- `--password` sets the password of that user, as in `--password "Str0ng-Passw0rd"`. It has to meet Identity's default password rules: at least six characters, with an uppercase letter, a lowercase letter, a digit and a symbol; a password that breaks them stops the first run with an error listing the rules it breaks. Without `--user` it has no effect.

Without `--password`, the first run generates a password and prints it once on the console:

```
Seeded alice@example.com with a one-time generated password: ...
Copy it now, then press Enter to start the server...
```

On an interactive console the provider waits for Enter, so the password can be copied before the startup log scrolls it away. When input is redirected, as in a container or a CI job, it starts right away.

The user is created only when no user with that email address exists yet. Identity keeps only a salted hash of the password, in `users.db`.

## Creating a project from an IDE

Once the package is installed, the template is available in the IDE's new-project dialog as "Abblix OpenID Provider (ASP.NET Identity + EF Core)". Its `ide.host.json` marks the HTTPS port, the sample user and its password as visible, which is the setting Visual Studio reads to show an option in that dialog (per the `ide.host.json` schema).

## What the project contains

```
MyProvider/
├── ClientAssets/                  npm project that bundles Bootstrap with Vite
├── Controllers/
│   ├── AuthController.cs          the login page; checks the password with Identity
│   └── HomeController.cs
├── Models/
├── OidcStore/
│   ├── DatabaseKeysProvider.cs    signing keys stored in SQLite
│   ├── DurableClientStore.cs      client registrations stored in SQLite
│   └── OidcStoreDbContext.cs
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── Views/
├── wwwroot/
├── AppDbContext.cs                the Identity tables
├── IdentityUserInfoProvider.cs    the user's claims for ID tokens and UserInfo
├── MyProvider.csproj
├── Program.cs                     the issuer, Identity, the stores and the seeding
├── appsettings.json               connection strings, password hashing work factor
├── appsettings.Development.json   the demo clients
└── requests.http                  requests to send against the running provider
```

`appsettings.Development.json` declares two demo clients under `Oidc:Clients`, both with the secret `secret`, which is published with this template:

- `test_client`, for the authorization code flow with PKCE. Its redirect addresses are on `https://localhost:5002`, so a client application you run there can sign users in through the provider.
- `console_client`, for the client credentials grant only. Its tokens are addressed to the provider itself: their audience is the issuer.

`appsettings.json` declares no client, and `dotnet publish` leaves `appsettings.Development.json` out of the published app.

## Running the project

```shell
cd MyProvider
dotnet run
```

`dotnet run` uses the launch profile, so the provider runs in the Development environment at `https://localhost:<port>`, the port chosen at creation. The discovery document is at `https://localhost:<port>/.well-known/openid-configuration`. Trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`, so browsers and clients accept the address.

The first run creates `users.db` and `oidc.db` in the project folder, generates the signing key and seeds the demo clients and the `--user` account. The signing key is encrypted with ASP.NET Core Data Protection, whose key ring is kept in the `dataprotection-keys` folder. Later runs reuse all of them.

With the provider running, open `requests.http` in Visual Studio, JetBrains Rider or VS Code with the REST Client extension and send its requests in turn: the discovery document, the published keys, an access token for `console_client` and the introspection of that token. They need no client application.

A client seeded into `oidc.db` keeps its stored row on later runs. When its entry in `Oidc:Clients` changes, the provider logs a warning naming the client; delete its row from the `Clients` table to seed it again from the configuration.

## Before deploying

The build prints warnings (CS1030) for what a real deployment replaces: the local Data Protection key ring, the signing key kept in the database, the client store holding secret hashes, the demo clients and, when `--user` is given without `--password`, the printed password. Each warning names the fix. Besides them:

- Set `Issuer` in `Program.cs` to the provider's public address, the one clients validate its tokens against.
- Set `AllowedHosts` in `appsettings.json` to the issuer's host name instead of `*`.
- Delete the rows of `test_client` and `console_client` from `oidc.db`, or start from a database that never ran in Development.
