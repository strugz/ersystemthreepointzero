import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { colors } from '@/shared/design/tokens'

export default createVuetify({
  theme: {
    defaultTheme: 'erLight',
    themes: { erLight: { dark: false, colors: {
      primary: colors.brand,
      secondary: colors.accent,
      background: colors.canvas,
      surface: colors.surface,
      'surface-variant': colors.brandSoft,
      'on-background': colors.ink,
      'on-surface': colors.ink,
      outline: colors.border,
      success: colors.success,
      warning: colors.warning,
      error: colors.error,
      info: colors.info
    } } }
  },
  defaults: {
    VBtn: { rounded: 'lg', elevation: 0 },
    VCard: { rounded: 'xl', elevation: 0 },
    VTextField: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' },
    VSelect: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' },
    VTextarea: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' }
  }
})
