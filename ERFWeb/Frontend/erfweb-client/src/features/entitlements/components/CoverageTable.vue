<script setup lang="ts">
import { computed, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import type { CoverageRow } from '../types';

/**
 * Every active employee and who signs for them on a date. A problem string means that level is
 * unresolved or ambiguous for the employee; those rows are flagged and can be isolated.
 */
const props = withDefaults(
  defineProps<{
    rows: CoverageRow[];
    date: string;
    loading?: boolean;
    error?: string | null;
  }>(),
  { loading: false, error: null },
);

const emit = defineEmits<{
  'update:date': [value: string];
  retry: [];
}>();

const { xs } = useDisplay();

const onlyProblems = ref(false);
const search = ref('');

const hasProblem = (r: CoverageRow) => !!r.level1Problem || !!r.level2Problem;
const problemCount = computed(() => props.rows.filter(hasProblem).length);

const visible = computed(() => {
  const q = search.value.trim().toLowerCase();
  return props.rows.filter(
    (r) =>
      (!onlyProblems.value || hasProblem(r)) &&
      (!q || r.fullName.toLowerCase().includes(q) || (r.departmentName ?? '').toLowerCase().includes(q)),
  );
});

const filtered = computed(() => onlyProblems.value || search.value.trim() !== '');

function clearFilters() {
  onlyProblems.value = false;
  search.value = '';
}

function onDate(value: string | null) {
  if (value) emit('update:date', value);
}
</script>

<template>
  <v-card>
    <div class="filters">
      <v-text-field
        v-model="search"
        density="compact"
        hide-details
        clearable
        prepend-inner-icon="mdi-magnify"
        placeholder="Search name or department"
        class="filters__search"
      />
      <v-text-field
        :model-value="date"
        type="date"
        density="compact"
        hide-details
        label="As of"
        class="filters__date"
        @update:model-value="onDate"
      />
      <v-switch
        v-model="onlyProblems"
        density="compact"
        hide-details
        color="warning"
        :label="`Only problems (${problemCount})`"
        class="filters__switch"
      />
      <v-spacer class="d-none d-sm-block" />
      <span class="card-meta d-none d-sm-inline">{{ visible.length }} of {{ rows.length }}</span>
    </div>
    <v-divider />

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-if="error && !loading" type="error" class="ma-4">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="emit('retry')">Retry</v-btn></template>
    </v-alert>

    <AppEmptyState
      v-else-if="!loading && visible.length === 0"
      :icon="filtered ? 'mdi-account-search-outline' : 'mdi-account-group-outline'"
      :title="onlyProblems && !search ? 'Everyone has a signatory' : filtered ? 'Nobody matches' : 'No active employees'"
      :hint="onlyProblems && !search ? 'Every active employee resolves to exactly one Level 1 and Level 2 on this date.' : filtered ? 'Try clearing the search or filter.' : undefined"
    >
      <v-btn v-if="filtered" class="press" variant="text" @click="clearFilters">Clear filters</v-btn>
    </AppEmptyState>

    <!-- Phone: stacked rows -->
    <ul v-else-if="!loading && xs" class="rows">
      <li v-for="r in visible" :key="r.userId" class="row" :class="{ 'row--problem': hasProblem(r) }">
        <div class="d-flex align-center ga-2 flex-wrap">
          <span class="font-weight-medium">{{ r.fullName }}</span>
          <v-chip v-if="hasProblem(r)" size="x-small" label variant="tonal" color="warning" prepend-icon="mdi-alert-outline">Problem</v-chip>
        </div>
        <div class="text-caption text-medium-emphasis">{{ r.departmentName ?? 'No department' }}</div>
        <div class="text-caption">
          <span class="lbl mr-1">L1</span>
          <span v-if="r.level1Problem" class="problem">{{ r.level1Problem }}</span>
          <span v-else>{{ r.level1Name ?? '—' }}</span>
        </div>
        <div class="text-caption">
          <span class="lbl mr-1">L2</span>
          <span v-if="r.level2Problem" class="problem">{{ r.level2Problem }}</span>
          <span v-else>{{ r.level2Name ?? '—' }}</span>
        </div>
      </li>
    </ul>

    <!-- Tablet / desktop: ledger -->
    <div v-else-if="!loading" class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th>Employee</th>
            <th class="d-none d-md-table-cell">Department</th>
            <th>Level 1</th>
            <th>Level 2</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in visible" :key="r.userId" :class="{ 'row--problem': hasProblem(r) }">
            <td>
              <div class="d-flex align-center ga-2 flex-wrap">
                <span class="font-weight-medium">{{ r.fullName }}</span>
                <v-chip v-if="hasProblem(r)" size="x-small" label variant="tonal" color="warning" prepend-icon="mdi-alert-outline">Problem</v-chip>
              </div>
              <div class="text-caption text-medium-emphasis d-md-none">{{ r.departmentName ?? '—' }}</div>
            </td>
            <td class="d-none d-md-table-cell text-medium-emphasis">{{ r.departmentName ?? '—' }}</td>
            <td>
              <span v-if="r.level1Problem" class="problem">{{ r.level1Problem }}</span>
              <span v-else>{{ r.level1Name ?? '—' }}</span>
            </td>
            <td>
              <span v-if="r.level2Problem" class="problem">{{ r.level2Problem }}</span>
              <span v-else>{{ r.level2Name ?? '—' }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </v-card>
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
.filters__date {
  flex: 0 1 180px;
  min-width: 160px;
}
.filters__switch {
  flex: 0 0 auto;
}
@media (max-width: 599.98px) {
  .filters__search,
  .filters__date {
    flex: 1 1 100%;
    max-width: none;
  }
}
.table-wrap {
  overflow-x: auto;
}
.row--problem td,
li.row--problem {
  background: rgba(183, 121, 31, 0.06);
}
.problem {
  color: rgb(var(--v-theme-warning));
}
.rows {
  list-style: none;
  margin: 0;
  padding: 0;
}
.row {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}
.row:last-child {
  border-bottom: 0;
}
</style>
