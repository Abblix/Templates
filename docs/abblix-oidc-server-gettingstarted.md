# abblix-oidc-server-gettingstarted

`abblix-oidc-server-gettingstarted` creates a minimal OpenID Provider on ASP.NET Core MVC and Abblix OIDC Server 3.0, meant for learning how the server fits together. Users and clients are kept in memory and the signing key is generated at every startup, so nothing survives a restart. Comments in the code and build warnings point at what a real provider does instead. To start a real project, use [abblix-oidc-server](abblix-oidc-server.md).

The template lives in the [OpenIDProviderTemplate](../OpenIDProviderTemplate) folder. What changed between versions is in its [changelog](../OpenIDProviderTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.

Node.js is not needed: the pages load Bootstrap and jQuery from a CDN.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder, or `OpenIDProvider` in an IDE.

A project with every option set:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider --https-port 7100 --user alice@example.com --password "Passw0rd!"
```

## Options

| Option | Default | Example | What it does |
| --- | --- | --- | --- |
| `--https-port` | a free port between 7000 and 7300 | `--https-port 7100` | The HTTPS port the provider is served on. It is also written into the issuer in `Program.cs`, into the launch profile and into `requests.http`. `0` picks a free port, as when the option is left out. |
| `--user` | none, so the user store starts empty | `--user alice@example.com` | Adds a development user to the in-memory store, with the name "Demo User". The value is the user's email address and login. The login page asks for an email address, so a value such as `dev` is created but cannot be typed into the page. The value is written into `Program.cs` as a C# string, so it must not contain `"` or `\`. |
| `--password` | `Passw0rd!` | `--password "Passw0rd!"` | The password of the `--user` account. It is written into `Program.cs` and compared in plain text. Like `--user`, it must not contain `"` or `\`. Ignored without `--user`. |

`dotnet new abblix-oidc-server-gettingstarted -h` prints the options of the version you have installed, with their types and defaults.

## Creating a project from an IDE

After `dotnet new install Abblix.Templates`, Visual Studio lists the template in its New Project dialog as "Abblix OpenID Provider - Getting Started (in-memory)" and proposes `OpenIDProvider` as the project name. In JetBrains Rider it appears under Custom Templates in the New Solution dialog.

The Visual Studio dialog offers none of the options above. A project created there gets a generated port and no user. To set the options, create the project from the command line and open it in the IDE.

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

`appsettings.Development.json` declares one demo client under `Oidc:Clients`: `console_client`, for the client credentials grant only, with the secret `secret`, which is published with this template. Its tokens are addressed to the provider itself, so no API registered with the provider accepts them. `appsettings.json` declares no client, and `dotnet publish` does not copy `appsettings.Development.json`.

No client signs users in. To sign in as the `--user` account, add a client for the authorization code flow to `Oidc:Clients`, with the redirect address of your client application.

## Running the project

```shell
cd MyProvider
dotnet run
```

`dotnet run` uses the launch profile, so the provider runs in the Development environment at `https://localhost:<port>`, the port chosen at creation. The discovery document is at `https://localhost:<port>/.well-known/openid-configuration`. Trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`, or browsers and clients refuse the address.

With the provider running, open `requests.http` in Visual Studio, JetBrains Rider or VS Code with the REST Client extension and send its requests in turn: the discovery document, the published keys, an access token for `console_client` and the introspection of that token. They need no client application.

Every start generates a new signing key, so tokens issued before a restart no longer validate.

## Before going further

The build prints warnings (CS1030) for each choice a real provider replaces: the in-memory user store, the plain-text passwords and their check without lockout, the signing key generated at startup and the in-memory cache. Each warning names the fix and links the matching guide. Before deploying, also set `Issuer` in `Program.cs` to the provider's public address and `AllowedHosts` in `appsettings.json` to its host name instead of `*`.
