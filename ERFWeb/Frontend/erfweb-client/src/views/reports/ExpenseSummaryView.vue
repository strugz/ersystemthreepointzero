<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppExportButton from '@/shared/ui/AppExportButton.vue';
import { addDaysIso, formatDate, formatMoney, todayIso } from '@/shared/format';
import { saveFile, type ExportFormat } from '@/shared/download';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { useSessionStore } from '@/app/stores/session';
import { categoryLabel } from '@/features/expenses/labels';
import type { ExpenseCategory } from '@/features/expenses/types';
import { entitlementLookupsApi } from '@/features/entitlements/api';
import type { DepartmentOption } from '@/features/entitlements/types';
import { reportingApi } from '@/features/reporting/api';
import type { ExpenseSummary, ExpenseSummaryQuery, SummaryScope } from '@/features/reporting/types';

// Expense summary (the web counterpart of the desktop Expense Summary): filed and approved expense
// lines in a period, totalled per employee and category. The scope choices follow the caller's
// roles; the API decides what each scope may see.
const session = useSessionStore();
const snack = useSnackbar();
const { compact } = useLayoutMode();

const isSignatory = session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2');
const isFinance = session.hasRole('Finance');

const scopeOptions = computed(() => {
  const items: { value: SummaryScope; title: string }[] = [{ value: 'Mine', title: 'My reports' }];
  if (isSignatory) items.push({ value: 'Approvals', title: 'Reports I sign for' });
  if (isFinance) items.push({ value: 'All', title: 'All employees' });
  return items;
});

function firstOfMonth(iso: string) {
  return `${iso.slice(0, 8)}01`;
}

const today = todayIso();
const from = ref(firstOfMonth(today));
const to = ref(today);
const scope = ref<SummaryScope>(isFinance ? 'All' : isSignatory ? 'Approvals' : 'Mine');
const departmentId = ref<number | null>(null);
const approvedOnly = ref(false);

const departments = ref<DepartmentOption[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const summary = ref<ExpenseSummary | null>(null);
const exporting = ref(false);

const categories = Object.keys(categoryLabel) as ExpenseCategory[];
// Hide category columns nobody used in the period, so the table fits; exports keep every column.
const usedCategories = computed(() => (summary.value ? categories.filter((c) => summary.value!.byCategory[c] !== 0) : categories));
const showDepartment = computed(() => scope.value !== 'Mine');

const periodError = computed(() => {
  if (!from.value || !to.value) return 'Pick a start and an end date.';
  if (to.value < from.value) return 'The end date must be on or after the start date.';
  return null;
});

function query(): ExpenseSummaryQuery {
  return {
    from: from.value,
    to: to.value,
    scope: scope.value,
    departmentId: showDepartment.value ? departmentId.value : null,
    approvedOnly: approvedOnly.value,
  };
}

async function load() {
  if (periodError.value) {
    summary.value = null;
    error.value = null;
    return;
  }
  loading.value = true;
  error.value = null;
  try {
    summary.value = await reportingApi.expenseSummary(query());
  } catch (err) {
    summary.value = null;
    error.value = describeError(err, 'Unable to load the summary.');
  } finally {
    loading.value = false;
  }
}

async function exportSummary(format: ExportFormat) {
  exporting.value = true;
  try {
    const file = await reportingApi.exportExpenseSummary(query(), format);
    saveFile(file.blob, file.fileName);
  } catch (err) {
    snack.fromError(err, 'Unable to export the summary.');
  } finally {
    exporting.value = false;
  }
}

function setPeriod(kind: 'month' | 'lastMonth' | 'last30') {
  if (kind === 'month') {
    from.value = firstOfMonth(today);
    to.value = today;
  } else if (kind === 'lastMonth') {
    const lastDay = addDaysIso(firstOfMonth(today), -1);
    from.value = firstOfMonth(lastDay);
    to.value = lastDay;
  } else {
    from.value = addDaysIso(today, -29);
    to.value = today;
  }
}

onMounted(async () => {
  void load();
  if (isSignatory || isFinance) {
    try {
      departments.value = await entitlementLookupsApi.departments();
    } catch {
      // The department filter is optional; the summary still works without it.
      departments.value = [];
    }
  }
});

watch([from, to, scope, departmentId, approvedOnly], () => void load());
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Expense summary' }]">
    <template #actions>
      <AppExportButton :formats="['Csv', 'Pdf']" :loading="exporting" :disabled="!!periodError || !summary" @export="exportSummary" />
    </template>
    <template #actions-mobile>
      <AppExportButton compact :formats="['Csv', 'Pdf']" :loading="exporting" :disabled="!!periodError || !summary" @export="exportSummary" />
    </template>

    <div v-if="!compact" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Expense summary</h1>
      <span class="text-body-2 text-medium-emphasis">Filed and approved expenses by expense date, totalled per employee and category.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-text-field v-model="from" type="date" label="From" density="compact" hide-details class="filters__date" />
        <v-text-field v-model="to" type="date" label="To" density="compact" hide-details class="filters__date" />
        <v-select v-if="scopeOptions.length > 1" v-model="scope" :items="scopeOptions" label="Reports" density="compact" hide-details class="filters__scope" />
        <v-autocomplete
          v-if="showDepartment && departments.length"
          v-model="departmentId"
          :items="departments"
          item-title="name"
          item-value="id"
          label="Department"
          density="compact"
          hide-details
          clearable
          class="filters__scope"
        />
        <v-switch v-model="approvedOnly" density="compact" hide-details color="primary" label="Approved only" class="flex-grow-0" />
      </div>
      <div class="filters filters--quick">
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('month')">This month</v-chip>
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('lastMonth')">Last month</v-chip>
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('last30')">Last 30 days</v-chip>
        <v-spacer class="d-none d-sm-block" />
        <span v-if="summary" class="card-meta">
          {{ summary.lineCount }} line{{ summary.lineCount === 1 ? '' : 's' }} · {{ summary.employees.length }} employee{{ summary.employees.length === 1 ? '' : 's' }}
        </span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="periodError" type="info" variant="tonal" class="ma-4">{{ periodError }}</v-alert>
      <v-alert v-else-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="summary && summary.employees.length === 0 && !loading"
        icon="mdi-chart-box-outline"
        title="No expenses in this period"
        :hint="`Nothing filed or approved with an expense date from ${formatDate(from)} to ${formatDate(to)}.`"
      />

      <!-- Phone and tablet (compact layout): one row per employee, the categories they used under the name -->
      <div v-else-if="summary && compact" class="rows">
        <div v-for="(e, i) in summary.employees" :key="e.employeeUserId" class="row stagger-in" :style="{ '--i': Math.min(i, 10) }">
          <div class="row__top">
            <div class="row__main">
              <div class="row__name">{{ e.employeeName }}</div>
              <div class="row__meta">
                <template v-if="e.departmentName">{{ e.departmentName }} · </template>{{ e.lineCount }} line{{ e.lineCount === 1 ? '' : 's' }}
              </div>
            </div>
            <span class="mono row__amt">{{ formatMoney(e.total) }}</span>
          </div>
          <div class="row__cats">
            <span v-for="c in usedCategories.filter((c) => e.byCategory[c])" :key="c">
              {{ categoryLabel[c] }} <span class="mono">{{ formatMoney(e.byCategory[c]) }}</span>
            </span>
          </div>
        </div>
        <div class="row row--total">
          <div class="row__top">
            <span class="row__name">Total</span>
            <span class="mono row__amt">{{ formatMoney(summary.total) }}</span>
          </div>
        </div>
      </div>

      <div v-else-if="summary" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th class="sticky-col">Employee</th>
              <th class="r">Lines</th>
              <th v-for="c in usedCategories" :key="c" class="r">{{ categoryLabel[c] }}</th>
              <th class="r">Total</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(e, i) in summary.employees" :key="e.employeeUserId" class="stagger-in" :style="{ '--i': Math.min(i, 10) }">
              <td class="sticky-col">
                <div>{{ e.employeeName }}</div>
                <div v-if="e.departmentName" class="text-caption text-medium-emphasis">{{ e.departmentName }}</div>
              </td>
              <td class="r mono">{{ e.lineCount }}</td>
              <td v-for="c in usedCategories" :key="c" class="r mono">{{ e.byCategory[c] ? formatMoney(e.byCategory[c]) : '—' }}</td>
              <td class="r mono font-weight-medium">{{ formatMoney(e.total) }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="totals">
              <td class="sticky-col">Total</td>
              <td class="r mono">{{ summary.lineCount }}</td>
              <td v-for="c in usedCategories" :key="c" class="r mono">{{ formatMoney(summary.byCategory[c]) }}</td>
              <td class="r mono">{{ formatMoney(summary.total) }}</td>
            </tr>
          </tfoot>
        </table>
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
.filters--quick {
  padding-top: 0;
  gap: 8px;
}
.filters__date {
  flex: 0 1 170px;
  min-width: 150px;
}
.filters__scope {
  flex: 0 1 220px;
  min-width: 180px;
}
@media (max-width: 959.98px) {
  .filters__date {
    flex: 1 1 calc(50% - 6px);
  }
  .filters__scope {
    flex: 1 1 100%;
  }
}
.totals td {
  font-weight: 600;
  border-top: 2px solid var(--line);
}
.table-wrap {
  overflow-x: auto;
}
.sticky-col {
  position: sticky;
  left: 0;
  z-index: 1;
  background: rgb(var(--v-theme-surface));
  min-width: 180px;
}
.rows {
  display: flex;
  flex-direction: column;
}
.row {
  padding: 12px 16px;
  border-bottom: 1px solid var(--line);
}
.row__top {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}
.row__main {
  flex: 1;
  min-width: 0;
}
.row__name {
  font-size: 14px;
  font-weight: 500;
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
.row__cats {
  display: flex;
  flex-wrap: wrap;
  gap: 2px 12px;
  margin-top: 6px;
  font-size: 12px;
  color: var(--ink-2);
}
.row--total {
  border-top: 2px solid var(--line);
  border-bottom: 0;
}
.row--total .row__top {
  justify-content: space-between;
  font-weight: 600;
}
</style>
