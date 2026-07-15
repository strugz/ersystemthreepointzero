import type { CashAdvance, ExpenseLine } from './types'

export function resolveExpenseAmount(expense: Pick<ExpenseLine, 'amount' | 'totalAmount'>): number {
  return expense.totalAmount !== 0 ? expense.totalAmount : expense.amount
}

export function calculateManagerAmounts(expenses: ExpenseLine[], cashAdvance: CashAdvance | null) {
  const filedExpenses = expenses.reduce((total, expense) => total + resolveExpenseAmount(expense), 0)
  const cashAdvanceAmount = cashAdvance?.amount ?? 0
  return { filedExpenses, cashAdvanceAmount, combinedTotal: filedExpenses + cashAdvanceAmount }
}
