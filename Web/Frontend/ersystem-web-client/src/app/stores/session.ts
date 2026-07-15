import { defineStore } from 'pinia'
import { ref } from 'vue'
import { authApi } from '@/features/auth/api'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'
import type { AuthenticatedUser, LoginRequest } from '@/features/auth/types'

export const useSessionStore = defineStore('session', () => {
  const user = ref<AuthenticatedUser | null>(null)
  const initialized = ref(false)
  async function initialize() {
    if (initialized.value) return
    try { user.value = await authApi.me() } catch { user.value = null }
    finally { initialized.value = true }
  }
  async function login(request: LoginRequest) { user.value = await authApi.login(request); initialized.value = true }
  async function logout() {
    try { await authApi.logout() }
    finally {
      useManagerReportsQueueStore().clear()
      user.value = null
      initialized.value = true
    }
  }
  return { user, initialized, initialize, login, logout }
})
