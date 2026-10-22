import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { fileURLToPath, URL } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';

// Dev server proxies /api (including /api/health) to the ERFWeb API host so the SPA and API are same-origin.
// The target comes from VITE_API_TARGET in .env / .env.local (or the shell). It may be the plain
// Kestrel URL (http://localhost:5080) or the Visual Studio IIS Express SSL URL (https://localhost:44387);
// `secure: false` lets the proxy accept the self-signed ASP.NET Core dev certificate in that case.
// The version users see at the bottom of every page: package.json "version" plus the commit it was built
// from. Render exposes the commit as RENDER_GIT_COMMIT; elsewhere git is asked, and "local" is the fallback.
const appVersion = (JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf-8')) as { version: string }).version;

function buildCommit(): string {
  const fromHost = process.env.RENDER_GIT_COMMIT ?? process.env.VITE_BUILD_COMMIT;
  if (fromHost) return fromHost.slice(0, 7);
  try {
    return execSync('git rev-parse --short=7 HEAD', { stdio: ['ignore', 'pipe', 'ignore'] }).toString().trim();
  } catch {
    return 'local';
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');
  const apiTarget = (env.VITE_API_TARGET).replace(/\/+$/, '');
  // Set VITE_API_PROXY_STRIP_PREFIX=true when the target already maps to the API's /api root, e.g. the
  // sr.mdmpi.com.ph nginx rule that forwards /erf/* to /api/*: /api/auth/me then goes to /erf/auth/me.
  const stripApiPrefix = env.VITE_API_PROXY_STRIP_PREFIX === 'true';

  return {
    plugins: [vue()],
    define: {
      __APP_VERSION__: JSON.stringify(appVersion),
      __APP_BUILD__: JSON.stringify(buildCommit()),
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          rewrite: stripApiPrefix ? (path) => path.replace(/^\/api/, '') : undefined,
        },
      },
    },
  };
});
