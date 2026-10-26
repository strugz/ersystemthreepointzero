<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppExportButton from '@/shared/ui/AppExportButton.vue';
import { formatMoney } from '@/shared/format';
import { saveFile } from '@/shared/download';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { useSessionStore } from '@/app/stores/session';
import { categoryLabel } from '@/features/expenses/labels';
import type { ExpenseCategory } from '@/features/expenses/types';
import { reportingApi } from '@/features/reporting/api';
import type { CostCenterBudgetReport, CostCenterBudgetRow } from '@/features/reporting/types';

// Cost center budgets: each department's overall budget for a calendar year against what its
// employees spent (filed and approved expense lines dated in the year). Monitoring only. The API
// decides which cost centers the caller sees: all for Finance, a signatory's covered departments.
const session = useSessionStore();
const snack = useSnackbar();
const { xs } = useDisplay();

const isFinance = session.hasRole('Finance');
const thisYear = new Date().getFullYear();
// Budgets are set for the current year; earlier years stay saved and can be looked back on here.
const years = [thisYear, thisYear - 1, thisYear - 2, thisYear - 3, thisYear - 4];

const year = ref(thisYear);
const approvedOnly = ref(false);
const loading = ref(false);
const error = ref<string | null>(null);
const report = ref<CostCenterBudgetReport | null>(null);
const exporting = ref(false);
const expanded = ref<number | null>(null);

const categories = Object.keys(categoryLabel) as ExpenseCategory[];

async function load() {
  loading.value = true;
  error.value = null;
  try {
    report.value = await reportingApi.costCenterBudgets(year.value, approvedOnly.value);
  } catch (err) {
    report.value = null;
    error.value = describeError(err, 'Unable to load the budgets.');
  } finally {
    loading.value = false;
  }
}

async function exportBudgets() {
  exporting.value = true;
  try {
    const file = await reportingApi.exportCostCenterBudgets(year.value, approvedOnly.value);
    saveFile(file.blob, file.fileName);
  } catch (err) {
    snack.fromError(err, 'Unable to export the budgets.');
  } finally {
    exporting.value = false;
  }
}

function toggle(row: CostCenterBudgetRow) {
  expanded.value = expanded.value === row.costCenterId ? null : row.costCenterId;
}

/** Categories the cost center used, so the employee table fits; the export keeps every column. */
function usedCategories(row: CostCenterBudgetRow) {
  return categories.filter((c) => row.byCategory[c] !== 0);
}

/** Bar fill, capped at 100; null without a usable budget. */
function barValue(row: CostCenterBudgetRow) {
  if (row.percentUsed !== null) return Math.min(row.percentUsed, 100);
  return row.overBudget ? 100 : null;
}

function barColor(row: CostCenterBudgetRow) {
  if (row.overBudget) return 'error';
  return (row.percentUsed ?? 0) >= 90 ? 'warning' : 'primary';
}

const totalRemaining = computed(() => (report.value ? report.value.totalBudget - report.value.totalUsed : 0));
const emptyHint = computed(() =>
  isFinance
    ? 'No active cost centers yet. The System Administrator adds them, with their yearly budget, in Master data.'
    : 'None of the departments your scope covers has an active cost center. Ask the System Administrator to add one in Master data.',
);

onMounted(load);
watch([year, approvedOnly], () => {
  expanded.value = null;
  void load();
});
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Budgets' }]">
    <template #actions>
      <AppExportButton :loading="exporting" :disabled="!report || report.costCenters.length === 0" @export="exportBudgets" />
    </template>
    <template #bottom>
      <AppExportButton :loading="exporting" :disabled="!report || report.costCenters.length === 0" @export="exportBudgets" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Budgets</h1>
      <span class="text-body-2 text-medium-emphasis">Each department's yearly budget against what its employees spent. For monitoring only.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-select v-model="year" :items="years" label="Year" density="compact" hide-details class="filters__year" />
        <v-switch v-model="approvedOnly" density="compact" hide-details color="primary" label="Approved only" class="flex-grow-0" />
        <v-spacer class="d-none d-sm-block" />
        <span v-if="report && report.costCenters.length" class="card-meta">
          Budget {{ formatMoney(report.totalBudget) }} · Used {{ formatMoney(report.totalUsed) }} · Remaining
          <span :class="{ 'text-error': totalRemaining < 0 }">{{ formatMoney(totalRemaining) }}</span>
        </span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="report && report.costCenters.length === 0 && !loading"
        icon="mdi-chart-donut"
        title="No cost centers to show"
        :hint="emptyHint"
      />

      <div v-else-if="report" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th>Cost center</th>
              <th class="r">Budget {{ report.year }}</th>
              <th class="r">Used</th>
              <th class="r">Remaining</th>
              <th class="usage">% used</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="(row, i) in report.costCenters" :key="row.costCenterId">
              <tr
                class="stagger-in budget-row"
                :style="{ '--i': Math.min(i, 10) }"
                tabindex="0"
                role="button"
                :aria-expanded="expanded === row.costCenterId"
                @click="toggle(row)"
                @keydown.enter.prevent="toggle(row)"
                @keydown.space.prevent="toggle(row)"
              >
                <td>
                  <div class="d-flex align-center ga-1">
                    <v-icon size="small" :icon="expanded === row.costCenterId ? 'mdi-chevron-down' : 'mdi-chevron-right'" />
                    <span class="mono">{{ row.code }}</span>
                    <span>· {{ row.name }}</span>
                  </div>
                  <div class="text-caption text-medium-emphasis pl-6">{{ row.departmentName }}</div>
                </td>
                <td class="r mono">
                  <span v-if="row.budget !== null">{{ formatMoney(row.budget) }}</span>
                  <span v-else class="text-medium-emphasis">Not set</span>
                </td>
                <td class="r mono">{{ formatMoney(row.used) }}</td>
                <td class="r mono" :class="{ 'text-error font-weight-medium': row.overBudget }">
                  {{ row.remaining === null ? '—' : formatMoney(row.remaining) }}
                </td>
                <td class="usage">
                  <div v-if="barValue(row) !== null" class="d-flex align-center ga-2">
                    <v-progress-linear :model-value="barValue(row)!" :color="barColor(row)" height="6" rounded class="flex-grow-1" />
                    <span class="mono usage__pct" :class="{ 'text-error': row.overBudget }">
                      {{ row.percentUsed === null ? 'Over' : `${row.percentUsed}%` }}
                    </span>
                  </div>
                  <span v-else class="text-medium-emphasis">—</span>
                </td>
              </tr>
              <tr v-if="expanded === row.costCenterId" class="detail-row">
                <td colspan="5">
                  <p v-if="row.employees.length === 0" class="text-body-2 text-medium-emphasis ma-0">Nothing spent in {{ report.year }}.</p>
                  <div v-else class="table-wrap">
                    <table class="ledger ledger--inner">
                      <thead>
                        <tr>
                          <th>Employee</th>
                          <th class="r">Lines</th>
                          <th v-for="c in usedCategories(row)" :key="c" class="r">{{ categoryLabel[c] }}</th>
                          <th class="r">Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="e in row.employees" :key="e.employeeUserId">
                          <td>{{ e.employeeName }}</td>
                          <td class="r mono">{{ e.lineCount }}</td>
                          <td v-for="c in usedCategories(row)" :key="c" class="r mono">{{ e.byCategory[c] ? formatMoney(e.byCategory[c]) : '—' }}</td>
                          <td class="r mono font-weight-medium">{{ formatMoney(e.total) }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
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
.filters__year {
  flex: 0 1 140px;
  min-width: 120px;
}
.budget-row {
  cursor: pointer;
}
.budget-row:focus-visible {
  outline: 2px solid rgb(var(--v-theme-primary));
  outline-offset: -2px;
}
.usage {
  min-width: 160px;
}
.usage__pct {
  min-width: 48px;
  text-align: right;
}
.detail-row > td {
  padding: 8px 12px 16px 36px;
}
.ledger--inner {
  font-size: 0.875rem;
}
</style>
