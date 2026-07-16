<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import { ApiError } from '@/shared/api/client'
import AppBreadcrumbs from '@/shared/components/AppBreadcrumbs.vue'
import AppDate from '@/shared/components/AppDate.vue'
import AppDateTime from '@/shared/components/AppDateTime.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppLoadingOverlay from '@/shared/components/AppLoadingOverlay.vue'
import AppMobileBackNavigation from '@/shared/components/AppMobileBackNavigation.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppReportReviewHero from '@/shared/components/AppReportReviewHero.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import { useAsyncAction } from '@/shared/composables/useAsyncAction'
import { useSnackbar } from '@/shared/composables/useSnackbar'
import { hasDisplayText } from '@/shared/utils/reportReview'
import { financeReceiptApi } from '@/features/finance-receipts/api'
import ReceiveReceiptsDialog from '@/features/finance-receipts/ReceiveReceiptsDialog.vue'
import type { FinanceReceiptDetail } from '@/features/finance-receipts/types'

const route = useRoute()
const { mdAndUp } = useDisplay()
const snackbar = useSnackbar()
const reportId = computed(() => String(route.params.reportId))
const report = ref<FinanceReceiptDetail | null>(null)
const reportLabel = computed(() => report.value?.erfReferenceNumber.trim() || 'Receipt monitoring')
const desktopSubtitle = computed(() => [
  report.value?.employeeName,
  report.value?.department
]
  .filter(hasDisplayText).join(' · '))
const receiptStatus = computed(() => report.value?.physicalReceiptsReceived ? 'Received' : 'Pending')
const loading = ref(false)
const error = ref('')
const receiveOpen = ref(false)
const action = useAsyncAction()

async function load() {
  loading.value = true
  error.value = ''
  try { report.value = await financeReceiptApi.detail(reportId.value) }
  catch (caught) { error.value = caught instanceof Error ? caught.message : 'Unable to load this receipt record.' }
  finally { loading.value = false }
}

async function receive(remarks: string) {
  if (!report.value) return
  try {
    await action.run(() => financeReceiptApi.receive(reportId.value, remarks, report.value!.rowVersion))
    receiveOpen.value = false
    snackbar.success('Physical receipts were marked as received.')
    await load()
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 409) {
      snackbar.error('This record changed. The latest data has been loaded.')
      receiveOpen.value = false
      await load()
    } else snackbar.error(caught instanceof Error ? caught.message : 'The action failed.')
  }
}

onMounted(load)
</script>

<template>
  <section class="report-detail-shell">
    <AppBreadcrumbs
      v-if="mdAndUp"
      :items="[{ title: 'Finance receipts', to: '/finance/receipts' }, { title: reportLabel }]"
    />
    <div
      class="report-detail-page position-relative"
      style="min-height: 160px"
    >
      <AppLoadingOverlay :loading="loading" />
      <AppErrorAlert :message="error || action.error.value" />
      <template v-if="report">
        <div :class="{ 'has-mobile-workflow-actions': !mdAndUp && !report.physicalReceiptsReceived }">
          <AppPageHeader
            v-if="mdAndUp"
            title="Physical receipt monitoring"
            :subtitle="desktopSubtitle"
          >
            <v-btn
              v-if="!report.physicalReceiptsReceived"
              color="primary"
              prepend-icon="mdi-inbox-arrow-down"
              @click="receiveOpen = true"
            >
              Mark physical receipts received
            </v-btn>
          </AppPageHeader>
          <template v-else>
            <AppMobileBackNavigation
              to="/finance/receipts"
              label="Finance receipts"
              accessible-label="Back to Finance receipts"
            />
            <AppReportReviewHero
              eyebrow="Receipt monitoring"
              :employee-name="report.employeeName"
              :department="report.department"
              :reference="report.erfReferenceNumber"
            >
              <template #status>
                <AppStatusChip :status="receiptStatus" />
              </template>
            </AppReportReviewHero>
          </template>

          <v-row>
            <v-col
              cols="12"
              md="7"
            >
              <v-card
                title="Report identification"
                class="mb-4 border"
                variant="flat"
              >
                <v-card-text class="detail-grid">
                  <div v-if="hasDisplayText(report.erfReferenceNumber)">
                    <span class="field-label">ERF reference</span>{{ report.erfReferenceNumber }}
                  </div>
                  <div v-if="hasDisplayText(report.reportType)">
                    <span class="field-label">Report type</span>{{ report.reportType }}
                  </div>
                  <div>
                    <span class="field-label">Employee ID</span>{{ report.employeeUserId }}
                  </div>
                  <div>
                    <span class="field-label">From</span><AppDate :value="report.dateFrom" />
                  </div>
                  <div>
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
            </v-col>

            <v-col
              cols="12"
              md="5"
            >
              <v-card
                title="Physical receipt tracking"
                class="border"
                variant="flat"
              >
                <v-card-text class="detail-stack">
                  <div v-if="mdAndUp">
                    <span class="field-label">Receipt state</span>
                    <AppStatusChip :status="receiptStatus" />
                  </div>
                  <template v-if="report.physicalReceiptsReceived">
                    <div v-if="hasDisplayText(report.receivedByName)">
                      <span class="field-label">Received by</span>{{ report.receivedByName }}
                    </div>
                    <div v-if="report.receivedDateUtc">
                      <span class="field-label">Received date</span><AppDateTime :value="report.receivedDateUtc" />
                    </div>
                    <div v-if="hasDisplayText(report.remarks)">
                      <span class="field-label">Remarks</span>{{ report.remarks }}
                    </div>
                  </template>
                  <p
                    v-else
                    class="muted ma-0"
                  >
                    Waiting for the employee to submit the physical documents to Finance.
                  </p>
                </v-card-text>
              </v-card>
            </v-col>
          </v-row>
        </div>
      </template>
    </div>
  </section>

  <ReceiveReceiptsDialog
    v-model="receiveOpen"
    :loading="action.loading.value"
    @submit="receive"
  />

  <div
    v-if="report && !report.physicalReceiptsReceived && !mdAndUp"
    class="mobile-workflow-actions mobile-workflow-actions--single"
  >
    <v-btn
      color="primary"
      prepend-icon="mdi-inbox-arrow-down"
      @click="receiveOpen = true"
    >
      Mark receipts received
    </v-btn>
  </div>
</template>
