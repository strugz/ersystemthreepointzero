import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/features/auth/api'
import { ApiError, apiRequest, invalidateAntiforgery, refreshAntiforgery } from '@/shared/api/client'

const user = { userId: 1, username: 'USER', fullName: 'Test User', userLevel: 'User', roles: [] }

function jsonResponse(body: unknown, status = 200): Response {
  return { ok: status >= 200 && status < 300, status, json: vi.fn().mockResolvedValue(body) } as unknown as Response
}

function emptyResponse(status = 204): Response {
  return { ok: status >= 200 && status < 300, status } as Response
}

function requestHeaders(fetchMock: ReturnType<typeof vi.fn>, call: number): Headers {
  return new Headers((fetchMock.mock.calls[call][1] as RequestInit).headers)
}

beforeEach(() => {
  vi.stubEnv('VITE_API_BASE_URL', '')
})

afterEach(() => {
  invalidateAntiforgery()
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
})

describe('antiforgery token lifecycle', () => {
  it('uses the configured API base URL', async () => {
    vi.stubEnv('VITE_API_BASE_URL', 'https://sr.mdmpi.com.ph/')
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse(user))
    vi.stubGlobal('fetch', fetchMock)

    await apiRequest('/erf/auth/me')

    expect(fetchMock.mock.calls[0][0]).toBe('https://sr.mdmpi.com.ph/erf/auth/me')
  })

  it('refreshes the token before and after login', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'anonymous-token' }))
      .mockResolvedValueOnce(jsonResponse(user))
      .mockResolvedValueOnce(jsonResponse({ token: 'authenticated-token' }))
      .mockResolvedValueOnce(emptyResponse())
    vi.stubGlobal('fetch', fetchMock)

    await authApi.login({ username: 'USER', password: 'secret' })
    await apiRequest<void>('/erf/action', { method: 'POST' })

    expect(fetchMock.mock.calls.map(call => call[0])).toEqual([
      '/erf/auth/antiforgery', '/erf/auth/login', '/erf/auth/antiforgery', '/erf/action'
    ])
    expect(requestHeaders(fetchMock, 1).get('X-CSRF-TOKEN')).toBe('anonymous-token')
    expect(requestHeaders(fetchMock, 3).get('X-CSRF-TOKEN')).toBe('authenticated-token')
  })

  it('invalidates the authenticated token after logout', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'authenticated-token' }))
      .mockResolvedValueOnce(emptyResponse())
      .mockResolvedValueOnce(jsonResponse({ token: 'anonymous-token' }))
      .mockResolvedValueOnce(emptyResponse())
    vi.stubGlobal('fetch', fetchMock)

    await refreshAntiforgery()
    await authApi.logout()
    await apiRequest<void>('/erf/action', { method: 'POST' })

    expect(requestHeaders(fetchMock, 1).get('X-CSRF-TOKEN')).toBe('authenticated-token')
    expect(fetchMock.mock.calls[2][0]).toBe('/erf/auth/antiforgery')
    expect(requestHeaders(fetchMock, 3).get('X-CSRF-TOKEN')).toBe('anonymous-token')
  })

  it('invalidates the token after an unauthorized response', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'expired-session-token' }))
      .mockResolvedValueOnce(jsonResponse({ title: 'Unauthorized' }, 401))
      .mockResolvedValueOnce(jsonResponse({ token: 'anonymous-token' }))
      .mockResolvedValueOnce(emptyResponse())
    vi.stubGlobal('fetch', fetchMock)

    await refreshAntiforgery()
    await expect(apiRequest('/erf/auth/me')).rejects.toBeInstanceOf(ApiError)
    await apiRequest<void>('/erf/action', { method: 'POST' })

    expect(fetchMock.mock.calls[2][0]).toBe('/erf/auth/antiforgery')
    expect(requestHeaders(fetchMock, 3).get('X-CSRF-TOKEN')).toBe('anonymous-token')
  })

  it('refreshes and retries once for a recognized antiforgery failure', async () => {
    const failure = { title: 'Validation failed', status: 400, code: 'antiforgery_validation_failed' }
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'stale-token' }))
      .mockResolvedValueOnce(jsonResponse(failure, 400))
      .mockResolvedValueOnce(jsonResponse({ token: 'fresh-token' }))
      .mockResolvedValueOnce(emptyResponse())
    vi.stubGlobal('fetch', fetchMock)

    await refreshAntiforgery()
    await apiRequest<void>('/erf/action', { method: 'POST' })

    expect(fetchMock).toHaveBeenCalledTimes(4)
    expect(requestHeaders(fetchMock, 1).get('X-CSRF-TOKEN')).toBe('stale-token')
    expect(requestHeaders(fetchMock, 3).get('X-CSRF-TOKEN')).toBe('fresh-token')
  })

  it('does not retry an antiforgery failure more than once', async () => {
    const failure = { title: 'Validation failed', status: 400, code: 'antiforgery_validation_failed' }
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'stale-token' }))
      .mockResolvedValueOnce(jsonResponse(failure, 400))
      .mockResolvedValueOnce(jsonResponse({ token: 'fresh-token' }))
      .mockResolvedValueOnce(jsonResponse(failure, 400))
    vi.stubGlobal('fetch', fetchMock)

    await refreshAntiforgery()
    await expect(apiRequest<void>('/erf/action', { method: 'POST' })).rejects.toMatchObject({ status: 400 })

    expect(fetchMock).toHaveBeenCalledTimes(4)
  })
})
