import type { ManagerReportFilters } from './types'

export function createManagerReportFilters(): ManagerReportFilters {
  return { search: '', status: 'pending', reportType: '', dateFrom: '', dateTo: '' }
}
