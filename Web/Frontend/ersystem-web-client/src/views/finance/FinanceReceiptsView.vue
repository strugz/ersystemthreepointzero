<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppDate from '@/shared/components/AppDate.vue'
import AppDateTime from '@/shared/components/AppDateTime.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppFilterBar from '@/shared/components/AppFilterBar.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import { useServerTable } from '@/shared/composables/useServerTable'
import { financeReceiptApi } from '@/features/finance-receipts/api'
import type { FinanceReceiptFilters, FinanceReceiptListItem } from '@/features/finance-receipts/types'

const router = useRouter()
const headers = [
  { title: 'ERF reference', key: 'erfReferenceNumber' }, { title: 'Employee', key: 'employeeName' },
  { title: 'Report type', key: 'reportType' },
  { title: 'From', key: 'dateFrom' }, { title: 'Receipt state', key: 'physicalReceiptsReceived', sortable: false },
  { title: 'Received', key: 'receivedDateUtc', sortable: false }
]
const table = useServerTable<FinanceReceiptListItem, FinanceReceiptFilters>(financeReceiptApi.list, {
  search: '', financeStatus: '', physicalReceiptsReceived: undefined, reportType: '', dateFrom: '', dateTo: ''
})
const receiptStates = [
  { title: 'All', value: undefined }, { title: 'Pending receipt', value: false }, { title: 'Received', value: true }
]
function openReport(value: unknown) { void router.push(`/finance/receipts/${encodeURIComponent((value as FinanceReceiptListItem).reportId)}`) }
function clearFilters() {
  Object.assign(table.filters, { search: '', financeStatus: '', physicalReceiptsReceived: undefined, reportType: '', dateFrom: '', dateTo: '' })
  void table.load()
}
onMounted(table.load)
</script>

<template>
  <AppPageHeader
    title="Finance receipt reviewer"
    subtitle="Track physical documents for fully approved reports"
  >
    <v-btn
      prepend-icon="mdi-refresh"
      :loading="table.loading.value"
      @click="table.load"
    >
      Refresh
    </v-btn>
  </AppPageHeader>
  <AppFilterBar>
    <v-text-field
      v-model="table.filters.search"
      label="Employee, report, or reference"
      prepend-inner-icon="mdi-magnify"
      clearable
      @update:model-value="table.search"
    />
    <v-select
      v-model="table.filters.physicalReceiptsReceived"
      label="Receipt state"
      :items="receiptStates"
      @update:model-value="table.search"
    />
    <v-text-field
      v-model="table.filters.reportType"
      label="Report type"
      clearable
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
      variant="text"
      @click="clearFilters"
    >
      Clear
    </v-btn>
  </AppFilterBar>
  <AppErrorAlert
    :message="table.error.value"
    class="mb-4"
  />
  <AppServerTable
    :headers="headers"
    :items="table.items.value"
    :total="table.total.value"
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
  </AppServerTable>
</template>
