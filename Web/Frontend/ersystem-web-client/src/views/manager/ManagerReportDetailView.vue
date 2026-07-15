<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useSessionStore } from '@/app/stores/session'
import { ApiError } from '@/shared/api/client'
import AppBreadcrumbs from '@/shared/components/AppBreadcrumbs.vue'
import AppDate from '@/shared/components/AppDate.vue'
import AppDateTime from '@/shared/components/AppDateTime.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppLoadingOverlay from '@/shared/components/AppLoadingOverlay.vue'
import AppMoney from '@/shared/components/AppMoney.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppReceiptViewer from '@/shared/components/AppReceiptViewer.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import { useAsyncAction } from '@/shared/composables/useAsyncAction'
import { useSnackbar } from '@/shared/composables/useSnackbar'
import { managerApprovalApi } from '@/features/manager-approvals/api'
import { calculateManagerAmounts, resolveExpenseAmount } from '@/features/manager-approvals/amounts'
import {
  createExpenseTableHeaders,
  formatReceiptSize,
  hasAmountSummary,
  hasCashAdvanceData,
  hasDisplayMoney,
  hasDisplayText,
  hasExpenseMetadata,
  receiptDisplayName
} from '@/features/manager-approvals/detailPresentation'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'
import { canCurrentManagerAct } from '@/features/manager-approvals/workflowPresentation'
import ManagerApprovalDialogs from '@/features/manager-approvals/ManagerApprovalDialogs.vue'
import type { ManagerReportDetail, ReceiptAttachment } from '@/features/manager-approvals/types'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const { mdAndUp, smAndDown } = useDisplay()
const snackbar = useSnackbar()
const managerQueue = useManagerReportsQueueStore()
const reportId = computed(() => String(route.params.reportId))
const report = ref<ManagerReportDetail | null>(null)
const loading = ref(false)
const error = ref('')
const approveOpen = ref(false)
const returnOpen = ref(false)
const action = useAsyncAction()
const previewOpen = ref(false)
const openingAttachmentId = ref<number | null>(null)
const previewUrl = ref('')
const previewType = ref('')
const previewTitle = ref('')
const amounts = computed(() => calculateManagerAmounts(report.value?.expenses ?? [], report.value?.cashAdvance ?? null))
const canTakeAction = computed(() => report.value
  ? canCurrentManagerAct(report.value, session.user?.userId)
  : false)
const isApproved = computed(() => report.value?.status.trim().toLowerCase() === 'approved')
const desktopSubtitle = computed(() => [report.value?.employeeName, report.value?.department].filter(hasDisplayText).join(' · '))
const expenseHeaders = computed(() => createExpenseTableHeaders(report.value?.expenses ?? []))
const showAmountSummary = computed(() => hasAmountSummary(report.value?.expenses ?? [], report.value?.cashAdvance ?? null))
const showCashAdvance = computed(() => hasCashAdvanceData(report.value?.cashAdvance ?? null))
const showSideColumn = computed(() => showCashAdvance.value || (report.value?.approvalTrail.length ?? 0) > 0)

async function load() {
  loading.value = true
  error.value = ''
  try { report.value = await managerApprovalApi.detail(reportId.value) }
  catch (caught) { error.value = caught instanceof Error ? caught.message : 'Unable to load this report.' }
  finally { loading.value = false }
}

async function approve() {
  if (!report.value || !canTakeAction.value) return
  try {
    const result = await action.run(() => managerApprovalApi.approve(reportId.value, report.value!.rowVersion))
    if (!result) return
    approveOpen.value = false
    managerQueue.applyWorkflowResult(result)
    snackbar.success('The report was approved.')
    await router.replace('/manager/reports')
  } catch (caught) { await handleActionFailure(caught) }
}

async function returnReport(reason: string) {
  if (!report.value || !canTakeAction.value) return
  try {
    const result = await action.run(() => managerApprovalApi.returnReport(reportId.value, reason, report.value!.rowVersion))
    if (!result) return
    returnOpen.value = false
    managerQueue.applyWorkflowResult(result)
    snackbar.success('The report was returned to the employee.')
    await router.replace('/manager/reports')
  } catch (caught) { await handleActionFailure(caught) }
}

async function handleActionFailure(caught: unknown) {
  if (caught instanceof ApiError && caught.status === 409) {
    snackbar.error('This report changed. The latest data has been loaded.')
    await load()
  } else snackbar.error(caught instanceof Error ? caught.message : 'The action failed.')
}

async function preview(attachment: ReceiptAttachment, index: number) {
  if (openingAttachmentId.value != null) return
  releasePreview()
  openingAttachmentId.value = attachment.id
  try {
    const blob = await managerApprovalApi.attachment(attachment.id)
    if (blob.size === 0) throw new Error('The receipt file is empty.')
    previewUrl.value = URL.createObjectURL(blob)
    previewType.value = blob.type || attachment.contentType || 'application/octet-stream'
    previewTitle.value = receiptDisplayName(attachment, index)
    previewOpen.value = true
  } catch (caught) { snackbar.error(caught instanceof Error ? caught.message : 'Unable to open the receipt.') }
  finally { openingAttachmentId.value = null }
}
function requestClosePreview() { previewOpen.value = false }
function releasePreview() {
  previewOpen.value = false
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = ''
  previewType.value = ''
  previewTitle.value = ''
}
function openPreviewExternally() {
  if (previewUrl.value) window.open(previewUrl.value, '_blank', 'noopener,noreferrer')
}
function downloadPreview() {
  if (!previewUrl.value) return
  const link = document.createElement('a')
  link.href = previewUrl.value
  link.download = previewTitle.value || 'scanned-receipt'
  link.rel = 'noopener'
  document.body.appendChild(link)
  link.click()
  link.remove()
}
onMounted(load)
onBeforeUnmount(releasePreview)
</script>

<template>
  <AppBreadcrumbs
    v-if="mdAndUp"
    :items="[{ title: 'Manager approvals', to: '/manager/reports' }, { title: reportId }]"
  />
  <div
    class="manager-detail-page position-relative"
    style="min-height: 160px"
  >
    <AppLoadingOverlay :loading="loading" />
    <AppErrorAlert :message="error || action.error.value" />
    <template v-if="report">
      <div :class="{ 'has-mobile-workflow-actions': !mdAndUp && canTakeAction }">
        <AppPageHeader
          v-if="mdAndUp"
          :title="`Report ${report.reportId}`"
          :subtitle="desktopSubtitle"
        >
          <div class="d-flex flex-wrap align-center ga-2">
            <AppStatusChip :status="report.status" />
            <template v-if="canTakeAction">
              <v-btn
                color="error"
                variant="outlined"
                prepend-icon="mdi-undo"
                @click="returnOpen = true"
              >
                Return
              </v-btn>
              <v-btn
                color="success"
                prepend-icon="mdi-check"
                @click="approveOpen = true"
              >
                Approve
              </v-btn>
            </template>
          </div>
        </AppPageHeader>
        <template v-else>
          <div class="manager-detail-mobile-nav">
            <v-btn
              icon="mdi-arrow-left"
              variant="text"
              to="/manager/reports"
              aria-label="Back to Manager approvals"
            />
            <span>Manager approvals</span>
          </div>
          <section class="manager-report-hero">
            <div class="d-flex align-center justify-space-between ga-3 mb-3">
              <span class="manager-report-hero__eyebrow">Expense report</span>
              <AppStatusChip :status="report.status" />
            </div>
            <h1
              v-if="hasDisplayText(report.employeeName)"
              class="manager-report-hero__title"
            >
              {{ report.employeeName }}
            </h1>
            <p
              v-if="hasDisplayText(report.department)"
              class="manager-report-hero__department"
            >
              {{ report.department }}
            </p>
            <div class="manager-report-hero__reference">
              {{ report.reportId }}
            </div>
          </section>
        </template>
        <v-card
          v-if="showAmountSummary"
          class="amount-summary border mb-4"
          variant="flat"
        >
          <v-card-text class="pa-0">
            <div class="amount-summary__primary">
              <span class="field-label">Combined grand total</span>
              <span class="amount-summary__total"><AppMoney :value="amounts.combinedTotal" /></span>
            </div>
            <v-divider />
            <div
              class="amount-summary__breakdown"
              :class="{ 'amount-summary__breakdown--single': !report.expenses.length || !hasDisplayMoney(report.cashAdvance?.amount) }"
            >
              <div
                v-if="report.expenses.length"
                class="amount-summary__metric"
              >
                <span class="field-label">Total filed expenses</span>
                <strong><AppMoney :value="amounts.filedExpenses" /></strong>
              </div>
              <div
                v-if="hasDisplayMoney(report.cashAdvance?.amount)"
                class="amount-summary__metric"
              >
                <span class="field-label">Cash advance</span>
                <strong><AppMoney :value="amounts.cashAdvanceAmount" /></strong>
              </div>
            </div>
          </v-card-text>
        </v-card>
        <v-alert
          v-if="isApproved"
          type="success"
          variant="tonal"
          icon="mdi-check-circle-outline"
          class="mb-4"
          title="Approval complete"
          text="This report is approved. No further action is required."
        />
        <v-row>
          <v-col
            cols="12"
            :lg="showSideColumn ? 8 : 12"
          >
            <v-card
              title="Report summary"
              class="mb-4 border"
              variant="flat"
            >
              <v-card-text class="detail-grid">
                <div>
                  <span class="field-label">Approval step</span>{{ report.currentStep }} of {{ report.totalSteps }}
                </div>
                <div v-if="hasDisplayText(report.reportType)">
                  <span class="field-label">Report type</span>{{ report.reportType }}
                </div>
                <div v-if="hasDisplayText(report.erfReferenceNumber)">
                  <span class="field-label">ERF reference</span>{{ report.erfReferenceNumber }}
                </div>
                <div v-if="hasDisplayText(report.dateFrom)">
                  <span class="field-label">From</span><AppDate :value="report.dateFrom" />
                </div>
                <div v-if="hasDisplayText(report.dateTo)">
                  <span class="field-label">To</span><AppDate :value="report.dateTo" />
                </div>
                <div
                  v-if="hasDisplayText(report.description)"
                  class="grid-wide"
                >
                  <span class="field-label">Description</span>{{ report.description }}
                </div>
              </v-card-text>
            </v-card>
            <v-card
              v-if="report.expenses.length"
              title="Expenses"
              class="mb-4 border"
              variant="flat"
            >
              <v-data-table
                v-if="mdAndUp"
                :headers="expenseHeaders"
                :items="report.expenses"
                density="comfortable"
              >
                <template #item.transactionDate="{ item }">
                  <AppDate :value="item.transactionDate" />
                </template>
                <template #item.amount="{ item }">
                  <strong class="text-primary"><AppMoney :value="resolveExpenseAmount(item)" /></strong>
                </template>
              </v-data-table>
              <v-card-text
                v-else
                class="expense-mobile-list"
              >
                <v-card
                  v-for="expense in report.expenses"
                  :key="expense.id ?? `${expense.transactionDate}-${expense.particulars}`"
                  class="border"
                  variant="flat"
                >
                  <v-card-text>
                    <div class="d-flex align-start justify-space-between ga-3">
                      <div v-if="hasDisplayText(expense.particulars) || hasDisplayText(expense.category)">
                        <div
                          v-if="hasDisplayText(expense.particulars)"
                          class="font-weight-bold"
                        >
                          {{ expense.particulars }}
                        </div>
                        <div
                          v-if="hasDisplayText(expense.category)"
                          class="text-caption muted"
                        >
                          {{ expense.category }}
                        </div>
                      </div>
                      <span class="expense-card__amount"><AppMoney :value="resolveExpenseAmount(expense)" /></span>
                    </div>
                    <v-divider
                      v-if="hasExpenseMetadata(expense)"
                      class="my-3"
                    />
                    <div
                      v-if="hasExpenseMetadata(expense)"
                      class="detail-grid"
                    >
                      <div v-if="hasDisplayText(expense.transactionDate)">
                        <span class="field-label">Date</span><AppDate :value="expense.transactionDate" />
                      </div>
                      <div v-if="hasDisplayText(expense.location)">
                        <span class="field-label">Location</span>{{ expense.location }}
                      </div>
                      <div
                        v-if="hasDisplayText(expense.remarks)"
                        class="grid-wide"
                      >
                        <span class="field-label">Remarks</span>{{ expense.remarks }}
                      </div>
                    </div>
                  </v-card-text>
                </v-card>
              </v-card-text>
            </v-card>
            <v-card
              v-if="report.attachments.length"
              title="Scanned receipts"
              class="mb-4 border"
              variant="flat"
            >
              <v-list>
                <v-list-item
                  v-for="(attachment, index) in report.attachments"
                  :key="attachment.id"
                  class="receipt-list-item"
                  :title="receiptDisplayName(attachment, index)"
                  :subtitle="[formatReceiptSize(attachment.fileSizeBytes), !mdAndUp ? 'Tap to open' : ''].filter(Boolean).join(' · ')"
                  prepend-icon="mdi-paperclip"
                  :aria-label="`Open ${receiptDisplayName(attachment, index)}`"
                  :disabled="openingAttachmentId != null"
                  tag="button"
                  type="button"
                  link
                  @click="preview(attachment, index)"
                >
                  <template #append>
                    <v-progress-circular
                      v-if="openingAttachmentId === attachment.id"
                      indeterminate
                      color="primary"
                      size="22"
                      width="2"
                      :aria-label="`Opening ${receiptDisplayName(attachment, index)}`"
                    />
                    <span
                      v-else
                      class="receipt-list-item__action"
                      aria-hidden="true"
                    >
                      <span v-if="mdAndUp">Open</span>
                      <v-icon icon="mdi-chevron-right" />
                    </span>
                  </template>
                </v-list-item>
              </v-list>
            </v-card>
          </v-col>
          <v-col
            v-if="showSideColumn"
            cols="12"
            lg="4"
          >
            <v-card
              v-if="showCashAdvance && report.cashAdvance"
              title="Cash advance"
              class="mb-4 border"
              variant="flat"
            >
              <v-card-text class="detail-stack">
                <div v-if="hasDisplayMoney(report.cashAdvance.amount)">
                  <span class="field-label">Amount</span><AppMoney :value="report.cashAdvance.amount" />
                </div>
                <div v-if="hasDisplayText(report.cashAdvance.date)">
                  <span class="field-label">Date</span>{{ report.cashAdvance.date }}
                </div>
                <div v-if="hasDisplayText(report.cashAdvance.referenceNumber)">
                  <span class="field-label">Reference</span>{{ report.cashAdvance.referenceNumber }}
                </div>
                <div v-if="hasDisplayText(report.cashAdvance.referenceDocument)">
                  <span class="field-label">Document</span>{{ report.cashAdvance.referenceDocument }}
                </div>
                <div v-if="hasDisplayText(report.cashAdvance.revolvingFund)">
                  <span class="field-label">Revolving fund</span>{{ report.cashAdvance.revolvingFund }}
                </div>
              </v-card-text>
            </v-card>
            <v-card
              v-if="report.approvalTrail.length"
              title="Approval trail"
              class="border"
              variant="flat"
            >
              <v-list lines="two">
                <v-list-item
                  v-for="step in report.approvalTrail"
                  :key="`${step.sort}-${step.approverUserId}`"
                  :title="`${step.sort}. ${step.approverName}`"
                >
                  <template #subtitle>
                    <AppStatusChip :status="step.status" /> <AppDateTime
                      v-if="step.occurredAtUtc"
                      :value="step.occurredAtUtc"
                    />
                  </template>
                </v-list-item>
              </v-list>
            </v-card>
          </v-col>
        </v-row>
      </div>
    </template>
  </div>
  <ManagerApprovalDialogs
    v-if="canTakeAction"
    v-model:approve-open="approveOpen"
    v-model:return-open="returnOpen"
    :loading="action.loading.value"
    @approve="approve"
    @return-report="returnReport"
  />
  <v-dialog
    v-model="previewOpen"
    max-width="1000"
    :fullscreen="smAndDown"
    @after-leave="releasePreview"
  >
    <v-card class="receipt-preview-dialog">
      <v-card-title class="d-flex align-center ga-3">
        <span class="receipt-preview-dialog__title">{{ previewTitle }}</span><v-spacer /><v-btn
          icon="mdi-close"
          variant="text"
          aria-label="Close receipt preview"
          @click="requestClosePreview"
        />
      </v-card-title>
      <v-card-text>
        <AppReceiptViewer
          v-if="previewUrl"
          :url="previewUrl"
          :content-type="previewType"
          :title="previewTitle"
          @open-external="openPreviewExternally"
          @download="downloadPreview"
        />
      </v-card-text>
    </v-card>
  </v-dialog>
  <div
    v-if="report && !mdAndUp && canTakeAction"
    class="mobile-workflow-actions"
  >
    <v-btn
      color="error"
      variant="outlined"
      prepend-icon="mdi-undo"
      @click="returnOpen = true"
    >
      Return
    </v-btn>
    <v-btn
      color="success"
      prepend-icon="mdi-check"
      @click="approveOpen = true"
    >
      Approve
    </v-btn>
  </div>
</template>
