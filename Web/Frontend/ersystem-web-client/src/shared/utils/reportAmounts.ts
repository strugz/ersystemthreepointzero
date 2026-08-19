import type { CashAdvance, ExpenseLine } from '@/shared/types/reportReview'

export type BalanceDueTo = 'MDMPI' | 'EMPLOYEE'

export function resolveExpenseAmount(expense: Pick<ExpenseLine, 'amount' | 'totalAmount'>): number {
  return expense.totalAmount !== 0 ? expense.totalAmount : expense.amount
}

export function calculateReportAmounts(expenses: ExpenseLine[], cashAdvance: CashAdvance | null) {
  const filedExpenses = expenses.reduce((total, expense) => total + resolveExpenseAmount(expense), 0)
  const cashAdvanceAmount = cashAdvance?.amount ?? 0
  const difference = cashAdvanceAmount - filedExpenses
  const balanceDueTo: BalanceDueTo = difference < 0 ? 'EMPLOYEE' : 'MDMPI'
  return { filedExpenses, cashAdvanceAmount, balanceDueAmount: Math.abs(difference), balanceDueTo }
}
