import { apiBlob, apiRequest, buildQuery } from '@/shared/api/client'
import type { PagedResult } from '@/shared/types/api'
import type { ManagerReportDetail, ManagerReportFilters, ManagerReportListItem, WorkflowActionResult } from './types'

export const managerApprovalApi = {
  list(query: ManagerReportFilters & Record<string, unknown>) {
    return apiRequest<PagedResult<ManagerReportListItem>>(`/erf/manager/reports${buildQuery(query)}`)
  },
  detail(reportId: string) {
    return apiRequest<ManagerReportDetail>(`/erf/manager/reports/${encodeURIComponent(reportId)}`)
  },
  attachment(attachmentId: number) {
    return apiBlob(`/erf/manager/attachments/${attachmentId}`)
  },
  approve(reportId: string, rowVersion: string) {
    return apiRequest<WorkflowActionResult>(`/erf/manager/reports/${encodeURIComponent(reportId)}/approve`, {
      method: 'POST', body: JSON.stringify({ rowVersion })
    })
  },
  returnReport(reportId: string, reason: string, rowVersion: string) {
    return apiRequest<WorkflowActionResult>(`/erf/manager/reports/${encodeURIComponent(reportId)}/return`, {
      method: 'POST', body: JSON.stringify({ reason, rowVersion })
    })
  }
}
