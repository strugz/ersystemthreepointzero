import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AppRefreshButton from '@/shared/components/AppRefreshButton.vue'

const displayState = vi.hoisted(() => ({ mobile: false }))

vi.mock('vuetify', async () => {
  const { computed } = await import('vue')
  return {
    useDisplay: () => ({ smAndDown: computed(() => displayState.mobile) })
  }
})

const VBtnStub = {
  inheritAttrs: false,
  props: ['icon', 'prependIcon', 'loading', 'width', 'height'],
  emits: ['click'],
  template: `
    <button
      v-bind="$attrs"
      :data-icon="icon"
      :data-prepend-icon="prependIcon"
      :data-loading="String(loading)"
      :data-width="width"
      :data-height="height"
      @click="$emit('click')"
    ><slot /></button>
  `
}

function mountButton(loading = false) {
  return mount(AppRefreshButton, {
    props: { accessibleLabel: 'Refresh reports', loading },
    global: { stubs: { VBtn: VBtnStub } }
  })
}

describe('AppRefreshButton', () => {
  beforeEach(() => { displayState.mobile = false })

  it('shows an icon and text on desktop', () => {
    const button = mountButton().get('button')

    expect(button.text()).toBe('Refresh')
    expect(button.attributes('data-prepend-icon')).toBe('mdi-refresh')
    expect(button.attributes('aria-label')).toBe('Refresh reports')
  })

  it('shows an accessible 44 pixel icon-only button on mobile', () => {
    displayState.mobile = true
    const button = mountButton().get('button')

    expect(button.text()).toBe('')
    expect(button.attributes('data-icon')).toBe('mdi-refresh')
    expect(button.attributes('data-width')).toBe('44')
    expect(button.attributes('data-height')).toBe('44')
    expect(button.attributes('aria-label')).toBe('Refresh reports')
    expect(button.attributes('title')).toBe('Refresh')
  })

  it('forwards loading state and emits one refresh event per click', async () => {
    const wrapper = mountButton(true)
    const button = wrapper.get('button')

    expect(button.attributes('data-loading')).toBe('true')
    await button.trigger('click')
    expect(wrapper.emitted('refresh')).toHaveLength(1)
  })
})
