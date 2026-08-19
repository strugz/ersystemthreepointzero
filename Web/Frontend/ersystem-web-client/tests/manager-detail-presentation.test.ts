import { describe, expect, it } from 'vitest'
import {
  createExpenseTableHeaders,
  expenseLineCountLabel,
  expensePresentationKey,
  formatReceiptSize,
  hasAmountSummary,
  hasDisplayMoney,
  hasDisplayNumber,
  hasDisplayText,
  hasExpenseDetailText,
  receiptDisplayName
} from '@/features/manager-approvals/detailPresentation'
import type { CashAdvance, ExpenseLine, ReceiptAttachment } from '@/features/manager-approvals/types'

function expense(overrides: Partial<ExpenseLine> = {}): ExpenseLine {
  return {
    id: 1, transactionDate: null, isPerDiem: false, particulars: '', invoiceNumber: '', multiplier: null,
    expenseType: '', category: '', amount: 0, vatAmount: null, totalAmount: 0, location: '', remarks: '',
    workWith: '', serviceNumber: '', instrument: '', serialNumber: '', minusDays: '', totalDays: '',
    computation: '', ...overrides
  }
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

  it('shows the amount summary only for expense lines or non-zero cash', () => {
    expect(hasAmountSummary([], null)).toBe(false)
    expect(hasAmountSummary([], cashAdvance({ amount: 0 }))).toBe(false)
    expect(hasAmountSummary([expense()], null)).toBe(true)
    expect(hasAmountSummary([], cashAdvance({ amount: 100 }))).toBe(true)
  })

  it('derives desktop expense columns from populated values', () => {
    expect(createExpenseTableHeaders([expense()])).toEqual([{ title: 'Filed amount', key: 'amount', align: 'end' }])
    expect(createExpenseTableHeaders([
      expense({ particulars: 'Meal' }),
      expense({ transactionDate: '2026-06-22', category: 'Food', vatAmount: 12 })
    ])).toEqual([
      { title: 'Date', key: 'transactionDate' },
      { title: 'Particulars', key: 'particulars' },
      { title: 'Category', key: 'category' },
      { title: 'VAT', key: 'vatAmount', align: 'end' },
      { title: 'Filed amount', key: 'amount', align: 'end' }
    ])
  })

  it('hides legacy placeholder details and creates deterministic expansion keys', () => {
    expect(hasExpenseDetailText('N/A')).toBe(false)
    expect(hasExpenseDetailText(' none ')).toBe(false)
    expect(hasExpenseDetailText('0')).toBe(false)
    expect(hasExpenseDetailText('Local')).toBe(true)
    expect(hasDisplayNumber(0)).toBe(false)
    expect(hasDisplayNumber(2)).toBe(true)
    expect(expensePresentationKey(expense({ id: 42 }), 0)).toBe('expense-id-42')
    expect(expensePresentationKey(expense({ id: null }), 3)).toBe('expense-index-3')
  })

  it('labels one or multiple expense lines clearly', () => {
    expect(expenseLineCountLabel(1)).toBe('1 expense')
    expect(expenseLineCountLabel(3)).toBe('3 expenses')
  })

  it('provides useful receipt labels without displaying empty metadata', () => {
    expect(receiptDisplayName(attachment({ fileName: ' Invoice.pdf ' }), 0)).toBe('Invoice.pdf')
    expect(receiptDisplayName(attachment(), 1)).toBe('Scanned receipt 2')
    expect(formatReceiptSize(0)).toBe('')
    expect(formatReceiptSize(1025)).toBe('2 KB')
  })
})
