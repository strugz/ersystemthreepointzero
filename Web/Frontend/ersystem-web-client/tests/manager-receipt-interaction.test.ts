import { flushPromises, shallowMount } from '@vue/test-utils'
import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import ManagerReportDetailView from '@/views/manager/ManagerReportDetailView.vue'
import AppExpenseReview from '@/shared/components/AppExpenseReview.vue'
import AppMobileBackNavigation from '@/shared/components/AppMobileBackNavigation.vue'
import AppReceiptList from '@/shared/components/AppReceiptList.vue'
import AppReportAmountSummary from '@/shared/components/AppReportAmountSummary.vue'
import type { ExpenseLine, ManagerReportDetail } from '@/features/manager-approvals/types'

const mocks = vi.hoisted(() => ({
  routeReportId: '24fce0c6-0c63-4973-994d-be5af93b2610',
  detail: vi.fn(),
  attachment: vi.fn(),
  snackbarError: vi.fn(),
  routerReplace: vi.fn()
}))

vi.mock('vuetify', () => ({
  useDisplay: () => ({ mdAndUp: ref(false), smAndDown: ref(true) })
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { reportId: mocks.routeReportId } }),
  useRouter: () => ({ replace: mocks.routerReplace })
}))

vi.mock('@/app/stores/session', () => ({
  useSessionStore: () => ({ user: { userId: 7 } })
}))

vi.mock('@/features/manager-approvals/queueStore', () => ({
  useManagerReportsQueueStore: () => ({ applyWorkflowResult: vi.fn() })
}))

vi.mock('@/shared/composables/useSnackbar', () => ({
  useSnackbar: () => ({ success: vi.fn(), error: mocks.snackbarError })
}))

vi.mock('@/features/manager-approvals/api', () => ({
  managerApprovalApi: {
    detail: mocks.detail,
    attachment: mocks.attachment,
    approve: vi.fn(),
    returnReport: vi.fn()
  }
}))

function report(overrides: Partial<ManagerReportDetail> = {}): ManagerReportDetail {
  return {
    reportId: mocks.routeReportId,
    employeeUserId: 1,
    employeeName: 'Employee',
    department: '',
    dateFrom: null,
    dateTo: null,
    description: '',
    reportType: '',
    currentStep: 1,
    totalSteps: 1,
    status: 'For Approval',
    rowVersion: 'version',
    erfReferenceNumber: '',
    expenses: [],
    cashAdvance: null,
    attachments: [
      { id: 10, fileName: 'Invoice.pdf', contentType: 'application/pdf', fileSizeBytes: 200, createdDateUtc: '' },
      { id: 11, fileName: 'Photo.png', contentType: 'image/png', fileSizeBytes: 300, createdDateUtc: '' }
    ],
    approvalTrail: [{ approverUserId: 7, approverName: 'Manager', sort: 1, occurredAtUtc: null, status: 'Pending' }],
    ...overrides
  }
}

function expense(overrides: Partial<ExpenseLine> = {}): ExpenseLine {
  return {
    id: 5, transactionDate: null, isPerDiem: false, particulars: 'Taxi', invoiceNumber: '', multiplier: null,
    expenseType: '', category: '', amount: 100, vatAmount: null, totalAmount: 100, location: '', remarks: '',
    workWith: '', serviceNumber: '', instrument: '', serialNumber: '', minusDays: '', totalDays: '',
    computation: '', ...overrides
  }
}

const passThrough = { template: '<div><slot /><slot name="append" /><slot name="subtitle" /></div>' }
const receiptViewerStub = {
  name: 'AppReceiptViewer',
  emits: ['openExternal', 'download'],
  template: '<div data-test="receipt-viewer"><button data-test="external" @click="$emit(\'openExternal\')" /><button data-test="download" @click="$emit(\'download\')" /></div>'
}

function mountView() {
  return shallowMount(ManagerReportDetailView, {
    global: {
      stubs: {
        VCard: passThrough,
        VCardText: passThrough,
        VCardTitle: passThrough,
        VRow: passThrough,
        VCol: passThrough,
        VList: passThrough,
        VExpansionPanels: {
          name: 'VExpansionPanels',
          props: {
            modelValue: { type: Array, default: () => [] },
            multiple: { type: Boolean, default: false }
          },
          template: '<div><slot /></div>'
        },
        VListItem: {
          emits: ['click'],
          template: '<button v-bind="$attrs" @click="$emit(\'click\')"><slot /><slot name="append" /></button>'
        },
        VDialog: { props: ['modelValue'], template: '<div v-if="modelValue"><slot /></div>' },
        VBtn: { emits: ['click'], template: '<button v-bind="$attrs" @click="$emit(\'click\')"><slot /></button>' },
        AppReceiptViewer: receiptViewerStub
      }
    }
  })
}

describe('Manager receipt interaction', () => {
  beforeEach(() => {
    mocks.detail.mockReset().mockResolvedValue(report())
    mocks.attachment.mockReset()
    mocks.snackbarError.mockReset()
    mocks.routerReplace.mockReset()
    vi.stubGlobal('open', vi.fn())
    vi.stubGlobal('URL', {
      createObjectURL: vi.fn((blob: Blob) => `blob:${blob.type}`),
      revokeObjectURL: vi.fn()
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('provides the mobile Back row as an accessible in-flow navigation landmark', async () => {
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('.report-detail-shell').exists()).toBe(true)
    const navigation = wrapper.getComponent(AppMobileBackNavigation)
    expect(navigation.props()).toMatchObject({
      to: '/manager/reports',
      label: 'Manager approvals',
      accessibleLabel: 'Back to Manager approvals'
    })
  })

  it('shows the ERF reference and settlement summary without exposing the route UUID or a cash card', async () => {
    mocks.detail.mockResolvedValue(report({
      erfReferenceNumber: 'ER-1430820260602-165400',
      cashAdvance: { amount: 500, date: '2026-07-15', referenceDocument: 'Voucher', referenceNumber: 'CA-1', revolvingFund: '' }
    }))
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('ER-1430820260602-165400')
    expect(wrapper.text()).not.toContain(mocks.routeReportId)
    const amountSummary = wrapper.getComponent(AppReportAmountSummary)
    expect(amountSummary.props('expenses')).toEqual([])
    expect(amountSummary.props('cashAdvance')).toEqual({
      amount: 500,
      date: '2026-07-15',
      referenceDocument: 'Voucher',
      referenceNumber: 'CA-1',
      revolvingFund: ''
    })
    expect(wrapper.find('[title="Cash advance"]').exists()).toBe(false)
  })

  it('uses the status chip without a duplicate approved-state banner', async () => {
    mocks.detail.mockResolvedValue(report({ status: 'Approved' }))
    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).not.toContain('Approval complete')
    expect(wrapper.text()).not.toContain('No further action is required')
  })

  it('opens the first mobile expense while allowing multiple panels', async () => {
    const expenses = [expense(), expense({ id: 6, particulars: 'Meal' })]
    const wrapper = shallowMount(AppExpenseReview, {
      props: { expenses, desktop: false },
      global: {
        stubs: {
          VCard: passThrough,
          VCardTitle: passThrough,
          VChip: passThrough,
          VExpansionPanels: {
            name: 'VExpansionPanels',
            props: {
              modelValue: { type: Array, default: () => [] },
              multiple: { type: Boolean, default: false }
            },
            template: '<div><slot /></div>'
          }
        }
      }
    })

    const panels = wrapper.getComponent({ name: 'VExpansionPanels' })
    expect(panels.props('modelValue')).toEqual(['expense-id-5'])
    expect(panels.props('multiple')).toBe(true)
  })

  it('opens the full receipt row and exposes external and download actions', async () => {
    mocks.attachment.mockResolvedValue(new Blob(['pdf'], { type: 'application/pdf' }))
    const anchorClick = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)
    const wrapper = mountView()
    await flushPromises()

    const receiptList = wrapper.getComponent(AppReceiptList)
    receiptList.vm.$emit('open', report().attachments[0], 0)
    await flushPromises()

    expect(mocks.attachment).toHaveBeenCalledWith(10)
    expect(wrapper.find('[data-test="receipt-viewer"]').exists()).toBe(true)
    await wrapper.get('[data-test="external"]').trigger('click')
    await wrapper.get('[data-test="download"]').trigger('click')
    expect(window.open).toHaveBeenCalledWith('blob:application/pdf', '_blank', 'noopener,noreferrer')
    expect(anchorClick).toHaveBeenCalledOnce()
  })

  it('rejects an empty receipt without opening the viewer', async () => {
    mocks.attachment.mockResolvedValue(new Blob([], { type: 'application/pdf' }))
    const wrapper = mountView()
    await flushPromises()

    wrapper.getComponent(AppReceiptList).vm.$emit('open', report().attachments[0], 0)
    await flushPromises()

    expect(mocks.snackbarError).toHaveBeenCalledWith('The receipt file is empty.')
    expect(wrapper.find('[data-test="receipt-viewer"]').exists()).toBe(false)
  })

  it('surfaces secured endpoint failures without replacing the report', async () => {
    mocks.attachment.mockRejectedValue(new Error('You do not have access to this receipt.'))
    const wrapper = mountView()
    await flushPromises()

    const receiptList = wrapper.getComponent(AppReceiptList)
    receiptList.vm.$emit('open', report().attachments[0], 0)
    await flushPromises()

    expect(mocks.snackbarError).toHaveBeenCalledWith('You do not have access to this receipt.')
    expect(receiptList.props('attachments')).toHaveLength(2)
  })

  it('revokes the previous object URL when another receipt opens and on unmount', async () => {
    mocks.attachment
      .mockResolvedValueOnce(new Blob(['pdf'], { type: 'application/pdf' }))
      .mockResolvedValueOnce(new Blob(['image'], { type: 'image/png' }))
    const wrapper = mountView()
    await flushPromises()

    const receiptList = wrapper.getComponent(AppReceiptList)
    receiptList.vm.$emit('open', report().attachments[0], 0)
    await flushPromises()
    receiptList.vm.$emit('open', report().attachments[1], 1)
    await flushPromises()

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:application/pdf')
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:image/png')
  })
})
