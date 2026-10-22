import 'vuetify/styles';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';
import { aliases, mdi } from 'vuetify/iconsets/mdi';
import { initialThemeName } from '@/app/theme';

// "Ledger" palette: cool paper, ink text, MDMPI navy as the primary accent and brass as the
// secondary highlight (active nav, hero rules). Status hues stay muted so navy and brass lead.
const erLight = {
  dark: false,
  colors: {
    primary: '#1f3864',
    'on-primary': '#ffffff',
    secondary: '#a8741f',
    'on-secondary': '#ffffff',
    background: '#f3f4f7',
    surface: '#ffffff',
    'on-surface': '#161a23',
    'surface-variant': '#eceef3',
    'on-surface-variant': '#555b69',
    error: '#b23a2e',
    success: '#2f7a4a',
    warning: '#b7791f',
    info: '#2a4a80',
  },
  variables: {
    'border-color': '#161a23',
    'border-opacity': 0.1,
    'medium-emphasis-opacity': 0.62,
  },
};

// "Midnight ledger": deep navy-ink surfaces rather than neutral grey, so dark mode keeps the brand.
const erDark = {
  dark: true,
  colors: {
    primary: '#9cb8ef',
    'on-primary': '#0b1530',
    secondary: '#e2b765',
    'on-secondary': '#2a1d05',
    background: '#0a0f1a',
    surface: '#111827',
    'on-surface': '#e6e9f0',
    'surface-variant': '#1a2335',
    'on-surface-variant': '#a3abbd',
    error: '#ef8f84',
    success: '#7fcf9a',
    warning: '#e9bc6c',
    info: '#9cb8ef',
  },
  variables: {
    'border-color': '#e6e9f0',
    'border-opacity': 0.12,
  },
};

export default createVuetify({
  // Register every Vuetify component/directive globally. Without this (or vite-plugin-vuetify's
  // auto-import), <v-card>, <v-text-field> etc. are unknown elements and render as empty tags.
  components,
  directives,
  icons: { defaultSet: 'mdi', aliases, sets: { mdi } },
  theme: {
    // Resolved from the saved preference before mount so the first paint is already in the right theme.
    defaultTheme: initialThemeName(),
    themes: { erLight, erDark },
  },
  defaults: {
    VBtn: { rounded: 'lg', elevation: 0 },
    VCard: { rounded: 'lg', variant: 'outlined', elevation: 0 },
    VTextField: { variant: 'outlined', density: 'comfortable', color: 'primary' },
    VTextarea: { variant: 'outlined', density: 'comfortable', color: 'primary' },
    VSelect: { variant: 'outlined', density: 'comfortable', color: 'primary' },
    VCombobox: { variant: 'outlined', density: 'comfortable', color: 'primary' },
    VChip: { rounded: 'md' },
    VAlert: { rounded: 'lg', variant: 'tonal', density: 'comfortable' },
    VDialog: { transition: 'dialog-transition' },
  },
});
