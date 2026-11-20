# Changelog: abblix-react

What changed in the project that the ReactTemplate creates, version by version. The versions are those of the Abblix.Templates package that ships the template; a version that left the template untouched says so.

## 3.0.0

### Upgrading a project created from 2.x

- Breaking: the short name `react-net` replaces `react-net10`, which 2.x listed among its aliases and which names no .NET version any more. A script that runs `dotnet new react-net10` stops finding the template; it runs `dotnet new react-net` or `dotnet new abblix-react` instead.
- Linting moves from ESLint to oxlint: `npm run lint` runs `oxlint` with the React plugin enabled in `.oxlintrc.json`, and `eslint.config.js` and the ESLint packages are gone.
- The front-end dependencies move to TypeScript 7.0, Vite 8.3.8, `@vitejs/plugin-react` 6 and React 19.2.7. The host targets .NET 11 and references Microsoft.AspNetCore.SpaProxy 11.0.0, up from .NET 10 and 10.0.3, so it builds and runs on the .NET 11 SDK alone.
- `vite.config.ts` exports and reads the ASP.NET Core development certificate only when serving (the dev server or `vite preview`), never when building, so `npm run build` no longer needs `dotnet dev-certs` or the exported PEM files. In 2.x every config load read the certificate files, exporting them first when they were missing, so a build where the export failed, such as on a build agent without the .NET SDK, failed with it.
- `dotnet new` no longer runs `npm install` after creating the project, so it no longer asks for permission to run a script. A Debug build and a publish install the packages with `npm ci`, exactly as `ClientApp/package-lock.json` records them, whenever that lock is newer than the last install, and a publish then builds them. In 2.x both ran `npm install`, a Debug build only when `ClientApp/node_modules` was missing. A Release build still needs no Node.js.

### Added

- `--https-port` sets the HTTPS port the host is served on, and `--spa-proxy-port` the port of the Vite dev server the host starts during development. When either is omitted or 0, a port free on the machine is picked at random as the project is created, between 7000 and 7300 for the host and between 5173 and 5273 for Vite. In 2.x every project used 5000 and 3000.
- The Vite dev server forwards paths under `/api` to the host: `server.proxy` in `vite.config.ts` targets the host's HTTPS address and trusts the exported development certificate. In 2.x such a call stayed on the Vite dev server, which answered it with `index.html`, or with 404 when the request asked only for JSON. A project created from 2.x gets the same by adding `proxy: { '/api': { target: 'https://localhost:<port>', ca: fs.readFileSync(certPath, 'utf-8') } }` to `server` in its `vite.config.ts`, with the host's port.
- `Program.cs` maps `/api/greeting`, and `App.tsx` fetches it and shows the message under the heading, so a new project shows the call reaching the host from the dev server and from a published build.
- The Visual Studio New Project dialog shows the template with the Abblix icon and proposes `ReactApp` as the project name. `ide.host.json` marks `--https-port` and `--spa-proxy-port` as options for that dialog to show.

### Changed

- The browser tab shows the Abblix icon (`favicon.svg`) instead of the Vite one.
- The Vite dev server listens on `localhost` only. 2.x set `server.host: true`, which listens on every network address, and with the `/api` proxy that would let any machine on the network call the host's endpoints through Vite while the host itself listens on `localhost`.
- The Vite dev server exports the development certificate on every start. Outside Windows it makes the certificate folder accessible to its owner only before the export, and the unencrypted key file readable and writable by its owner only after it, whatever permissions they had before. 2.x exported it only when the PEM files were missing, so after the certificate was renewed the dev server kept serving the old copy.
- The READMEs describe oxlint instead of ESLint, and give the application's address as `https://localhost:<port>` with the port chosen at creation, where 2.x gave `http://localhost:5000`. `@types/node` moves from 24 to 26.

### Fixed

- The short names `react-net`, `react-typescript` and `react-vite` create a project. 2.x listed them under `aliases`, a property the template engine does not read, so only `abblix-react` found the template.
- A new project no longer receives a prebuilt `ClientApp/dist` from the package; 2.0.0 and 2.0.1 shipped one it never needed.

### Security

- The lockfile of 2.0.1 pinned development tools with published advisories. The high-severity ones include Vite 7.3.1 (GHSA-p9ff-h696-f583, arbitrary file read through the dev server WebSocket; GHSA-v2wj-q39q-566r, `server.fs.deny` bypass; GHSA-587h-7wmj-h5pm in launch-editor), Rollup 4.57.1 (GHSA-mw96-cpmx-2vgc, arbitrary file write), postcss 8.5.6 (GHSA-6g55-p6wh-862q and GHSA-r28c-9q8g-f849, arbitrary .map file read) and source-map-js 1.2.1 (GHSA-68fv-2mgg-jv7q, denial of service). None of these packages reach the published application.
- With the move to Vite 8 and oxlint, the lockfile now pins Vite 8.3.8, postcss 8.5.33 and source-map-js 1.2.2, and `npm audit` reports no advisory against it.

## 2.0.1 - 2026-02-19

- The front-end dependencies move to React 19.2, Vite 7.3, ESLint 9.39, TypeScript 5.9, `@vitejs/plugin-react` 5.1 and `eslint-plugin-react-hooks` 7, matching the output of `create-vite` 8.3.0.
- `main.tsx` imports `StrictMode` and `createRoot` by name.
- The TypeScript configuration compiles to ES2022 for the app and ES2023 for the Node side, and turns on `verbatimModuleSyntax`, `erasableSyntaxOnly` and `noUncheckedSideEffectImports`.

## 2.0.0 - 2026-02-19

- The host targets .NET 10 and references Microsoft.AspNetCore.SpaProxy 10.0.3, up from .NET 9 and 8.0.7.
- The front end moves to React 19, Vite 6 and ESLint 9 with the flat configuration in `eslint.config.js`, which replaces `.eslintrc.cjs`.
- The unused `react-router-dom` dependency is removed.
- Publishing takes the Vite output from `ClientApp/dist` instead of `ClientApp/build`.
- The name listed under `aliases` changes from `react-net8` to `react-net10`. The template engine read neither; 3.0.0 replaces both with the short name `react-net`.

## 1.2.0 - 2025-08-14

- The host targets .NET 9, up from .NET 8.

## 1.0.2 - 2024-08-23

- No change to this template.

## 1.0.1 - 2024-08-22

- README corrections only.

## 1.0.0 - 2024-08-22

- First release: an ASP.NET Core host for .NET 8 with a React 18, TypeScript and Vite 5 front end in `ClientApp`, served over HTTPS with the ASP.NET Core development certificate and proxied through Microsoft.AspNetCore.SpaProxy during development.
