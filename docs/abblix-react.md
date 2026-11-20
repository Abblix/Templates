# abblix-react

`abblix-react` creates an ASP.NET Core host with a single-page application built with React, TypeScript and Vite. During development the host starts the Vite dev server and redirects the browser to it, and a published build serves the compiled application from `wwwroot`.

The template lives in the [ReactTemplate](../ReactTemplate) folder. What changed between versions is in its [changelog](../ReactTemplate/.template.config/CHANGELOG.md).

## Requirements

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.
- [Node.js](https://nodejs.org/) with npm. A Debug build and `dotnet publish` install the npm packages with `npm ci`, exactly as `ClientApp/package-lock.json` records them, whenever that lock is newer than the last install; `dotnet run` starts the Vite dev server with npm, and `dotnet publish` builds the application. A Release build does not need Node.js.

## Creating a project from the command line

Install the template package once:

```shell
dotnet new install Abblix.Templates
```

Then create a project:

```shell
dotnet new abblix-react -n MyApp
```

`-n` names the project and its folder. Without it, the project takes the name of the current folder.

A project with the main options set:

```shell
dotnet new abblix-react -n MyApp --https-port 7100 --spa-proxy-port 5200
```

The short names `react-net10`, `react-typescript` and `react-vite` create the same project.

## Options

`dotnet new abblix-react -h` lists every option of the installed version, with its type, default and description. The main ones:

- `--https-port` sets the HTTPS port the ASP.NET Core host is served on, as in `--https-port 7100`, and writes it into the launch profile and into the proxy target in `ClientApp/vite.config.ts`. When the option is omitted or 0, a port free on the machine is picked at random between 7000 and 7300 as the project is created.
- `--spa-proxy-port` sets the port of the Vite dev server, as in `--spa-proxy-port 5200`, and writes it into `SpaProxyServerUrl` in the project file and into `server.port` in `ClientApp/vite.config.ts`. When the option is omitted or 0, a port free on the machine is picked at random between 5173 and 5273 as the project is created.

## Creating a project from an IDE

Once the package is installed, the template is available in the IDE's new-project dialog as "ASP.NET Core MVC with React and Vite". Its `ide.host.json` marks both ports as visible, which is the setting Visual Studio reads to show an option in that dialog (per the `ide.host.json` schema).

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
│   └── vite.config.ts             the dev server port, its HTTPS certificate, the /api proxy
├── Properties/launchSettings.json the https launch profile, on the chosen port
├── MyApp.csproj                   the SPA proxy and the npm steps of build and publish
├── Program.cs                     the host, with the /api/greeting endpoint
├── README.md
├── appsettings.json
└── appsettings.Development.json
```

## Running the project

```shell
cd MyApp
dotnet run
```

`dotnet run` builds the host, installing the npm packages on the first run, and starts it at `https://localhost:<port>`, the port chosen at creation. The SPA proxy then starts the Vite dev server with `npm run dev`, and the browser opens on the host's address, which redirects it to the Vite dev server at `https://localhost:<spa-proxy-port>`. There, changes under `ClientApp/src` reload without restarting the host.

The Vite dev server serves HTTPS with the ASP.NET Core development certificate. On every start it exports the certificate with `dotnet dev-certs` into `%APPDATA%\ASP.NET\https` on Windows or `~/.aspnet/https` elsewhere. Trust the certificate once with `dotnet dev-certs https --trust`, so the browser accepts both addresses.

The application calls the host by relative paths: `App.tsx` fetches `/api/greeting`, an endpoint in `Program.cs`, and shows its message. When the page is loaded from the Vite address, the call goes to the dev server, and `server.proxy` in `ClientApp/vite.config.ts` forwards every path under `/api` to the host at `https://localhost:<port>`. The proxy checks the host's certificate against the exported development certificate, the one the host serves. A published host answers the same paths itself. A new endpoint under `/api` needs no change to the proxy; another prefix is added to `server.proxy`.

In `ClientApp`, `npm run lint` runs oxlint and `npm run build` type-checks with `tsc` and builds the application into `ClientApp/dist`.

## Publishing

```shell
dotnet publish -c Release
```

Publishing installs the npm packages, builds the application and copies `ClientApp/dist` into the published `wwwroot`. The published host serves the application and answers a path without a file extension that matches nothing else with `index.html`, so a client-side route survives a reload.
