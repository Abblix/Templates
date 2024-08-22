<a name="top"></a>
[![Abblix React Template](https://resources.abblix.com/imgs/jpg/templates-github-banner-react.jpg)](https://github.com/Abblix/Templates)
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
- [Prerequisites](#-prerequisites)
- [Installing the Templates](#-installing-the-templates)
- [Creating a New Project](#%EF%B8%8F-creating-a-new-project)
- [Running the Project](#%EF%B8%8F-running-the-project)
- [Project Structure](#-project-structure)
- [License](#-license)
- [Acknowledgements](#-acknowledgements)
- [Key Contacts & Resources](#-key-contacts--resources)

## 🚀 About the Template

This template provides a starting point for building an ASP.NET Core WebAPI project with a React frontend using TypeScript and Vite. It sets up a basic project structure and configuration to help you get started quickly.

### Features

- **ASP.NET Core WebAPI**: A powerful framework for building web APIs with .NET.
- **React**: A popular JavaScript library for building user interfaces.
- **TypeScript**: A typed superset of JavaScript that compiles to plain JavaScript.
- **Vite**: A fast build tool and development server for modern web projects.

## 📋 Prerequisites

Before you begin using the Abblix ASP.NET Core Templates, ensure that your development environment is properly set up with the following tools:

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**: This SDK is essential for building and running ASP.NET Core applications. It includes the necessary compilers, libraries, and tools to develop, test, and deploy your applications. Make sure to download and install the latest version to fully leverage the capabilities of the templates.
- **[Node.js and npm](https://nodejs.org/)**: Node.js is required to manage the frontend dependencies for projects using React and Vite. npm (Node Package Manager) comes bundled with Node.js and is used to install and manage these dependencies. Having Node.js and npm installed ensures that you can seamlessly integrate and build the frontend parts of your application, especially when using modern JavaScript frameworks and tools like React and Vite.

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
# This allows you to install the template locally without accessing an online repository
dotnet new install path/to/Abblix.Templates.x.x.x.nupkg
```

This method allows you to install the template manually using the `.nupkg` file.

## 🛠️ Creating a New Project

You can create a new project using this template with any of the specified short names or aliases.

### Using the Primary Short Name

```shell
# Creates a new project using the primary short name 'abblix-react'
# The '-n MyNewProject' specifies the name of the new project folder
dotnet new abblix-react -n MyNewProject
```

### Using Alternative Short Names

```shell
# Creates a new project using the 'react-net8' short name
# Useful if you want to highlight .NET 8 usage in your project
dotnet new react-net8 -n MyNewProject

# Creates a new project using the 'react-typescript' short name
# Ideal if your focus is on TypeScript support
dotnet new react-typescript -n MyNewProject

# Creates a new project using the 'react-vite' short name
# Best if you want to emphasize the use of Vite in the project setup
dotnet new react-vite -n MyNewProject
```

## ⚙️ Running the Project

Once you've created your new project, you're ready to run it and see everything in action. Follow these steps to navigate through the project setup, restore dependencies, and launch both the .NET backend and the React frontend together.


   ```shell
   # Move into the project directory you just created
   cd MyNewProject

   # Restore all the necessary NuGet packages for the .NET project
   dotnet restore

   # Build the project to ensure everything is set up correctly
   dotnet build

   # Move into the ClientApp directory, where the React application resides
   cd ClientApp

   # Install all the required npm packages for React and Vite
   npm install

   # Start both the ASP.NET Core backend and the Vite development server
   dotnet run
   ```

   This will start the ASP.NET Core backend and the Vite development server. You can access the application in your browser at `http://localhost:5000`.

## 📂 Project Structure

Understanding the structure of your project is crucial for effective development. Below is an overview of the directory layout generated by the template, highlighting key folders and files. This structure organizes your backend and frontend components, ensuring a clean separation of concerns and ease of navigation.

```
/MyNewProject
├── Controllers/
├── ClientApp/
│   ├── src/
│   │   ├── App.tsx
│   │   ├── index.tsx
│   │   └── ...
│   ├── public/
│   │   └── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── MyNewProject.csproj
└── Program.cs
```

- **Controllers/**: Contains API controllers.
- **ClientApp/**: Contains the React frontend application.
- **Properties/**: Contains project settings files.
- **appsettings.json**: Configuration settings for the application.
- **MyNewProject.csproj**: Project file for the .NET project.
- **Program.cs**: Entry point for the .NET application.


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

- **[Email](mailto:support@abblix.com)**: Send us your inquiries or support requests via support@abblix.com.
- **[Website](https://www.abblix.com/abblix-oidc-server)**: Visit the official page for more information.
- **[GitHub Repository](https://github.com/Abblix/Oidc.Server)**: Explore the source code and contribute to the Abblix OIDC Server.
- **[Getting Started Guide](https://github.com/Abblix/Oidc.Server.GettingStarted)**: Follow our step-by-step guide to create an OpenID Connect provider using ASP.NET MVC.
- **[Abblix Documentation](https://docs.abblix.com/docs)**: Access detailed documentation for all our products and services.

We look forward to assisting you and ensuring your experience with our products is successful and enjoyable!

[Back to top](#top)
