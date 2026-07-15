import { describe, expect, it } from 'vitest'
import { formatDate, formatMoney } from '@/shared/utils/format'

describe('format helpers', () => {
  it('formats missing values consistently', () => {
    expect(formatDate(null)).toBe('—')
    expect(formatMoney(null)).toBe('—')
  })

  it('formats Philippine peso values', () => {
    expect(formatMoney(1234.5)).toContain('1,234.50')
  })
})
