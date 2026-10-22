import { fileURLToPath, URL } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';

// Dev server proxies /api (including /api/health) to the ERFWeb API host so the SPA and API are same-origin.
// The target comes from VITE_API_TARGET in .env / .env.local (or the shell). It may be the plain
// Kestrel URL (http://localhost:5080) or the Visual Studio IIS Express SSL URL (https://localhost:44387);
// `secure: false` lets the proxy accept the self-signed ASP.NET Core dev certificate in that case.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');
  const apiTarget = (env.VITE_API_TARGET).replace(/\/+$/, '');
  // Set VITE_API_PROXY_STRIP_PREFIX=true when the target already maps to the API's /api root, e.g. the
  // sr.mdmpi.com.ph nginx rule that forwards /erf/* to /api/*: /api/auth/me then goes to /erf/auth/me.
  const stripApiPrefix = env.VITE_API_PROXY_STRIP_PREFIX === 'true';

  return {
    plugins: [vue()],
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
