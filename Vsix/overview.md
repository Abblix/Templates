# Abblix Templates

Three project templates for the Visual Studio New Project dialog. Two of them create an OpenID Provider on ASP.NET Core and Abblix OIDC Server; the third creates an ASP.NET Core host with a React application. Search the dialog for the template's name.

This extension carries the same template package that `dotnet new install Abblix.Templates` installs from nuget.org, at the same version, so a project created here matches one created on the command line.

All three projects target .NET 11, so the .NET 11 SDK has to be installed.

## Abblix OpenID Provider (ASP.NET Identity + EF Core)

An OpenID Provider to start a real project from. Users are kept by ASP.NET Core Identity on EF Core with SQLite, and client registrations and the signing key are stored in SQLite too, so they survive a restart. The build bundles Bootstrap with Vite, so it needs Node.js with npm.

Options in the dialog:

- Host style: a Minimal API host (the default) or an MVC host.
- HTTPS port: the port the provider is served on, which goes into its issuer address. Leave it at 0 to have a free port picked when the project is created.
- Sample user email and Sample user password: the development user seeded into the Identity store on the first run. The password has to meet Identity's default rules: six characters or more, with an uppercase letter, a lowercase letter, a digit and a symbol.

Documentation: [abblix-oidc-server](https://github.com/Abblix/Templates/blob/master/docs/abblix-oidc-server.md)

## Abblix OpenID Provider - Getting Started (in-memory)

A small provider for learning how the server fits together. Users and clients live in memory and the signing key is generated at every start, so nothing survives a restart. Comments in the code and build warnings point at what a real provider does instead.

Options in the dialog:

- Host style: a Minimal API host (the default) or an MVC host.
- HTTPS port: the port the provider is served on, which goes into its issuer address. Leave it at 0 to have a free port picked when the project is created.
- Sample user email and Sample user password: the in-memory development user. Without a password, one is generated at every start and printed on the console.

Documentation: [abblix-oidc-server-gettingstarted](https://github.com/Abblix/Templates/blob/master/docs/abblix-oidc-server-gettingstarted.md)

## ASP.NET Core MVC with React and Vite

An ASP.NET Core host with a single-page application written in React and TypeScript and built by Vite. In development the host starts the Vite dev server; a published build serves the compiled application from `wwwroot`. A Debug build and publishing need Node.js with npm.

Options in the dialog:

- HTTPS port: the port the ASP.NET Core host is served on.
- Vite dev server port: the port of the Vite dev server during development.

A port left at 0 is replaced with a free one when the project is created.

Documentation: [abblix-react](https://github.com/Abblix/Templates/blob/master/docs/abblix-react.md)

## Source and support

The templates are developed at [github.com/Abblix/Templates](https://github.com/Abblix/Templates), where each template keeps a changelog. Questions and bug reports go to the repository's issues.
