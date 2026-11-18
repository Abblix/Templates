# Changelog: abblix-react

What changed in the project that the ReactTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template; a version that left the template untouched says so.

## 3.0.0

### Upgrading a project created from 2.x

- Linting moves from ESLint to oxlint: `npm run lint` runs `oxlint` with the React plugin enabled in `.oxlintrc.json`, and `eslint.config.js` and the ESLint packages are gone.
- The front-end dependencies move to TypeScript 7.0, Vite 8.3.8, `@vitejs/plugin-react` 6 and React 19.2.7. The host still targets .NET 10 and references Microsoft.AspNetCore.SpaProxy 10.0.9, up from 10.0.3.
- `vite.config.ts` exports and reads the ASP.NET Core development certificate only when the dev server runs, so `npm run build` no longer needs `dotnet dev-certs` or the exported PEM files. In 2.x every config load ran the export, so a build where it failed, such as on a build agent without the .NET SDK, failed with it.
- `dotnet new` no longer runs `npm install` after creating the project, so it no longer asks for permission to run a script. A Debug build installs the packages when `ClientApp/node_modules` is missing, and a publish installs and builds them, as before.

### Added

- `--https-port` sets the HTTPS port the host is served on, and `--spa-proxy-port` the port of the Vite dev server the host proxies to during development. When either is omitted or 0, a free port is picked, between 7000 and 7300 for the host and between 5173 and 5273 for Vite, so two projects created one after another no longer share a port. In 2.x every project used 5000 and 3000.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `ReactApp` as the project name.

### Changed

- The browser tab shows the Abblix icon (`favicon.svg`) instead of the Vite one.

### Fixed

- A new project no longer receives a prebuilt `ClientApp/dist` from the package; 2.0.0 and 2.0.1 shipped a stale one.

### Security

- The lockfile moves Vite to 8.3.8, postcss to 8.5.33 and source-map-js to 1.2.2, past GHSA-587h-7wmj-h5pm, GHSA-r825-w8mm-5pxw, GHSA-698v-4r3x-3vcv, GHSA-rq7h-c2jc-7f22 and GHSA-9jrq-w75r-8gcw in Vite, GHSA-68fv-2mgg-jv7q in source-map-js and the source map path traversal in postcss below 8.5.23. All of them are development dependencies.

## 2.0.1 - 2026-02-19

- The front-end dependencies move to React 19.2, Vite 7.3, ESLint 9.39, TypeScript 5.9, `@vitejs/plugin-react` 5.1 and `eslint-plugin-react-hooks` 7, matching the output of `create-vite` 8.3.0.
- `main.tsx` imports `StrictMode` and `createRoot` by name.
- The TypeScript configuration compiles to ES2022 for the app and ES2023 for the Node side, and turns on `verbatimModuleSyntax`, `erasableSyntaxOnly` and `noUncheckedSideEffectImports`.

## 2.0.0 - 2026-02-19

- The host targets .NET 10 and references Microsoft.AspNetCore.SpaProxy 10.0.3, up from .NET 9 and 8.0.7.
- The front end moves to React 19, Vite 6 and ESLint 9 with the flat configuration in `eslint.config.js`, which replaces `.eslintrc.cjs`.
- The unused `react-router-dom` dependency is removed.
- Publishing takes the Vite output from `ClientApp/dist` instead of `ClientApp/build`.
- The alias `react-net8` becomes `react-net10`.

## 1.2.0 - 2025-08-14

- The host targets .NET 9, up from .NET 8.

## 1.0.2 - 2024-08-23

- No change to this template.

## 1.0.1 - 2024-08-22

- README corrections only.

## 1.0.0 - 2024-08-17

- First release: an ASP.NET Core host for .NET 8 with a React 18, TypeScript and Vite 5 front end in `ClientApp`, served over HTTPS with the ASP.NET Core development certificate and proxied through Microsoft.AspNetCore.SpaProxy during development.
