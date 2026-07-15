import { shallowMount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppMoney from '@/shared/components/AppMoney.vue'
import ManagerExpenseCard from '@/features/manager-approvals/ManagerExpenseCard.vue'
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

const passThrough = { template: '<div><slot /></div>' }

function mountCard(props: { expense: ExpenseLine; index: number; value: string }) {
  return shallowMount(ManagerExpenseCard, {
    props,
    global: {
      stubs: {
        VExpansionPanel: passThrough,
        VExpansionPanelTitle: passThrough,
        VExpansionPanelText: passThrough,
        VChip: passThrough
      }
    }
  })
}

describe('ManagerExpenseCard', () => {
  it('shows the compact review header with filed amount and VAT', () => {
    const wrapper = mountCard({
      index: 0,
      value: 'expense-id-1',
      expense: expense({
        transactionDate: '2026-06-22',
        particulars: 'ChatGPT Plus subscription',
        category: 'Others',
        location: 'MDMPI',
        amount: 900,
        vatAmount: 132,
        totalAmount: 1100,
        remarks: 'Required for development work'
      })
    })

    expect(wrapper.text()).toContain('Expense 1')
    expect(wrapper.text()).toContain('Filed amount')
    expect(wrapper.text()).toContain('ChatGPT Plus subscription')
    expect(wrapper.text()).toContain('Others')
    expect(wrapper.text()).toContain('VAT')
    expect(wrapper.findAllComponents(AppMoney).map(component => component.props('value'))).toEqual([1100, 132])
  })

  it('falls back to the legacy amount and omits empty optional details', () => {
    const wrapper = mountCard({
      index: 1,
      value: 'expense-id-1',
      expense: expense({ particulars: 'Taxi fare', amount: 325, totalAmount: 0 })
    })

    expect(wrapper.text()).toContain('Expense 2')
    expect(wrapper.text()).toContain('Taxi fare')
    expect(wrapper.findComponent(AppMoney).props('value')).toBe(325)
    expect(wrapper.find('.manager-expense-card__category').exists()).toBe(false)
    expect(wrapper.find('.manager-expense-card__vat').exists()).toBe(false)
  })
})
