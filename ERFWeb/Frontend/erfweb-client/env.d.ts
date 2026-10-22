/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

/** package.json "version", injected by vite.config.ts. */
declare const __APP_VERSION__: string;
/** Short commit the bundle was built from, or "local". */
declare const __APP_BUILD__: string;
