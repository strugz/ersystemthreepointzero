<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import AppDate from '@/shared/components/AppDate.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppFilterBar from '@/shared/components/AppFilterBar.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import AppStickyQueueControls from '@/shared/components/AppStickyQueueControls.vue'
import { useServerTable } from '@/shared/composables/useServerTable'
import { managerApprovalApi } from '@/features/manager-approvals/api'
import { createManagerReportFilters } from '@/features/manager-approvals/filters'
import { managerReportTypes } from '@/features/manager-approvals/reportTypes'
import type { ManagerReportFilters, ManagerReportListItem } from '@/features/manager-approvals/types'

const router = useRouter()
const { smAndDown } = useDisplay()
const headers = [
  { title: 'Report', key: 'reportId' }, { title: 'Employee', key: 'employeeName' },
  { title: 'Department', key: 'department' }, { title: 'Report type', key: 'reportType' },
  { title: 'From', key: 'dateFrom' }, { title: 'Step', key: 'currentStep', sortable: false },
  { title: 'Status', key: 'status', sortable: false }
]
const table = useServerTable<ManagerReportListItem, ManagerReportFilters>(managerApprovalApi.list, createManagerReportFilters())
function reportPath(report: ManagerReportListItem) { return `/manager/reports/${encodeURIComponent(report.reportId)}` }
function openReport(value: unknown) { void router.push(reportPath(value as ManagerReportListItem)) }
function clearFilters() {
  Object.assign(table.filters, createManagerReportFilters())
  table.page.value = 1
  void table.load()
}
onMounted(table.load)
</script>

<template>
  <AppStickyQueueControls>
    <AppPageHeader
      title="Manager approvals"
      subtitle="Reports currently assigned to your approval step"
    >
      <v-btn
        :icon="smAndDown ? 'mdi-refresh' : undefined"
        :prepend-icon="smAndDown ? undefined : 'mdi-refresh'"
        :loading="table.loading.value"
        aria-label="Refresh Manager approvals"
        @click="table.load"
      >
        <span v-if="!smAndDown">Refresh</span>
      </v-btn>
    </AppPageHeader>
    <AppFilterBar mobile-title="Approval filters">
      <template #primary>
        <v-text-field
          v-model="table.filters.search"
          label="Employee or report"
          prepend-inner-icon="mdi-magnify"
          clearable
          @update:model-value="table.search"
        />
      </template>
      <template #filters>
        <v-select
          v-model="table.filters.status"
          label="Status"
          :items="[{ title: 'Pending', value: 'pending' }, { title: 'Completed', value: 'completed' }]"
          @update:model-value="table.search"
        />
        <v-select
          v-model="table.filters.reportType"
          label="Report type"
          :items="managerReportTypes"
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
    <template #item.currentStep="{ item }">
      {{ item.currentStep }} of {{ item.totalSteps }}
    </template>
    <template #item.status="{ item }">
      <AppStatusChip :status="item.status" />
    </template>
    <template #mobile-item="{ item }">
      <v-card
        class="queue-card border"
        variant="flat"
        :to="reportPath(item as ManagerReportListItem)"
      >
        <v-card-text>
          <div class="d-flex align-start justify-space-between ga-3">
            <div>
              <div class="text-h6 font-weight-bold">
                {{ (item as ManagerReportListItem).employeeName }}
              </div>
              <div class="text-body-2 muted">
                {{ (item as ManagerReportListItem).reportId }} · {{ (item as ManagerReportListItem).department || 'No department' }}
              </div>
            </div>
            <AppStatusChip :status="(item as ManagerReportListItem).status" />
          </div>
          <v-divider class="my-3" />
          <div class="detail-grid">
            <div><span class="field-label">Report type</span>{{ (item as ManagerReportListItem).reportType || '—' }}</div>
            <div><span class="field-label">Filed from</span><AppDate :value="(item as ManagerReportListItem).dateFrom" /></div>
            <div><span class="field-label">Approval step</span>{{ (item as ManagerReportListItem).currentStep }} of {{ (item as ManagerReportListItem).totalSteps }}</div>
          </div>
        </v-card-text>
      </v-card>
    </template>
  </AppServerTable>
</template>
