import { createRouter, createWebHistory } from 'vue-router'
import { useSessionStore } from '@/app/stores/session'
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: () => import('@/views/auth/LoginView.vue'), meta: { guest: true } },
    { path: '/', component: AuthenticatedLayout, children: [
      { path: '', redirect: '/manager/reports' },
      { path: 'manager/reports', component: () => import('@/views/manager/ManagerReportsView.vue'), meta: { role: 'Manager' } },
      { path: 'manager/reports/:reportId', component: () => import('@/views/manager/ManagerReportDetailView.vue'), meta: { role: 'Manager' } },
      { path: 'finance/receipts', component: () => import('@/views/finance/FinanceReceiptsView.vue'), meta: { role: 'Finance' } },
      { path: 'finance/receipts/:reportId', component: () => import('@/views/finance/FinanceReceiptDetailView.vue'), meta: { role: 'Finance' } },
      { path: 'forbidden', component: () => import('@/views/ForbiddenView.vue') }
    ] },
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

router.beforeEach(async to => {
  const session = useSessionStore()
  await session.initialize()
  if (to.meta.guest) return session.user ? '/' : true
  if (!session.user) return { path: '/login', query: { returnUrl: to.fullPath } }
  const requiredRole = to.meta.role as string | undefined
  if (requiredRole && !session.user.roles.includes(requiredRole)) return '/forbidden'
  if (to.path === '/' || to.redirectedFrom?.path === '/') {
    return session.user.roles.includes('Manager') ? '/manager/reports' : session.user.roles.includes('Finance') ? '/finance/receipts' : '/forbidden'
  }
  return true
})

export default router
