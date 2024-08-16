# OpenID Connect Provider Template

This template provides a starting point for building an OpenID Connect Provider using ASP.NET Core and the Abblix OIDC Server. It includes the basic configuration and components necessary to set up an identity provider.

## Features

- **ASP.NET Core**: A cross-platform framework for building modern, cloud-based web applications.
- **OpenID Connect**: A simple identity layer on top of the OAuth 2.0 protocol.
- **Abblix OIDC Server**: A powerful .NET library that implements the OpenID Connect protocol with a focus on flexibility, reusability, and reliability.
- **JWT Tokens**: Secure tokens for API authentication.
- **User Management**: Basic user management and storage for test purposes only.

## About Abblix OIDC Server

The Abblix OIDC Server is an officially certified .NET library by the OpenID Foundation, designed to provide reliable and secure authentication. It offers effortless integration with ASP.NET WebApi, utilizing standard controller classes, binding, and routing mechanisms. The library is built on the principles of Hexagonal Architecture, ensuring flexibility and modularity, which allows you to tailor solutions to your precise needs.

With a modular design that separates validation, request processing, and response formatting, the Abblix OIDC Server promotes code reusability and extension. It also supports Dependency Injection using the standard .NET DI container for better code organization and object lifecycle management.

## Installing the Templates

First, you need to install the template from NuGet, or you can use the local `.nupkg` file.

### Installing from NuGet

```sh
dotnet new install Abblix.Templates
```

### Installing from Local `.nupkg` File

```sh
dotnet new install path/to/Abblix.Templates.1.0.0.nupkg
```

## Creating a New Project

You can create a new project using this template with any of the specified short names or aliases.

### Using the Primary Short Name

```sh
dotnet new abblix-oidc-server -n MyOpenIDProvider
```

### Using Alternative Short Names

```sh
dotnet new oidc-server -n MyOpenIDProvider
dotnet new oauth2-server -n MyOpenIDProvider
dotnet new openidconnect -n MyOpenIDProvider
```

## Running the Project

1. Navigate to the project directory:

   ```sh
   cd MyOpenIDProvider
   ```

2. Restore and build the .NET project:

   ```sh
   dotnet restore
   dotnet build
   ```

3. Run the .NET application:

   ```sh
   dotnet run
   ```

   You can access the application in your browser at `http://localhost:5001`.

## Project Structure

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
- **wwwroot/**: This folder contains static files like JavaScript, CSS, and images that are served directly by the web server.
- **Properties/**: Contains project settings files, including `launchSettings.json`.
- **appsettings.json**: Configuration settings for the application.
- **appsettings.Development.json**: Development-specific configuration settings.
- **MyOpenIDProvider.csproj**: Project file for the .NET project.
- **Program.cs**: Entry point for the .NET application.
- **TestUserStorage.cs**: In-memory storage for user credentials, useful for testing and development purposes.
   
## Configuration

The template uses `appsettings.json` and `appsettings.Development.json` for configuration. You can customize settings such as the issuer name, token expiration, and user credentials.

## Customization

You can extend or modify the template by:

- Adding new controllers and views.
- Integrating with a real user store or identity provider.
- Configuring additional authentication methods.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contacts

For more details about our products, services, or any general information regarding the our flagship product - Abblix OIDC Server, feel free to reach out to us. We are here to provide support and answer any questions you may have. Below are the best ways to contact our team:

- **Email**: Send us your inquiries or support requests at [support@abblix.com](mailto:support@abblix.com).
- **Website**: Visit the official Abblix OIDC Server page for more information: [Abblix OIDC Server](https://www.abblix.com/abblix-oidc-server).

We look forward to assisting you and ensuring your experience with our products is successful and enjoyable!

## Acknowledgements

- [Abblix OIDC Server GitHub](https://github.com/Abblix/Oidc.Server)
- [Getting Started with Abblix OIDC Server](https://github.com/Abblix/Oidc.Server.GettingStarted)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
