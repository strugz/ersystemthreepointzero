<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import { useThemeStore } from '@/app/stores/theme';
import type { ThemePreference } from '@/app/theme';
import { roleLabel } from '@/features/auth/labels';
import { entitlementReaderRoles, hasAnyRole, rateNavRoles } from '@/features/entitlements/access';
import AppErmsMark from '@/shared/ui/AppErmsMark.vue';
import AppSnackbar from '@/shared/ui/AppSnackbar.vue';
import { appVersionLabel } from '@/shared/appVersion';

/**
 * Application shell.
 *  - Desktop / tablet (>= 600px): a left navigation pane the user opens or closes: a 72px icon rail
 *    (closed) or a 248px pane with labels (open). The choice is remembered in this browser.
 *    64px white header with a breadcrumb.
 *  - Phone (< 600px): no rail; a 56px header with a back button, and a bottom bar — either the
 *    page's own sticky actions (#bottom slot) or the global bottom navigation.
 */
export interface Crumb {
  title: string;
  to?: RouteLocationRaw;
  mono?: boolean;
}

const props = withDefaults(
  defineProps<{
    crumbs?: Crumb[];
    /** Where the phone back button goes; defaults to the previous crumb. */
    back?: RouteLocationRaw;
    /** Narrow content column (forms) instead of the full-width page grid. */
    narrow?: boolean;
  }>(),
  { crumbs: () => [], back: undefined, narrow: false },
);

const session = useSessionStore();
const theme = useThemeStore();
const router = useRouter();
const route = useRoute();
const { xs } = useDisplay();

interface NavItem {
  title: string;
  icon: string;
  to: RouteLocationRaw;
  exact: boolean;
}

const nav: NavItem[] = [
  { title: 'Dashboard', icon: 'mdi-view-dashboard-outline', to: { name: 'home' }, exact: true },
  { title: 'My reports', icon: 'mdi-file-document-multiple-outline', to: { name: 'reports' }, exact: false },
  { title: 'New report', icon: 'mdi-plus', to: { name: 'report-new' }, exact: true },
  { title: 'Summary', icon: 'mdi-chart-box-outline', to: { name: 'expense-summary' }, exact: true },
];

// Administration entries, gated by role. The API enforces access; this only decides what to show.
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

// Work queues: approvals for signatories, the receipt queue for Finance, verifications for the
// Expense Verifier. Gating is display only.
const workNav = computed<NavItem[]>(() => {
  const items: NavItem[] = [];
  if (session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2')) {
    items.push({ title: 'Approvals', icon: 'mdi-check-decagram-outline', to: { name: 'approvals' }, exact: false });
    items.push({ title: 'Delegations', icon: 'mdi-account-arrow-right-outline', to: { name: 'delegations' }, exact: false });
  }
  if (session.hasRole('Finance')) {
    items.push({ title: 'Finance queue', icon: 'mdi-receipt-text-check-outline', to: { name: 'finance-queue' }, exact: false });
  }
  if (session.hasRole('ExpenseVerifier')) {
    items.push({ title: 'Verifications', icon: 'mdi-magnify-scan', to: { name: 'verifications' }, exact: false });
  }
  return items;
});

// Signatory structure entries: scope and rates for every entitlement reader; the System
// Administrator maintains both. Gating is display only.
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

/** Groups shown after the main nav, each behind a separator on the rail and a divider in "More". */
const navGroups = computed(() =>
  [
    { label: 'Work queues', items: workNav.value },
    { label: 'Signatory structure', items: entitlementNav.value },
    { label: 'Administration', items: adminNav.value },
  ].filter((g) => g.items.length > 0),
);

const entitlementRouteNames = new Set(['scope', 'rate-profiles', 'rate-profile', 'approvals', 'delegations', 'report-review', 'finance-queue', 'verifications']);

const roleSummary = computed(() => (session.user?.roles ?? []).map(roleLabel).join(' · '));
const moreOpen = ref(false);

// Open/closed navigation pane: a per-browser convenience, so storage failures just fall back to closed.
const NAV_KEY = 'erfweb.nav.expanded';
function readExpanded(): boolean {
  try {
    return localStorage.getItem(NAV_KEY) === '1';
  } catch {
    return false;
  }
}
const expanded = ref(readExpanded());
function toggleNav() {
  expanded.value = !expanded.value;
  try {
    localStorage.setItem(NAV_KEY, expanded.value ? '1' : '0');
  } catch {
    // Storage blocked (private window): the toggle still works for this page view.
  }
}
const moreRouteActive = computed(() => {
  const name = String(route.name ?? '');
  return name.startsWith('admin-') || entitlementRouteNames.has(name);
});

const initials = computed(() =>
  (session.user?.fullName ?? '?')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join(''),
);

const backTarget = computed<RouteLocationRaw | undefined>(() => {
  if (props.back) return props.back;
  const prev = props.crumbs.length > 1 ? props.crumbs[props.crumbs.length - 2] : undefined;
  return prev?.to;
});

const lastCrumb = computed(() => props.crumbs[props.crumbs.length - 1]);

const themeOptions: { value: ThemePreference; label: string; icon: string }[] = [
  { value: 'light', label: 'Light', icon: 'mdi-white-balance-sunny' },
  { value: 'dark', label: 'Dark', icon: 'mdi-weather-night' },
  { value: 'system', label: 'System', icon: 'mdi-monitor' },
];

async function logout() {
  await session.logout();
  await router.push({ name: 'login' });
}
</script>

<template>
  <!-- Navigation pane: tablet and desktop. Closed = icon rail, open = icons with labels. -->
  <v-navigation-drawer
    v-if="!xs"
    permanent
    :rail="!expanded"
    rail-width="72"
    width="248"
    class="rail"
    :class="{ 'rail--open': expanded }"
    theme="erDark"
  >
    <div class="rail__inner">
      <div class="rail__top">
        <router-link :to="{ name: 'home' }" class="rail__logo press" aria-label="ERFWeb home">
          <AppErmsMark :size="40" inverse />
        </router-link>
        <span v-if="expanded" class="rail__brand">ERFWeb</span>
      </div>

      <v-tooltip :text="expanded ? 'Close navigation' : 'Open navigation'" location="end" open-delay="300" :disabled="expanded">
        <template #activator="{ props: tip }">
          <button
            v-bind="tip"
            type="button"
            class="rail__btn rail__toggle press"
            :aria-label="expanded ? 'Close navigation' : 'Open navigation'"
            :aria-expanded="expanded"
            @click="toggleNav"
          >
            <v-icon :icon="expanded ? 'mdi-menu-open' : 'mdi-menu'" size="22" />
            <span v-if="expanded" class="rail__label">Close menu</span>
          </button>
        </template>
      </v-tooltip>

      <nav class="rail__nav" aria-label="Main">
        <v-tooltip v-for="item in nav" :key="item.title" :text="item.title" location="end" open-delay="300" :disabled="expanded">
          <template #activator="{ props: tip }">
            <router-link v-bind="tip" :to="item.to" custom>
              <template #default="{ navigate, isActive, isExactActive, href }">
                <a
                  :href="href"
                  class="rail__btn press"
                  :class="{ 'rail__btn--on': item.exact ? isExactActive : isActive }"
                  :aria-label="item.title"
                  @click.prevent="navigate()"
                >
                  <v-icon :icon="item.icon" size="22" />
                  <span v-if="expanded" class="rail__label">{{ item.title }}</span>
                </a>
              </template>
            </router-link>
          </template>
        </v-tooltip>

        <template v-for="group in navGroups" :key="group.label">
          <div v-if="expanded" class="rail__group">{{ group.label }}</div>
          <div v-else class="rail__sep" role="separator" :aria-label="group.label" />
          <v-tooltip v-for="item in group.items" :key="item.title" :text="item.title" location="end" open-delay="300" :disabled="expanded">
            <template #activator="{ props: tip }">
              <router-link v-bind="tip" :to="item.to" custom>
                <template #default="{ navigate, isActive, isExactActive, href }">
                  <a
                    :href="href"
                    class="rail__btn press"
                    :class="{ 'rail__btn--on': item.exact ? isExactActive : isActive }"
                    :aria-label="item.title"
                    @click.prevent="navigate()"
                  >
                    <v-icon :icon="item.icon" size="22" />
                    <span v-if="expanded" class="rail__label">{{ item.title }}</span>
                  </a>
                </template>
              </router-link>
            </template>
          </v-tooltip>
        </template>
      </nav>

      <v-menu location="end bottom" :offset="8" :theme="theme.isDark ? 'erDark' : 'erLight'">
        <template #activator="{ props: menu }">
          <button v-bind="menu" class="rail__account press" type="button" :aria-label="`Account: ${session.user?.fullName ?? ''}`">
            <span class="rail__avatar">{{ initials }}</span>
            <span v-if="expanded" class="rail__who">
              <span class="rail__who-name">{{ session.user?.fullName }}</span>
              <span class="rail__who-roles">{{ roleSummary }}</span>
            </span>
          </button>
        </template>
        <v-card min-width="240" class="pa-1">
          <v-list density="compact" class="py-0">
            <v-list-item :title="session.user?.fullName" :subtitle="roleSummary" />
            <v-divider class="my-1" />
            <div class="theme-pick" role="radiogroup" aria-label="Appearance">
              <span class="lbl theme-pick__lbl">Appearance</span>
              <div class="theme-pick__seg">
                <button
                  v-for="o in themeOptions"
                  :key="o.value"
                  type="button"
                  role="radio"
                  class="theme-pick__opt press"
                  :class="{ 'theme-pick__opt--on': theme.preference === o.value }"
                  :aria-checked="theme.preference === o.value"
                  @click="theme.setPreference(o.value)"
                >
                  <v-icon :icon="o.icon" size="16" />
                  <span>{{ o.label }}</span>
                </button>
              </div>
            </div>
            <v-divider class="my-1" />
            <v-list-item prepend-icon="mdi-lock-reset" title="Change password" :to="{ name: 'change-password' }" />
            <v-list-item prepend-icon="mdi-logout" title="Sign out" @click="logout" />
          </v-list>
        </v-card>
      </v-menu>
    </div>
  </v-navigation-drawer>

  <!-- Header -->
  <v-app-bar flat color="surface" :height="xs ? 56 : 64" class="shell-header">
    <template v-if="xs">
      <v-btn
        v-if="backTarget"
        class="press"
        icon="mdi-chevron-left"
        variant="text"
        :to="backTarget"
        aria-label="Back"
      />
      <div v-else class="rail__logo rail__logo--sm ml-3" aria-hidden="true"><AppErmsMark :size="32" :inverse="theme.isDark" /></div>
      <div class="shell-header__title">
        <slot name="title-mobile">
          <div class="text-body-1 font-weight-medium text-truncate" :class="{ mono: lastCrumb?.mono }">
            {{ lastCrumb?.title ?? 'ERFWeb' }}
          </div>
        </slot>
      </div>
      <v-spacer />
      <slot name="actions-mobile" />
    </template>

    <template v-else>
      <nav class="crumbs ml-8" aria-label="Breadcrumb">
        <template v-for="(c, i) in crumbs" :key="i">
          <router-link v-if="c.to && i < crumbs.length - 1" :to="c.to" class="crumbs__link">{{ c.title }}</router-link>
          <span v-else class="crumbs__current" :class="{ mono: c.mono }">{{ c.title }}</span>
          <v-icon v-if="i < crumbs.length - 1" icon="mdi-chevron-right" size="14" class="crumbs__sep" />
        </template>
      </nav>
      <v-spacer />
      <span v-if="session.user" class="text-body-2 text-medium-emphasis mr-3 d-none d-md-inline">
        {{ session.user.fullName }}
      </span>
      <v-tooltip :text="theme.isDark ? 'Switch to light mode' : 'Switch to dark mode'" location="bottom" open-delay="300">
        <template #activator="{ props: tip }">
          <button
            v-bind="tip"
            type="button"
            class="theme-btn press mr-3"
            :aria-label="theme.isDark ? 'Switch to light mode' : 'Switch to dark mode'"
            @click="theme.toggle()"
          >
            <v-icon :icon="theme.isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night'" size="20" />
          </button>
        </template>
      </v-tooltip>
      <div class="d-flex align-center ga-2 mr-8">
        <slot name="actions" />
      </div>
    </template>
  </v-app-bar>

  <v-main>
    <div class="page page-enter" :class="{ 'page--narrow': narrow }">
      <slot />
      <p class="app-version">{{ appVersionLabel }}</p>
    </div>
  </v-main>

  <!-- Phone bottom: page actions when provided, otherwise global navigation -->
  <v-footer v-if="xs && $slots.bottom" app class="bottom-bar" color="surface">
    <slot name="bottom" />
  </v-footer>
  <v-bottom-navigation v-else-if="xs" grow color="primary" bg-color="surface" class="bottom-nav" :elevation="0">
    <v-btn v-for="item in nav" :key="item.title" :to="item.to" :exact="item.exact" class="press">
      <v-icon :icon="item.icon" />
      <span class="text-caption">{{ item.title }}</span>
    </v-btn>
    <!-- Phones have no rail or avatar menu, so scope, rates, administration and sign-out live behind "More". -->
    <v-menu v-model="moreOpen" location="top end" :offset="8">
      <template #activator="{ props: menu }">
        <v-btn v-bind="menu" class="press" :active="moreRouteActive" aria-label="More" aria-haspopup="menu">
          <v-icon icon="mdi-dots-horizontal" />
          <span class="text-caption">More</span>
        </v-btn>
      </template>
      <v-card min-width="220" class="pa-1">
        <v-list density="compact" class="py-0">
          <v-list-item :title="session.user?.fullName" :subtitle="roleSummary" />
          <template v-for="group in navGroups" :key="group.label">
            <v-divider class="my-1" />
            <v-list-subheader class="more__group">{{ group.label }}</v-list-subheader>
            <v-list-item v-for="item in group.items" :key="item.title" :prepend-icon="item.icon" :title="item.title" :to="item.to" />
          </template>
          <v-divider class="my-1" />
          <v-list-subheader class="more__group">Appearance</v-list-subheader>
          <v-list-item
            v-for="o in themeOptions"
            :key="o.value"
            :prepend-icon="o.icon"
            :title="o.label"
            :active="theme.preference === o.value"
            color="primary"
            @click.stop="theme.setPreference(o.value)"
          />
          <v-divider class="my-1" />
          <v-list-item prepend-icon="mdi-lock-reset" title="Change password" :to="{ name: 'change-password' }" />
          <v-list-item prepend-icon="mdi-logout" title="Sign out" @click="logout" />
        </v-list>
      </v-card>
    </v-menu>
  </v-bottom-navigation>

  <AppSnackbar />
</template>

<style scoped>
/* The rail is a navy brand surface in both themes (rendered with the dark Vuetify theme so icons,
   tooltips and ripples read on it); --rail-* tokens keep it a touch brighter in light mode. */
.rail {
  background: linear-gradient(180deg, var(--rail-1), var(--rail-2)) !important;
  border-right: 1px solid var(--brand-line) !important;
  color: var(--on-brand);
}
.rail__inner {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 0;
  gap: 8px;
}
.rail__top {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}
.rail__brand {
  font-weight: 600;
  font-size: 16px;
  letter-spacing: -0.01em;
  color: var(--on-brand);
  white-space: nowrap;
}
.rail__brand::after {
  content: '';
  display: block;
  width: 18px;
  height: 2px;
  margin-top: 3px;
  border-radius: 2px;
  background: var(--brand-accent);
}
.rail__logo {
  flex: none;
  width: 40px;
  height: 40px;
  /* Matches the mark's own corner (rx 15 of 64) so the shadow and focus ring follow the tile. */
  border-radius: 23%;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  display: flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
}
.rail__logo--sm {
  width: 32px;
  height: 32px;
  box-shadow: none;
  margin-bottom: 0;
}
.rail__nav {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}
.rail__sep {
  width: 28px;
  height: 1px;
  margin: 6px auto;
  background: var(--brand-line);
}
.rail__btn {
  position: relative;
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  flex: none;
  border: 0;
  background: none;
  cursor: pointer;
  font: inherit;
  color: var(--on-brand-2);
  text-decoration: none;
  transition: background-color 120ms ease, color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.rail__btn--on {
  background: rgba(255, 255, 255, 0.12);
  color: var(--on-brand);
}
/* Brass marker on the active entry (inside the button, since the drawer clips overflow). */
.rail__btn--on::before {
  content: '';
  position: absolute;
  left: 0;
  top: 12px;
  bottom: 12px;
  width: 3px;
  border-radius: 0 3px 3px 0;
  background: var(--brand-accent);
}
@media (hover: hover) and (pointer: fine) {
  .rail__btn:hover {
    color: var(--on-brand);
    background: rgba(255, 255, 255, 0.07);
  }
  .rail__btn--on:hover {
    background: rgba(255, 255, 255, 0.12);
  }
}
.rail__btn:focus-visible,
.rail__logo:focus-visible {
  outline: 2px solid var(--brand-accent);
  outline-offset: 2px;
}
.rail__toggle {
  margin-bottom: 4px;
}
.rail__account {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 0;
  background: none;
  padding: 0;
  font: inherit;
  color: var(--on-brand);
  text-align: left;
  border-radius: 10px;
  cursor: pointer;
}
.rail__account:focus-visible {
  outline: 2px solid var(--brand-accent);
  outline-offset: 2px;
}
.rail__avatar {
  flex: none;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #f0cd85, #c79236);
  color: #2a1d05;
  font-size: 12px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.12);
}
.rail__label {
  flex: 1;
  min-width: 0;
  text-align: left;
  font-size: 14px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.rail__group {
  margin: 12px 12px 2px;
  font-size: 11px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.42);
  white-space: nowrap;
}
.rail__who {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.rail__who-name {
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.rail__who-roles {
  font-size: 11px;
  color: var(--on-brand-2);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Open pane: rows span the width, icon then label. */
.rail--open .rail__inner {
  align-items: stretch;
  padding: 16px 12px;
}
.rail--open .rail__top {
  padding: 0 2px;
}
.rail--open .rail__btn {
  width: 100%;
  justify-content: flex-start;
  padding: 0 10px;
}
.rail--open .rail__account {
  padding: 4px;
}

.shell-header {
  border-bottom: 1px solid var(--line) !important;
  /* Frosted header: content scrolls under it instead of being cut by a solid bar. */
  background: color-mix(in srgb, rgb(var(--v-theme-surface)) 78%, transparent) !important;
  backdrop-filter: saturate(1.6) blur(14px);
  -webkit-backdrop-filter: saturate(1.6) blur(14px);
}
.theme-btn {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  border: 1px solid var(--line);
  background: var(--card);
  color: var(--ink-2);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: color 120ms ease, border-color 120ms ease, transform var(--dur-press) var(--ease-out);
}
@media (hover: hover) and (pointer: fine) {
  .theme-btn:hover {
    color: var(--ink);
    border-color: var(--line-2);
  }
}
.theme-btn:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.theme-pick {
  padding: 6px 12px 8px;
}
.theme-pick__lbl {
  display: block;
  margin-bottom: 6px;
}
.theme-pick__seg {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 2px;
  padding: 3px;
  border-radius: 10px;
  background: var(--line);
}
.theme-pick__opt {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  height: 30px;
  border: 0;
  border-radius: 8px;
  background: none;
  font: inherit;
  font-size: 12px;
  color: var(--ink-2);
  cursor: pointer;
  transition: background-color 120ms ease, color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.theme-pick__opt--on {
  background: var(--card);
  color: var(--ink);
  font-weight: 500;
  box-shadow: var(--shadow-1);
}
.theme-pick__opt:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 1px;
}
.shell-header__title {
  min-width: 0;
  margin-left: 4px;
}
.crumbs {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--ink-3);
  min-width: 0;
}
.crumbs__link {
  color: var(--ink-3);
  text-decoration: none;
}
.crumbs__link:hover {
  color: var(--ink);
}
.crumbs__current {
  color: var(--ink);
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.crumbs__sep {
  color: var(--line-2);
}

.page {
  padding: 28px 40px 40px;
  max-width: 1600px;
}
.page--narrow {
  max-width: 940px;
}
@media (max-width: 959.98px) {
  .page {
    padding: 22px 24px 32px;
  }
}
@media (max-width: 599.98px) {
  .page {
    padding: 16px 16px 24px;
  }
}

.bottom-bar {
  border-top: 1px solid var(--line) !important;
  padding: 12px 16px calc(12px + env(safe-area-inset-bottom)) !important;
  gap: 10px;
}
.bottom-bar :deep(.v-btn) {
  flex: 1;
  height: 48px;
  font-size: 15px;
}
.more__group {
  min-height: 28px;
  font-size: 11px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}
.bottom-nav {
  border-top: 1px solid var(--line) !important;
}
.app-version {
  margin: 32px 0 0;
  text-align: center;
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), 0.5);
  font-variant-numeric: tabular-nums;
}
</style>
