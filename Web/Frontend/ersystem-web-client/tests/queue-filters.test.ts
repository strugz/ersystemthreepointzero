import { describe, expect, it } from 'vitest'
import { createFinanceReceiptFilters } from '@/features/finance-receipts/filters'
import { createManagerReportFilters } from '@/features/manager-approvals/filters'
import { managerReportTypes } from '@/features/manager-approvals/reportTypes'
import { reportTypes } from '@/shared/data/reportTypes'

describe('queue filter defaults', () => {
  it('starts and resets Finance to pending receipt', () => {
    expect(createFinanceReceiptFilters().physicalReceiptsReceived).toBe(false)
    expect(createFinanceReceiptFilters()).toEqual(createFinanceReceiptFilters())
  })

  it('provides one shared report-type list for Manager and Finance queues', () => {
    expect(managerReportTypes).toBe(reportTypes)
    expect(managerReportTypes.map(item => item.value)).toEqual([
      '',
      'Replenishment of Revolving fund',
      'Liquidation for Cash Advance',
      'Reimbursement'
    ])
  })

  it('resets the Manager report-type filter to All', () => {
    expect(createManagerReportFilters().reportType).toBe('')
  })
})
