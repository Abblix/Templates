import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import { env } from 'process';
import { execSync } from 'child_process';

const certFolder = (env.APPDATA ?? '') !== '' ? `${env.APPDATA}/ASP.NET/https` : `${env.HOME}/.aspnet/https`;

// Define the paths
const certPath = path.resolve(certFolder, `localhost.pem`);
const keyPath = path.resolve(certFolder, `localhost.key`);

// The development certificate is a DEV SERVER concern, so it is exported and read only when serving.
// At module scope it ran on every config load, which made `vite build` depend on `dotnet dev-certs`
// succeeding and on the PEM files existing - neither of which holds on a build agent, where the export
// fails and takes the production build down with it. The error names vite.config.ts, so it reads as a
// broken config rather than a dev-only step running in the wrong mode.
// It is exported on every start: the proxy trusts exactly this file, so a copy left from a certificate
// since renewed would make the proxy refuse the host.
function devServerHttps() {
  console.log(`Exporting the development certificate (${certPath}, ${keyPath})...`);
  // The key is written unencrypted, and a folder or key left by an earlier export keeps whatever permissions
  // it had. Outside Windows, where Unix modes apply, the folder is made owner-only (0700) before the export,
  // so the fresh key never sits where other users can enter, and the key owner read-write (0600) after it.
  // dev-certs refuses to export into a missing folder, hence mkdirSync.
  const unixModes = process.platform !== 'win32';
  fs.mkdirSync(certFolder, { recursive: true });
  if (unixModes) {
    fs.chmodSync(certFolder, 0o700);
  }
  execSync(`dotnet dev-certs https --export-path "${certPath}" --format Pem --no-password`);
  if (unixModes) {
    fs.chmodSync(keyPath, 0o600);
  }

  return {
    cert: fs.readFileSync(certPath, 'utf-8'),
    key: fs.readFileSync(keyPath, 'utf-8'),
  };
}

// The app calls the host by relative paths, which the dev server forwards to the host's HTTPS address.
// The host serves the same development certificate, so the proxy trusts it as its CA rather than
// turning certificate checks off.
function devServer() {
  const https = devServerHttps();
  return {
    https,
    proxy: {
      '/api': { target: 'https://localhost:5000', ca: https.cert },
    },
  };
}

export default defineConfig(({ command }) => ({
  plugins: [react()],
  server: {
    // No `host`: the dev server stays on localhost. On every network address, the /api proxy would hand the
    // host's endpoints to any machine on the network.
    port: 3000,
    ...(command === 'serve' ? devServer() : {}),
  },
}))
