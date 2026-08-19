import type { FinanceReceiptFilters } from './types'

export function createFinanceReceiptFilters(): FinanceReceiptFilters {
  return {
    search: '',
    financeStatus: '',
    physicalReceiptsReceived: false,
    reportType: '',
    dateFrom: '',
    dateTo: ''
  }
}
