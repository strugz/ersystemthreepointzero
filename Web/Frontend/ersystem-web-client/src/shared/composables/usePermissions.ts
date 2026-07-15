import { computed } from 'vue'
import { useSessionStore } from '@/app/stores/session'

export function usePermissions() {
  const session = useSessionStore()
  const isManager = computed(() => session.user?.roles.includes('Manager') ?? false)
  const isFinance = computed(() => session.user?.roles.includes('Finance') ?? false)
  return { isManager, isFinance }
}
