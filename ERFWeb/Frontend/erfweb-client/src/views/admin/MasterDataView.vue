<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { Ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import MasterDataTable from '@/features/admin/components/MasterDataTable.vue';
import type { Draft, MasterDataColumn, MasterDataField } from '@/features/admin/components/MasterDataTable.vue';
import type { CostCenter, Currency, Department, ExpenseCategoryInfo, FareMode, Site } from '@/features/admin/types';

const route = useRoute();
const router = useRouter();
const snack = useSnackbar();
const { xs } = useDisplay();

type Tab = 'departments' | 'cost-centers' | 'currencies' | 'sites' | 'fare-modes' | 'categories';
const tabs: { value: Tab; title: string; icon: string }[] = [
  { value: 'departments', title: 'Departments', icon: 'mdi-office-building-outline' },
  { value: 'cost-centers', title: 'Cost centers', icon: 'mdi-sitemap-outline' },
  { value: 'currencies', title: 'Currencies', icon: 'mdi-currency-usd' },
  { value: 'sites', title: 'Sites', icon: 'mdi-map-marker-outline' },
  { value: 'fare-modes', title: 'Fare modes', icon: 'mdi-taxi' },
  { value: 'categories', title: 'Expense categories', icon: 'mdi-shape-outline' },
];
const isTab = (v: unknown): v is Tab => tabs.some((t) => t.value === v);
const tab = ref<Tab>(isTab(route.query.tab) ? route.query.tab : 'departments');
watch(tab, (t) => router.replace({ query: { ...route.query, tab: t } }));

/** One loader per entity, each with its own loading / error / saving state. */
function useEntity<T>(fetch: () => Promise<T[]>) {
  const items = ref<T[]>([]) as Ref<T[]>;
  const loading = ref(false);
  const loaded = ref(false);
  const error = ref<string | null>(null);
  const saving = ref(false);
  const errors = ref<Record<string, string[]> | null>(null);
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      items.value = await fetch();
      loaded.value = true;
    } catch (err) {
      error.value = 'Unable to load this list.';
      snack.fromError(err, error.value);
    } finally {
      loading.value = false;
    }
  }
  return { items, loading, loaded, error, saving, errors, load };
}

const departments = useEntity<Department>(() => adminApi.departments());
const costCenters = useEntity<CostCenter>(() => adminApi.costCenters());
const currencies = useEntity<Currency>(() => adminApi.currencies());
const sites = useEntity<Site>(() => adminApi.sites());
const fareModes = useEntity<FareMode>(() => adminApi.fareModes());
const categories = useEntity<ExpenseCategoryInfo>(() => adminApi.expenseCategories());

const entityFor = { departments, 'cost-centers': costCenters, currencies, sites, 'fare-modes': fareModes, categories } as const;

// Load lazily on first visit of each tab; departments are also needed for the cost-center form.
watch(
  tab,
  (t) => {
    const e = entityFor[t];
    if (!e.loaded.value && !e.loading.value) void e.load();
    if (t === 'cost-centers' && !departments.loaded.value && !departments.loading.value) void departments.load();
  },
  { immediate: true },
);

/**
 * Save helper shared by the four editable entities: runs create or update, replaces the row in place,
 * closes the dialog and maps server field errors. A 409 reloads the whole list.
 */
async function runSave<T>(
  entity: ReturnType<typeof useEntity<T>>,
  table: { close: () => void } | null,
  existing: T | null,
  action: () => Promise<T>,
  sameRow: (a: T, b: T) => boolean,
  what: string,
) {
  entity.saving.value = true;
  entity.errors.value = null;
  try {
    const saved = await action();
    if (existing) {
      entity.items.value = entity.items.value.map((i) => (sameRow(i, existing) ? saved : i));
    } else {
      entity.items.value = [...entity.items.value, saved];
    }
    table?.close();
    snack.success(`${what} ${existing ? 'updated' : 'added'}.`);
  } catch (err) {
    snack.fromError(err, `Unable to save the ${what.toLowerCase()}.`);
    if (err instanceof ApiError) {
      if (err.status === 409) await entity.load();
      if (err.problem?.errors) entity.errors.value = err.problem.errors;
    }
  } finally {
    entity.saving.value = false;
  }
}

const str = (d: Draft, k: string) => String(d[k] ?? '');
const strOrNull = (d: Draft, k: string) => str(d, k) || null;
const bool = (d: Draft, k: string) => d[k] === true;
const num = (d: Draft, k: string): number | null => (d[k] === null || d[k] === '' ? null : Number(d[k]));

// ---- departments ------------------------------------------------------------
const departmentsTable = ref<{ close: () => void } | null>(null);
const departmentColumns: MasterDataColumn<Department>[] = [
  { key: 'code', label: 'Code', mono: true },
  { key: 'name', label: 'Name' },
  { key: 'reviewedBy', label: 'Reviewed by (R47)', hideOnPhone: true, render: (d) => d.reviewedBy || 'Rules default' },
];
const departmentFields: MasterDataField[] = [
  { key: 'code', label: 'Code', type: 'text', maxlength: 20, upper: true },
  { key: 'name', label: 'Name', type: 'text', required: true, maxlength: 100 },
  {
    key: 'reviewedBy',
    label: 'Reviewed by (R47)',
    type: 'text',
    maxlength: 60,
    hint: "Printed in REVIEWED BY for this department's employees unless an employee has their own; leave blank to use the default on the Rules page",
  },
  { key: 'isActive', label: 'Active', type: 'switch' },
];
const departmentDraft = (d: Department): Draft => ({ code: d.code ?? '', name: d.name, reviewedBy: d.reviewedBy ?? '', isActive: d.isActive });
function saveDepartment(draft: Draft, existing: Department | null) {
  const input = { code: strOrNull(draft, 'code'), name: str(draft, 'name'), reviewedBy: str(draft, 'reviewedBy').trim(), isActive: bool(draft, 'isActive') };
  void runSave(
    departments,
    departmentsTable.value,
    existing,
    () => (existing ? adminApi.updateDepartment(existing.id, { ...input, rowVersion: existing.rowVersion }) : adminApi.createDepartment(input)),
    (a, b) => a.id === b.id,
    'Department',
  );
}

// ---- cost centers -----------------------------------------------------------
const costCentersTable = ref<{ close: () => void } | null>(null);
const costCenterColumns: MasterDataColumn<CostCenter>[] = [
  { key: 'code', label: 'Code', mono: true },
  { key: 'name', label: 'Name' },
  { key: 'departmentName', label: 'Department', hideOnPhone: true },
];
const costCenterFields = computed<MasterDataField[]>(() => [
  { key: 'code', label: 'Code', type: 'text', required: true, maxlength: 20, upper: true },
  { key: 'name', label: 'Name', type: 'text', required: true, maxlength: 100 },
  {
    key: 'departmentId',
    label: 'Department',
    type: 'select',
    items: departments.items.value.filter((d) => d.isActive).map((d) => ({ value: d.id, title: d.name })),
    hint: 'Optional; limits which accounts can pick this cost center',
  },
  { key: 'isActive', label: 'Active', type: 'switch' },
]);
const costCenterDraft = (c: CostCenter): Draft => ({ code: c.code, name: c.name, departmentId: c.departmentId, isActive: c.isActive });
function saveCostCenter(draft: Draft, existing: CostCenter | null) {
  const input = { code: str(draft, 'code'), name: str(draft, 'name'), departmentId: num(draft, 'departmentId'), isActive: bool(draft, 'isActive') };
  void runSave(
    costCenters,
    costCentersTable.value,
    existing,
    () => (existing ? adminApi.updateCostCenter(existing.id, { ...input, rowVersion: existing.rowVersion }) : adminApi.createCostCenter(input)),
    (a, b) => a.id === b.id,
    'Cost center',
  );
}

// ---- currencies -------------------------------------------------------------
const currenciesTable = ref<{ close: () => void } | null>(null);
const currencyColumns: MasterDataColumn<Currency>[] = [
  { key: 'code', label: 'Code', mono: true },
  { key: 'name', label: 'Name' },
  { key: 'isDefault', label: 'Default', render: (c) => (c.isDefault ? 'Default' : '') },
];
const currencyFields: MasterDataField[] = [
  { key: 'code', label: 'ISO code', type: 'text', required: true, maxlength: 3, upper: true, readonlyOnEdit: true, hint: 'e.g. PHP, USD' },
  { key: 'name', label: 'Name', type: 'text', required: true, maxlength: 100 },
  { key: 'isDefault', label: 'Default currency for new reports', type: 'switch' },
  { key: 'isActive', label: 'Active', type: 'switch' },
];
const currencyDraft = (c: Currency): Draft => ({ code: c.code, name: c.name, isDefault: c.isDefault, isActive: c.isActive });
function saveCurrency(draft: Draft, existing: Currency | null) {
  const input = { code: str(draft, 'code'), name: str(draft, 'name'), isDefault: bool(draft, 'isDefault'), isActive: bool(draft, 'isActive') };
  void runSave(
    currencies,
    currenciesTable.value,
    existing,
    async () => {
      const saved = existing ? await adminApi.updateCurrency(existing.code, { ...input, rowVersion: existing.rowVersion }) : await adminApi.createCurrency(input);
      // Only one currency can be the default; the server moves the flag, so refresh the others.
      if (saved.isDefault) await currencies.load();
      return saved;
    },
    (a, b) => a.code === b.code,
    'Currency',
  );
}

// ---- sites ------------------------------------------------------------------
const sitesTable = ref<{ close: () => void } | null>(null);
const siteColumns: MasterDataColumn<Site>[] = [
  { key: 'code', label: 'Code', mono: true },
  { key: 'name', label: 'Name' },
];
const siteFields: MasterDataField[] = [
  { key: 'code', label: 'Code', type: 'text', required: true, maxlength: 20, upper: true },
  { key: 'name', label: 'Name', type: 'text', required: true, maxlength: 100 },
  { key: 'isActive', label: 'Active', type: 'switch' },
];
const siteDraft = (s: Site): Draft => ({ code: s.code, name: s.name, isActive: s.isActive });
function saveSite(draft: Draft, existing: Site | null) {
  const input = { code: str(draft, 'code'), name: str(draft, 'name'), isActive: bool(draft, 'isActive') };
  void runSave(
    sites,
    sitesTable.value,
    existing,
    () => (existing ? adminApi.updateSite(existing.id, { ...input, rowVersion: existing.rowVersion }) : adminApi.createSite(input)),
    (a, b) => a.id === b.id,
    'Site',
  );
}

// ---- fare modes -------------------------------------------------------------
// Sort order and the legacy tbFare id are typed as text; anything that is not a whole number is
// sent as-is for the server to reject with a field error.
const fareModesTable = ref<{ close: () => void } | null>(null);
const fareModeColumns: MasterDataColumn<FareMode>[] = [
  { key: 'code', label: 'Code', mono: true },
  { key: 'name', label: 'Name' },
  { key: 'sortOrder', label: 'Order', mono: true, hideOnPhone: true },
  {
    key: 'requiresDetail',
    label: 'Notes',
    hideOnPhone: true,
    render: (m) => [m.requiresDetail ? 'Employee types the mode' : '', m.receiptExpected ? 'Receipt expected' : ''].filter(Boolean).join(' · '),
  },
];
const fareModeFields: MasterDataField[] = [
  { key: 'code', label: 'Code', type: 'text', required: true, maxlength: 20, upper: true, hint: 'e.g. TAXI, GRAB, JEEP' },
  { key: 'name', label: 'Name', type: 'text', required: true, maxlength: 60 },
  { key: 'sortOrder', label: 'Sort order', type: 'text', required: true, maxlength: 4, hint: 'Lower numbers are listed first; Others is 999' },
  { key: 'legacyFareId', label: 'Legacy tbFare id', type: 'text', maxlength: 9, hint: 'Optional; the matching desktop fare' },
  { key: 'requiresDetail', label: 'Employee types the mode (like Others)', type: 'switch' },
  { key: 'receiptExpected', label: 'Usually has an official receipt', type: 'switch' },
  { key: 'isActive', label: 'Active', type: 'switch' },
];
const fareModeDraft = (m: FareMode): Draft => ({
  code: m.code,
  name: m.name,
  sortOrder: String(m.sortOrder),
  legacyFareId: m.legacyFareId === null ? '' : String(m.legacyFareId),
  requiresDetail: m.requiresDetail,
  receiptExpected: m.receiptExpected,
  isActive: m.isActive,
});
function saveFareMode(draft: Draft, existing: FareMode | null) {
  const input = {
    code: str(draft, 'code'),
    name: str(draft, 'name'),
    sortOrder: Number(str(draft, 'sortOrder')),
    legacyFareId: num(draft, 'legacyFareId'),
    requiresDetail: bool(draft, 'requiresDetail'),
    receiptExpected: bool(draft, 'receiptExpected'),
    isActive: bool(draft, 'isActive'),
  };
  void runSave(
    fareModes,
    fareModesTable.value,
    existing,
    async () => {
      const saved = existing ? await adminApi.updateFareMode(existing.id, { ...input, rowVersion: existing.rowVersion }) : await adminApi.createFareMode(input);
      // The list is ordered by sort order; reload so a changed order lands in place.
      await fareModes.load();
      return saved;
    },
    (a, b) => a.id === b.id,
    'Fare mode',
  );
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Master data' }]">
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-4">
      <h1 class="page-title">Master data</h1>
      <span class="text-body-2 text-medium-emphasis">Reference lists shared by expense reports and the R47. Deactivate rather than delete.</span>
    </div>

    <v-tabs v-model="tab" color="primary" density="comfortable" :show-arrows="xs" class="mb-4 tabs">
      <v-tab v-for="t in tabs" :key="t.value" :value="t.value" class="press text-none">
        <v-icon :icon="t.icon" size="18" class="mr-2 d-none d-sm-inline-flex" />{{ t.title }}
      </v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="departments">
        <MasterDataTable
          ref="departmentsTable"
          title="Departments"
          singular="department"
          :items="departments.items.value"
          :columns="departmentColumns"
          :fields="departmentFields"
          :key-of="(d: Department) => d.id"
          :to-draft="departmentDraft"
          :loading="departments.loading.value"
          :error="departments.error.value"
          :saving="departments.saving.value"
          :errors="departments.errors.value"
          empty-icon="mdi-office-building-outline"
          @save="saveDepartment"
          @retry="departments.load"
          @clear-errors="departments.errors.value = null"
        />
      </v-window-item>

      <v-window-item value="cost-centers">
        <MasterDataTable
          ref="costCentersTable"
          title="Cost centers"
          singular="cost center"
          :items="costCenters.items.value"
          :columns="costCenterColumns"
          :fields="costCenterFields"
          :key-of="(c: CostCenter) => c.id"
          :to-draft="costCenterDraft"
          :loading="costCenters.loading.value"
          :error="costCenters.error.value"
          :saving="costCenters.saving.value"
          :errors="costCenters.errors.value"
          empty-icon="mdi-sitemap-outline"
          @save="saveCostCenter"
          @retry="costCenters.load"
          @clear-errors="costCenters.errors.value = null"
        />
      </v-window-item>

      <v-window-item value="currencies">
        <MasterDataTable
          ref="currenciesTable"
          title="Currencies"
          singular="currency"
          :items="currencies.items.value"
          :columns="currencyColumns"
          :fields="currencyFields"
          :key-of="(c: Currency) => c.code"
          :to-draft="currencyDraft"
          :loading="currencies.loading.value"
          :error="currencies.error.value"
          :saving="currencies.saving.value"
          :errors="currencies.errors.value"
          empty-icon="mdi-currency-usd"
          @save="saveCurrency"
          @retry="currencies.load"
          @clear-errors="currencies.errors.value = null"
        />
      </v-window-item>

      <v-window-item value="sites">
        <MasterDataTable
          ref="sitesTable"
          title="Sites"
          singular="site"
          :items="sites.items.value"
          :columns="siteColumns"
          :fields="siteFields"
          :key-of="(s: Site) => s.id"
          :to-draft="siteDraft"
          :loading="sites.loading.value"
          :error="sites.error.value"
          :saving="sites.saving.value"
          :errors="sites.errors.value"
          empty-icon="mdi-map-marker-outline"
          @save="saveSite"
          @retry="sites.load"
          @clear-errors="sites.errors.value = null"
        />
      </v-window-item>

      <v-window-item value="fare-modes">
        <MasterDataTable
          ref="fareModesTable"
          title="Fare modes"
          singular="fare mode"
          :items="fareModes.items.value"
          :columns="fareModeColumns"
          :fields="fareModeFields"
          :key-of="(m: FareMode) => m.id"
          :to-draft="fareModeDraft"
          :loading="fareModes.loading.value"
          :error="fareModes.error.value"
          :saving="fareModes.saving.value"
          :errors="fareModes.errors.value"
          empty-icon="mdi-taxi"
          @save="saveFareMode"
          @retry="fareModes.load"
          @clear-errors="fareModes.errors.value = null"
        />
      </v-window-item>

      <v-window-item value="categories">
        <v-card>
          <div class="card-h">
            <div class="d-flex align-baseline ga-3 flex-wrap">
              <span class="card-t">Expense categories</span>
              <span class="card-meta">Fixed by the R47 form layout; read-only</span>
            </div>
          </div>
          <v-progress-linear v-if="categories.loading.value" indeterminate color="primary" height="2" />
          <v-alert v-if="categories.error.value && !categories.loading.value" type="error" class="ma-4">
            {{ categories.error.value }}
            <template #append><v-btn class="press" size="small" variant="text" @click="categories.load">Retry</v-btn></template>
          </v-alert>
          <AppEmptyState v-else-if="!categories.loading.value && categories.items.value.length === 0" icon="mdi-shape-outline" title="No categories returned" />
          <div v-else-if="!categories.loading.value" class="table-wrap">
            <table class="ledger">
              <thead>
                <tr>
                  <th>Category</th>
                  <th>Label</th>
                  <th>R47 column</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="c in categories.items.value" :key="c.category">
                  <td class="mono">{{ c.category }}</td>
                  <td>{{ c.label }}</td>
                  <td class="mono">{{ c.r47Column }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </v-card>
      </v-window-item>
    </v-window>
  </AuthenticatedLayout>
</template>

<style scoped>
.tabs {
  border-bottom: 1px solid var(--line);
}
.table-wrap {
  overflow-x: auto;
}
</style>
