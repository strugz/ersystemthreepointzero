import 'vuetify/styles';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';
import { aliases, mdi } from 'vuetify/iconsets/mdi';

// "Ledger" palette: warm paper, ink text, MDMPI navy as the single accent.
// Status hues are deliberately muted so the navy stays the loudest colour on screen.
const erLight = {
  dark: false,
  colors: {
    primary: '#1f3864',
    'on-primary': '#ffffff',
    secondary: '#5d5a53',
    background: '#f4f3ef',
    surface: '#fffefb',
    'on-surface': '#1c1b19',
    'surface-variant': '#ecebe6',
    'on-surface-variant': '#5d5a53',
    error: '#b23a2e',
    success: '#2f7a4a',
    warning: '#b7791f',
    info: '#2a4a80',
  },
  variables: {
    'border-color': '#1c1b19',
    'border-opacity': 0.1,
    'medium-emphasis-opacity': 0.62,
  },
};

const erDark = {
  dark: true,
  colors: {
    primary: '#9db4dc',
    'on-primary': '#0f1c33',
    secondary: '#b3afa6',
    background: '#151513',
    surface: '#1e1e1b',
    'on-surface': '#ecebe6',
    'surface-variant': '#2a2a26',
    'on-surface-variant': '#b3afa6',
    error: '#e08a80',
    success: '#7bc08f',
    warning: '#e0b36a',
    info: '#9db4dc',
  },
  variables: {
    'border-color': '#ecebe6',
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
    defaultTheme: 'erLight',
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
