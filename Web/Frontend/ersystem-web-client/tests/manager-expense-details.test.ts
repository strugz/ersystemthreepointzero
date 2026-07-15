import { shallowMount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppMoney from '@/shared/components/AppMoney.vue'
import ManagerExpenseDetails from '@/features/manager-approvals/ManagerExpenseDetails.vue'
import type { ExpenseLine } from '@/features/manager-approvals/types'

function expense(overrides: Partial<ExpenseLine> = {}): ExpenseLine {
  return {
    id: 1,
    transactionDate: null,
    isPerDiem: false,
    particulars: '',
    invoiceNumber: '',
    multiplier: null,
    expenseType: '',
    category: '',
    amount: 0,
    vatAmount: null,
    totalAmount: 0,
    location: '',
    remarks: '',
    workWith: '',
    serviceNumber: '',
    instrument: '',
    serialNumber: '',
    minusDays: '',
    totalDays: '',
    computation: '',
    ...overrides
  }
}

describe('ManagerExpenseDetails', () => {
  it('shows all populated business-review fields', () => {
    const wrapper = shallowMount(ManagerExpenseDetails, {
      props: {
        expense: expense({
          transactionDate: '2026-07-15',
          isPerDiem: true,
          invoiceNumber: 'OR-100',
          multiplier: 2,
          expenseType: 'Local',
          category: 'Others',
          amount: 100,
          vatAmount: 12,
          totalAmount: 224,
          location: 'MDMPI',
          remarks: 'Employee note',
          workWith: 'Support team',
          serviceNumber: 'SR-9',
          instrument: 'Device',
          serialNumber: 'SN-8',
          minusDays: '1',
          totalDays: '3',
          computation: '(3 Days - 1 Day) * 100'
        })
      },
      global: { stubs: { VChip: { template: '<span><slot /></span>' } } }
    })

    for (const expected of [
      'Invoice/OR number', 'OR-100', 'Expense type', 'Local', 'Category', 'Others', 'Base amount',
      'VAT amount', 'Filed amount', 'Multiplier', 'Per diem', 'Included', 'Location', 'MDMPI',
      'Work with', 'Support team', 'Instrument', 'Device', 'Serial number', 'SN-8', 'Service number',
      'SR-9', 'Total days', '3', 'Less days', '1', 'Computation', 'Employee remarks', 'Employee note'
    ]) expect(wrapper.text()).toContain(expected)
    expect(wrapper.findAllComponents(AppMoney).map(component => component.props('value'))).toEqual([100, 12, 224])
  })

  it('hides blank, zero, and legacy placeholder fields', () => {
    const wrapper = shallowMount(ManagerExpenseDetails, {
      props: {
        expense: expense({
          amount: 325,
          totalAmount: 0,
          invoiceNumber: ' ',
          workWith: 'NONE',
          serviceNumber: 'N/A',
          instrument: 'n/a',
          serialNumber: 'N/A',
          totalDays: '0',
          computation: '0'
        })
      }
    })

    expect(wrapper.text()).toContain('Filed amount')
    expect(wrapper.text()).not.toContain('Invoice/OR number')
    expect(wrapper.text()).not.toContain('Work with')
    expect(wrapper.text()).not.toContain('Service number')
    expect(wrapper.text()).not.toContain('Instrument')
    expect(wrapper.text()).not.toContain('Serial number')
    expect(wrapper.text()).not.toContain('Total days')
    expect(wrapper.text()).not.toContain('Computation')
    expect(wrapper.findAllComponents(AppMoney).map(component => component.props('value'))).toEqual([325, 325])
  })
})
