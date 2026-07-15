import { reactive } from 'vue'

type SnackbarColor = 'success' | 'error' | 'warning' | 'info'
const state = reactive({ open: false, message: '', color: 'info' as SnackbarColor })

export function useSnackbar() {
  function show(message: string, color: SnackbarColor = 'info') { state.message = message; state.color = color; state.open = true }
  return { state, show, success: (message: string) => show(message, 'success'), error: (message: string) => show(message, 'error') }
}
