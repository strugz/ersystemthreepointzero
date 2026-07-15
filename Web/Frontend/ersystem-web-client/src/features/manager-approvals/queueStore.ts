import { defineStore } from 'pinia'
import { ref } from 'vue'
import { createServerTableState } from '@/shared/composables/useServerTable'
import { managerApprovalApi } from './api'
import { createManagerReportFilters } from './filters'
import type { ManagerReportFilters, ManagerReportListItem, WorkflowActionResult } from './types'

export const useManagerReportsQueueStore = defineStore('manager-reports-queue', () => {
  const table = createServerTableState<ManagerReportListItem, ManagerReportFilters>(managerApprovalApi.list, createManagerReportFilters())
  const loaded = ref(false)
  const stale = ref(false)
  const ownerUserId = ref<number | null>(null)
  const scrollPosition = ref(0)

  async function ensureLoaded(userId: number | null) {
    if (ownerUserId.value !== userId) clear(userId)
    if (!loaded.value) await refresh()
  }

  async function refresh() {
    const succeeded = await table.load()
    if (succeeded) {
      loaded.value = true
      stale.value = false
    }
    return succeeded
  }

  function applyWorkflowResult(result: WorkflowActionResult) {
    const index = table.items.value.findIndex(item => item.reportId === result.reportId)
    if (index >= 0) {
      if (table.filters.status === 'pending') {
        table.items.value.splice(index, 1)
        table.total.value = Math.max(0, table.total.value - 1)
      } else {
        table.items.value[index] = { ...table.items.value[index], status: result.status, rowVersion: result.rowVersion }
      }
    }
    stale.value = true
  }

  function captureScroll(position: number) { scrollPosition.value = Math.max(0, position) }

  function clear(userId: number | null = null) {
    table.reset()
    loaded.value = false
    stale.value = false
    ownerUserId.value = userId
    scrollPosition.value = 0
  }

  return {
    ...table,
    loaded,
    stale,
    ownerUserId,
    scrollPosition,
    ensureLoaded,
    refresh,
    applyWorkflowResult,
    captureScroll,
    clear
  }
})
