<a name="top"></a>
[![Abblix Templates](https://resources.abblix.com/imgs/jpg/templates-github-banner.jpg)](https://github.com/Abblix/Templates)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](#)
[![language](https://img.shields.io/badge/language-C%23-239120)](https://learn.microsoft.com/ru-ru/dotnet/csharp/tour-of-csharp/overview)
[![OS](https://img.shields.io/badge/OS-linux%2C%20windows%2C%20macOS-0078D4)](#)
[![CPU](https://img.shields.io/badge/CPU-x86%2C%20x64%2C%20ARM%2C%20ARM64-FF8C00)](#)
[![GitHub release](https://img.shields.io/github/v/release/Abblix/Templates)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

⭐ Star us on GitHub - it motivates us a lot!

[![Share](https://img.shields.io/badge/share-000000?logo=x&logoColor=white)](https://x.com/intent/tweet?text=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates%20%23ASPNetCore%20%23Templates%20%23DotNet)
[![Share](https://img.shields.io/badge/share-1877F2?logo=facebook&logoColor=white)](https://www.facebook.com/sharer/sharer.php?u=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/sharing/share-offsite/?url=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-FF4500?logo=reddit&logoColor=white)](https://www.reddit.com/submit?title=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0088CC?logo=telegram&logoColor=white)](https://t.me/share/url?url=https://github.com/Abblix/Templates&text=Check%20out%20this%20project%20on%20GitHub)

## Table of Contents
- [About the Templates](#-about-the-templates)
- [Template Pages](#-template-pages)
- [Changelogs](#-changelogs)
- [Prerequisites](#-prerequisites)
- [Installing the Templates](#-installing-the-templates)
- [Adding the Server to an Existing Application](#-adding-the-server-to-an-existing-application)
- [License](#-license)
- [Acknowledgements](#-acknowledgements)
- [Key Contacts & Resources](#-key-contacts--resources)

## 🚀 About the Templates

Abblix ASP.NET Core Templates includes three templates designed to simplify and accelerate your development process. These templates provide a solid foundation for building modern web applications and identity providers with ASP.NET Core.

- [ASP.NET Core MVC with React and Vite](https://github.com/Abblix/Templates/tree/master/ReactTemplate) (`abblix-react`):
An ASP.NET Core MVC host with a React single-page app written in TypeScript and built by Vite. In development the host starts the Vite dev server, which forwards `/api` calls to the host; a published app serves the built pages from `wwwroot`.
- OpenID Connect Provider Templates:
Two templates for building an OpenID Connect Provider using ASP.NET Core in conjunction with Abblix OIDC Server 3.0. Each generates a Minimal API host by default and an MVC host with `--host mvc`; both styles share the settings and clients, and a new project of either signs a user in through its own demo client in Development. Both serve the OpenID endpoints at whatever host name a request arrives with and build the endpoint addresses in the discovery document from it, so before deploying set `AllowedHosts` in `appsettings.json` to the issuer's host name instead of the `*` they ship with.
  - `abblix-oidc-server` ([OpenIDProviderIdentityTemplate](https://github.com/Abblix/Templates/tree/master/OpenIDProviderIdentityTemplate)): the starting point for a real project. Users are kept by ASP.NET Core Identity on EF Core with SQLite, and the signing key and client registrations are stored in SQLite as well, so a restart keeps them.
  - `abblix-oidc-server-gettingstarted` ([OpenIDProviderTemplate](https://github.com/Abblix/Templates/tree/master/OpenIDProviderTemplate)): a minimal provider for learning, with an in-memory user store, clients declared in `appsettings.Development.json` under `Oidc:Clients` and a signing key generated at every startup. Its build warnings mark the in-memory user store, the generated signing key and the in-memory cache as choices to replace before production.

## 📖 Template Pages

Each template has a page with the `dotnet new` command and its main options, creating the project from an IDE, what the created project contains and how to run it:

- `abblix-oidc-server`: [OpenID Provider on ASP.NET Core Identity](https://github.com/Abblix/Templates/blob/master/docs/abblix-oidc-server.md)
- `abblix-oidc-server-gettingstarted`: [OpenID Provider for learning, in memory](https://github.com/Abblix/Templates/blob/master/docs/abblix-oidc-server-gettingstarted.md)
- `abblix-react`: [ASP.NET Core with React and Vite](https://github.com/Abblix/Templates/blob/master/docs/abblix-react.md)

## 📜 Changelogs

Each template keeps a changelog of its own, so anyone who created a project from an earlier version can see what has changed in the template since then:

- `abblix-oidc-server`: [OpenIDProviderIdentityTemplate changelog](https://github.com/Abblix/Templates/blob/master/OpenIDProviderIdentityTemplate/.template.config/CHANGELOG.md)
- `abblix-oidc-server-gettingstarted`: [OpenIDProviderTemplate changelog](https://github.com/Abblix/Templates/blob/master/OpenIDProviderTemplate/.template.config/CHANGELOG.md)
- `abblix-react`: [ReactTemplate changelog](https://github.com/Abblix/Templates/blob/master/ReactTemplate/.template.config/CHANGELOG.md)

## 📋 Prerequisites

Before you begin using the Abblix ASP.NET Core Templates, ensure that your development environment is properly set up with the following tools:

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0): This SDK is essential for building and running ASP.NET Core applications. It includes the necessary compilers, libraries, and tools to develop, test, and deploy your applications. All three templates target .NET 11.
- [Node.js and npm](https://nodejs.org/): Node.js is required to manage the frontend dependencies for projects using React and Vite. npm (Node Package Manager) comes bundled with Node.js and is used to install and manage these dependencies. Having Node.js and npm installed ensures that you can seamlessly integrate and build the frontend parts of your application, especially when using modern JavaScript frameworks and tools like React and Vite.

## 📝 Installing the Templates

To start using the Abblix ASP.NET Core Templates, you can install them in your development environment from NuGet, from the Visual Studio Marketplace, or from a local `.nupkg` file.

### From NuGet

If you have access to NuGet.org, this method is preferable. Run:

```bash
# Install the template package from NuGet
dotnet new install Abblix.Templates
```

This command downloads and installs the latest version of the templates directly from NuGet.org.

### From the Visual Studio Marketplace

In Visual Studio 2022 or later, open Extensions > Manage Extensions, search for [Abblix Templates](https://marketplace.visualstudio.com/items?itemName=Abblix.Templates) and install it. After Visual Studio restarts, the three templates appear in the New Project dialog. The extension carries the same templates as the NuGet package of the same version.

### From a Local `.nupkg` File

If you don't have access to NuGet.org, but you have a local `.nupkg` file, run:

```bash
# Install the template from the local .nupkg file
# Replace "path/to/Abblix.Templates.x.x.x.nupkg" with the actual file path
# This allows you to install the template locally without accessing an online repository
dotnet new install path/to/Abblix.Templates.x.x.x.nupkg
```

This method allows you to install the template manually using the `.nupkg` file.

## 🧩 Adding the Server to an Existing Application

The templates start a new project. To turn an application you already have into an OpenID Connect provider, this repository carries an agent skill, [add-abblix-oidc-server](skills/add-abblix-oidc-server/SKILL.md), which a coding agent follows to take the same steps the OpenID Provider templates take: it adds the package, registers the server with one tenant, chooses how consent is given, maps the endpoints for MVC or Minimal API, hands the user from a login page back to the server, declares a first client, and loads a signing key that stays the same across restarts.

The skill is in the common `SKILL.md` format. Copy the `skills/add-abblix-oidc-server` folder into the skills directory your agent reads (for Claude Code, `.claude/skills/` in the project or in your home directory), then ask the agent to add Abblix OIDC Server to the application. The skill is versioned with the templates in this repository and is not part of the `Abblix.Templates` package, so `dotnet new install` neither installs it nor lists it.

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
