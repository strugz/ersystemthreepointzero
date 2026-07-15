import { describe, expect, it } from 'vitest'
import { calculateManagerAmounts, resolveExpenseAmount } from '@/features/manager-approvals/amounts'
import type { CashAdvance, ExpenseLine } from '@/features/manager-approvals/types'

function expense(amount: number, totalAmount: number): ExpenseLine {
  return { id: null, transactionDate: null, particulars: '', category: '', location: '', amount, totalAmount, remarks: '' }
}

function cashAdvance(amount: number | null): CashAdvance {
  return { amount, date: '', referenceDocument: '', referenceNumber: '', revolvingFund: '' }
}

describe('manager amount calculations', () => {
  it('uses the filed total amount when it is non-zero', () => {
    expect(resolveExpenseAmount(expense(100, 250))).toBe(250)
  })

  it('falls back to the legacy amount when the total is zero', () => {
    expect(resolveExpenseAmount(expense(125, 0))).toBe(125)
  })

  it('adds displayed expense amounts and cash advance into the combined total', () => {
    expect(calculateManagerAmounts([expense(100, 0), expense(20, 80)], cashAdvance(500))).toEqual({
      filedExpenses: 180,
      cashAdvanceAmount: 500,
      combinedTotal: 680
    })
  })

  it('treats a missing cash advance as zero', () => {
    expect(calculateManagerAmounts([expense(100, 0)], null)).toEqual({
      filedExpenses: 100,
      cashAdvanceAmount: 0,
      combinedTotal: 100
    })
  })
})
