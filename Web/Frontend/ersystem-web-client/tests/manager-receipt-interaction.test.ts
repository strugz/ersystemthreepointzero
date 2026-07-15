import { flushPromises, shallowMount } from '@vue/test-utils'
import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import ManagerReportDetailView from '@/views/manager/ManagerReportDetailView.vue'
import type { ManagerReportDetail } from '@/features/manager-approvals/types'

const mocks = vi.hoisted(() => ({
  detail: vi.fn(),
  attachment: vi.fn(),
  snackbarError: vi.fn(),
  routerReplace: vi.fn()
}))

vi.mock('vuetify', () => ({
  useDisplay: () => ({ mdAndUp: ref(false), smAndDown: ref(true) })
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { reportId: 'ER-1' } }),
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

function report(): ManagerReportDetail {
  return {
    reportId: 'ER-1',
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
    approvalTrail: [{ approverUserId: 7, approverName: 'Manager', sort: 1, occurredAtUtc: null, status: 'Pending' }]
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

  it('opens the full receipt row and exposes external and download actions', async () => {
    mocks.attachment.mockResolvedValue(new Blob(['pdf'], { type: 'application/pdf' }))
    const anchorClick = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)
    const wrapper = mountView()
    await flushPromises()

    const receiptRow = wrapper.get('button[aria-label="Open Invoice.pdf"]')
    expect(receiptRow.element.tagName).toBe('BUTTON')
    expect(receiptRow.attributes('type')).toBe('button')
    await receiptRow.trigger('click')
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

    await wrapper.get('button[aria-label="Open Invoice.pdf"]').trigger('click')
    await flushPromises()

    expect(mocks.snackbarError).toHaveBeenCalledWith('The receipt file is empty.')
    expect(wrapper.find('[data-test="receipt-viewer"]').exists()).toBe(false)
  })

  it('surfaces secured endpoint failures without replacing the report', async () => {
    mocks.attachment.mockRejectedValue(new Error('You do not have access to this receipt.'))
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('button[aria-label="Open Invoice.pdf"]').trigger('click')
    await flushPromises()

    expect(mocks.snackbarError).toHaveBeenCalledWith('You do not have access to this receipt.')
    expect(wrapper.find('button[aria-label="Open Photo.png"]').exists()).toBe(true)
  })

  it('revokes the previous object URL when another receipt opens and on unmount', async () => {
    mocks.attachment
      .mockResolvedValueOnce(new Blob(['pdf'], { type: 'application/pdf' }))
      .mockResolvedValueOnce(new Blob(['image'], { type: 'image/png' }))
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('button[aria-label="Open Invoice.pdf"]').trigger('click')
    await flushPromises()
    await wrapper.get('button[aria-label="Open Photo.png"]').trigger('click')
    await flushPromises()

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:application/pdf')
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:image/png')
  })
})
