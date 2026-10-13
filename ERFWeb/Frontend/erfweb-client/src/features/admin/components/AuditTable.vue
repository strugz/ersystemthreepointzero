<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useDisplay } from 'vuetify';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDateTime } from '@/shared/format';
import { knownAuditEvents } from '../labels';
import type { AuditEntry, SortDirection } from '../types';

/**
 * Audit log ledger with event / date filters and an expandable JSON detail per row. The parent owns
 * the query and the fetch; filters are a v-model so the view can seed them from the URL.
 */
export interface AuditFilters {
  event: string | null;
  from: string | null;
  to: string | null;
  direction: SortDirection;
  administrationOnly: boolean;
}

const props = withDefaults(
  defineProps<{
    items: AuditEntry[];
    filters: AuditFilters;
    total?: number;
    page?: number;
    totalPages?: number;
    loading?: boolean;
    error?: string | null;
    /** Finance sees the whole log; the "administration only" switch is for administrators. */
    showAdminOnlySwitch?: boolean;
  }>(),
  { total: 0, page: 1, totalPages: 1, loading: false, error: null, showAdminOnlySwitch: true },
);

const emit = defineEmits<{
  'update:filters': [filters: AuditFilters];
  'update:page': [page: number];
  retry: [];
}>();

const { xs } = useDisplay();

// Event names seen in the current page extend the known list so the combobox stays useful.
const eventItems = computed(() => {
  const set = new Set<string>(knownAuditEvents);
  for (const e of props.items) set.add(e.event);
  return [...set].sort();
});

function patch(partial: Partial<AuditFilters>) {
  emit('update:filters', { ...props.filters, ...partial });
}

const dateError = computed(() =>
  props.filters.from && props.filters.to && props.filters.to < props.filters.from ? 'To must be on or after From.' : undefined,
);

// ---- expandable detail -------------------------------------------------------
const expanded = ref<Set<number>>(new Set());
watch(
  () => props.items,
  () => expanded.value.clear(),
);

function toggle(id: number) {
  const next = new Set(expanded.value);
  if (next.has(id)) next.delete(id);
  else next.add(id);
  expanded.value = next;
}

function prettyDetail(detail: string | null): string {
  if (!detail) return '—';
  try {
    return JSON.stringify(JSON.parse(detail), null, 2);
  } catch {
    return detail;
  }
}

function summary(detail: string | null): string {
  if (!detail) return '';
  try {
    const parsed = JSON.parse(detail) as unknown;
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return Object.entries(parsed as Record<string, unknown>)
        .slice(0, 3)
        .map(([k, v]) => `${k}: ${typeof v === 'object' ? '…' : String(v)}`)
        .join(' · ');
    }
    return detail;
  } catch {
    return detail;
  }
}

const hasFilters = computed(() => !!props.filters.event || !!props.filters.from || !!props.filters.to);

function clearFilters() {
  patch({ event: null, from: null, to: null });
}
</script>

<template>
  <v-card>
    <div class="filters">
      <v-combobox
        :model-value="filters.event"
        :items="eventItems"
        density="compact"
        hide-details
        clearable
        label="Event"
        class="filters__event"
        @update:model-value="(v: string | null) => patch({ event: v || null })"
      />
      <v-text-field
        :model-value="filters.from ?? ''"
        type="date"
        density="compact"
        hide-details
        label="From"
        class="filters__date"
        @update:model-value="(v: string) => patch({ from: v || null })"
      />
      <v-text-field
        :model-value="filters.to ?? ''"
        type="date"
        density="compact"
        :hide-details="!dateError"
        :error-messages="dateError"
        label="To"
        class="filters__date"
        @update:model-value="(v: string) => patch({ to: v || null })"
      />
      <v-btn-toggle
        :model-value="filters.direction"
        density="compact"
        variant="outlined"
        divided
        mandatory
        class="filters__dir"
        @update:model-value="(v: SortDirection) => patch({ direction: v })"
      >
        <v-btn value="Desc" size="small" aria-label="Newest first"><v-icon icon="mdi-sort-clock-descending-outline" /></v-btn>
        <v-btn value="Asc" size="small" aria-label="Oldest first"><v-icon icon="mdi-sort-clock-ascending-outline" /></v-btn>
      </v-btn-toggle>
      <v-switch
        v-if="showAdminOnlySwitch"
        :model-value="filters.administrationOnly"
        density="compact"
        hide-details
        color="primary"
        label="Administration only"
        @update:model-value="(v: boolean | null) => patch({ administrationOnly: !!v })"
      />
      <v-spacer class="d-none d-sm-block" />
      <span v-if="!loading" class="card-meta d-none d-sm-inline">{{ total }} event{{ total === 1 ? '' : 's' }}</span>
    </div>
    <v-divider />

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-if="error && !loading" type="error" class="ma-4">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="emit('retry')">Retry</v-btn></template>
    </v-alert>

    <AppEmptyState
      v-else-if="!loading && items.length === 0"
      icon="mdi-clipboard-text-clock-outline"
      :title="hasFilters ? 'No events match your filters' : 'No audit events yet'"
      :hint="hasFilters ? 'Try widening the date range or clearing the event filter.' : 'Security-sensitive actions are recorded here as they happen.'"
    >
      <v-btn v-if="hasFilters" class="press" variant="text" @click="clearFilters">Clear filters</v-btn>
    </AppEmptyState>

    <div v-else-if="!loading" class="table-wrap">
      <table class="ledger audit">
        <thead>
          <tr>
            <th style="width: 32px"></th>
            <th>When</th>
            <th>Event</th>
            <th class="d-none d-sm-table-cell">Actor</th>
            <th class="d-none d-md-table-cell">Report</th>
            <th class="d-none d-lg-table-cell">Detail</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="(e, i) in items" :key="e.id">
            <tr class="row-link stagger-in" :style="{ '--i': Math.min(i, 10) }" tabindex="0" :aria-expanded="expanded.has(e.id)" @click="toggle(e.id)" @keydown.enter="toggle(e.id)" @keydown.space.prevent="toggle(e.id)">
              <td><v-icon :icon="expanded.has(e.id) ? 'mdi-chevron-down' : 'mdi-chevron-right'" size="18" class="text-medium-emphasis" /></td>
              <td class="text-no-wrap">{{ formatDateTime(e.occurredAt) }}</td>
              <td class="mono font-weight-medium text-no-wrap">{{ e.event }}</td>
              <td class="d-none d-sm-table-cell">{{ e.actorName ?? (e.actorUserId !== null ? `#${e.actorUserId}` : 'System') }}</td>
              <td class="mono d-none d-md-table-cell text-truncate" style="max-width: 160px">{{ e.reportId || '—' }}</td>
              <td class="text-medium-emphasis d-none d-lg-table-cell text-truncate" style="max-width: 360px">{{ summary(e.detail) }}</td>
            </tr>
            <tr v-if="expanded.has(e.id)" class="audit__detail">
              <td></td>
              <td :colspan="xs ? 2 : 5">
                <div class="audit__meta text-caption text-medium-emphasis">
                  <span>Actor: {{ e.actorName ?? '—' }}<template v-if="e.actorUserId !== null"> (#{{ e.actorUserId }})</template></span>
                  <span v-if="e.reportId">Report: <span class="mono">{{ e.reportId }}</span></span>
                  <span v-if="e.correlationId">Correlation: <span class="mono">{{ e.correlationId }}</span></span>
                </div>
                <pre class="audit__json mono">{{ prettyDetail(e.detail) }}</pre>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <div v-if="!loading && totalPages > 1" class="d-flex justify-center py-2">
      <v-pagination :model-value="page" :length="totalPages" density="comfortable" :total-visible="xs ? 5 : 7" @update:model-value="(p: number) => emit('update:page', p)" />
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
.filters__event {
  flex: 1 1 220px;
  max-width: 320px;
}
.filters__date {
  flex: 0 1 170px;
  min-width: 150px;
}
.filters__dir {
  height: 40px;
}
@media (max-width: 599.98px) {
  .filters__event,
  .filters__date {
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
.audit__detail td {
  background: var(--paper);
  vertical-align: top;
}
.audit__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 8px;
}
.audit__json {
  margin: 0;
  padding: 10px 12px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: rgb(var(--v-theme-surface));
  font-size: 12px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
  max-height: 360px;
  overflow: auto;
}
</style>
