<script setup lang="ts">
import { nextTick, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { onBeforeRouteLeave, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useSessionStore } from '@/app/stores/session'
import AppDate from '@/shared/components/AppDate.vue'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import AppFilterBar from '@/shared/components/AppFilterBar.vue'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'
import AppServerTable from '@/shared/components/AppServerTable.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import AppStickyQueueControls from '@/shared/components/AppStickyQueueControls.vue'
import { createManagerReportFilters } from '@/features/manager-approvals/filters'
import { useManagerReportsQueueStore } from '@/features/manager-approvals/queueStore'
import { managerReportTypes } from '@/features/manager-approvals/reportTypes'
import type { ManagerReportListItem } from '@/features/manager-approvals/types'

const router = useRouter()
const session = useSessionStore()
const { smAndDown } = useDisplay()
const headers = [
  { title: 'Report', key: 'reportId' }, { title: 'Employee', key: 'employeeName' },
  { title: 'Department', key: 'department' }, { title: 'Report type', key: 'reportType' },
  { title: 'From', key: 'dateFrom' }, { title: 'Step', key: 'currentStep', sortable: false },
  { title: 'Status', key: 'status', sortable: false }
]
const queue = useManagerReportsQueueStore()
const { items, total, loading, error, page, pageSize, filters, stale, scrollPosition } = storeToRefs(queue)
function reportPath(report: ManagerReportListItem) { return `/manager/reports/${encodeURIComponent(report.reportId)}` }
function openReport(value: unknown) { void router.push(reportPath(value as ManagerReportListItem)) }
function clearFilters() {
  Object.assign(filters.value, createManagerReportFilters())
  page.value = 1
  void queue.refresh()
}
onMounted(async () => {
  await queue.ensureLoaded(session.user?.userId ?? null)
  await nextTick()
  requestAnimationFrame(() => window.scrollTo({ top: scrollPosition.value }))
  if (stale.value) void queue.refresh()
})
onBeforeRouteLeave(() => { queue.captureScroll(window.scrollY) })
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
        :loading="loading"
        aria-label="Refresh Manager approvals"
        @click="queue.refresh"
      >
        <span v-if="!smAndDown">Refresh</span>
      </v-btn>
    </AppPageHeader>
    <AppFilterBar mobile-title="Approval filters">
      <template #primary>
        <v-text-field
          v-model="filters.search"
          label="Employee or report"
          prepend-inner-icon="mdi-magnify"
          clearable
          @update:model-value="queue.search"
        />
      </template>
      <template #filters>
        <v-select
          v-model="filters.status"
          label="Status"
          :items="[{ title: 'Pending', value: 'pending' }, { title: 'Completed', value: 'completed' }]"
          @update:model-value="queue.search"
        />
        <v-select
          v-model="filters.reportType"
          label="Report type"
          :items="managerReportTypes"
          @update:model-value="queue.search"
        />
        <v-text-field
          v-model="filters.dateFrom"
          label="Filed from"
          type="date"
          @update:model-value="queue.search"
        />
        <v-text-field
          v-model="filters.dateTo"
          label="Filed to"
          type="date"
          @update:model-value="queue.search"
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
    :message="error"
    class="mb-4"
  />
  <AppServerTable
    :headers="headers"
    :items="items"
    :total="total"
    :page="page"
    :loading="loading"
    :items-per-page="pageSize"
    @update-options="queue.updateOptions"
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
