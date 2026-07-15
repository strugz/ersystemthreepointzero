import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'

describe('AppStatusChip', () => {
  it('uses the centralized display label', () => {
    const wrapper = mount(AppStatusChip, {
      props: { status: 'Receipts Received' },
      global: { stubs: { VChip: { template: '<span><slot /></span>' } } }
    })
    expect(wrapper.text()).toContain('Receipts received')
  })
})
