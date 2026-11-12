<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppExportButton from '@/shared/ui/AppExportButton.vue';
import { addDaysIso, formatDate, formatMoney, todayIso } from '@/shared/format';
import { saveFile } from '@/shared/download';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { entitlementLookupsApi } from '@/features/entitlements/api';
import type { DepartmentOption } from '@/features/entitlements/types';
import { reportingApi } from '@/features/reporting/api';
import type { DailyMealClaimQuery, DailyMealClaimReport } from '@/features/reporting/types';

// Daily meal claims (Finance requirement 3): for each day, the employees who claimed their meal budget and which
// meals. Finance only; the API enforces it.
const snack = useSnackbar();
const { xs } = useDisplay();

const MEALS = [
  { key: 'breakfast', label: 'Breakfast' },
  { key: 'lunch', label: 'Lunch' },
  { key: 'dinner', label: 'Dinner' },
  { key: 'otMeal', label: 'OT meal' },
  { key: 'other', label: 'Other' },
] as const;

function firstOfMonth(iso: string) {
  return `${iso.slice(0, 8)}01`;
}

const today = todayIso();
const from = ref(firstOfMonth(today));
const to = ref(today);
const departmentId = ref<number | null>(null);
const approvedOnly = ref(false);

const departments = ref<DepartmentOption[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const report = ref<DailyMealClaimReport | null>(null);
const exporting = ref(false);

const periodError = computed(() => {
  if (!from.value || !to.value) return 'Pick a start and an end date.';
  if (to.value < from.value) return 'The end date must be on or after the start date.';
  return null;
});

const weekday = new Intl.DateTimeFormat(undefined, { weekday: 'long' });
function dayLabel(iso: string) {
  const [y, m, d] = iso.split('-').map(Number);
  return `${weekday.format(new Date(y!, m! - 1, d!))}, ${formatDate(iso)}`;
}

function query(): DailyMealClaimQuery {
  return { from: from.value, to: to.value, departmentId: departmentId.value, approvedOnly: approvedOnly.value };
}

async function load() {
  if (periodError.value) {
    report.value = null;
    error.value = null;
    return;
  }
  loading.value = true;
  error.value = null;
  try {
    report.value = await reportingApi.dailyMealClaims(query());
  } catch (err) {
    report.value = null;
    error.value = describeError(err, 'Unable to load the meal claims.');
  } finally {
    loading.value = false;
  }
}

async function exportClaims() {
  exporting.value = true;
  try {
    const file = await reportingApi.exportDailyMealClaims(query());
    saveFile(file.blob, file.fileName);
  } catch (err) {
    snack.fromError(err, 'Unable to export the meal claims.');
  } finally {
    exporting.value = false;
  }
}

function setPeriod(kind: 'month' | 'lastMonth' | 'last7') {
  if (kind === 'month') {
    from.value = firstOfMonth(today);
    to.value = today;
  } else if (kind === 'lastMonth') {
    const lastDay = addDaysIso(firstOfMonth(today), -1);
    from.value = firstOfMonth(lastDay);
    to.value = lastDay;
  } else {
    from.value = addDaysIso(today, -6);
    to.value = today;
  }
}

onMounted(async () => {
  void load();
  try {
    departments.value = await entitlementLookupsApi.departments();
  } catch {
    // The department filter is optional; the report still works without it.
    departments.value = [];
  }
});

watch([from, to, departmentId, approvedOnly], () => void load());
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Meal claims' }]">
    <template #actions>
      <AppExportButton :loading="exporting" :disabled="!!periodError || !report" @export="exportClaims" />
    </template>
    <template #bottom>
      <AppExportButton :loading="exporting" :disabled="!!periodError || !report" @export="exportClaims" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Meal claims</h1>
      <span class="text-body-2 text-medium-emphasis">Who claimed their meal budget each day, from filed and approved reports.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-text-field v-model="from" type="date" label="From" density="compact" hide-details class="filters__date" />
        <v-text-field v-model="to" type="date" label="To" density="compact" hide-details class="filters__date" />
        <v-autocomplete
          v-if="departments.length"
          v-model="departmentId"
          :items="departments"
          item-title="name"
          item-value="id"
          label="Department"
          density="compact"
          hide-details
          clearable
          class="filters__dept"
        />
        <v-switch v-model="approvedOnly" density="compact" hide-details color="primary" label="Approved only" class="flex-grow-0" />
      </div>
      <div class="filters filters--quick">
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('last7')">Last 7 days</v-chip>
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('month')">This month</v-chip>
        <v-chip size="small" variant="outlined" class="press" @click="setPeriod('lastMonth')">Last month</v-chip>
        <v-spacer class="d-none d-sm-block" />
        <span v-if="report" class="card-meta">
          {{ report.days.length }} day{{ report.days.length === 1 ? '' : 's' }} · {{ report.employeeDays }} employee-day{{ report.employeeDays === 1 ? '' : 's' }} ·
          {{ formatMoney(report.total) }}
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
        v-else-if="report && report.days.length === 0 && !loading"
        icon="mdi-silverware-fork-knife"
        title="No meal claims in this period"
        :hint="`No filed or approved meal lines with an expense date from ${formatDate(from)} to ${formatDate(to)}.`"
      />

      <!-- Phone: a header per day, then one row per employee with the meals claimed -->
      <div v-else-if="report && xs" class="rows">
        <template v-for="day in report.days" :key="day.date">
          <div class="row row--day">
            <div class="row__top">
              <div class="row__main">
                <div class="row__name">{{ dayLabel(day.date) }}</div>
                <div class="row__meta">{{ day.employeeCount }} employee{{ day.employeeCount === 1 ? '' : 's' }}</div>
              </div>
              <span class="mono row__amt">{{ formatMoney(day.amount) }}</span>
            </div>
          </div>
          <div v-for="e in day.employees" :key="e.employeeUserId" class="row">
            <div class="row__top">
              <div class="row__main">
                <div class="row__name">{{ e.employeeName }}</div>
                <div v-if="e.departmentName" class="row__meta">{{ e.departmentName }}</div>
              </div>
              <span class="mono row__amt">{{ formatMoney(e.amount) }}</span>
            </div>
            <div class="row__meals">{{ MEALS.filter((m) => e[m.key]).map((m) => m.label).join(' · ') }}</div>
            <div v-if="e.referenceNos.length" class="row__refs mono">{{ e.referenceNos.join(', ') }}</div>
          </div>
        </template>
        <div class="row row--total">
          <div class="row__top">
            <span class="row__name">Total</span>
            <span class="mono row__amt">{{ formatMoney(report.total) }}</span>
          </div>
        </div>
      </div>

      <div v-else-if="report" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th class="sticky-col">Employee</th>
              <th>ERF reference</th>
              <th v-for="m in MEALS" :key="m.key" class="c">{{ m.label }}</th>
              <th class="r">Amount</th>
            </tr>
          </thead>
          <tbody v-for="day in report.days" :key="day.date">
            <tr class="day">
              <td colspan="2" class="sticky-col">
                <strong>{{ dayLabel(day.date) }}</strong>
                <span class="text-medium-emphasis"> · {{ day.employeeCount }} employee{{ day.employeeCount === 1 ? '' : 's' }}</span>
              </td>
              <td v-for="m in MEALS" :key="m.key" class="c mono">{{ day[m.key] || '—' }}</td>
              <td class="r mono">{{ formatMoney(day.amount) }}</td>
            </tr>
            <tr v-for="e in day.employees" :key="e.employeeUserId">
              <td class="sticky-col">
                <div>{{ e.employeeName }}</div>
                <div v-if="e.departmentName" class="text-caption text-medium-emphasis">{{ e.departmentName }}</div>
              </td>
              <td class="mono text-caption">{{ e.referenceNos.join(', ') || '—' }}</td>
              <td v-for="m in MEALS" :key="m.key" class="c">
                <v-icon v-if="e[m.key]" icon="mdi-check" size="small" color="success" :aria-label="m.label" />
              </td>
              <td class="r mono">{{ formatMoney(e.amount) }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="totals">
              <td colspan="2" class="sticky-col">Total</td>
              <td :colspan="MEALS.length" />
              <td class="r mono">{{ formatMoney(report.total) }}</td>
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
.filters__dept {
  flex: 0 1 220px;
  min-width: 180px;
}
@media (max-width: 599.98px) {
  .filters__date {
    flex: 1 1 calc(50% - 6px);
  }
  .filters__dept {
    flex: 1 1 100%;
  }
}
.c {
  text-align: center;
}
.day td {
  background: var(--paper);
  border-top: 2px solid var(--line);
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
.day .sticky-col {
  background: var(--paper);
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
.row__meals {
  margin-top: 4px;
  font-size: 13px;
  color: var(--ink-2);
}
.row__refs {
  margin-top: 2px;
  font-size: 12px;
  color: var(--ink-3);
  overflow-wrap: anywhere;
}
.row--day {
  background: var(--paper);
  border-top: 2px solid var(--line);
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
