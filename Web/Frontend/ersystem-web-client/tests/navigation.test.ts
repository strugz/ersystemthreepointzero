import { describe, expect, it } from 'vitest'
import { resolveNavigationDestination } from '@/app/router/navigation'

describe('authenticated navigation destinations', () => {
  it.each([
    ['/manager/reports', 'manager'],
    ['/manager/reports/ER-1', 'manager'],
    ['/finance/receipts', 'finance'],
    ['/finance/receipts/ER-1', 'finance'],
    ['/account', 'account'],
    ['/forbidden', '']
  ] as const)('maps %s to %s', (path, destination) => {
    expect(resolveNavigationDestination(path)).toBe(destination)
  })
})
