import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppReceiptViewer from '@/shared/components/AppReceiptViewer.vue'

const global = {
  stubs: {
    VAlert: { template: '<div><slot /></div>' },
    VBtn: { emits: ['click'], template: '<button @click="$emit(\'click\')"><slot /></button>' }
  }
}

describe('AppReceiptViewer', () => {
  it('previews images and emits both external file actions', async () => {
    const wrapper = mount(AppReceiptViewer, {
      props: { url: 'blob:image', contentType: 'IMAGE/PNG', title: 'Receipt image' },
      global
    })

    expect(wrapper.get('img').attributes()).toMatchObject({ src: 'blob:image', alt: 'Receipt image' })
    const actions = wrapper.findAll('button')
    await actions[0].trigger('click')
    await actions[1].trigger('click')
    expect(wrapper.emitted('openExternal')).toHaveLength(1)
    expect(wrapper.emitted('download')).toHaveLength(1)
  })

  it('previews PDFs with content-type parameters', () => {
    const wrapper = mount(AppReceiptViewer, {
      props: { url: 'blob:pdf', contentType: 'application/pdf; charset=binary', title: 'Invoice.pdf' },
      global
    })
    expect(wrapper.get('iframe').attributes()).toMatchObject({ src: 'blob:pdf', title: 'Invoice.pdf' })
  })

  it('keeps open and download actions for unsupported files', () => {
    const wrapper = mount(AppReceiptViewer, {
      props: { url: 'blob:file', contentType: 'application/octet-stream', title: 'Receipt file' },
      global
    })
    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.find('iframe').exists()).toBe(false)
    expect(wrapper.text()).toContain('cannot be previewed here')
    expect(wrapper.findAll('button')).toHaveLength(2)
  })
})
