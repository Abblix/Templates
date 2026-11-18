# Changelog: abblix-react

What changed in the project that the ReactTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template; a version that left the template untouched says so.

## 3.0.0

### Upgrading a project created from 2.x

- Linting moves from ESLint to oxlint: `npm run lint` runs `oxlint` with the React plugin enabled in `.oxlintrc.json`, and `eslint.config.js` and the ESLint packages are gone.
- The front-end dependencies move to TypeScript 7.0, Vite 8.3.8, `@vitejs/plugin-react` 6 and React 19.2.7. The host still targets .NET 10 and references Microsoft.AspNetCore.SpaProxy 10.0.9, up from 10.0.3.
- `vite.config.ts` exports and reads the ASP.NET Core development certificate only when the dev server runs, so `npm run build` no longer needs `dotnet dev-certs` or the exported PEM files. In 2.x every config load read the certificate files, exporting them first when they were missing, so a build where the export failed, such as on a build agent without the .NET SDK, failed with it.
- `dotnet new` no longer runs `npm install` after creating the project, so it no longer asks for permission to run a script. A Debug build installs the packages when `ClientApp/node_modules` is missing, and a publish installs and builds them, as before.

### Added

- `--https-port` sets the HTTPS port the host is served on, and `--spa-proxy-port` the port of the Vite dev server the host proxies to during development. When either is omitted or 0, a free port is picked, between 7000 and 7300 for the host and between 5173 and 5273 for Vite, so two projects created one after another no longer share a port. In 2.x every project used 5000 and 3000.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `ReactApp` as the project name.

### Changed

- The browser tab shows the Abblix icon (`favicon.svg`) instead of the Vite one.

### Fixed

- A new project no longer receives a prebuilt `ClientApp/dist` from the package; 2.0.0 and 2.0.1 shipped one it never needed.

### Security

- The lockfile of 2.0.1 pinned development tools with published advisories. The high-severity ones include Vite 7.3.1 (GHSA-p9ff-h696-f583, arbitrary file read through the dev server WebSocket; GHSA-v2wj-q39q-566r, `server.fs.deny` bypass; GHSA-587h-7wmj-h5pm in launch-editor), Rollup 4.57.1 (GHSA-mw96-cpmx-2vgc, arbitrary file write), postcss 8.5.6 (GHSA-6g55-p6wh-862q and GHSA-r28c-9q8g-f849, arbitrary .map file read) and source-map-js 1.2.1 (GHSA-68fv-2mgg-jv7q, denial of service). None of these packages reach the published application.
- With the move to Vite 8 and oxlint, the lockfile now pins Vite 8.3.8, postcss 8.5.33 and source-map-js 1.2.2, which leaves every advisory against the 2.0.1 tools behind except one: picomatch (GHSA-fc5q-39xc-vjpf, a high-severity ReDoS) has no fixed release yet, so `npm audit` still reports it.

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

## 1.0.0 - 2024-08-22

- First release: an ASP.NET Core host for .NET 8 with a React 18, TypeScript and Vite 5 front end in `ClientApp`, served over HTTPS with the ASP.NET Core development certificate and proxied through Microsoft.AspNetCore.SpaProxy during development.
