import { flushPromises, shallowMount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import type { ManagerReportListItem } from '@/features/manager-approvals/types'
import ManagerReportsView from '@/views/manager/ManagerReportsView.vue'

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  routerPush: vi.fn()
}))

vi.mock('vue-router', () => ({
  onBeforeRouteLeave: vi.fn(),
  useRouter: () => ({ push: mocks.routerPush })
}))

vi.mock('@/app/stores/session', () => ({
  useSessionStore: () => ({ user: { userId: 7 } })
}))

vi.mock('@/features/manager-approvals/api', () => ({
  managerApprovalApi: { list: mocks.list }
}))

function report(): ManagerReportListItem {
  return {
    reportId: '7e45dbb8-5ad9-40a5-bf08-d5f4892a6187',
    erfReferenceNumber: 'ER-1430820260602-165400',
    employeeUserId: 14308,
    employeeName: 'Jay Bryan C. Abaoag',
    department: 'IMS',
    dateFrom: '2026-06-20',
    dateTo: '2026-06-20',
    description: 'Reimbursement',
    reportType: 'Reimbursement',
    currentStep: 1,
    totalSteps: 1,
    status: 'Approved',
    rowVersion: 'row-version'
  }
}

describe('Manager reports view', () => {
  beforeEach(() => {
    const pinia = createPinia()
    setActivePinia(pinia)
    mocks.list.mockReset().mockResolvedValue({
      items: [report()],
      total: 1,
      page: 1,
      pageSize: 25
    })
    mocks.routerPush.mockReset()
    vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => {
      callback(0)
      return 1
    })
    vi.spyOn(window, 'scrollTo').mockImplementation(() => undefined)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('displays the ERF reference column while retaining UUID routing', async () => {
    const wrapper = shallowMount(ManagerReportsView)
    await flushPromises()

    expect(wrapper.find('.queue-page-shell').exists()).toBe(true)
    const table = wrapper.getComponent(AppServerTable)
    expect(table.props('headers')[0]).toEqual({
      title: 'ERF reference',
      key: 'erfReferenceNumber'
    })
    expect((table.props('items') as ManagerReportListItem[])[0].erfReferenceNumber)
      .toBe('ER-1430820260602-165400')

    table.vm.$emit('clickRow', report())
    expect(mocks.routerPush).toHaveBeenCalledWith(
      '/manager/reports/7e45dbb8-5ad9-40a5-bf08-d5f4892a6187'
    )
  })
})
