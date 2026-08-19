<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useSessionStore } from '@/app/stores/session'
import { ApiError } from '@/shared/api/client'
import AppApprovalTrail from '@/shared/components/AppApprovalTrail.vue'
import AppBreadcrumbs from '@/shared/components/AppBreadcrumbs.vue'
import AppDate from '@/shared/components/AppDate.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppExpenseReview from '@/shared/components/AppExpenseReview.vue'
import AppLoadingOverlay from '@/shared/components/AppLoadingOverlay.vue'
import AppMobileBackNavigation from '@/shared/components/AppMobileBackNavigation.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppReceiptList from '@/shared/components/AppReceiptList.vue'
import AppReceiptViewer from '@/shared/components/AppReceiptViewer.vue'
import AppReportAmountSummary from '@/shared/components/AppReportAmountSummary.vue'
import AppReportReviewHero from '@/shared/components/AppReportReviewHero.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import { useAsyncAction } from '@/shared/composables/useAsyncAction'
import { useReceiptPreview } from '@/shared/composables/useReceiptPreview'
import { useSnackbar } from '@/shared/composables/useSnackbar'
import { managerApprovalApi } from '@/features/manager-approvals/api'
import {
  hasDisplayText
} from '@/features/manager-approvals/detailPresentation'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'
import { canCurrentManagerAct } from '@/features/manager-approvals/workflowPresentation'
import ManagerApprovalDialogs from '@/features/manager-approvals/ManagerApprovalDialogs.vue'
import type { ManagerReportDetail } from '@/features/manager-approvals/types'

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
const canTakeAction = computed(() => report.value
  ? canCurrentManagerAct(report.value, session.user?.userId)
  : false)
const breadcrumbTitle = computed(() => report.value?.erfReferenceNumber.trim() || 'Report details')
const desktopSubtitle = computed(() => [
  report.value?.employeeName,
  report.value?.department
].filter(hasDisplayText).join(' · '))
const showSideColumn = computed(() => (report.value?.approvalTrail.length ?? 0) > 0)
const {
  open: previewOpen,
  openingAttachmentId,
  url: previewUrl,
  contentType: previewType,
  title: previewTitle,
  preview,
  requestClose: requestClosePreview,
  release: releasePreview,
  openExternally: openPreviewExternally,
  download: downloadPreview
} = useReceiptPreview(
  managerApprovalApi.attachment,
  message => snackbar.error(message)
)

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

onMounted(load)
</script>

<template>
  <section class="report-detail-shell">
    <AppBreadcrumbs
      v-if="mdAndUp"
      :items="[{ title: 'Manager approvals', to: '/manager/reports' }, { title: breadcrumbTitle }]"
    />
    <div
      class="report-detail-page position-relative"
      style="min-height: 160px"
    >
      <AppLoadingOverlay :loading="loading" />
      <AppErrorAlert :message="error || action.error.value" />
      <template v-if="report">
        <div :class="{ 'has-mobile-workflow-actions': !mdAndUp && canTakeAction }">
          <AppPageHeader
            v-if="mdAndUp"
            title="Expense report review"
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
            <AppMobileBackNavigation
              to="/manager/reports"
              label="Manager approvals"
              accessible-label="Back to Manager approvals"
            />
            <AppReportReviewHero
              eyebrow="Expense report"
              :employee-name="report.employeeName"
              :department="report.department"
              :reference="report.erfReferenceNumber"
            >
              <template #status>
                <AppStatusChip :status="report.status" />
              </template>
            </AppReportReviewHero>
          </template>
          <AppReportAmountSummary
            :expenses="report.expenses"
            :cash-advance="report.cashAdvance"
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
              <AppExpenseReview
                :expenses="report.expenses"
                :desktop="mdAndUp"
              />
              <AppReceiptList
                :attachments="report.attachments"
                :opening-attachment-id="openingAttachmentId"
                :desktop="mdAndUp"
                @open="preview"
              />
            </v-col>
            <v-col
              v-if="showSideColumn"
              cols="12"
              lg="4"
            >
              <AppApprovalTrail :items="report.approvalTrail" />
            </v-col>
          </v-row>
        </div>
      </template>
    </div>
  </section>
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
