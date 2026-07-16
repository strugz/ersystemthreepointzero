import { shallowMount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppMobileBackNavigation from '@/shared/components/AppMobileBackNavigation.vue'

describe('AppMobileBackNavigation', () => {
  it('renders an accessible route action and workflow landmark', () => {
    const wrapper = shallowMount(AppMobileBackNavigation, {
      props: {
        to: '/finance/receipts',
        label: 'Finance receipts',
        accessibleLabel: 'Back to Finance receipts'
      },
      global: {
        stubs: {
          VBtn: {
            props: ['to', 'ariaLabel'],
            template: '<button :to="to" :aria-label="ariaLabel" />'
          }
        }
      }
    })

    expect(wrapper.attributes('aria-label')).toBe('Finance receipts')
    const button = wrapper.get('button')
    expect(button.attributes('to')).toBe('/finance/receipts')
    expect(button.attributes('aria-label')).toBe('Back to Finance receipts')
  })
})
