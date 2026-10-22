import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import type { UserRole } from '@/features/auth/types';
import { entitlementReaderRoles } from '@/features/entitlements/access';

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean;
    roles?: UserRole[];
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
  },
  {
    path: '/',
    name: 'home',
    component: () => import('@/views/HomeView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/reports',
    name: 'reports',
    component: () => import('@/views/reports/MyReportsView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/reports/new',
    name: 'report-new',
    component: () => import('@/views/reports/NewReportView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/reports/:id',
    name: 'report-edit',
    component: () => import('@/views/reports/ReportEditorView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/reports/:id/pdf',
    name: 'report-pdf',
    component: () => import('@/views/reports/ReportPdfView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/summaries',
    name: 'expense-summary',
    component: () => import('@/views/reports/ExpenseSummaryView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/approvals',
    name: 'approvals',
    component: () => import('@/views/approvals/ApprovalInboxView.vue'),
    meta: { requiresAuth: true, roles: ['SignatoryLevel1', 'SignatoryLevel2'] },
  },
  {
    path: '/delegations',
    name: 'delegations',
    component: () => import('@/views/delegations/DelegationsView.vue'),
    meta: { requiresAuth: true, roles: ['SignatoryLevel1', 'SignatoryLevel2'] },
  },
  {
    path: '/review/:id',
    name: 'report-review',
    component: () => import('@/views/approvals/ReportReviewView.vue'),
    meta: { requiresAuth: true, roles: ['SignatoryLevel1', 'SignatoryLevel2', 'Finance', 'ExpenseVerifier'] },
  },
  {
    path: '/verifications',
    name: 'verifications',
    component: () => import('@/views/verification/VerificationQueueView.vue'),
    meta: { requiresAuth: true, roles: ['ExpenseVerifier'] },
  },
  {
    path: '/finance',
    name: 'finance-queue',
    component: () => import('@/views/finance/FinanceQueueView.vue'),
    meta: { requiresAuth: true, roles: ['Finance'] },
  },
  {
    path: '/admin/users',
    name: 'admin-users',
    component: () => import('@/views/admin/UsersView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator'] },
  },
  {
    path: '/admin/users/new',
    name: 'admin-user-new',
    component: () => import('@/views/admin/UserCreateView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator'] },
  },
  {
    path: '/admin/users/:id',
    name: 'admin-user',
    component: () => import('@/views/admin/UserDetailView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator'] },
  },
  {
    path: '/admin/master-data',
    name: 'admin-master-data',
    component: () => import('@/views/admin/MasterDataView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator'] },
  },
  {
    path: '/admin/settings',
    name: 'admin-settings',
    component: () => import('@/views/admin/SettingsView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator'] },
  },
  {
    path: '/admin/audit',
    name: 'admin-audit',
    component: () => import('@/views/admin/AuditView.vue'),
    meta: { requiresAuth: true, roles: ['SystemAdministrator', 'Finance'] },
  },
  {
    path: '/scope',
    name: 'scope',
    component: () => import('@/views/entitlements/ScopeView.vue'),
    meta: { requiresAuth: true, roles: entitlementReaderRoles },
  },
  {
    path: '/rates',
    name: 'rate-profiles',
    component: () => import('@/views/entitlements/RateProfilesView.vue'),
    meta: { requiresAuth: true, roles: entitlementReaderRoles },
  },
  {
    path: '/rates/:id',
    name: 'rate-profile',
    component: () => import('@/views/entitlements/RateProfileDetailView.vue'),
    meta: { requiresAuth: true, roles: entitlementReaderRoles },
  },
  {
    path: '/forbidden',
    name: 'forbidden',
    component: () => import('@/views/ForbiddenView.vue'),
  },
  { path: '/:pathMatch(.*)*', redirect: '/' },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to) => {
  const session = useSessionStore();
  await session.initialize();

  if (to.meta.requiresAuth && !session.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  if (to.name === 'login' && session.isAuthenticated) {
    return { name: 'home' };
  }

  if (to.meta.roles && !to.meta.roles.some((role) => session.hasRole(role))) {
    return { name: 'forbidden' };
  }

  return true;
});

export default router;
