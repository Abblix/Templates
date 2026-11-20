<a name="top"></a>
[![Abblix React Template](https://resources.abblix.com/imgs/jpg/templates-github-banner-react.jpg)](https://github.com/Abblix/Templates)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](#)
[![language](https://img.shields.io/badge/language-C%23-239120)](https://learn.microsoft.com/ru-ru/dotnet/csharp/tour-of-csharp/overview)
[![OS](https://img.shields.io/badge/OS-linux%2C%20windows%2C%20macOS-0078D4)](#)
[![CPU](https://img.shields.io/badge/CPU-x86%2C%20x64%2C%20ARM%2C%20ARM64-FF8C00)](#)
[![GitHub release](https://img.shields.io/github/v/release/Abblix/Templates)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

⭐ Star us on GitHub, it motivates us a lot!

[![Share](https://img.shields.io/badge/share-000000?logo=x&logoColor=white)](https://x.com/intent/tweet?text=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates%20%23ASPNetCore%20%23Templates%20%23DotNet)
[![Share](https://img.shields.io/badge/share-1877F2?logo=facebook&logoColor=white)](https://www.facebook.com/sharer/sharer.php?u=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/sharing/share-offsite/?url=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-FF4500?logo=reddit&logoColor=white)](https://www.reddit.com/submit?title=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0088CC?logo=telegram&logoColor=white)](https://t.me/share/url?url=https://github.com/Abblix/Templates&text=Check%20out%20this%20project%20on%20GitHub)

## Table of Contents
- [About the Template](#-about-the-template)
- [Prerequisites](#-prerequisites)
- [Installing the Templates](#-installing-the-templates)
- [Creating a New Project](#%EF%B8%8F-creating-a-new-project)
- [Running the Project](#%EF%B8%8F-running-the-project)
- [Project Structure](#-project-structure)
- [License](#-license)
- [Acknowledgements](#-acknowledgements)
- [Key Contacts & Resources](#-key-contacts--resources)

## 🚀 About the Template

This template creates an ASP.NET Core host with a single-page application built with React, TypeScript and Vite. During development the host starts the Vite dev server and redirects the browser to it; a published build serves the compiled application from `wwwroot`.

### Features

- ASP.NET Core 11: the host, which serves the published application and falls back to `index.html` for client-side routes.
- React 19 and TypeScript 7: the application in `ClientApp`.
- Vite 8: the dev server with hot reload, and the production build.
- oxlint: linting with the React plugin enabled.

## 📋 Prerequisites

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0). The project targets .NET 11.
- [Node.js and npm](https://nodejs.org/): a Debug build and `dotnet publish` install the npm packages with `npm ci`, exactly as `ClientApp/package-lock.json` records them, whenever that lock is newer than the last install; `dotnet run` starts the Vite dev server with npm, and `dotnet publish` builds the application. A Release build does not need Node.js.

## 📝 Installing the Templates

To start using the Abblix ASP.NET Core Templates, you can install them in your development environment either from NuGet or from a local `.nupkg` file.

### From NuGet

If you have access to NuGet.org, this method is preferable. Run:

```bash
# Install the template package from NuGet
dotnet new install Abblix.Templates
```

This command downloads and installs the latest version of the templates directly from NuGet.org.

### From a Local `.nupkg` File

If you don't have access to NuGet.org, but you have a local `.nupkg` file, run:

```bash
# Install the template from the local .nupkg file
# Replace "path/to/Abblix.Templates.x.x.x.nupkg" with the actual file path
dotnet new install path/to/Abblix.Templates.x.x.x.nupkg
```

## 🛠️ Creating a New Project

Create a project with the short name `abblix-react`:

```shell
# The '-n MyNewProject' specifies the name of the new project and its folder
dotnet new abblix-react -n MyNewProject
```

The short names `react-dotnet`, `react-typescript` and `react-vite` create the same project.

`--https-port` sets the HTTPS port of the host and `--spa-proxy-port` the port of the Vite dev server. When either is omitted or 0, a port free on the machine is picked at random as the project is created. `dotnet new abblix-react -h` lists every option.

## ⚙️ Running the Project

```shell
# Move into the project directory you just created
cd MyNewProject

# Build the host, install the npm packages on the first run, and start the host and the Vite dev server
dotnet run
```

The host starts at `https://localhost:5000` and the browser opens there, then follows the redirect to the Vite dev server at `https://localhost:3000`, where changes under `ClientApp/src` reload without restarting the host. Both ports were chosen when the project was created: the ones given with `--https-port` and `--spa-proxy-port`, or free ports the template picked.

The application calls the host by relative paths. `App.tsx` fetches `/api/greeting`, an endpoint in `Program.cs`, and shows its message under the heading. When the page comes from the Vite dev server, the call goes to Vite, which forwards every path under `/api` to the host at `https://localhost:5000`. A published host answers it itself. To forward another prefix, add it to `server.proxy` in `ClientApp/vite.config.ts`.

The Vite dev server serves HTTPS with the ASP.NET Core development certificate. Trust it once with `dotnet dev-certs https --trust`. The host serves the same certificate, and the proxy checks the host against it.

In `ClientApp`, `npm run lint` runs oxlint and `npm run build` type-checks with `tsc` and builds the application into `ClientApp/dist`. `dotnet publish -c Release` builds the application and copies it into the published `wwwroot`.

## 📂 Project Structure

```
/MyNewProject
├── ClientApp/
│   ├── public/
│   ├── src/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── .oxlintrc.json
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── MyNewProject.csproj
└── Program.cs
```

- ClientApp/: the React application, with its npm scripts described in `ClientApp/README.md`.
- ClientApp/vite.config.ts: the Vite dev server's port, its HTTPS certificate, and the proxy that forwards `/api` to the host.
- Properties/launchSettings.json: the `https` launch profile on the host's port.
- MyNewProject.csproj: the SPA proxy settings and the npm steps of build and publish.
- Program.cs: the host, with the `/api/greeting` endpoint, serving static files and falling back to `index.html`.

## 📃 License

This project is licensed under the MIT License. You can review the full license text at the following link: [MIT License](https://opensource.org/licenses/MIT).

## 🙏 Acknowledgements

We would like to acknowledge the following technologies that have been instrumental in the development of the Abblix ASP.NET Core Templates:

- [ASP.NET Core](https://learn.microsoft.com/aspnet/core): A powerful framework for building modern web applications, providing the backbone for our server-side implementations.
- [React](https://react.dev/): A leading JavaScript library for creating dynamic user interfaces, which has been key to the frontend structure of our templates.
- [TypeScript](https://www.typescriptlang.org/): A superset of JavaScript that adds static types, helping us write more reliable and maintainable code.
- [Vite](https://vite.dev/): A fast and modern build tool that has significantly improved the development experience for our projects.

## 🔗 Key Contacts & Resources

For more details about our products, services, or any general information regarding the Abblix OIDC Server, feel free to reach out to us. We are here to provide support and answer any questions you may have. Below are the best ways to contact our team:

- [Email](mailto:support@abblix.com): Send us your inquiries or support requests via support@abblix.com.
- [Website](https://www.abblix.com/abblix-oidc-server): Visit the official page for more information.
- [GitHub Repository](https://github.com/Abblix/Oidc.Server): Explore the source code and contribute to the Abblix OIDC Server.
- [Getting Started Guide](https://github.com/Abblix/Oidc.Server.GettingStarted): Follow our step-by-step guide to create an OpenID Connect provider using ASP.NET MVC.
- [Abblix Documentation](https://docs.abblix.com/): Access detailed documentation for all our products and services.

We look forward to assisting you and ensuring your experience with our products is successful and enjoyable!

[Back to top](#top)
