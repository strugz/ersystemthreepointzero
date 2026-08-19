export interface FinanceReceiptListItem {
  reportId: string
  employeeUserId: number
  employeeName: string
  dateFrom: string | null
  dateTo: string | null
  description: string
  reportType: string
  erfReferenceNumber: string
  cashReferenceNumber: string
  financeStatus: string
  physicalReceiptsReceived: boolean
  receivedDateUtc: string | null
  rowVersion: string
}

export interface FinanceReceiptDetail extends Omit<FinanceReceiptListItem, 'cashReferenceNumber'> {
  receivedByUserId: number | null
  receivedByName: string
  remarks: string
  department: string
}

export interface FinanceReceiptFilters extends Record<string, unknown> {
  search: string
  financeStatus: string
  physicalReceiptsReceived: boolean | undefined
  reportType: string
  dateFrom: string
  dateTo: string
}

export interface ReceiveResult { reportId: string; financeStatus: string; rowVersion: string }
