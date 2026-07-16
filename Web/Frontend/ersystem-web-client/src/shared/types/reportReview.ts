export interface ExpenseLine {
  id: number | null
  transactionDate: string | null
  isPerDiem: boolean
  particulars: string
  invoiceNumber: string
  multiplier: number | null
  expenseType: string
  category: string
  amount: number
  vatAmount: number | null
  totalAmount: number
  location: string
  remarks: string
  workWith: string
  serviceNumber: string
  instrument: string
  serialNumber: string
  minusDays: string
  totalDays: string
  computation: string
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
