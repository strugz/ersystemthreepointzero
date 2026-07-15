export interface ManagerReportListItem {
  reportId: string
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

export interface ExpenseLine {
  id: number | null
  transactionDate: string | null
  particulars: string
  category: string
  location: string
  amount: number
  totalAmount: number
  remarks: string
}

export interface CashAdvance {
  amount: number | null
  date: string
  referenceDocument: string
  referenceNumber: string
  revolvingFund: string
}

export interface ReceiptAttachment {
  id: number
  fileName: string
  contentType: string
  fileSizeBytes: number
  createdDateUtc: string
}

export interface ApprovalTrailItem {
  approverUserId: number
  approverName: string
  sort: number
  occurredAtUtc: string | null
  status: string
}

export interface ManagerReportDetail extends ManagerReportListItem {
  erfReferenceNumber: string
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
