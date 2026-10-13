<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDate, formatMoney } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { expensesApi } from '@/features/expenses/api';
import { reportStatusOptions, reportTypeLabel } from '@/features/expenses/labels';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import type { ExpenseReportListItem, PagedResult, ReportStatus } from '@/features/expenses/types';

const router = useRouter();
const route = useRoute();
const snack = useSnackbar();
const { xs } = useDisplay();

const page = ref(1);
const pageSize = 15;
// Dashboard tiles deep-link here with ?status=...
const allowedStatuses: ReportStatus[] = ['New', 'ForApproval', 'Returned', 'Approved', 'Rejected'];
const initialStatus =
  typeof route.query.status === 'string' && allowedStatuses.includes(route.query.status as ReportStatus)
    ? (route.query.status as ReportStatus)
    : null;
const status = ref<ReportStatus | null>(initialStatus);
const search = ref('');
const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<ExpenseReportListItem> | null>(null);

let searchTimer: ReturnType<typeof setTimeout> | undefined;

async function load() {
  loading.value = true;
  error.value = null;
  try {
    result.value = await expensesApi.list({
      page: page.value,
      pageSize,
      status: status.value ?? undefined,
      search: search.value.trim() || undefined,
    });
  } catch (err) {
    error.value = 'Unable to load your reports.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch([page, status], load);
watch(search, () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    page.value = 1;
    void load();
  }, 250);
});

function open(item: ExpenseReportListItem) {
  router.push({ name: 'report-edit', params: { id: item.id } });
}

function period(item: ExpenseReportListItem): string {
  return item.dateFrom || item.dateTo ? `${formatDate(item.dateFrom)} – ${formatDate(item.dateTo)}` : '—';
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'My reports' }]">
    <template #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :to="{ name: 'report-new' }">New report</v-btn>
    </template>
    <template #actions-mobile>
      <v-btn class="press" icon="mdi-plus" variant="text" :to="{ name: 'report-new' }" aria-label="New report" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">My expense reports</h1>
      <span class="text-body-2 text-medium-emphasis">Drafts stay editable; filed reports are read-only until returned or reopened.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-text-field
          v-model="search"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
          placeholder="Search purpose or reference no."
          class="filters__search"
        />
        <v-select v-model="status" :items="reportStatusOptions" density="compact" hide-details clearable label="Status" class="filters__status" />
        <v-spacer class="d-none d-sm-block" />
        <span v-if="result" class="card-meta d-none d-sm-inline">{{ result.total }} report{{ result.total === 1 ? '' : 's' }}</span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="result && result.items.length === 0 && !loading"
        icon="mdi-file-document-outline"
        :title="search || status ? 'No reports match your filters' : 'You have no expense reports yet'"
        :hint="search || status ? 'Try clearing the search or status filter.' : 'Start a new report, add your expenses, attach receipts, then file it for approval.'"
      >
        <v-btn v-if="!search && !status" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :to="{ name: 'report-new' }">New report</v-btn>
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
              <span class="mono row__ref" :class="{ 'text-medium-emphasis': !item.referenceNo }">{{ item.referenceNo || 'Draft' }}</span>
              <ReportStatusChip :status="item.status" />
            </div>
            <div class="row__purpose">{{ item.purpose || reportTypeLabel[item.reportType] }}</div>
            <div class="row__meta">{{ period(item) }} · {{ item.lineCount }} line{{ item.lineCount === 1 ? '' : 's' }}</div>
          </div>
          <span class="mono row__amt">{{ formatMoney(item.totalAmount) }}</span>
        </button>
      </div>

      <!-- Tablet / desktop: ledger -->
      <div v-else-if="result" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th>Reference</th>
              <th class="d-none d-md-table-cell">Type</th>
              <th>Purpose</th>
              <th>Period</th>
              <th class="r">Lines</th>
              <th class="r">Total</th>
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
              <td class="mono font-weight-medium" :class="{ 'text-medium-emphasis': !item.referenceNo }">{{ item.referenceNo || 'Draft' }}</td>
              <td class="text-medium-emphasis d-none d-md-table-cell">{{ reportTypeLabel[item.reportType] }}</td>
              <td class="text-truncate" style="max-width: 320px">{{ item.purpose || '—' }}</td>
              <td class="text-no-wrap text-medium-emphasis">{{ period(item) }}</td>
              <td class="r mono">{{ item.lineCount }}</td>
              <td class="r mono font-weight-medium">{{ formatMoney(item.totalAmount) }}</td>
              <td><ReportStatusChip :status="item.status" /></td>
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
.filters__status {
  flex: 0 1 200px;
  min-width: 160px;
}
@media (max-width: 599.98px) {
  .filters__search,
  .filters__status {
    flex: 1 1 100%;
    max-width: none;
  }
}
.table-wrap {
  overflow-x: auto;
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
  gap: 2px;
}
.row__top {
  display: flex;
  align-items: center;
  gap: 8px;
}
.row__ref {
  font-size: 13px;
  font-weight: 500;
}
.row__purpose {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.row__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.row__amt {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}
</style>
