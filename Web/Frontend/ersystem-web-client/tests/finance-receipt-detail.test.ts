import { flushPromises, shallowMount } from '@vue/test-utils'
import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/shared/api/client'
import AppApprovalTrail from '@/shared/components/AppApprovalTrail.vue'
import AppExpenseReview from '@/shared/components/AppExpenseReview.vue'
import AppMobileBackNavigation from '@/shared/components/AppMobileBackNavigation.vue'
import AppReceiptList from '@/shared/components/AppReceiptList.vue'
import AppReportAmountSummary from '@/shared/components/AppReportAmountSummary.vue'
import ReceiveReceiptsDialog from '@/features/finance-receipts/ReceiveReceiptsDialog.vue'
import type { FinanceReceiptDetail } from '@/features/finance-receipts/types'
import FinanceReceiptDetailView from '@/views/finance/FinanceReceiptDetailView.vue'

const mocks = vi.hoisted(() => ({
  detail: vi.fn(),
  receive: vi.fn(),
  snackbarSuccess: vi.fn(),
  snackbarError: vi.fn()
}))

vi.mock('vuetify', () => ({
  useDisplay: () => ({ mdAndUp: ref(false), smAndDown: ref(true) })
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { reportId: 'ER-24' } })
}))

vi.mock('@/shared/composables/useSnackbar', () => ({
  useSnackbar: () => ({
    success: mocks.snackbarSuccess,
    error: mocks.snackbarError
  })
}))

vi.mock('@/features/finance-receipts/api', () => ({
  financeReceiptApi: {
    detail: mocks.detail,
    receive: mocks.receive
  }
}))

function detail(overrides: Partial<FinanceReceiptDetail> = {}): FinanceReceiptDetail {
  return {
    reportId: 'ER-24',
    employeeUserId: 42,
    employeeName: 'Finance Employee',
    department: 'Operations',
    dateFrom: '2026-07-01',
    dateTo: '2026-07-15',
    description: 'Field travel',
    reportType: 'Reimbursement',
    erfReferenceNumber: 'ERF-2026-24',
    financeStatus: 'Pending',
    physicalReceiptsReceived: false,
    receivedDateUtc: null,
    rowVersion: 'row-version-1',
    receivedByUserId: null,
    receivedByName: '',
    remarks: '',
    ...overrides
  }
}

const passThrough = { template: '<div><slot /><slot name="append" /><slot name="subtitle" /></div>' }

function mountView() {
  return shallowMount(FinanceReceiptDetailView, {
    global: {
      stubs: {
        VAlert: { props: ['title'], template: '<div>{{ title }}<slot /></div>' },
        VBtn: { emits: ['click'], template: '<button v-bind="$attrs" @click="$emit(\'click\')"><slot /></button>' },
        VCard: passThrough,
        VCardText: passThrough,
        VCardTitle: passThrough,
        VCol: passThrough,
        VDialog: { props: ['modelValue'], template: '<div v-if="modelValue"><slot /></div>' },
        VRow: passThrough
      }
    }
  })
}

describe('Finance receipt detail', () => {
  beforeEach(() => {
    mocks.detail.mockReset().mockResolvedValue(detail())
    mocks.receive.mockReset()
    mocks.snackbarSuccess.mockReset()
    mocks.snackbarError.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('renders receipt monitoring without financial or expense-review sections', async () => {
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('.report-detail-shell').exists()).toBe(true)
    expect(wrapper.findComponent(AppReportAmountSummary).exists()).toBe(false)
    expect(wrapper.findComponent(AppExpenseReview).exists()).toBe(false)
    expect(wrapper.findComponent(AppReceiptList).exists()).toBe(false)
    expect(wrapper.findComponent(AppApprovalTrail).exists()).toBe(false)
    expect(wrapper.text()).not.toContain('Cash advance')
    expect(wrapper.text()).not.toContain('Total filed expenses')
    expect(wrapper.text()).not.toContain('Waiting for physical receipts')
    expect(wrapper.text()).toContain('Waiting for the employee to submit the physical documents to Finance.')
    expect(wrapper.getComponent(AppMobileBackNavigation).props()).toMatchObject({
      to: '/finance/receipts',
      label: 'Finance receipts',
      accessibleLabel: 'Back to Finance receipts'
    })
    expect(wrapper.find('.mobile-workflow-actions').text()).toContain('Mark receipts received')
  })

  it('shows completed receipt metadata without another workflow action', async () => {
    mocks.detail.mockResolvedValue(detail({
      financeStatus: 'Received',
      physicalReceiptsReceived: true,
      receivedByUserId: 9,
      receivedByName: 'Finance Officer',
      receivedDateUtc: '2026-07-16T06:00:00Z',
      remarks: 'Originals complete'
    }))
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Finance Officer')
    expect(wrapper.text()).toContain('Originals complete')
    expect(wrapper.text()).not.toContain('Physical receipts received')
    expect(wrapper.find('.mobile-workflow-actions').exists()).toBe(false)
  })

  it('reloads current Finance state after a stale receipt confirmation', async () => {
    mocks.receive.mockRejectedValue(new ApiError(409, 'Stale row version.'))
    const wrapper = mountView()
    await flushPromises()

    wrapper.getComponent(ReceiveReceiptsDialog).vm.$emit('submit', 'Complete')
    await flushPromises()

    expect(mocks.receive).toHaveBeenCalledWith('ER-24', 'Complete', 'row-version-1')
    expect(mocks.detail).toHaveBeenCalledTimes(2)
    expect(mocks.snackbarError).toHaveBeenCalledWith('This record changed. The latest data has been loaded.')
  })

})
