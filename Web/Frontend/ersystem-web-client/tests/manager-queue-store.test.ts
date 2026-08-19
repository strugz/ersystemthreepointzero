import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'
import type { ManagerReportListItem } from '@/features/manager-approvals/types'

const mocks = vi.hoisted(() => ({ list: vi.fn() }))

vi.mock('@/features/manager-approvals/api', () => ({
  managerApprovalApi: { list: mocks.list }
}))

function report(reportId = 'ER-1'): ManagerReportListItem {
  return {
    reportId,
    erfReferenceNumber: `ERF-${reportId}`,
    employeeUserId: 7,
    employeeName: 'Jay Bryan C. Abaoag',
    department: 'IMS',
    dateFrom: '2025-10-14',
    dateTo: '2025-10-14',
    description: '',
    reportType: 'Reimbursement',
    currentStep: 1,
    totalSteps: 1,
    status: 'For Approval',
    rowVersion: 'old-version'
  }
}

describe('Manager reports queue store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mocks.list.mockReset()
  })

  it('retains loaded rows, query state, and scroll position across route use', async () => {
    mocks.list.mockResolvedValue({ items: [report()], total: 1, page: 1, pageSize: 25 })
    const queue = useManagerReportsQueueStore()

    await queue.ensureLoaded(7)
    queue.filters.search = 'Jay'
    queue.page = 2
    queue.captureScroll(420)
    await queue.ensureLoaded(7)

    expect(mocks.list).toHaveBeenCalledTimes(1)
    expect(queue.items).toHaveLength(1)
    expect(queue.filters.search).toBe('Jay')
    expect(queue.page).toBe(2)
    expect(queue.scrollPosition).toBe(420)
  })

  it('refreshes cached rows created before the ERF list contract was added', async () => {
    const legacyRow = { ...report() } as ManagerReportListItem
    delete (legacyRow as Partial<ManagerReportListItem>).erfReferenceNumber
    mocks.list
      .mockResolvedValueOnce({ items: [legacyRow], total: 1, page: 1, pageSize: 25 })
      .mockResolvedValueOnce({ items: [report()], total: 1, page: 1, pageSize: 25 })
    const queue = useManagerReportsQueueStore()

    await queue.ensureLoaded(7)
    await queue.ensureLoaded(7)

    expect(mocks.list).toHaveBeenCalledTimes(2)
    expect(queue.items[0].erfReferenceNumber).toBe('ERF-ER-1')
  })

  it('removes a completed pending assignment before reconciling with the server', async () => {
    mocks.list
      .mockResolvedValueOnce({ items: [report()], total: 1, page: 1, pageSize: 25 })
      .mockResolvedValueOnce({ items: [], total: 0, page: 1, pageSize: 25 })
    const queue = useManagerReportsQueueStore()
    await queue.ensureLoaded(7)

    queue.applyWorkflowResult({ reportId: 'ER-1', status: 'Approved', rowVersion: 'new-version' })

    expect(queue.items).toEqual([])
    expect(queue.total).toBe(0)
    expect(queue.stale).toBe(true)

    await queue.refresh()
    expect(queue.stale).toBe(false)
    expect(mocks.list).toHaveBeenCalledTimes(2)
  })

  it('keeps cached rows and the stale marker when reconciliation fails', async () => {
    mocks.list
      .mockResolvedValueOnce({ items: [report()], total: 1, page: 1, pageSize: 25 })
      .mockRejectedValueOnce(new Error('Refresh failed'))
    const queue = useManagerReportsQueueStore()
    await queue.ensureLoaded(7)
    queue.filters.status = 'completed'
    queue.applyWorkflowResult({ reportId: 'ER-1', status: 'Approved', rowVersion: 'new-version' })

    await queue.refresh()

    expect(queue.items[0]).toMatchObject({ status: 'Approved', rowVersion: 'new-version' })
    expect(queue.error).toBe('Refresh failed')
    expect(queue.stale).toBe(true)
  })

  it('clears the previous user cache before loading for another user', async () => {
    mocks.list
      .mockResolvedValueOnce({ items: [report('ER-OLD')], total: 1, page: 1, pageSize: 25 })
      .mockResolvedValueOnce({ items: [report('ER-NEW')], total: 1, page: 1, pageSize: 25 })
    const queue = useManagerReportsQueueStore()
    await queue.ensureLoaded(7)
    queue.captureScroll(300)

    await queue.ensureLoaded(8)

    expect(queue.items.map(item => item.reportId)).toEqual(['ER-NEW'])
    expect(queue.ownerUserId).toBe(8)
    expect(queue.scrollPosition).toBe(0)
  })
})
