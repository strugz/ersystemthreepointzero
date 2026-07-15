import { describe, expect, it } from 'vitest'
import {
  createExpenseTableHeaders,
  formatReceiptSize,
  hasAmountSummary,
  hasCashAdvanceData,
  hasDisplayMoney,
  hasDisplayText,
  hasExpenseMetadata,
  receiptDisplayName
} from '@/features/manager-approvals/detailPresentation'
import type { CashAdvance, ExpenseLine, ReceiptAttachment } from '@/features/manager-approvals/types'

function expense(overrides: Partial<ExpenseLine> = {}): ExpenseLine {
  return { id: 1, transactionDate: null, particulars: '', category: '', location: '', amount: 0, totalAmount: 0, remarks: '', ...overrides }
}

function cashAdvance(overrides: Partial<CashAdvance> = {}): CashAdvance {
  return { amount: null, date: '', referenceDocument: '', referenceNumber: '', revolvingFund: '', ...overrides }
}

function attachment(overrides: Partial<ReceiptAttachment> = {}): ReceiptAttachment {
  return { id: 1, fileName: '', contentType: 'application/pdf', fileSizeBytes: 0, createdDateUtc: '', ...overrides }
}

describe('Manager detail presentation', () => {
  it('hides null, undefined, blank text, and optional zero money', () => {
    expect(hasDisplayText(null)).toBe(false)
    expect(hasDisplayText(undefined)).toBe(false)
    expect(hasDisplayText('   ')).toBe(false)
    expect(hasDisplayText(' Reference ')).toBe(true)
    expect(hasDisplayMoney(null)).toBe(false)
    expect(hasDisplayMoney(0)).toBe(false)
    expect(hasDisplayMoney(-25)).toBe(true)
  })

  it('hides a zero and otherwise empty cash advance card', () => {
    expect(hasCashAdvanceData(cashAdvance({ amount: 0 }))).toBe(false)
    expect(hasCashAdvanceData(cashAdvance({ amount: 0, referenceNumber: 'CA-12' }))).toBe(true)
    expect(hasCashAdvanceData(cashAdvance({ revolvingFund: 'Operations' }))).toBe(true)
  })

  it('shows the amount summary only for expense lines or non-zero cash', () => {
    expect(hasAmountSummary([], null)).toBe(false)
    expect(hasAmountSummary([], cashAdvance({ amount: 0 }))).toBe(false)
    expect(hasAmountSummary([expense()], null)).toBe(true)
    expect(hasAmountSummary([], cashAdvance({ amount: 100 }))).toBe(true)
  })

  it('derives desktop expense columns from populated values', () => {
    expect(createExpenseTableHeaders([expense()])).toEqual([{ title: 'Amount', key: 'amount', align: 'end' }])
    expect(createExpenseTableHeaders([
      expense({ particulars: 'Meal' }),
      expense({ transactionDate: '2026-06-22', category: 'Food' })
    ])).toEqual([
      { title: 'Date', key: 'transactionDate' },
      { title: 'Particulars', key: 'particulars' },
      { title: 'Category', key: 'category' },
      { title: 'Amount', key: 'amount', align: 'end' }
    ])
  })

  it('detects optional mobile expense metadata', () => {
    expect(hasExpenseMetadata(expense())).toBe(false)
    expect(hasExpenseMetadata(expense({ location: 'MDMPI' }))).toBe(true)
    expect(hasExpenseMetadata(expense({ remarks: '  ' }))).toBe(false)
  })

  it('provides useful receipt labels without displaying empty metadata', () => {
    expect(receiptDisplayName(attachment({ fileName: ' Invoice.pdf ' }), 0)).toBe('Invoice.pdf')
    expect(receiptDisplayName(attachment(), 1)).toBe('Scanned receipt 2')
    expect(formatReceiptSize(0)).toBe('')
    expect(formatReceiptSize(1025)).toBe('2 KB')
  })
})
