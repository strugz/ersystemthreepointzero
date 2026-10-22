<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDateTime } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { adminApi } from '@/features/admin/api';
import { activeOptions, userSortOptions } from '@/features/admin/labels';
import { roleLabel, roleOptions } from '@/features/auth/labels';
import type { UserRole } from '@/features/auth/types';
import type { Department, PagedResult, SortDirection, UserListItem, UserSort } from '@/features/admin/types';

const router = useRouter();
const snack = useSnackbar();
const { xs } = useDisplay();

const page = ref(1);
const pageSize = 20;
const sort = ref<UserSort>('fullname');
const direction = ref<SortDirection>('Asc');
const search = ref('');
const departmentId = ref<number | null>(null);
const role = ref<UserRole | null>(null);
const isActive = ref<boolean | null>(true);

const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<UserListItem> | null>(null);
const departments = ref<Department[]>([]);

let searchTimer: ReturnType<typeof setTimeout> | undefined;

async function load() {
  loading.value = true;
  error.value = null;
  try {
    result.value = await adminApi.listUsers({
      page: page.value,
      pageSize,
      sort: sort.value,
      direction: direction.value,
      search: search.value.trim() || undefined,
      departmentId: departmentId.value ?? undefined,
      role: role.value ?? undefined,
      isActive: isActive.value ?? undefined,
    });
  } catch (err) {
    error.value = 'Unable to load users.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void load();
  adminApi
    .departments(false)
    .then((d) => (departments.value = d))
    .catch(() => undefined); // The filter is optional; the list still works without it.
});

watch(page, load);
watch([sort, direction, departmentId, role, isActive], () => {
  page.value = 1;
  void load();
});
watch(search, () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    page.value = 1;
    void load();
  }, 250);
});

const hasFilters = () => !!search.value || departmentId.value !== null || role.value !== null || isActive.value !== true;

function clearFilters() {
  search.value = '';
  departmentId.value = null;
  role.value = null;
  isActive.value = true;
}

function open(item: UserListItem) {
  router.push({ name: 'admin-user', params: { id: item.id } });
}

function sortBy(key: UserSort) {
  if (sort.value === key) {
    direction.value = direction.value === 'Asc' ? 'Desc' : 'Asc';
  } else {
    sort.value = key;
    direction.value = 'Asc';
  }
}

function sortIcon(key: UserSort): string | undefined {
  if (sort.value !== key) return undefined;
  return direction.value === 'Asc' ? 'mdi-arrow-up' : 'mdi-arrow-down';
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Users' }]">
    <template #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-account-plus-outline" :to="{ name: 'admin-user-new' }">New user</v-btn>
    </template>
    <template #actions-mobile>
      <v-btn class="press" icon="mdi-account-plus-outline" variant="text" :to="{ name: 'admin-user-new' }" aria-label="New user" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Users</h1>
      <span class="text-body-2 text-medium-emphasis">Accounts, roles and signatures. A granted role applies at once.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-text-field
          v-model="search"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
          placeholder="Search name, username or email"
          class="filters__search"
        />
        <v-select
          v-model="departmentId"
          :items="departments.map((d) => ({ value: d.id, title: d.name }))"
          density="compact"
          hide-details
          clearable
          label="Department"
          class="filters__select"
        />
        <v-select v-model="role" :items="roleOptions" density="compact" hide-details clearable label="Role" class="filters__select" />
        <v-select v-model="isActive" :items="activeOptions" density="compact" hide-details clearable label="Status" class="filters__select filters__select--sm" />
        <v-select
          v-if="xs"
          v-model="sort"
          :items="userSortOptions"
          density="compact"
          hide-details
          label="Sort by"
          class="filters__select filters__select--sm"
        />
        <v-spacer class="d-none d-sm-block" />
        <span v-if="result" class="card-meta d-none d-sm-inline">{{ result.total }} account{{ result.total === 1 ? '' : 's' }}</span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="result && result.items.length === 0 && !loading"
        icon="mdi-account-search-outline"
        :title="hasFilters() ? 'No users match your filters' : 'No users yet'"
        :hint="hasFilters() ? 'Try clearing the search or filters.' : 'Create the first account.'"
      >
        <v-btn v-if="hasFilters()" class="press" variant="text" @click="clearFilters">Clear filters</v-btn>
        <v-btn v-else class="press" color="primary" variant="flat" prepend-icon="mdi-account-plus-outline" :to="{ name: 'admin-user-new' }">New user</v-btn>
      </AppEmptyState>

      <!-- Phone: list rows -->
      <div v-else-if="result && xs" class="rows">
        <button
          v-for="(item, i) in result.items"
          :key="item.id"
          type="button"
          class="row press stagger-in"
          :style="{ '--i': Math.min(i, 10) }"
          @click="open(item)"
        >
          <div class="row__main">
            <div class="row__top">
              <span class="row__name">{{ item.fullName }}</span>
              <span v-if="!item.isActive" class="pill pill--neutral">Inactive</span>
            </div>
            <div class="row__meta mono">{{ item.username }}<template v-if="item.departmentName"> · {{ item.departmentName }}</template></div>
            <div v-if="item.activeRoles.length" class="row__roles">
              <span v-for="r in item.activeRoles" :key="r" class="pill pill--success">{{ roleLabel(r) }}</span>
            </div>
          </div>
          <v-icon icon="mdi-chevron-right" size="18" class="text-medium-emphasis" />
        </button>
      </div>

      <!-- Tablet / desktop: ledger -->
      <div v-else-if="result" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th><button type="button" class="sort" @click="sortBy('fullname')">Name <v-icon v-if="sortIcon('fullname')" :icon="sortIcon('fullname')" size="14" /></button></th>
              <th><button type="button" class="sort" @click="sortBy('username')">Username <v-icon v-if="sortIcon('username')" :icon="sortIcon('username')" size="14" /></button></th>
              <th class="d-none d-md-table-cell"><button type="button" class="sort" @click="sortBy('department')">Department <v-icon v-if="sortIcon('department')" :icon="sortIcon('department')" size="14" /></button></th>
              <th>Roles</th>
              <th class="d-none d-lg-table-cell">Signature</th>
              <th class="d-none d-lg-table-cell"><button type="button" class="sort" @click="sortBy('created')">Created <v-icon v-if="sortIcon('created')" :icon="sortIcon('created')" size="14" /></button></th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(item, i) in result.items"
              :key="item.id"
              class="row-link stagger-in"
              :style="{ '--i': Math.min(i, 10) }"
              tabindex="0"
              @click="open(item)"
              @keydown.enter="open(item)"
            >
              <td>
                <div class="font-weight-medium">{{ item.fullName }}</div>
                <div v-if="item.position" class="text-caption text-medium-emphasis">{{ item.position }}</div>
              </td>
              <td class="mono">{{ item.username }}</td>
              <td class="text-medium-emphasis d-none d-md-table-cell">{{ item.departmentName || '—' }}</td>
              <td>
                <div class="d-flex flex-wrap ga-1">
                  <span v-for="r in item.activeRoles" :key="r" class="pill pill--success">{{ roleLabel(r) }}</span>
                  <span v-if="!item.activeRoles.length" class="text-medium-emphasis">Employee</span>
                </div>
              </td>
              <td class="d-none d-lg-table-cell">
                <v-icon :icon="item.hasSignature ? 'mdi-check-circle-outline' : 'mdi-alert-circle-outline'" size="18" :color="item.hasSignature ? 'success' : 'warning'" :aria-label="item.hasSignature ? 'Signature on file' : 'No signature'" />
              </td>
              <td class="text-no-wrap text-medium-emphasis d-none d-lg-table-cell">{{ formatDateTime(item.createdAt) }}</td>
              <td><span class="pill" :class="item.isActive ? 'pill--success' : 'pill--neutral'">{{ item.isActive ? 'Active' : 'Inactive' }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="result && result.totalPages > 1" class="d-flex justify-center py-2">
        <v-pagination v-model="page" :length="result.totalPages" density="comfortable" :total-visible="xs ? 5 : 7" />
      </div>
    </v-card>
  </AuthenticatedLayout>
</template>

<style scoped>
.filters {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  padding: 12px;
}
.filters__search {
  flex: 1 1 240px;
  max-width: 360px;
}
.filters__select {
  flex: 0 1 200px;
  min-width: 160px;
}
.filters__select--sm {
  flex-basis: 150px;
  min-width: 130px;
}
@media (max-width: 599.98px) {
  .filters__search,
  .filters__select {
    flex: 1 1 100%;
    max-width: none;
  }
}
.table-wrap {
  overflow-x: auto;
}
.sort {
  background: none;
  border: 0;
  padding: 0;
  font: inherit;
  color: inherit;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 2px;
}
.sort:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.row-link {
  cursor: pointer;
}
.row-link:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
.rows {
  display: flex;
  flex-direction: column;
}
.row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 16px;
  min-height: 64px;
  border: 0;
  border-bottom: 1px solid var(--line);
  background: none;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
}
.row:last-child {
  border-bottom: 0;
}
.row__main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}
.row__top {
  display: flex;
  align-items: center;
  gap: 8px;
}
.row__name {
  font-size: 14px;
  font-weight: 500;
}
.row__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.row__roles {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
</style>
