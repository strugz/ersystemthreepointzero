/**
 * A test build (VITE_APP_MODE=test) shows the System Administrator's test-only Delete buttons. The API
 * still refuses them unless its appsettings.json has App:Mode Test, so this only decides what is shown.
 */
export const isTestMode = import.meta.env.VITE_APP_MODE === 'test';
