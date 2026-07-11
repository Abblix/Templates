# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and fast linting via [oxlint](https://oxc.rs/docs/guide/usage/linter.html).

The React integration uses [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react/README.md), which relies on Babel for Fast Refresh.

## Scripts

- `npm run dev` starts the Vite dev server with HMR
- `npm run build` type-checks with `tsc` and produces a production build
- `npm run preview` serves the production build locally
- `npm run lint` runs oxlint

## Linting

Linting is handled by oxlint, a Rust-based linter that ships as a single binary with no plugin dependencies. Rules are configured in `.oxlintrc.json`. The React plugin is enabled there, so React and React Hooks rules are active out of the box.

oxlint intentionally does not perform type-aware analysis (it does not run the TypeScript type checker). The `npm run build` step already type-checks the project through `tsc`. If you later need type-aware lint rules (for example `no-floating-promises`), add ESLint with `typescript-eslint` alongside oxlint and wire it up with [eslint-plugin-oxlint](https://github.com/oxc-project/eslint-plugin-oxlint) to avoid running the same rules twice.
