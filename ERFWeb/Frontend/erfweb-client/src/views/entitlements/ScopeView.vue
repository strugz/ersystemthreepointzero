<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { todayIso } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { entitlementLookupsApi, scopeApi } from '@/features/entitlements/api';
import { hasAnyRole, scopeWriterRoles } from '@/features/entitlements/access';
import { peopleFromCoverage } from '@/features/entitlements/forms';
import { scopeLevelOptions } from '@/features/entitlements/labels';
import ScopeAssignmentsTable from '@/features/entitlements/components/ScopeAssignmentsTable.vue';
import ScopeAssignmentDialog from '@/features/entitlements/components/ScopeAssignmentDialog.vue';
import CoverageTable from '@/features/entitlements/components/CoverageTable.vue';
import type {
  CoverageRow,
  CreateScopeAssignmentInput,
  DepartmentOption,
  PersonOption,
  ScopeAssignment,
  ScopeLevel,
  UpdateScopeAssignmentInput,
} from '@/features/entitlements/types';

const route = useRoute();
const router = useRouter();
const session = useSessionStore();
const snack = useSnackbar();
const { xs } = useDisplay();

// Write controls are presentation only; the API decides which level each actor may set.
const canWrite = computed(() => hasAnyRole(session.user?.roles, scopeWriterRoles));

type Tab = 'assignments' | 'coverage';
const tabs: { value: Tab; title: string; icon: string }[] = [
  { value: 'assignments', title: 'Assignments', icon: 'mdi-account-supervisor-outline' },
  { value: 'coverage', title: 'Coverage', icon: 'mdi-account-check-outline' },
];
const isTab = (v: unknown): v is Tab => tabs.some((t) => t.value === v);
const tab = ref<Tab>(isTab(route.query.tab) ? route.query.tab : 'assignments');
watch(tab, (t) => router.replace({ query: { ...route.query, tab: t } }));

// ---- assignments ---------------------------------------------------------------
// 0 = every level (a toggle button cannot carry a null value).
const level = ref<0 | ScopeLevel>(0);
const levelFilter = computed<ScopeLevel | null>(() => (level.value === 0 ? null : level.value));
const includeInactive = ref(false);
const assignments = ref<ScopeAssignment[]>([]);
const loadingAssignments = ref(true);
const assignmentsError = ref<string | null>(null);

async function loadAssignments() {
  loadingAssignments.value = true;
  assignmentsError.value = null;
  try {
    assignments.value = await scopeApi.list({ level: levelFilter.value ?? undefined, includeInactive: includeInactive.value || undefined });
  } catch (err) {
    assignmentsError.value = 'Unable to load scope assignments.';
    snack.fromError(err, assignmentsError.value);
  } finally {
    loadingAssignments.value = false;
  }
}
watch([level, includeInactive], loadAssignments);

const filtered = computed(() => level.value !== 0 || includeInactive.value);
function clearFilters() {
  level.value = 0;
  includeInactive.value = false;
}

// ---- coverage (also the people picker) -------------------------------------------
const coverageDate = ref(todayIso());
const coverage = ref<CoverageRow[]>([]);
const people = ref<PersonOption[]>([]);
const loadingCoverage = ref(true);
const coverageError = ref<string | null>(null);

async function loadCoverage() {
  loadingCoverage.value = true;
  coverageError.value = null;
  try {
    coverage.value = await scopeApi.coverage(coverageDate.value);
    // The people list follows today's coverage only; another date is a view, not a roster.
    if (coverageDate.value === todayIso() || people.value.length === 0) {
      people.value = peopleFromCoverage(coverage.value);
    }
  } catch (err) {
    coverageError.value = 'Unable to load coverage.';
    snack.fromError(err, coverageError.value);
  } finally {
    loadingCoverage.value = false;
  }
}
watch(coverageDate, loadCoverage);

const problemCount = computed(() => coverage.value.filter((r) => r.level1Problem || r.level2Problem).length);

// ---- departments ------------------------------------------------------------------
const departments = ref<DepartmentOption[]>([]);
async function loadDepartments() {
  try {
    departments.value = await entitlementLookupsApi.departments();
  } catch (err) {
    snack.fromError(err, 'Unable to load departments.');
  }
}

onMounted(() => {
  void loadAssignments();
  void loadCoverage();
  if (canWrite.value) void loadDepartments();
});

// ---- create / edit -------------------------------------------------------------
const dialog = ref(false);
const editing = ref<ScopeAssignment | null>(null);
const saving = ref(false);
const errors = ref<Record<string, string[]> | null>(null);

function openCreate() {
  editing.value = null;
  errors.value = null;
  dialog.value = true;
}

function openEdit(a: ScopeAssignment) {
  editing.value = a;
  errors.value = null;
  dialog.value = true;
}

// A 409 means someone else changed the assignment; reload so the user sees the current state.
async function handleMutationError(err: unknown, fallback: string): Promise<Record<string, string[]> | null> {
  snack.fromError(err, fallback);
  if (err instanceof ApiError && err.status === 409) {
    // Close so the next edit starts from the reloaded row rather than the stale draft.
    dialog.value = false;
    await loadAssignments();
  }
  return err instanceof ApiError && err.problem?.errors ? err.problem.errors : null;
}

async function afterSave(message: string) {
  dialog.value = false;
  snack.success(message);
  // Coverage depends on every assignment; refresh both.
  await Promise.all([loadAssignments(), loadCoverage()]);
}

async function create(input: CreateScopeAssignmentInput) {
  saving.value = true;
  errors.value = null;
  try {
    const created = await scopeApi.create(input);
    await afterSave(`Scope created for ${created.holderName}.`);
  } catch (err) {
    errors.value = await handleMutationError(err, 'Unable to create the scope assignment.');
  } finally {
    saving.value = false;
  }
}

async function update(input: UpdateScopeAssignmentInput) {
  if (!editing.value) return;
  saving.value = true;
  errors.value = null;
  try {
    const saved = await scopeApi.update(editing.value.id, input);
    await afterSave(`Scope for ${saved.holderName} saved.`);
  } catch (err) {
    errors.value = await handleMutationError(err, 'Unable to save the scope assignment.');
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Scope' }]">
    <template v-if="canWrite" #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="openCreate">New assignment</v-btn>
    </template>
    <template v-if="canWrite" #actions-mobile>
      <v-btn class="press" icon="mdi-plus" variant="text" aria-label="New assignment" @click="openCreate" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-4">
      <h1 class="page-title">Scope</h1>
      <span class="text-body-2 text-medium-emphasis">Who signs for whom. Every employee needs exactly one Level 1 and one Level 2.</span>
    </div>

    <v-tabs v-model="tab" color="primary" density="comfortable" class="mb-4 tabs">
      <v-tab v-for="t in tabs" :key="t.value" :value="t.value" class="press text-none">
        <v-icon :icon="t.icon" size="18" class="mr-2 d-none d-sm-inline-flex" />{{ t.title }}
        <v-badge
          v-if="t.value === 'coverage' && problemCount > 0"
          :content="problemCount"
          color="warning"
          inline
          class="ml-1"
          :aria-label="`${problemCount} coverage problems`"
        />
      </v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="assignments">
        <v-card>
          <div class="filters">
            <v-btn-toggle v-model="level" mandatory density="compact" variant="outlined" divided color="primary" aria-label="Level">
              <v-btn :value="0" class="text-none">All levels</v-btn>
              <v-btn v-for="o in scopeLevelOptions" :key="o.value" :value="o.value" class="text-none">{{ o.title }}</v-btn>
            </v-btn-toggle>
            <v-switch v-model="includeInactive" density="compact" hide-details color="primary" label="Include inactive" />
            <v-spacer class="d-none d-sm-block" />
            <span v-if="!loadingAssignments" class="card-meta d-none d-sm-inline">
              {{ assignments.length }} assignment{{ assignments.length === 1 ? '' : 's' }}
            </span>
          </div>
          <v-divider />
          <ScopeAssignmentsTable
            :items="assignments"
            :loading="loadingAssignments"
            :error="assignmentsError"
            :can-write="canWrite"
            :filtered="filtered"
            @open="openEdit"
            @create="openCreate"
            @retry="loadAssignments"
            @clear-filters="clearFilters"
          />
        </v-card>
      </v-window-item>

      <v-window-item value="coverage">
        <CoverageTable v-model:date="coverageDate" :rows="coverage" :loading="loadingCoverage" :error="coverageError" @retry="loadCoverage" />
      </v-window-item>
    </v-window>

    <ScopeAssignmentDialog
      v-if="canWrite"
      v-model="dialog"
      :assignment="editing"
      :people="people"
      :departments="departments"
      :default-level="levelFilter"
      :saving="saving"
      :errors="errors"
      @create="create"
      @update="update"
    />
  </AuthenticatedLayout>
</template>

<style scoped>
.tabs {
  border-bottom: 1px solid var(--line);
}
.filters {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  padding: 12px;
}
</style>
