import { ref } from 'vue'
import { ApiError } from '@/shared/api/client'

export function useAsyncAction() {
  const loading = ref(false)
  const error = ref('')
  async function run<T>(action: () => Promise<T>): Promise<T | undefined> {
    if (loading.value) return undefined
    loading.value = true
    error.value = ''
    try { return await action() }
    catch (caught) {
      error.value = caught instanceof ApiError || caught instanceof Error ? caught.message : 'The action failed.'
      throw caught
    } finally { loading.value = false }
  }
  return { loading, error, run }
}
