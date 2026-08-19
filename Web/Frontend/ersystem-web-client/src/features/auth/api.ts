import { apiRequest, invalidateAntiforgery, refreshAntiforgery } from '@/shared/api/client'
import type { AuthenticatedUser, LoginRequest } from './types'

export const authApi = {
  me: () => apiRequest<AuthenticatedUser>('/erf/auth/me'),
  login: async (request: LoginRequest) => {
    await refreshAntiforgery()
    const user = await apiRequest<AuthenticatedUser>('/erf/auth/login', { method: 'POST', body: JSON.stringify(request) })
    await refreshAntiforgery()
    return user
  },
  logout: async () => {
    try { await apiRequest<void>('/erf/auth/logout', { method: 'POST' }) }
    finally { invalidateAntiforgery() }
  }
}
