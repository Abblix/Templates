<a name="top"></a>
[![Abblix OpenID Provider Template](https://resources.abblix.com/imgs/jpg/templates-github-banner-openidprovider.jpg)](https://github.com/Abblix/Templates)
[![.NET](https://img.shields.io/badge/.NET-6.0%2C%207.0%2C%208.0-512BD4)](#)
[![language](https://img.shields.io/badge/language-C%23-239120)](https://learn.microsoft.com/ru-ru/dotnet/csharp/tour-of-csharp/overview)
[![OS](https://img.shields.io/badge/OS-linux%2C%20windows%2C%20macOS-0078D4)](#)
[![CPU](https://img.shields.io/badge/CPU-x86%2C%20x64%2C%20ARM%2C%20ARM64-FF8C00)](#)
[![GitHub release](https://img.shields.io/github/v/release/Abblix/Templates)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

⭐ Star us on GitHub — it motivates us a lot!

[![Share](https://img.shields.io/badge/share-000000?logo=x&logoColor=white)](https://x.com/intent/tweet?text=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates%20%23ASPNetCore%20%23Templates%20%23DotNet)
[![Share](https://img.shields.io/badge/share-1877F2?logo=facebook&logoColor=white)](https://www.facebook.com/sharer/sharer.php?u=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/sharing/share-offsite/?url=https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-FF4500?logo=reddit&logoColor=white)](https://www.reddit.com/submit?title=Check%20out%20this%20project%20on%20GitHub:%20https://github.com/Abblix/Templates)
[![Share](https://img.shields.io/badge/share-0088CC?logo=telegram&logoColor=white)](https://t.me/share/url?url=https://github.com/Abblix/Templates&text=Check%20out%20this%20project%20on%20GitHub)

## Table of Contents
- [About the Template](#-about-the-template)
- [About Abblix OIDC Server](#%EF%B8%8F-about-abblix-oidc-server)
- [Prerequisites](#-prerequisites)
- [Installing the Templates](#-installing-the-templates)
- [Creating a New Project](#%EF%B8%8F-creating-a-new-project)
- [Project Structure](#-project-structure)
- [Configuration](#-configuration)
- [Running the Project](#%EF%B8%8F-running-the-project) 
- [Customization](#-customization)
- [License](#-license)
- [Acknowledgements](#-acknowledgements)
- [Key Contacts & Resources](#-key-contacts--resources)

## 🚀 About the Template

This template provides a starting point for building an OpenID Connect Provider using ASP.NET Core and the Abblix OIDC Server. It includes the basic configuration and components necessary to set up an identity provider.

### Features

- **ASP.NET Core**: A cross-platform framework for building modern, cloud-based web applications.
- **OpenID Connect**: A simple identity layer on top of the OAuth 2.0 protocol.
- **Abblix OIDC Server**: A powerful .NET library that implements the OpenID Connect protocol with a focus on flexibility, reusability, and reliability.
- **JWT Tokens**: Secure tokens for API authentication.
- **User Management**: Basic user management and storage for test purposes only.

## 🛡️ About Abblix OIDC Server

**Abblix OIDC Server** is a .NET library designed to provide comprehensive support for OAuth2 and OpenID Connect on the server side. It adheres to high standards of flexibility, reusability, and reliability, utilizing well-known software design patterns, including modular and hexagonal architectures. These patterns ensure the following benefits:

- **Modularity**: Different parts of the library can function independently, enhancing the library's modularity and allowing for easier maintenance and updates.
- **Testability**: Improved separation of concerns makes the code more testable.
- **Maintainability**: Clear structure and separation facilitate better management of the codebase.

The library also supports Dependency Injection through the standard .NET DI container, aiding in the organization and management of code. Specifically tailored for seamless integration with ASP.NET WebApi, Abblix OIDC Server employs standard controller classes, binding, and routing mechanisms, simplifying the integration of OpenID Connect into your services.

## 📋 Prerequisites

Before you begin using the Abblix ASP.NET Core Templates, ensure that your development environment is properly set up with the following tools:

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**: This SDK is essential for building and running ASP.NET Core applications. It includes the necessary compilers, libraries, and tools to develop, test, and deploy your applications. Make sure to download and install the latest version to fully leverage the capabilities of the templates.
- **[Node.js and npm](https://nodejs.org/)**: Node.js is required to manage the frontend dependencies for projects using React and Vite. npm (Node Package Manager) comes bundled with Node.js and is used to install and manage these dependencies. Having Node.js and npm installed ensures that you can seamlessly integrate and build the frontend parts of your application, especially when using modern JavaScript frameworks and tools like React and Vite.

## 📝 Installing the Templates

First, you need to install the template from NuGet, or you can use the local `.nupkg` file.

### Installing from NuGet

```shell
# Install the template package from NuGet
dotnet new install Abblix.Templates
```

### Installing from Local `.nupkg` File

```shell
# Install the template from the local .nupkg file
# Replace "path/to/Abblix.Templates.x.x.x.nupkg" with the actual file path
# and update "x.x.x" where x.x.x is the actual number of the version
dotnet new install path/to/Abblix.Templates.x.x.x.nupkg
```

## 🛠️ Creating a New Project

You can create a new project using this template with any of the specified short names or aliases.

### Using the Primary Short Name

```shell
# Creates a new project using the primary short name 'abblix-oidc-server'
# The '-n MyOpenIDProvider' specifies the name of the new project folder
dotnet new abblix-oidc-server -n MyOpenIDProvider
```

### Using Alternative Short Names

```shell
# Creates a new project using the 'oidc-server' short name
dotnet new oidc-server -n MyOpenIDProvider

# Creates a new project using the 'oauth2-server' short name
dotnet new oauth2-server -n MyOpenIDProvider

# Creates a new project using the 'openidconnect' short name
dotnet new openidconnect -n MyOpenIDProvider
```

## 📂 Project Structure

Understanding the structure of your project is crucial for effective development. Below is an overview of the directory layout generated by the template, highlighting key folders and files. This structure organizes your backend components, ensuring a clean separation of concerns and ease of navigation.

```
/MyOpenIDProvider
├── Controllers/
│   └── ...
├── Models/
│   └── ...
├── Views/
│   └── ...
├── wwwroot/
│   └── ...
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── MyOpenIDProvider.csproj
├── Program.cs
└── TestUserStorage.cs
```

- **Controllers/**: Contains API controllers.
- **Models/**: Contains the data models and classes used throughout the application.
- **Views/**: Razor views for rendering HTML responses and UI components.
- **wwwroot/**: Contains static files like JavaScript, CSS, and images that are served directly by the web server.
- **Properties/**: Contains project settings files, including `launchSettings.json`.
- **appsettings.json**: Configuration settings for the application.
- **appsettings.Development.json**: Development-specific configuration settings.
- **MyOpenIDProvider.csproj**: Project file for the .NET project.
- **Program.cs**: Entry point for the .NET application.
- **TestUserStorage.cs**: In-memory storage for user credentials, useful for testing and development purposes.

## 🔧 Configuration

The template uses `appsettings.json` and `appsettings.Development.json` for configuration. You can customize settings such as the issuer name, token expiration, and user credentials.

Configuring and connecting clients to your Abblix OIDC Server is a crucial step in setting up your identity provider. This process involves defining client credentials, configuring allowed grant types, setting up redirect URIs, and more. Each client must be properly configured to securely interact with your OpenID Connect server, ensuring that only authorized applications can request tokens and access user data.

For a comprehensive guide on how to configure clients, including examples and best practices, please refer to the detailed documentation: [Configuring Clients](https://docs.abblix.com/docs/getting-started-guide#configuring-openidproviderapp).

## ⚙️ Running the Project

Once you've created your new project, you're ready to run it and see everything in action. Follow these steps to navigate through the project setup, restore dependencies, and launch both the .NET backend and the React frontend together.

```shell
# Move into the project directory you just created
cd MyOpenIDProvider

# Restore all the necessary NuGet packages for the .NET project
dotnet restore

# Build the project to ensure everything is set up correctly
dotnet build

# Start the .NET application
dotnet run
```

You can access the application in your browser at `http://localhost:5001`.

## 🎨 Customization

You can extend or modify the template by:

- Adding new controllers and views.
- Integrating with a real user store or identity provider.
- Configuring additional authentication methods.

## 📃 License

This project is licensed under the MIT License. You can review the full license text at the following link: [MIT License](https://opensource.org/licenses/MIT).

## 🙏 Acknowledgements

We would like to acknowledge the following technologies that have been instrumental in the development of the Abblix ASP.NET Core Templates:

- **[ASP.NET Core](https://docs.microsoft.com/aspnet/core)**: A powerful framework for building modern web applications, providing the backbone for our server-side implementations.
- **[React](https://reactjs.org/)**: A leading JavaScript library for creating dynamic user interfaces, which has been key to the frontend structure of our templates.
- **[TypeScript](https://www.typescriptlang.org/)**: A superset of JavaScript that adds static types, helping us write more reliable and maintainable code.
- **[Vite](https://vitejs.dev/)**: A fast and modern build tool that has significantly improved the development experience for our projects.

## 🔗 Key Contacts & Resources

For more details about our products, services, or any general information regarding the Abblix OIDC Server, feel free to reach out to us. We are here to provide support and answer any questions you may have. Below are the best ways to contact our team:

- **[Email](mailto:support@abblix.com)**: Send us your inquiries or support requests at support@abblix.com.
- **[Website](https://www.abblix.com/abblix-oidc-server)**: Visit the official page for more information.
- **[GitHub Repository](https://github.com/Abblix/Oidc.Server)**: Explore the source code and contribute to the Abblix OIDC Server.
- **[Getting Started Guide](https://github.com/Abblix/Oidc.Server.GettingStarted)**: Follow our step-by-step guide to create an OpenID Connect provider using ASP.NET MVC.
- **[Abblix Documentation](https://docs.abblix.com/docs)**: Access detailed documentation for all our products and services.

We look forward to assisting you and ensuring your experience with our products is successful and enjoyable!

[Back to top](#top)
