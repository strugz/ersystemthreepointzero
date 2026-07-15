import type { CashAdvance, ExpenseLine, ReceiptAttachment } from './types'

export interface ExpenseTableHeader {
  title: string
  key: string
  align?: 'start' | 'end'
}

export function hasDisplayText(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0
}

export function hasDisplayMoney(value: number | null | undefined): value is number {
  return value != null && value !== 0
}

export function hasCashAdvanceData(cashAdvance: CashAdvance | null): cashAdvance is CashAdvance {
  return cashAdvance != null && (
    hasDisplayMoney(cashAdvance.amount)
    || hasDisplayText(cashAdvance.date)
    || hasDisplayText(cashAdvance.referenceDocument)
    || hasDisplayText(cashAdvance.referenceNumber)
    || hasDisplayText(cashAdvance.revolvingFund)
  )
}

export function hasAmountSummary(expenses: ExpenseLine[], cashAdvance: CashAdvance | null): boolean {
  return expenses.length > 0 || hasDisplayMoney(cashAdvance?.amount)
}

export function hasExpenseMetadata(expense: ExpenseLine): boolean {
  return hasDisplayText(expense.transactionDate) || hasDisplayText(expense.location) || hasDisplayText(expense.remarks)
}

export function createExpenseTableHeaders(expenses: ExpenseLine[]): ExpenseTableHeader[] {
  const headers: ExpenseTableHeader[] = []
  if (expenses.some(expense => hasDisplayText(expense.transactionDate))) headers.push({ title: 'Date', key: 'transactionDate' })
  if (expenses.some(expense => hasDisplayText(expense.particulars))) headers.push({ title: 'Particulars', key: 'particulars' })
  if (expenses.some(expense => hasDisplayText(expense.category))) headers.push({ title: 'Category', key: 'category' })
  headers.push({ title: 'Amount', key: 'amount', align: 'end' })
  return headers
}

export function receiptDisplayName(attachment: ReceiptAttachment, index: number): string {
  return hasDisplayText(attachment.fileName) ? attachment.fileName.trim() : `Scanned receipt ${index + 1}`
}

export function formatReceiptSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return ''
  return `${Math.ceil(bytes / 1024)} KB`
}
