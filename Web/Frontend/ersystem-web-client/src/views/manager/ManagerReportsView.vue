<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppDate from '@/shared/components/AppDate.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppFilterBar from '@/shared/components/AppFilterBar.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import { useServerTable } from '@/shared/composables/useServerTable'
import { managerApprovalApi } from '@/features/manager-approvals/api'
import type { ManagerReportFilters, ManagerReportListItem } from '@/features/manager-approvals/types'

const router = useRouter()
const headers = [
  { title: 'Report', key: 'reportId' }, { title: 'Employee', key: 'employeeName' },
  { title: 'Department', key: 'department' }, { title: 'Report type', key: 'reportType' },
  { title: 'From', key: 'dateFrom' }, { title: 'Step', key: 'currentStep', sortable: false },
  { title: 'Status', key: 'status', sortable: false }
]
const table = useServerTable<ManagerReportListItem, ManagerReportFilters>(managerApprovalApi.list, {
  search: '', status: 'pending', reportType: '', dateFrom: '', dateTo: ''
})
function openReport(value: unknown) { void router.push(`/manager/reports/${encodeURIComponent((value as ManagerReportListItem).reportId)}`) }
function clearFilters() {
  Object.assign(table.filters, { search: '', status: 'pending', reportType: '', dateFrom: '', dateTo: '' })
  void table.load()
}
onMounted(table.load)
</script>

<template>
  <AppPageHeader
    title="Manager approvals"
    subtitle="Reports currently assigned to your approval step"
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
      label="Employee or report"
      prepend-inner-icon="mdi-magnify"
      clearable
      @update:model-value="table.search"
    />
    <v-select
      v-model="table.filters.status"
      label="Status"
      :items="[{ title: 'Pending', value: 'pending' }, { title: 'Completed', value: 'completed' }]"
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
      label="Filed from"
      type="date"
      @update:model-value="table.search"
    />
    <v-text-field
      v-model="table.filters.dateTo"
      label="Filed to"
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
    <template #item.currentStep="{ item }">
      {{ item.currentStep }} of {{ item.totalSteps }}
    </template>
    <template #item.status="{ item }">
      <AppStatusChip :status="item.status" />
    </template>
  </AppServerTable>
</template>
