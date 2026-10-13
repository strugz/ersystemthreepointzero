<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { RouteLocationRaw } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import { roleLabel } from '@/features/auth/labels';
import { entitlementReaderRoles, hasAnyRole, rateNavRoles } from '@/features/entitlements/access';
import AppSnackbar from '@/shared/ui/AppSnackbar.vue';

/**
 * Application shell.
 *  - Desktop / tablet (>= 600px): 72px icon rail on the left, 64px white header with a breadcrumb.
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

// Signatory structure entries: scope for every entitlement reader; rates for all of them except a
// System Administrator who holds no other reader role (they only manage scope). Gating is display only.
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
    { label: 'Signatory structure', items: entitlementNav.value },
    { label: 'Administration', items: adminNav.value },
  ].filter((g) => g.items.length > 0),
);

const entitlementRouteNames = new Set(['scope', 'rate-profiles', 'rate-profile']);

const roleSummary = computed(() => (session.user?.roles ?? []).map(roleLabel).join(' · '));
const moreOpen = ref(false);
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

async function logout() {
  await session.logout();
  await router.push({ name: 'login' });
}
</script>

<template>
  <!-- Icon rail: tablet and desktop -->
  <v-navigation-drawer v-if="!xs" permanent rail rail-width="72" class="rail" color="surface">
    <div class="rail__inner">
      <router-link :to="{ name: 'home' }" class="rail__logo press" aria-label="ERFWeb home">
        <img src="/icon-192.png" alt="" class="rail__logo-img" />
      </router-link>

      <nav class="rail__nav" aria-label="Main">
        <v-tooltip v-for="item in nav" :key="item.title" :text="item.title" location="end" open-delay="300">
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
                </a>
              </template>
            </router-link>
          </template>
        </v-tooltip>

        <template v-for="group in navGroups" :key="group.label">
          <div class="rail__sep" role="separator" :aria-label="group.label" />
          <v-tooltip v-for="item in group.items" :key="item.title" :text="item.title" location="end" open-delay="300">
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
                  </a>
                </template>
              </router-link>
            </template>
          </v-tooltip>
        </template>
      </nav>

      <v-menu location="end bottom" :offset="8">
        <template #activator="{ props: menu }">
          <button v-bind="menu" class="rail__avatar press" type="button" :aria-label="`Account: ${session.user?.fullName ?? ''}`">
            {{ initials }}
          </button>
        </template>
        <v-card min-width="240" class="pa-1">
          <v-list density="compact" class="py-0">
            <v-list-item :title="session.user?.fullName" :subtitle="roleSummary" />
            <v-divider class="my-1" />
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
      <div v-else class="rail__logo rail__logo--sm ml-3" aria-hidden="true"><img src="/icon-192.png" alt="" class="rail__logo-img" /></div>
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
      <span v-if="session.user" class="text-body-2 text-medium-emphasis mr-4 d-none d-md-inline">
        {{ session.user.fullName }}
      </span>
      <div class="d-flex align-center ga-2 mr-8">
        <slot name="actions" />
      </div>
    </template>
  </v-app-bar>

  <v-main>
    <div class="page page-enter" :class="{ 'page--narrow': narrow }">
      <slot />
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
          <v-list-item prepend-icon="mdi-logout" title="Sign out" @click="logout" />
        </v-list>
      </v-card>
    </v-menu>
  </v-bottom-navigation>

  <AppSnackbar />
</template>

<style scoped>
.rail {
  border-right: 1px solid var(--line) !important;
}
.rail__inner {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 0;
  gap: 8px;
}
.rail__logo {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  margin-bottom: 16px;
}
.rail__logo-img {
  width: 100%;
  height: 100%;
  display: block;
  object-fit: contain;
}
.rail__logo--sm {
  width: 32px;
  height: 32px;
  margin-bottom: 0;
}
.rail__nav {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}
.rail__sep {
  width: 28px;
  height: 1px;
  margin: 4px auto;
  background: var(--line);
}
.rail__btn {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--ink-3);
  text-decoration: none;
  transition: background-color 120ms ease, color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.rail__btn--on {
  background: var(--navy-soft);
  color: var(--navy);
}
@media (hover: hover) and (pointer: fine) {
  .rail__btn:hover {
    color: var(--ink);
    background: var(--line);
  }
  .rail__btn--on:hover {
    background: var(--navy-soft);
    color: var(--navy);
  }
}
.rail__btn:focus-visible,
.rail__avatar:focus-visible,
.rail__logo:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.rail__avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: 0;
  background: var(--navy-soft);
  color: var(--navy);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}

.shell-header {
  border-bottom: 1px solid var(--line) !important;
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
  font-weight: 500;
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
</style>
