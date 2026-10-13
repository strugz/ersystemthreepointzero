<script setup lang="ts">
import { useDisplay } from 'vuetify';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDate, formatMoney } from '@/shared/format';
import { scopeLevelLabel, scopeTypeLabel } from '../labels';
import type { ScopeAssignment } from '../types';

/** Scope assignment list. Rows open the edit dialog for writers; readers see the same facts read-only. */
withDefaults(
  defineProps<{
    items: ScopeAssignment[];
    loading?: boolean;
    error?: string | null;
    canWrite?: boolean;
    /** True when filters narrow the list, so the empty state says "no match" rather than "none yet". */
    filtered?: boolean;
  }>(),
  { loading: false, error: null, canWrite: false, filtered: false },
);

const emit = defineEmits<{
  open: [item: ScopeAssignment];
  create: [];
  retry: [];
  'clear-filters': [];
}>();

const { xs } = useDisplay();

function coverText(a: ScopeAssignment): string {
  if (a.type === 'Department') return a.departmentName ?? '—';
  const names = a.members.map((m) => m.fullName);
  const shown = names.slice(0, 3).join(', ') + (names.length > 3 ? ` +${names.length - 3}` : '');
  if (a.type === 'Individual') return shown || '—';
  return `${a.departmentName ?? '—'} except ${shown || 'nobody'}`;
}

function periodText(a: ScopeAssignment): string {
  return `${formatDate(a.effectiveFrom)} – ${a.effectiveTo ? formatDate(a.effectiveTo) : 'open'}`;
}

function onRow(a: ScopeAssignment) {
  emit('open', a);
}
</script>

<template>
  <div>
    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-if="error && !loading" type="error" class="ma-4">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="emit('retry')">Retry</v-btn></template>
    </v-alert>

    <AppEmptyState
      v-else-if="!loading && items.length === 0"
      icon="mdi-account-supervisor-outline"
      :title="filtered ? 'No assignments match your filters' : 'No scope assignments yet'"
      :hint="
        filtered
          ? 'Try another level or include inactive assignments.'
          : canWrite
            ? 'Say who signs for whom: a department, named people, or a department with exceptions.'
            : 'Nobody has set up who signs for whom yet.'
      "
    >
      <v-btn v-if="filtered" class="press" variant="text" @click="emit('clear-filters')">Clear filters</v-btn>
      <v-btn v-else-if="canWrite" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="emit('create')">New assignment</v-btn>
    </AppEmptyState>

    <!-- Phone: stacked rows -->
    <div v-else-if="!loading && xs" class="rows">
      <component
        :is="canWrite ? 'button' : 'div'"
        v-for="(a, i) in items"
        :key="a.id"
        :type="canWrite ? 'button' : undefined"
        class="row stagger-in"
        :class="{ 'press row--link': canWrite }"
        :style="{ '--i': Math.min(i, 10) }"
        @click="canWrite && onRow(a)"
      >
        <div class="row__main">
          <div class="row__top">
            <span class="row__name">{{ a.holderName }}</span>
            <span class="pill pill--neutral">{{ scopeLevelLabel[a.level] }}</span>
            <span v-if="!a.isActive" class="pill pill--neutral">Inactive</span>
            <v-chip v-if="a.isBootstrap" size="x-small" label variant="tonal" color="primary">Bootstrap</v-chip>
          </div>
          <div class="row__meta">{{ scopeTypeLabel[a.type] }} · {{ coverText(a) }}</div>
          <div class="row__meta">
            {{ periodText(a) }}<template v-if="a.approvalLimit !== null"> · limit {{ formatMoney(a.approvalLimit) }}</template>
          </div>
        </div>
        <v-icon v-if="canWrite" icon="mdi-chevron-right" size="18" class="text-medium-emphasis" />
      </component>
    </div>

    <!-- Tablet / desktop: ledger -->
    <div v-else-if="!loading" class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th>Signatory</th>
            <th>Level</th>
            <th>Covers</th>
            <th class="d-none d-md-table-cell r">Limit</th>
            <th class="d-none d-md-table-cell">Period</th>
            <th class="d-none d-lg-table-cell">Set by</th>
            <th>Status</th>
            <th v-if="canWrite" class="r"></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(a, i) in items"
            :key="a.id"
            class="stagger-in"
            :class="{ 'row-link': canWrite, 'text-medium-emphasis': !a.isActive }"
            :style="{ '--i': Math.min(i, 10) }"
            :tabindex="canWrite ? 0 : undefined"
            @click="canWrite && onRow(a)"
            @keydown.enter="canWrite && onRow(a)"
          >
            <td class="font-weight-medium">
              {{ a.holderName }}
              <v-chip v-if="a.isBootstrap" size="x-small" label variant="tonal" color="primary" class="ml-1">Bootstrap</v-chip>
            </td>
            <td class="text-no-wrap">{{ scopeLevelLabel[a.level] }}</td>
            <td>
              <div>{{ coverText(a) }}</div>
              <div class="text-caption text-medium-emphasis">{{ scopeTypeLabel[a.type] }}</div>
            </td>
            <td class="d-none d-md-table-cell r mono text-no-wrap">{{ a.approvalLimit === null ? '—' : formatMoney(a.approvalLimit) }}</td>
            <td class="d-none d-md-table-cell text-no-wrap">{{ periodText(a) }}</td>
            <td class="d-none d-lg-table-cell text-no-wrap">
              <div>{{ a.createdByName ?? '—' }}</div>
              <div v-if="a.note" class="text-caption text-medium-emphasis text-truncate" style="max-width: 220px" :title="a.note">{{ a.note }}</div>
            </td>
            <td><span class="pill" :class="a.isActive ? 'pill--success' : 'pill--neutral'">{{ a.isActive ? 'Active' : 'Inactive' }}</span></td>
            <td v-if="canWrite" class="r">
              <v-btn class="press" icon="mdi-pencil-outline" size="x-small" variant="text" :aria-label="`Edit scope of ${a.holderName}`" @click.stop="onRow(a)" />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
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
}
.row--link {
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
  flex-wrap: wrap;
  gap: 6px;
}
.row__name {
  font-size: 14px;
  font-weight: 500;
}
.row__meta {
  font-size: 12px;
  color: var(--ink-3);
}
</style>
