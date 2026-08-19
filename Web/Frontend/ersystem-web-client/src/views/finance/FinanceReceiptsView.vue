<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppDate from '@/shared/components/AppDate.vue'
import AppDateTime from '@/shared/components/AppDateTime.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppFilterBar from '@/shared/components/AppFilterBar.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppRefreshButton from '@/shared/components/AppRefreshButton.vue'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import AppStickyQueueControls from '@/shared/components/AppStickyQueueControls.vue'
import { useServerTable } from '@/shared/composables/useServerTable'
import { reportTypes } from '@/shared/data/reportTypes'
import { financeReceiptApi } from '@/features/finance-receipts/api'
import { createFinanceReceiptFilters } from '@/features/finance-receipts/filters'
import type { FinanceReceiptFilters, FinanceReceiptListItem } from '@/features/finance-receipts/types'

const router = useRouter()
const headers = [
  { title: 'ERF reference', key: 'erfReferenceNumber' }, { title: 'Employee', key: 'employeeName' },
  { title: 'Report type', key: 'reportType' },
  { title: 'From', key: 'dateFrom' }, { title: 'Receipt state', key: 'physicalReceiptsReceived', sortable: false },
  { title: 'Received', key: 'receivedDateUtc', sortable: false }
]
const table = useServerTable<FinanceReceiptListItem, FinanceReceiptFilters>(financeReceiptApi.list, createFinanceReceiptFilters())
const receiptStates = [
  { title: 'All', value: undefined }, { title: 'Pending receipt', value: false }, { title: 'Received', value: true }
]
function reportPath(report: FinanceReceiptListItem) { return `/finance/receipts/${encodeURIComponent(report.reportId)}` }
function openReport(value: unknown) { void router.push(reportPath(value as FinanceReceiptListItem)) }
function clearFilters() {
  Object.assign(table.filters, createFinanceReceiptFilters())
  table.page.value = 1
  void table.load()
}
onMounted(table.load)
</script>

<template>
  <section class="queue-page-shell">
    <AppStickyQueueControls>
      <AppPageHeader
        title="Finance receipt reviewer"
        subtitle="Track physical documents for fully approved reports"
      >
        <AppRefreshButton
          :loading="table.loading.value"
          accessible-label="Refresh Finance receipts"
          @refresh="table.load"
        />
      </AppPageHeader>
      <AppFilterBar mobile-title="Receipt filters">
        <template #primary>
          <v-text-field
            v-model="table.filters.search"
            label="Employee, report, or reference"
            prepend-inner-icon="mdi-magnify"
            clearable
            @update:model-value="table.search"
          />
        </template>
        <template #filters>
          <v-select
            v-model="table.filters.physicalReceiptsReceived"
            label="Receipt state"
            :items="receiptStates"
            @update:model-value="table.search"
          />
          <v-select
            v-model="table.filters.reportType"
            label="Report type"
            :items="reportTypes"
            @update:model-value="table.search"
          />
          <v-text-field
            v-model="table.filters.dateFrom"
            label="Report from"
            type="date"
            @update:model-value="table.search"
          />
          <v-text-field
            v-model="table.filters.dateTo"
            label="Report to"
            type="date"
            @update:model-value="table.search"
          />
          <v-btn
            class="queue-filter-clear"
            variant="text"
            @click="clearFilters"
          >
            Clear
          </v-btn>
        </template>
      </AppFilterBar>
    </AppStickyQueueControls>
    <AppErrorAlert
      :message="table.error.value"
      class="mb-4"
    />
    <AppServerTable
      :headers="headers"
      :items="table.items.value"
      :total="table.total.value"
      :page="table.page.value"
      :loading="table.loading.value"
      :items-per-page="table.pageSize.value"
      @update-options="table.updateOptions"
      @click-row="openReport"
    >
      <template #item.dateFrom="{ item }">
        <AppDate :value="item.dateFrom" />
      </template>
      <template #item.physicalReceiptsReceived="{ item }">
        <AppStatusChip :status="item.physicalReceiptsReceived ? 'Received' : 'Pending'" />
      </template>
      <template #item.receivedDateUtc="{ item }">
        <AppDateTime :value="item.receivedDateUtc" />
      </template>
      <template #mobile-item="{ item }">
        <v-card
          class="queue-card border"
          variant="flat"
          :to="reportPath(item as FinanceReceiptListItem)"
        >
          <v-card-text>
            <div class="d-flex align-start justify-space-between ga-3">
              <div>
                <div class="text-h6 font-weight-bold">
                  {{ (item as FinanceReceiptListItem).employeeName }}
                </div>
                <div class="text-body-2 muted">
                  {{ (item as FinanceReceiptListItem).erfReferenceNumber || (item as FinanceReceiptListItem).reportId }}
                </div>
              </div>
              <AppStatusChip :status="(item as FinanceReceiptListItem).physicalReceiptsReceived ? 'Received' : 'Pending'" />
            </div>
            <v-divider class="my-3" />
            <div class="detail-grid">
              <div><span class="field-label">Report type</span>{{ (item as FinanceReceiptListItem).reportType || '—' }}</div>
              <div><span class="field-label">Report from</span><AppDate :value="(item as FinanceReceiptListItem).dateFrom" /></div>
              <div v-if="(item as FinanceReceiptListItem).receivedDateUtc">
                <span class="field-label">Received</span><AppDateTime :value="(item as FinanceReceiptListItem).receivedDateUtc" />
              </div>
            </div>
          </v-card-text>
        </v-card>
      </template>
    </AppServerTable>
  </section>
</template>
