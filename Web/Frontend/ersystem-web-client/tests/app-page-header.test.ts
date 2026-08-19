import { shallowMount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'

describe('AppPageHeader', () => {
  it('keeps title copy and workflow actions in separate responsive regions', () => {
    const wrapper = shallowMount(AppPageHeader, {
      props: {
        title: 'Physical receipt monitoring',
        subtitle: 'Employee · Department · ERF-24'
      },
      slots: {
        default: '<button>Mark as received</button>'
      }
    })

    expect(wrapper.classes()).toContain('app-page-header')
    expect(wrapper.get('.app-page-header__title').text()).toBe('Physical receipt monitoring')
    expect(wrapper.get('.app-page-header__subtitle').text()).toContain('ERF-24')
    expect(wrapper.get('.app-page-header__actions button').text()).toBe('Mark as received')
  })
})
