<script setup lang="ts">
import { computed } from 'vue';
import { useDisplay } from 'vuetify';
import { formatDate, formatMoney } from '@/shared/format';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { categoryLabel, r47Column } from '../labels';
import type { ExpenseLine } from '../types';

// Lines grouped the way the R47 prints them: by date, then by Work With.
// Tablet/desktop render a ledger table; phones render the same groups as a list.
// `adjustable` is the approver's mode: the only change offered is lowering a line amount (with a reason).
const props = withDefaults(
  defineProps<{ lines: ExpenseLine[]; editable: boolean; adjustable?: boolean; busyLineId?: number | null }>(),
  { adjustable: false, busyLineId: null },
);

const emit = defineEmits<{
  add: [];
  edit: [line: ExpenseLine];
  remove: [line: ExpenseLine];
  adjust: [line: ExpenseLine];
}>();

const hasActions = computed(() => props.editable || props.adjustable);

function openLine(line: ExpenseLine) {
  if (props.editable) emit('edit', line);
  else if (props.adjustable) emit('adjust', line);
}

const { xs, smAndDown } = useDisplay();

interface Group {
  key: string;
  date: string;
  location: string | null;
  workWith: string | null;
  lines: ExpenseLine[];
  total: number;
}

const groups = computed<Group[]>(() => {
  const map = new Map<string, Group>();
  for (const line of props.lines) {
    const key = `${line.transactionDate}|${line.workWith ?? ''}`;
    let g = map.get(key);
    if (!g) {
      g = { key, date: line.transactionDate, location: line.location, workWith: line.workWith, lines: [], total: 0 };
      map.set(key, g);
    }
    g.lines.push(line);
    g.total += line.totalAmount;
  }
  return [...map.values()];
});

const grandTotal = computed(() => props.lines.reduce((s, l) => s + l.totalAmount, 0));
const vatTotal = computed(() => props.lines.reduce((s, l) => s + l.vatAmount, 0));

function groupLabel(g: Group): string {
  const parts = [g.location, g.workWith].filter(Boolean);
  return parts.length ? parts.join(' · ') : '—';
}

function subline(line: ExpenseLine): string | null {
  if (line.computation) return line.computation;
  if (line.multiplier > 1) return `${formatMoney(line.amount)} × ${line.multiplier}`;
  return null;
}

function adjustmentNote(line: ExpenseLine): string | null {
  const a = line.adjustment;
  if (!a) return null;
  const by = a.adjustedByName ? ` by ${a.adjustedByName}` : '';
  return `Lowered from ${formatMoney(a.originalTotal)}${by}${a.reason ? `: ${a.reason}` : ''}`;
}
</script>

<template>
  <div>
    <AppEmptyState
      v-if="lines.length === 0"
      icon="mdi-receipt-text-outline"
      title="No expense lines yet"
      :hint="editable ? 'Add each expense as a line. Lines are grouped by date and “Work With” on the printed R47.' : 'This report has no expense lines.'"
    >
      <v-btn v-if="editable" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="emit('add')">Add expense line</v-btn>
    </AppEmptyState>

    <!-- Phone: grouped list -->
    <div v-else-if="xs" class="list">
      <template v-for="g in groups" :key="g.key">
        <div class="list__group">
          <span>{{ formatDate(g.date) }} · {{ groupLabel(g) }}</span>
          <span class="mono">{{ formatMoney(g.total) }}</span>
        </div>
        <div v-for="line in g.lines" :key="line.id" class="list__row" :class="{ 'list__row--busy': busyLineId === line.id }">
          <button type="button" class="list__main press" :disabled="!hasActions" @click="openLine(line)">
            <span class="list__title">{{ line.particulars }}</span>
            <span class="list__sub">
              <span v-if="line.invoiceOrNo" class="mono">OR {{ line.invoiceOrNo }}</span>
              <span v-else>{{ categoryLabel[line.category] }}</span>
              <span v-if="subline(line)"> · {{ subline(line) }}</span>
            </span>
            <span v-if="line.adjustment" class="adj">{{ adjustmentNote(line) }}</span>
          </button>
          <span class="mono list__amt">{{ formatMoney(line.totalAmount) }}</span>
          <v-btn
            v-if="editable"
            class="press"
            icon="mdi-close"
            size="x-small"
            variant="text"
            aria-label="Remove line"
            :loading="busyLineId === line.id"
            @click="emit('remove', line)"
          />
        </div>
      </template>
      <div class="list__foot">
        <span>Grand total</span>
        <span class="mono">{{ formatMoney(grandTotal) }}</span>
      </div>
    </div>

    <!-- Tablet / desktop: ledger table -->
    <div v-else class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th style="width: 112px">Date</th>
            <th>Particulars</th>
            <th style="width: 132px">Invoice / OR</th>
            <th v-if="!smAndDown" style="width: 120px">Column</th>
            <th class="r" style="width: 96px">VAT</th>
            <th class="r" style="width: 120px">Total</th>
            <th v-if="hasActions" style="width: 84px"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="(g, gi) in groups" :key="g.key">
            <tr class="group stagger-in" :style="{ '--i': Math.min(gi, 8) }">
              <td>{{ formatDate(g.date) }}</td>
              <td :colspan="smAndDown ? 3 : 4">{{ groupLabel(g) }}</td>
              <td class="r mono">{{ formatMoney(g.total) }}</td>
              <td v-if="hasActions"></td>
            </tr>
            <tr v-for="line in g.lines" :key="line.id" class="line" :class="{ 'line--busy': busyLineId === line.id }">
              <td></td>
              <td>
                <div>
                  {{ line.particulars }}
                  <span v-if="smAndDown" class="pill pill--neutral col-pill">{{ r47Column(line.category, line.expenseType) }}</span>
                </div>
                <div v-if="subline(line)" class="text-caption text-medium-emphasis">{{ subline(line) }}</div>
                <div v-if="line.adjustment" class="adj">{{ adjustmentNote(line) }}</div>
              </td>
              <td class="mono text-medium-emphasis">{{ line.invoiceOrNo || '—' }}</td>
              <td v-if="!smAndDown">
                <span class="pill pill--neutral col-pill">{{ r47Column(line.category, line.expenseType) }}</span>
              </td>
              <td class="r mono text-medium-emphasis">{{ line.vatAmount ? formatMoney(line.vatAmount) : '—' }}</td>
              <td class="r mono">
                <div v-if="line.adjustment" class="adj-was">{{ formatMoney(line.adjustment.originalTotal) }}</div>
                {{ formatMoney(line.totalAmount) }}
              </td>
              <td v-if="adjustable && !editable" class="r text-no-wrap">
                <v-btn class="press" icon="mdi-arrow-down-bold-circle-outline" size="x-small" variant="text" aria-label="Lower amount" :loading="busyLineId === line.id" @click="emit('adjust', line)" />
              </td>
              <td v-else-if="editable" class="r text-no-wrap">
                <v-btn class="press" icon="mdi-pencil-outline" size="x-small" variant="text" aria-label="Edit line" :disabled="busyLineId === line.id" @click="emit('edit', line)" />
                <v-btn class="press" icon="mdi-close" size="x-small" variant="text" aria-label="Remove line" :loading="busyLineId === line.id" @click="emit('remove', line)" />
              </td>
            </tr>
          </template>
        </tbody>
        <tfoot>
          <tr>
            <td :colspan="smAndDown ? 3 : 4" class="r">Grand total</td>
            <td class="r mono">{{ vatTotal ? formatMoney(vatTotal) : '—' }}</td>
            <td class="r mono">{{ formatMoney(grandTotal) }}</td>
            <td v-if="hasActions"></td>
          </tr>
        </tfoot>
      </table>
    </div>
  </div>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.ledger tr.line td {
  transition: opacity var(--dur-fast) var(--ease-out);
}
.ledger tr.line--busy td {
  opacity: 0.5;
}
.adj {
  display: block;
  font-size: 12px;
  color: rgb(var(--v-theme-warning));
}
.adj-was {
  font-size: 11px;
  color: var(--ink-3);
  text-decoration: line-through;
}
.col-pill {
  height: 20px;
  font-size: 10.5px;
  letter-spacing: 0.04em;
  margin-left: 6px;
}

/* Phone list */
.list__group {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 16px;
  background: rgba(31, 56, 100, 0.04);
  font-size: 12px;
  font-weight: 500;
  color: var(--ink-2);
  border-top: 1px solid var(--line);
  border-bottom: 1px solid var(--line);
}
.list__row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 8px 0 16px;
  min-height: 52px;
  border-bottom: 1px solid var(--line);
  transition: opacity var(--dur-fast) var(--ease-out);
}
.list__row--busy {
  opacity: 0.5;
}
.list__main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  padding: 10px 0;
  background: none;
  border: 0;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
}
.list__main:disabled {
  cursor: default;
}
.list__title {
  font-size: 14px;
}
.list__sub {
  font-size: 12px;
  color: var(--ink-3);
}
.list__amt {
  font-size: 14px;
  white-space: nowrap;
}
.list__foot {
  display: flex;
  justify-content: space-between;
  padding: 12px 16px;
  font-weight: 600;
  font-size: 14px;
}
</style>
