import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import type { UserRole } from '@/features/auth/types';

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
