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
function devServerHttps() {
  if (!fs.existsSync(certPath) || !fs.existsSync(keyPath)) {
    console.log(`Certificate or key file not found. Generating new files (${certPath}, ${keyPath})...`);
    execSync(`dotnet dev-certs https --export-path ${certPath} --format Pem --no-password`);
  } else {
    console.log('Certificate and key files already exist. Skipping generation.');
  }

  return {
    cert: fs.readFileSync(certPath, 'utf-8'),
    key: fs.readFileSync(keyPath, 'utf-8'),
  };
}

export default defineConfig(({ command }) => ({
  plugins: [react()],
  server: {
    port: 3000,
    host: true,
    ...(command === 'serve' ? { https: devServerHttps() } : {}),
  },
}))
