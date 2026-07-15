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

const legacyEmptyExpenseValues = new Set(['0', 'N/A', 'NONE'])

export function hasExpenseDetailText(value: unknown): value is string {
  return hasDisplayText(value) && !legacyEmptyExpenseValues.has(value.trim().toUpperCase())
}

export function hasDisplayNumber(value: number | null | undefined): value is number {
  return value != null && Number.isFinite(value) && value !== 0
}

export function expensePresentationKey(expense: ExpenseLine, index: number): string {
  return expense.id == null ? `expense-index-${index}` : `expense-id-${expense.id}`
}

export function hasAmountSummary(expenses: ExpenseLine[], cashAdvance: CashAdvance | null): boolean {
  return expenses.length > 0 || hasDisplayMoney(cashAdvance?.amount)
}

export function expenseLineCountLabel(count: number): string {
  return `${count} ${count === 1 ? 'expense' : 'expenses'}`
}

export function createExpenseTableHeaders(expenses: ExpenseLine[]): ExpenseTableHeader[] {
  const headers: ExpenseTableHeader[] = []
  if (expenses.some(expense => hasDisplayText(expense.transactionDate))) headers.push({ title: 'Date', key: 'transactionDate' })
  if (expenses.some(expense => hasDisplayText(expense.particulars))) headers.push({ title: 'Particulars', key: 'particulars' })
  if (expenses.some(expense => hasDisplayText(expense.category))) headers.push({ title: 'Category', key: 'category' })
  if (expenses.some(expense => hasDisplayMoney(expense.vatAmount))) headers.push({ title: 'VAT', key: 'vatAmount', align: 'end' })
  headers.push({ title: 'Filed amount', key: 'amount', align: 'end' })
  return headers
}

export function receiptDisplayName(attachment: ReceiptAttachment, index: number): string {
  return hasDisplayText(attachment.fileName) ? attachment.fileName.trim() : `Scanned receipt ${index + 1}`
}

export function formatReceiptSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return ''
  return `${Math.ceil(bytes / 1024)} KB`
}
