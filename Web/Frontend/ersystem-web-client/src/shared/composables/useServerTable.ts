import { onBeforeUnmount, reactive, ref } from 'vue'
import type { PagedResult, SortDirection } from '@/shared/types/api'

export function createServerTableState<T, F extends Record<string, unknown>>(loader: (query: F & Record<string, unknown>) => Promise<PagedResult<T>>, initialFilters: F) {
  const items = ref<T[]>([])
  const total = ref(0)
  const loading = ref(false)
  const error = ref('')
  const page = ref(1)
  const pageSize = ref(25)
  const sortBy = ref('')
  const sortDirection = ref<SortDirection>('Ascending')
  const filters = reactive({ ...initialFilters }) as F
  let requestId = 0
  let timer: ReturnType<typeof setTimeout> | undefined

  async function load(): Promise<boolean> {
    const current = ++requestId
    loading.value = true
    error.value = ''
    try {
      const result = await loader({ ...filters, page: page.value, pageSize: pageSize.value, sortBy: sortBy.value, sortDirection: sortDirection.value })
      if (current !== requestId) return false
      items.value = result.items
      total.value = result.total
      return true
    } catch (caught) {
      if (current === requestId) error.value = caught instanceof Error ? caught.message : 'Unable to load records.'
      return false
    } finally { if (current === requestId) loading.value = false }
  }

  function search() { clearTimeout(timer); timer = setTimeout(() => { page.value = 1; void load() }, 300) }
  function updateOptions(options: { page: number; itemsPerPage: number; sortBy?: Array<{ key: string; order: 'asc' | 'desc' }> }) {
    page.value = options.page; pageSize.value = options.itemsPerPage
    sortBy.value = options.sortBy?.[0]?.key ?? ''; sortDirection.value = options.sortBy?.[0]?.order === 'desc' ? 'Descending' : 'Ascending'
    void load()
  }
  function reset() {
    requestId++
    clearTimeout(timer)
    items.value = []
    total.value = 0
    loading.value = false
    error.value = ''
    page.value = 1
    pageSize.value = 25
    sortBy.value = ''
    sortDirection.value = 'Ascending'
    for (const key of Object.keys(filters)) delete filters[key as keyof F]
    Object.assign(filters, initialFilters)
  }
  function dispose() { clearTimeout(timer) }
  return { items, total, loading, error, page, pageSize, sortBy, sortDirection, filters, load, search, updateOptions, reset, dispose }
}

export function useServerTable<T, F extends Record<string, unknown>>(loader: (query: F & Record<string, unknown>) => Promise<PagedResult<T>>, initialFilters: F) {
  const table = createServerTableState(loader, initialFilters)
  onBeforeUnmount(table.dispose)
  return table
}
