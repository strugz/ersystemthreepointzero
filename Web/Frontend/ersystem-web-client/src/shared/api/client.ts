import type { ProblemDetails } from '@/shared/types/api'

let antiforgeryToken = ''
const antiforgeryFailureCode = 'antiforgery_validation_failed'
const safeMethods = ['GET', 'HEAD', 'OPTIONS']

export class ApiError extends Error {
  constructor(public readonly status: number, message: string, public readonly problem?: ProblemDetails) { super(message) }
}

export async function refreshAntiforgery(): Promise<string> {
  const response = await fetch('/api/auth/antiforgery', { credentials: 'include' })
  if (!response.ok) throw new ApiError(response.status, 'Unable to initialize security token.')
  const body = await response.json() as { token: string }
  antiforgeryToken = body.token
  return antiforgeryToken
}

export function invalidateAntiforgery(): void {
  antiforgeryToken = ''
}

async function sendRequest<T>(path: string, init: RequestInit, allowAntiforgeryRetry: boolean): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase()
  const requiresAntiforgery = !safeMethods.includes(method)
  if (requiresAntiforgery && !antiforgeryToken) await refreshAntiforgery()
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (antiforgeryToken && requiresAntiforgery) headers.set('X-CSRF-TOKEN', antiforgeryToken)
  const response = await fetch(path, { ...init, headers, credentials: 'include' })
  if (response.status === 204) return undefined as T
  if (!response.ok) {
    let problem: ProblemDetails | undefined
    try { problem = await response.json() as ProblemDetails } catch { problem = undefined }
    if (response.status === 401) invalidateAntiforgery()
    if (requiresAntiforgery && allowAntiforgeryRetry && response.status === 400 && problem?.code === antiforgeryFailureCode) {
      invalidateAntiforgery()
      await refreshAntiforgery()
      return sendRequest<T>(path, init, false)
    }
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? 'The request failed.', problem)
  }
  return await response.json() as T
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  return sendRequest<T>(path, init, true)
}

export async function apiBlob(path: string): Promise<Blob> {
  const response = await fetch(path, { credentials: 'include' })
  if (!response.ok) {
    let problem: ProblemDetails | undefined
    try { problem = await response.json() as ProblemDetails } catch { problem = undefined }
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? 'The attachment could not be loaded.', problem)
  }
  return response.blob()
}

export function buildQuery(values: Record<string, unknown>): string {
  const params = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  })
  const query = params.toString()
  return query ? `?${query}` : ''
}
