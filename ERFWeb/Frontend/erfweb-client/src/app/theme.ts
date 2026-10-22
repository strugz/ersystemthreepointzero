/**
 * Theme preference helpers shared by the Vuetify plugin (first paint) and the theme store (runtime).
 * The preference is a per-browser convenience: storage failures fall back to following the OS.
 */
export type ThemePreference = 'light' | 'dark' | 'system';
export type ThemeName = 'erLight' | 'erDark';

const STORAGE_KEY = 'erfweb.theme';
const DARK_QUERY = '(prefers-color-scheme: dark)';

export function readThemePreference(): ThemePreference {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    return value === 'light' || value === 'dark' ? value : 'system';
  } catch {
    return 'system';
  }
}

export function writeThemePreference(preference: ThemePreference): void {
  try {
    if (preference === 'system') localStorage.removeItem(STORAGE_KEY);
    else localStorage.setItem(STORAGE_KEY, preference);
  } catch {
    // Storage blocked (private window): the choice still applies for this page view.
  }
}

export function systemPrefersDark(): boolean {
  return typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(DARK_QUERY).matches;
}

export function watchSystemScheme(onChange: () => void): void {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
  window.matchMedia(DARK_QUERY).addEventListener('change', onChange);
}

export function resolveThemeName(preference: ThemePreference): ThemeName {
  const dark = preference === 'dark' || (preference === 'system' && systemPrefersDark());
  return dark ? 'erDark' : 'erLight';
}

export function initialThemeName(): ThemeName {
  const name = resolveThemeName(readThemePreference());
  applyDocumentScheme(name);
  return name;
}

/** Keeps native scrollbars, form controls and the mobile browser chrome in step with the theme. */
export function applyDocumentScheme(name: ThemeName): void {
  if (typeof document === 'undefined') return;
  const dark = name === 'erDark';
  document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
  document.documentElement.dataset.theme = dark ? 'dark' : 'light';
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', dark ? '#0a0f1a' : '#1f3864');
}
