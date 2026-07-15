import { apiRequest, buildQuery } from '@/shared/api/client'
import type { PagedResult } from '@/shared/types/api'
import type { FinanceReceiptDetail, FinanceReceiptFilters, FinanceReceiptListItem, ReceiveResult } from './types'

export const financeReceiptApi = {
  list(query: FinanceReceiptFilters & Record<string, unknown>) {
    return apiRequest<PagedResult<FinanceReceiptListItem>>(`/api/finance/reports${buildQuery(query)}`)
  },
  detail(reportId: string) {
    return apiRequest<FinanceReceiptDetail>(`/api/finance/reports/${encodeURIComponent(reportId)}`)
  },
  receive(reportId: string, remarks: string, rowVersion: string) {
    return apiRequest<ReceiveResult>(`/api/finance/reports/${encodeURIComponent(reportId)}/receive`, {
      method: 'POST', body: JSON.stringify({ remarks: remarks.trim() || null, rowVersion })
    })
  }
}
