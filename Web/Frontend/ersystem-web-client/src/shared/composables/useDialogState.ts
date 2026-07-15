import { ref } from 'vue'

export function useDialogState() {
  const open = ref(false)
  const show = () => { open.value = true }
  const close = () => { open.value = false }
  return { open, show, close }
}
