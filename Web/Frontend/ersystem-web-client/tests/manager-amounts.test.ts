import { describe, expect, it } from 'vitest'
import { calculateManagerAmounts, resolveExpenseAmount } from '@/features/manager-approvals/amounts'
import type { CashAdvance, ExpenseLine } from '@/features/manager-approvals/types'

function expense(amount: number, totalAmount: number): ExpenseLine {
  return {
    id: null, transactionDate: null, isPerDiem: false, particulars: '', invoiceNumber: '', multiplier: null,
    expenseType: '', category: '', amount, vatAmount: null, totalAmount, location: '', remarks: '', workWith: '',
    serviceNumber: '', instrument: '', serialNumber: '', minusDays: '', totalDays: '', computation: ''
  }
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

  it('calculates a balance due to MDMPI when the cash advance is greater', () => {
    expect(calculateManagerAmounts([expense(100, 0), expense(20, 80)], cashAdvance(500))).toEqual({
      filedExpenses: 180,
      cashAdvanceAmount: 500,
      balanceDueAmount: 320,
      balanceDueTo: 'MDMPI'
    })
  })

  it('calculates a balance due to the employee when expenses are greater', () => {
    expect(calculateManagerAmounts([expense(100, 0)], null)).toEqual({
      filedExpenses: 100,
      cashAdvanceAmount: 0,
      balanceDueAmount: 100,
      balanceDueTo: 'EMPLOYEE'
    })
  })

  it('matches the desktop rule by assigning an exact zero balance to MDMPI', () => {
    expect(calculateManagerAmounts([expense(100, 0)], cashAdvance(100))).toEqual({
      filedExpenses: 100,
      cashAdvanceAmount: 100,
      balanceDueAmount: 0,
      balanceDueTo: 'MDMPI'
    })
  })
})
