# abblix-oidc-server-gettingstarted

`abblix-oidc-server-gettingstarted` creates a minimal OpenID Provider on ASP.NET Core and Abblix OIDC Server 3.0, as a Minimal API host or, with `--host mvc`, an MVC host, meant for learning how the server fits together. Users and clients are kept in memory and the signing key is generated at every startup, so nothing survives a restart. Comments in the code and build warnings point at what a real provider does instead. To start a real project, use [abblix-oidc-server](abblix-oidc-server.md).

The template lives in the [OpenIDProviderTemplate](../OpenIDProviderTemplate) folder. What changed between versions is in its [changelog](../OpenIDProviderTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.

The pages load Bootstrap from a CDN, and the MVC host's pages also load jQuery and jQuery Validation from CDNs, so the project builds with the .NET SDK alone.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder. The project has a user to sign in with on the login page, `demo@example.com`, whose password is generated at every start and printed on the console.

The same provider as an MVC host:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider --host mvc
```

A project with the main options set:

```shell
dotnet new abblix-oidc-server-gettingstarted -n MyProvider --https-port 7100 --user alice@example.com --password "Passw0rd!"
```

## Options

`dotnet new abblix-oidc-server-gettingstarted -h` lists every option of the installed version, with its type, default and description. The main ones:

- `--host` picks how the host serves the OpenID endpoints and its own pages. `minimal`, the default, maps them as Minimal API endpoints and references Abblix.Oidc.Server.MinimalApi; the login page and the other pages are rendered by `MinimalApiPages.cs`. `mvc` serves the endpoints as MVC controllers from Abblix.Oidc.Server.Mvc, and the pages as controllers with Razor views. Everything else is the same in both: the settings, the clients, the user store, the demo client and the sign-out pages. Both login pages check their antiforgery token in the handler, so a form submitted from another site is refused and an expired form is shown again.
- `--https-port` sets the HTTPS port the provider is served on, as in `--https-port 7100`. The port is written into the issuer in `Program.cs`, the launch profile and `requests.http`. When the option is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created.
- `--user` sets the login of the development user named Demo User in the in-memory store, as in `--user alice@example.com`, and `demo@example.com` when it is omitted. The value is the login typed on the login page: an email address or any other text on one line. The store holds the user only in Development, so a provider started in any other environment has none; with `--user ""`, it has none in Development either.
- `--password` sets the password of that user, as in `--password "Passw0rd!"`. It is written into `Program.cs` and compared in plain text. Without it, a password is generated at every start and printed once on the console.

## Creating a project from an IDE

Once the package is installed, the template is available in the IDE's new-project dialog as "Abblix OpenID Provider - Getting Started (in-memory)". Its `ide.host.json` marks the host style, the HTTPS port, the sample user and its password as visible, which is the setting Visual Studio reads to show an option in that dialog (per the `ide.host.json` schema).

## What the project contains

```
MyProvider/
├── Controllers/                   MVC only: AuthController.cs, the login page, and HomeController.cs
├── Models/                        MVC only
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── Views/                         MVC only
├── wwwroot/
├── .gitignore
├── DemoClient.cs                  the demo client's pages, served in Development
├── EndSessionPages.cs             the sign-out confirmation and signed-out pages
├── MinimalApiPages.cs             Minimal API only: the home, error and login pages
├── MyProvider.csproj
├── Pages.cs                       the frame of the pages rendered as plain HTML
├── Program.cs                     the issuer, the clients, the signing key and the --user account
├── README.md
├── TestUserStorage.cs             the in-memory user store
├── appsettings.json
├── appsettings.Development.json   the demo clients
└── requests.http                  requests to send against the running provider
```

`appsettings.Development.json` declares two demo clients under `Oidc:Clients`, both with the secret `secret`, which is published with this template:

- `test_client`, for the authorization code flow with PKCE and refresh tokens. Its redirect URI is the demo client's page in this project, `https://localhost:<port>/demo/callback`, and users sign out to `https://localhost:<port>/demo/signed-out`. A client application of your own adds its own addresses to `RedirectUris` and `PostLogoutRedirectUris`.
- `console_client`, for the client credentials grant only. Its tokens are addressed to the provider itself: their audience is the issuer.

`appsettings.json` declares no client, and `dotnet publish` leaves `appsettings.Development.json` out of the published app.

## Running the project

```shell
cd MyProvider
dotnet run
```

`dotnet run` uses the launch profile, so the provider runs in the Development environment at `https://localhost:<port>`, the port chosen at creation. The discovery document is at `https://localhost:<port>/.well-known/openid-configuration`. Trust the ASP.NET Core development certificate once with `dotnet dev-certs https --trust`, so browsers and clients accept the address.

With the provider running, open `requests.http` in Visual Studio, JetBrains Rider or VS Code with the REST Client extension and send its requests in turn: the discovery document, the published keys, an access token for `console_client` and the introspection of that token. They need no client application.

To sign in, open `https://localhost:<port>/` in a browser and choose "Sign in with the demo client". The provider shows its login page; sign in as `demo@example.com`, or the `--user` login, with the password the console printed at startup, or the `--password` given. The browser lands on the demo client's page, which shows the authorization code, its PKCE verifier and the token request that redeems them, and a button that signs out through the end-session endpoint: the provider asks to confirm, then sends the browser back to the demo client. `requests.http` then exchanges that code for tokens, reads the user's claims and refreshes the tokens.

Every start generates a new signing key and publishes it in place of the previous one.

## Before going further

The build prints warnings (CS1030) for each choice a real provider replaces: the in-memory user store, the plain-text passwords and their check without lockout, the signing key generated at startup and the in-memory cache. Each warning names the fix and links the matching guide. Before deploying, also set `Issuer` in `Program.cs` to the provider's public address and `AllowedHosts` in `appsettings.json` to its host name instead of `*`.
