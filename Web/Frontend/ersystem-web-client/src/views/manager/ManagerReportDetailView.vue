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
const previewUrl = ref('')
const previewType = ref('')
const previewTitle = ref('')
const amounts = computed(() => calculateManagerAmounts(report.value?.expenses ?? [], report.value?.cashAdvance ?? null))
const canTakeAction = computed(() => report.value
  ? canCurrentManagerAct(report.value, session.user?.userId)
  : false)
const isApproved = computed(() => report.value?.status.trim().toLowerCase() === 'approved')

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

async function preview(attachment: ReceiptAttachment) {
  closePreview()
  try {
    const blob = await managerApprovalApi.attachment(attachment.id)
    previewUrl.value = URL.createObjectURL(blob)
    previewType.value = attachment.contentType || blob.type
    previewTitle.value = attachment.fileName
    previewOpen.value = true
  } catch (caught) { snackbar.error(caught instanceof Error ? caught.message : 'Unable to open the receipt.') }
}
function closePreview() {
  previewOpen.value = false
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = ''
}
onMounted(load)
onBeforeUnmount(closePreview)
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
          :subtitle="`${report.employeeName} · ${report.department}`"
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
            <h1 class="manager-report-hero__title">
              {{ report.employeeName }}
            </h1>
            <p class="manager-report-hero__department">
              {{ report.department || 'No department' }}
            </p>
            <div class="manager-report-hero__reference">
              {{ report.reportId }}
            </div>
          </section>
        </template>
        <v-card
          class="amount-summary border mb-4"
          variant="flat"
        >
          <v-card-text class="pa-0">
            <div class="amount-summary__primary">
              <span class="field-label">Combined grand total</span>
              <span class="amount-summary__total"><AppMoney :value="amounts.combinedTotal" /></span>
            </div>
            <v-divider />
            <div class="amount-summary__breakdown">
              <div class="amount-summary__metric">
                <span class="field-label">Total filed expenses</span>
                <strong><AppMoney :value="amounts.filedExpenses" /></strong>
              </div>
              <div class="amount-summary__metric">
                <span class="field-label">Cash advance</span>
                <strong v-if="report.cashAdvance?.amount != null"><AppMoney :value="amounts.cashAdvanceAmount" /></strong>
                <span v-else>—</span>
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
            lg="8"
          >
            <v-card
              title="Report summary"
              class="mb-4 border"
              variant="flat"
            >
              <v-card-text class="detail-grid">
                <div><span class="field-label">Approval step</span>{{ report.currentStep }} of {{ report.totalSteps }}</div>
                <div><span class="field-label">Report type</span>{{ report.reportType || '—' }}</div>
                <div><span class="field-label">ERF reference</span>{{ report.erfReferenceNumber || '—' }}</div>
                <div><span class="field-label">From</span><AppDate :value="report.dateFrom" /></div>
                <div><span class="field-label">To</span><AppDate :value="report.dateTo" /></div>
                <div class="grid-wide">
                  <span class="field-label">Description</span>{{ report.description || '—' }}
                </div>
              </v-card-text>
            </v-card>
            <v-card
              title="Expenses"
              class="mb-4 border"
              variant="flat"
            >
              <v-data-table
                v-if="mdAndUp"
                :headers="[
                  { title: 'Date', key: 'transactionDate' }, { title: 'Particulars', key: 'particulars' },
                  { title: 'Category', key: 'category' }, { title: 'Amount', key: 'amount', align: 'end' }
                ]"
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
                v-else-if="report.expenses.length"
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
                      <div>
                        <div class="font-weight-bold">
                          {{ expense.particulars || 'Expense item' }}
                        </div>
                        <div class="text-caption muted">
                          {{ expense.category || 'Uncategorized' }}
                        </div>
                      </div>
                      <span class="expense-card__amount"><AppMoney :value="resolveExpenseAmount(expense)" /></span>
                    </div>
                    <v-divider class="my-3" />
                    <div class="detail-grid">
                      <div><span class="field-label">Date</span><AppDate :value="expense.transactionDate" /></div>
                      <div><span class="field-label">Location</span>{{ expense.location || '—' }}</div>
                      <div
                        v-if="expense.remarks"
                        class="grid-wide"
                      >
                        <span class="field-label">Remarks</span>{{ expense.remarks }}
                      </div>
                    </div>
                  </v-card-text>
                </v-card>
              </v-card-text>
              <v-card-text
                v-else
                class="muted"
              >
                No expense items were filed.
              </v-card-text>
            </v-card>
            <v-card
              title="Scanned receipts"
              class="mb-4 border"
              variant="flat"
            >
              <v-list v-if="report.attachments.length">
                <v-list-item
                  v-for="attachment in report.attachments"
                  :key="attachment.id"
                  :title="attachment.fileName"
                  :subtitle="`${Math.ceil(attachment.fileSizeBytes / 1024)} KB`"
                  prepend-icon="mdi-paperclip"
                >
                  <template #append>
                    <v-btn
                      variant="text"
                      :icon="mdAndUp ? undefined : 'mdi-eye'"
                      :prepend-icon="mdAndUp ? 'mdi-eye' : undefined"
                      :aria-label="`Preview ${attachment.fileName}`"
                      @click="preview(attachment)"
                    >
                      <span v-if="mdAndUp">Preview</span>
                    </v-btn>
                  </template>
                </v-list-item>
              </v-list>
              <v-card-text
                v-else
                class="muted"
              >
                No scanned receipts are attached.
              </v-card-text>
            </v-card>
          </v-col>
          <v-col
            cols="12"
            lg="4"
          >
            <v-card
              title="Cash advance"
              class="mb-4 border"
              variant="flat"
            >
              <v-card-text
                v-if="report.cashAdvance"
                class="detail-stack"
              >
                <div><span class="field-label">Amount</span><AppMoney :value="report.cashAdvance.amount" /></div>
                <div><span class="field-label">Reference</span>{{ report.cashAdvance.referenceNumber || '—' }}</div>
                <div><span class="field-label">Document</span>{{ report.cashAdvance.referenceDocument || '—' }}</div>
              </v-card-text>
              <v-card-text
                v-else
                class="muted"
              >
                No cash advance recorded.
              </v-card-text>
            </v-card>
            <v-card
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
    @after-leave="closePreview"
  >
    <v-card>
      <v-card-title class="d-flex align-center">
        <span>{{ previewTitle }}</span><v-spacer /><v-btn
          icon="mdi-close"
          variant="text"
          @click="closePreview"
        />
      </v-card-title>
      <v-card-text>
        <AppReceiptViewer
          v-if="previewUrl"
          :url="previewUrl"
          :content-type="previewType"
          :title="previewTitle"
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
