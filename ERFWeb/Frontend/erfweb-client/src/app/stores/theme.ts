import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import vuetify from '@/app/plugins/vuetify';
import {
  applyDocumentScheme,
  readThemePreference,
  resolveThemeName,
  watchSystemScheme,
  writeThemePreference,
} from '@/app/theme';
import type { ThemePreference } from '@/app/theme';

export const useThemeStore = defineStore('theme', () => {
  const preference = ref<ThemePreference>(readThemePreference());
  const isDark = computed(() => vuetify.theme.global.current.value.dark);

  function apply() {
    const name = resolveThemeName(preference.value);
    if (vuetify.theme.global.name.value === name) return;

    const swap = () => {
      vuetify.theme.change(name);
      applyDocumentScheme(name);
    };
    // A short crossfade hides the colour snap. Theme changes are occasional, so a little motion is
    // fine — but never for reduced-motion users or browsers without view transitions.
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    const doc = document as Document & { startViewTransition?: (cb: () => void) => unknown };
    if (!reduce && typeof doc.startViewTransition === 'function') doc.startViewTransition(swap);
    else swap();
  }

  function setPreference(next: ThemePreference) {
    preference.value = next;
    writeThemePreference(next);
    apply();
  }

  /** One-click toggle from the header: always lands on an explicit light or dark choice. */
  function toggle() {
    setPreference(isDark.value ? 'light' : 'dark');
  }

  // Follow the OS live while the user has not picked a side.
  watchSystemScheme(() => {
    if (preference.value === 'system') apply();
  });

  return { preference, isDark, setPreference, toggle };
});
