import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/app/stores/session'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'

const mocks = vi.hoisted(() => ({
  logout: vi.fn(),
  list: vi.fn()
}))

vi.mock('@/features/auth/api', () => ({
  authApi: { logout: mocks.logout }
}))

vi.mock('@/features/manager-approvals/api', () => ({
  managerApprovalApi: { list: mocks.list }
}))

describe('session store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mocks.logout.mockReset().mockResolvedValue(undefined)
  })

  it('clears the in-memory Manager queue when signing out', async () => {
    const session = useSessionStore()
    const queue = useManagerReportsQueueStore()
    session.user = { userId: 7, username: 'jay', fullName: 'Jay', userLevel: 'Manager', roles: ['Manager'] }
    queue.ownerUserId = 7
    queue.loaded = true
    queue.scrollPosition = 240
    queue.filters.search = 'private report'

    await session.logout()

    expect(session.user).toBeNull()
    expect(queue.loaded).toBe(false)
    expect(queue.ownerUserId).toBeNull()
    expect(queue.scrollPosition).toBe(0)
    expect(queue.filters.search).toBe('')
  })
})
