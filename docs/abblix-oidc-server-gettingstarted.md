# abblix-oidc-server-gettingstarted

`abblix-oidc-server-gettingstarted` creates a minimal OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 3.0, meant for learning how the server fits together. Users and clients are kept in memory and the signing key is generated at every startup, so nothing survives a restart. Comments in the code and build warnings point at what a real provider does instead. To start a real project, use [abblix-oidc-server](abblix-oidc-server.md).

The template lives in the [OpenIDProviderTemplate](../OpenIDProviderTemplate) folder. What changed between versions is in its [changelog](../OpenIDProviderTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.

The pages load Bootstrap and jQuery from a CDN, so the project builds with the .NET SDK alone.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder.

A project with the main options set:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider --https-port 7100 --user alice@example.com --password "Passw0rd!"
```

## Options

`dotnet new abblix-oidc-server-gettingstarted -h` lists every option of the installed version, with its type, default and description. The main ones:

- `--https-port` sets the HTTPS port the provider is served on, as in `--https-port 7100`. The port is written into the issuer in `Program.cs`, the launch profile and `requests.http`. When the option is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created.
- `--user` adds a development user named Demo User to the in-memory store, as in `--user alice@example.com`. The value is the login typed on the login page: an email address or any other text on one line. Without `--user`, the store starts with no user.
- `--password` sets the password of that user, as in `--password "Passw0rd!"`, which is also its default. It is written into `Program.cs` and compared in plain text. Without `--user` it has no effect.

## Creating a project from an IDE

Once the package is installed, the template is available in the IDE's new-project dialog as "Abblix OpenID Provider - Getting Started (in-memory)". Its `ide.host.json` marks the HTTPS port, the sample user and its password as visible, which is the setting Visual Studio reads to show an option in that dialog (per the `ide.host.json` schema).

## What the project contains

```
MyProvider/
├── Controllers/
│   ├── AuthController.cs          the login page
│   └── HomeController.cs
├── Models/
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── Views/
├── wwwroot/
├── MyProvider.csproj
├── Program.cs                     the issuer, the clients, the signing key and the --user account
├── README.md
├── TestUserStorage.cs             the in-memory user store
├── appsettings.json
├── appsettings.Development.json   the demo client
└── requests.http                  requests to send against the running provider
```

`appsettings.Development.json` declares one demo client under `Oidc:Clients`: `console_client`, for the client credentials grant only, with the secret `secret`, which is published with this template. Its tokens are addressed to the provider itself: their audience is the issuer. `appsettings.json` declares no client, and `dotnet publish` leaves `appsettings.Development.json` out of the published app.

## Running the project

```shell
cd MyProvider
dotnet run
```

`dotnet run` uses the launch profile, so the provider runs in the Development environment at `https://localhost:<port>`, the port chosen at creation. The discovery document is at `https://localhost:<port>/.well-known/openid-configuration`. Trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`, so browsers and clients accept the address.

With the provider running, open `requests.http` in Visual Studio, JetBrains Rider or VS Code with the REST Client extension and send its requests in turn: the discovery document, the published keys, an access token for `console_client` and the introspection of that token. They need no client application.

Every start generates a new signing key and publishes it in place of the previous one.

## Before going further

The build prints warnings (CS1030) for each choice a real provider replaces: the in-memory user store, the plain-text passwords and their check without lockout, the signing key generated at startup and the in-memory cache. Each warning names the fix and links the matching guide. Before deploying, also set `Issuer` in `Program.cs` to the provider's public address and `AllowedHosts` in `appsettings.json` to its host name instead of `*`.
