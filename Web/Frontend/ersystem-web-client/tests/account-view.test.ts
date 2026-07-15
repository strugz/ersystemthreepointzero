import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AccountView from '@/views/account/AccountView.vue'

const mocks = vi.hoisted(() => ({ logout: vi.fn(), replace: vi.fn() }))

vi.mock('@/app/stores/session', () => ({
  useSessionStore: () => ({
    user: { userId: 7, username: 'ROMEWEL', fullName: 'Romewel A. Magdalita', userLevel: 'User', roles: ['Finance', 'Manager'] },
    logout: mocks.logout
  })
}))
vi.mock('vue-router', () => ({ useRouter: () => ({ replace: mocks.replace }) }))

const global = {
  stubs: {
    AppPageHeader: { props: ['title', 'subtitle'], template: '<header><h1>{{ title }}</h1><p>{{ subtitle }}</p></header>' },
    VCard: { template: '<section><slot /></section>' },
    VCardText: { template: '<div><slot /></div>' },
    VAvatar: { template: '<div><slot /></div>' },
    VIcon: { template: '<i><slot /></i>' },
    VChip: { template: '<span><slot /></span>' },
    VDivider: true,
    VCardActions: { template: '<div><slot /></div>' },
    VBtn: { emits: ['click'], template: '<button @click="$emit(\'click\')"><slot /></button>' }
  }
}

describe('AccountView', () => {
  beforeEach(() => {
    mocks.logout.mockReset().mockResolvedValue(undefined)
    mocks.replace.mockReset().mockResolvedValue(undefined)
  })

  it('shows the signed-in identity and roles', () => {
    const wrapper = mount(AccountView, { global })
    expect(wrapper.text()).toContain('Romewel A. Magdalita')
    expect(wrapper.text()).toContain('ROMEWEL')
    expect(wrapper.text()).toContain('Finance')
    expect(wrapper.text()).toContain('Manager')
  })

  it('signs out and returns to Login', async () => {
    const wrapper = mount(AccountView, { global })
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(mocks.logout).toHaveBeenCalledOnce()
    expect(mocks.replace).toHaveBeenCalledWith('/login')
  })
})
