<script setup lang="ts">
import { computed, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDate, formatDateTime, formatMoney, todayIso } from '@/shared/format';
import { categoryOptions, mealOptions, rateKey, rateKeyLabel, rateTypeLabel } from '../labels';
import type { RateLine } from '../types';

/**
 * Rate lines of one profile. Lines are versioned per category/meal: the row shows the version in
 * force today (or the next scheduled one), and expands to the full history, newest first.
 */
const props = withDefaults(
  defineProps<{
    lines: RateLine[];
    canWrite?: boolean;
    disabled?: boolean;
  }>(),
  { canWrite: false, disabled: false },
);

const emit = defineEmits<{
  /** `from` is the version being superseded; null for a brand-new category/meal. */
  add: [from: RateLine | null];
}>();

const { xs } = useDisplay();
const today = todayIso();

type LineState = 'current' | 'scheduled' | 'ended';

function stateOf(l: RateLine): LineState {
  if (l.effectiveFrom > today) return 'scheduled';
  if (l.effectiveTo && l.effectiveTo < today) return 'ended';
  return 'current';
}

const stateLabel: Record<LineState, string> = { current: 'In force', scheduled: 'Scheduled', ended: 'Ended' };
const stateTone: Record<LineState, string> = { current: 'pill--success', scheduled: 'pill--warning', ended: 'pill--neutral' };

// Display order follows the expense category list, then the meal order.
const order = (l: RateLine) =>
  categoryOptions.findIndex((c) => c.value === l.category) * 10 + (l.subCategory ? mealOptions.findIndex((m) => m.value === l.subCategory) : 0);

interface Group {
  key: string;
  label: string;
  headline: RateLine;
  versions: RateLine[];
}

const groups = computed<Group[]>(() => {
  const map = new Map<string, RateLine[]>();
  for (const l of props.lines) {
    const k = rateKey(l);
    map.set(k, [...(map.get(k) ?? []), l]);
  }
  return [...map.entries()]
    .map(([key, versions]) => {
      const sorted = [...versions].sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom) || b.id - a.id);
      // Prefer the version in force today; else the nearest scheduled one; else the most recent.
      const headline =
        sorted.find((l) => stateOf(l) === 'current') ??
        [...sorted].reverse().find((l) => stateOf(l) === 'scheduled') ??
        sorted[0];
      return { key, label: rateKeyLabel(headline.category, headline.subCategory), headline, versions: sorted };
    })
    .sort((a, b) => order(a.headline) - order(b.headline));
});

const expanded = ref<Set<string>>(new Set());
function toggle(key: string) {
  const next = new Set(expanded.value);
  if (next.has(key)) next.delete(key);
  else next.add(key);
  expanded.value = next;
}

function amountText(l: RateLine): string {
  return `${l.currency || ''} ${formatMoney(l.amount)}`.trim();
}

function periodText(l: RateLine): string {
  return `${formatDate(l.effectiveFrom)} – ${l.effectiveTo ? formatDate(l.effectiveTo) : 'open'}`;
}

function rulesText(l: RateLine): string {
  const parts = [rateTypeLabel[l.rateType] ?? l.rateType];
  if (l.frequencyCap !== null) parts.push(`max ${l.frequencyCap}×`);
  if (l.requiresReceipt) parts.push('receipt required');
  return parts.join(' · ');
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">Rate lines</span>
        <span class="card-meta">{{ groups.length }} categor{{ groups.length === 1 ? 'y' : 'ies' }}</span>
      </div>
      <v-btn v-if="canWrite" class="press" size="small" variant="outlined" prepend-icon="mdi-plus" :disabled="disabled" @click="emit('add', null)">
        {{ xs ? 'Add' : 'Add line' }}
      </v-btn>
    </div>

    <AppEmptyState
      v-if="groups.length === 0"
      icon="mdi-cash-multiple"
      title="No rate lines yet"
      :hint="canWrite ? 'Add the meal, transport and other rates this profile grants.' : 'Nobody has added rates to this profile yet.'"
    >
      <v-btn v-if="canWrite" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :disabled="disabled" @click="emit('add', null)">Add line</v-btn>
    </AppEmptyState>

    <div v-else class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th>Category</th>
            <th class="r">Amount</th>
            <th class="d-none d-sm-table-cell">Rule</th>
            <th class="d-none d-md-table-cell">Effective</th>
            <th>Status</th>
            <th class="r"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="g in groups" :key="g.key">
            <tr>
              <td class="font-weight-medium">
                {{ g.label }}
                <div class="text-caption text-medium-emphasis d-sm-none">{{ rulesText(g.headline) }}</div>
              </td>
              <td class="r mono text-no-wrap">{{ amountText(g.headline) }}</td>
              <td class="d-none d-sm-table-cell">{{ rulesText(g.headline) }}</td>
              <td class="d-none d-md-table-cell text-no-wrap">{{ periodText(g.headline) }}</td>
              <td>
                <span class="pill" :class="stateTone[stateOf(g.headline)]">{{ stateLabel[stateOf(g.headline)] }}</span>
              </td>
              <td class="r text-no-wrap">
                <v-btn
                  v-if="g.versions.length > 1"
                  class="press"
                  size="x-small"
                  variant="text"
                  :append-icon="expanded.has(g.key) ? 'mdi-chevron-up' : 'mdi-chevron-down'"
                  :aria-expanded="expanded.has(g.key)"
                  :aria-label="`${expanded.has(g.key) ? 'Hide' : 'Show'} version history for ${g.label}`"
                  @click="toggle(g.key)"
                >
                  {{ g.versions.length }}
                </v-btn>
                <v-btn
                  v-if="canWrite"
                  class="press ml-1"
                  size="x-small"
                  variant="tonal"
                  color="primary"
                  :disabled="disabled"
                  :aria-label="`New version of ${g.label}`"
                  @click="emit('add', g.headline)"
                >
                  New version
                </v-btn>
              </td>
            </tr>
            <template v-if="expanded.has(g.key)">
              <tr v-for="v in g.versions" :key="v.id" class="history">
                <td colspan="6">
                  <div class="history__row">
                    <span class="mono text-no-wrap">{{ amountText(v) }}</span>
                    <span class="text-no-wrap">{{ periodText(v) }}</span>
                    <span class="pill" :class="stateTone[stateOf(v)]">{{ stateLabel[stateOf(v)] }}</span>
                    <v-chip v-if="v.backdateOverride" size="x-small" label variant="tonal" color="warning">Backdated</v-chip>
                    <span class="text-medium-emphasis">{{ rulesText(v) }}</span>
                    <span class="text-caption text-medium-emphasis history__who">
                      {{ v.changedByName ?? '—' }} · {{ formatDateTime(v.createdAt) }}<template v-if="v.changeReason"> · “{{ v.changeReason }}”</template>
                    </span>
                  </div>
                </td>
              </tr>
            </template>
          </template>
        </tbody>
      </table>
    </div>
  </v-card>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.history td {
  background: rgba(31, 56, 100, 0.03);
  padding-top: 8px;
  padding-bottom: 8px;
}
.history__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 14px;
  font-size: 12px;
  padding-left: 12px;
  border-left: 2px solid var(--line-2);
}
.history__who {
  flex-basis: 100%;
}
</style>
