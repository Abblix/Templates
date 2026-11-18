# abblix-react

`abblix-react` creates an ASP.NET Core host with a single-page application built with React, TypeScript and Vite. During development the host starts the Vite dev server and proxies to it, and a published build serves the compiled application from `wwwroot`.

The template lives in the [ReactTemplate](../ReactTemplate) folder. What changed between versions is in its [changelog](../ReactTemplate/.template.config/CHANGELOG.md).

## Requirements

- A .NET SDK that builds .NET 10, and the ASP.NET Core 10 runtime to run the host. The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) brings both; the .NET 11 SDK builds the project too when the ASP.NET Core 10 runtime is installed beside it.
- [Node.js](https://nodejs.org/) with npm. A Debug build installs the npm packages when `ClientApp/node_modules` is missing, `dotnet run` starts the Vite dev server with npm, and `dotnet publish` installs the packages and builds the application. A Release build alone does not run npm.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-react -n MyApp
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder, or `ReactApp` in an IDE.

A project with every option set:

```shell
dotnet new abblix-react -n MyApp --https-port 7100 --spa-proxy-port 5200
```

## Options

| Option | Default | Example | What it does |
| --- | --- | --- | --- |
| `--https-port` | a free port between 7000 and 7300 | `--https-port 7100` | The HTTPS port the ASP.NET Core host is served on, written into the launch profile. `0` picks a free port, as when the option is left out. |
| `--spa-proxy-port` | a free port between 5173 and 5273 | `--spa-proxy-port 5200` | The port of the Vite dev server, written into `SpaProxyServerUrl` in the project file and into `server.port` in `ClientApp/vite.config.ts`. `0` picks a free port, as when the option is left out. |

`dotnet new abblix-react -h` prints the options of the version you have installed, with their types and defaults.

## Creating a project from an IDE

After `dotnet new install Abblix.Templates`, Visual Studio lists the template in its New Project dialog as "ASP.NET Core MVC with React and Vite" and proposes `ReactApp` as the project name. In JetBrains Rider it appears under Custom Templates in the New Solution dialog.

The Visual Studio dialog offers none of the options above, so a project created there gets generated ports. To choose the ports, create the project from the command line and open it in the IDE.

## What the project contains

```
MyApp/
├── ClientApp/
│   ├── public/
│   ├── src/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── .oxlintrc.json             oxlint rules, with the React plugin enabled
│   ├── index.html
│   ├── package.json
│   ├── package-lock.json
│   ├── README.md                  the npm scripts and linting
│   ├── tsconfig.json
│   └── vite.config.ts             the dev server port and its HTTPS certificate
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── MyApp.csproj                   the SPA proxy and the npm steps of build and publish
├── Program.cs
├── README.md
├── appsettings.json
└── appsettings.Development.json
```

## Running the project

```shell
cd MyApp
dotnet run
```

`dotnet run` builds the host, installing the npm packages on the first run, and starts it at `https://localhost:<port>`, the port chosen at creation. The SPA proxy then starts the Vite dev server with `npm run dev`, and the browser opens on the host's address. Changes under `ClientApp/src` reload in the browser without restarting the host.

The Vite dev server serves HTTPS with the ASP.NET Core development certificate. On its first start it exports the certificate with `dotnet dev-certs` into `%APPDATA%\ASP.NET\https` on Windows or `~/.aspnet/https` elsewhere. Trust the certificate once with `dotnet dev-certs https --trust`, or the browser refuses both addresses.

In `ClientApp`, `npm run lint` runs oxlint and `npm run build` type-checks with `tsc` and builds the application into `ClientApp/dist`.

## Publishing

```shell
dotnet publish -c Release
```

Publishing installs the npm packages, builds the application and copies `ClientApp/dist` into the published `wwwroot`. The published host serves the application and answers any path it does not know with `index.html`.
