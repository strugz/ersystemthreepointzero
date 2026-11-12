import { computed } from 'vue';
import { useRouter } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import type { ThemePreference } from '@/app/theme';
import { roleLabel } from '@/features/auth/labels';
import { entitlementReaderRoles, hasAnyRole, rateNavRoles } from '@/features/entitlements/access';

export interface NavItem {
  title: string;
  /** One-word label for the phone bottom bar, where five items share 360px. */
  short?: string;
  /** Drawn as the filled button in the phone bottom bar. */
  primary?: boolean;
  icon: string;
  to: RouteLocationRaw;
  exact: boolean;
}

export interface NavGroup {
  label: string;
  items: NavItem[];
}

const nav: NavItem[] = [
  { title: 'Dashboard', short: 'Home', icon: 'mdi-view-dashboard-outline', to: { name: 'home' }, exact: true },
  { title: 'My reports', short: 'Reports', icon: 'mdi-file-document-multiple-outline', to: { name: 'reports' }, exact: false },
  { title: 'New report', short: 'New', primary: true, icon: 'mdi-plus', to: { name: 'report-new' }, exact: true },
  { title: 'Summary', icon: 'mdi-chart-box-outline', to: { name: 'expense-summary' }, exact: true },
];

// Open to every signed-in user; kept out of the main nav so the phone bottom bar stays at four items.
const wellnessNav: NavItem[] = [{ title: 'Healthy living', icon: 'mdi-heart-pulse', to: { name: 'random-quotes' }, exact: true }];

export const themeOptions: { value: ThemePreference; label: string; icon: string }[] = [
  { value: 'light', label: 'Light', icon: 'mdi-white-balance-sunny' },
  { value: 'dark', label: 'Dark', icon: 'mdi-weather-night' },
  { value: 'system', label: 'System', icon: 'mdi-monitor' },
];

/**
 * Navigation shared by the shell (rail, bottom bar) and the phone More page. Entries are gated by role
 * for display only; the API enforces access.
 */
export function useAppNavigation() {
  const session = useSessionStore();
  const router = useRouter();

  const adminNav = computed<NavItem[]>(() => {
    const items: NavItem[] = [];
    const isAdmin = session.hasRole('SystemAdministrator');
    if (isAdmin) {
      items.push(
        { title: 'Users', icon: 'mdi-account-multiple-outline', to: { name: 'admin-users' }, exact: false },
        { title: 'Master data', icon: 'mdi-database-outline', to: { name: 'admin-master-data' }, exact: false },
        { title: 'Rules', icon: 'mdi-tune-variant', to: { name: 'admin-settings' }, exact: false },
      );
    }
    if (isAdmin || session.hasRole('Finance')) {
      items.push({ title: 'Audit', icon: 'mdi-clipboard-text-clock-outline', to: { name: 'admin-audit' }, exact: false });
    }
    return items;
  });

  // Work queues: approvals for signatories, cost center budgets for signatories and Finance, the receipt
  // queue and signature approvals for Finance, verifications for the Expense Verifier.
  const workNav = computed<NavItem[]>(() => {
    const items: NavItem[] = [];
    if (session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2')) {
      items.push({ title: 'Approvals', icon: 'mdi-check-decagram-outline', to: { name: 'approvals' }, exact: false });
      items.push({ title: 'Delegations', icon: 'mdi-account-arrow-right-outline', to: { name: 'delegations' }, exact: false });
    }
    if (session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2') || session.hasRole('Finance')) {
      items.push({ title: 'Budgets', icon: 'mdi-chart-donut', to: { name: 'cost-center-budgets' }, exact: false });
    }
    if (session.hasRole('Finance')) {
      items.push({ title: 'Finance queue', icon: 'mdi-receipt-text-check-outline', to: { name: 'finance-queue' }, exact: false });
      items.push({ title: 'Meal claims', icon: 'mdi-silverware-fork-knife', to: { name: 'daily-meal-claims' }, exact: false });
      items.push({ title: 'Signatures', icon: 'mdi-draw-pen', to: { name: 'signature-approvals' }, exact: false });
    }
    if (session.hasRole('ExpenseVerifier')) {
      items.push({ title: 'Verifications', icon: 'mdi-magnify-scan', to: { name: 'verifications' }, exact: false });
    }
    return items;
  });

  // Signatory structure: scope and rates for every entitlement reader; the System Administrator maintains both.
  const entitlementNav = computed<NavItem[]>(() => {
    const roles = session.user?.roles;
    const items: NavItem[] = [];
    if (hasAnyRole(roles, entitlementReaderRoles)) {
      items.push({ title: 'Scope', icon: 'mdi-account-supervisor-outline', to: { name: 'scope' }, exact: false });
    }
    if (hasAnyRole(roles, rateNavRoles)) {
      items.push({ title: 'Rates', icon: 'mdi-cash-multiple', to: { name: 'rate-profiles' }, exact: false });
    }
    return items;
  });

  /** Groups shown after the main nav: behind a separator on the rail, as cards on the phone More page. */
  const navGroups = computed<NavGroup[]>(() =>
    [
      { label: 'Work queues', items: workNav.value },
      { label: 'Signatory structure', items: entitlementNav.value },
      { label: 'Administration', items: adminNav.value },
      { label: 'Wellness', items: wellnessNav },
    ].filter((g) => g.items.length > 0),
  );

  const roleSummary = computed(() => (session.user?.roles ?? []).map(roleLabel).join(' · '));

  const initials = computed(() =>
    (session.user?.fullName ?? '?')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? '')
      .join(''),
  );

  async function logout() {
    await session.logout();
    await router.push({ name: 'login' });
  }

  return { nav, navGroups, roleSummary, initials, logout };
}
