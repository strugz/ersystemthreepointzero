import type {
  ApprovalTrailItem,
  CashAdvance,
  ExpenseLine,
  ReceiptAttachment
} from '@/shared/types/reportReview'

export type {
  ApprovalTrailItem,
  CashAdvance,
  ExpenseLine,
  ReceiptAttachment
} from '@/shared/types/reportReview'

export interface ManagerReportListItem {
  reportId: string
  erfReferenceNumber: string
  employeeUserId: number
  employeeName: string
  department: string
  dateFrom: string | null
  dateTo: string | null
  description: string
  reportType: string
  currentStep: number
  totalSteps: number
  status: string
  rowVersion: string
}

export interface ManagerReportDetail extends ManagerReportListItem {
  expenses: ExpenseLine[]
  cashAdvance: CashAdvance | null
  attachments: ReceiptAttachment[]
  approvalTrail: ApprovalTrailItem[]
}

export interface ManagerReportFilters extends Record<string, unknown> {
  search: string
  status: string
  departmentId?: number
  reportType: string
  dateFrom: string
  dateTo: string
}

export interface WorkflowActionResult { reportId: string; status: string; rowVersion: string }
