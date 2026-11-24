<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useDisplay } from 'vuetify';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import { formatDate, formatMoney } from '@/shared/format';
import { categoryLabel } from '../labels';
import { groupLines } from '../lineOrder';
import type { ExpenseLine, ExpenseReportDetail } from '../types';
import ReceiptViewer from './ReceiptViewer.vue';

// Reviewers (approvers, Expense Verifiers, Finance) compare the lines with the scans. Every line is
// listed in full; one receipt shows at a time. Receipts belong to the report, not to a line, so the
// reviewer picks both. Actions only emit: the review page owns the approve / return / lower-amount
// dialogs and the API calls.
// Desktop and tablets held sideways: lines left, receipt right. Phones and upright tablets: receipt on
// top, the lines below it, Return and Approve in a bottom bar.
const props = defineProps<{ report: ExpenseReportDetail; startAttachmentId: number | null }>();
const open = defineModel<boolean>({ required: true });

const emit = defineEmits<{
  approve: [];
  return: [];
  adjust: [line: ExpenseLine];
}>();

const { compact } = useLayoutMode();
const { width, height } = useDisplay();
const stacked = computed(() => compact.value && width.value <= height.value);

const root = ref<HTMLElement | null>(null);
const fileIndex = ref(0);
const lineId = ref<number | null>(null);

const lines = computed(() => groupLines(props.report.lines).flatMap((g) => g.lines));
const lineIndex = computed(() => lines.value.findIndex((l) => l.id === lineId.value));
const review = computed(() => props.report.review ?? null);
const hasBottomBar = computed(() => stacked.value && (!!review.value?.canApprove || !!review.value?.canReturn));
const total = computed(() => lines.value.reduce((s, l) => s + l.totalAmount, 0));

watch(open, (isOpen) => {
  if (!isOpen) return;
  const at = props.report.attachments.findIndex((a) => a.id === props.startAttachmentId);
  fileIndex.value = Math.max(at, 0);
  if (lineIndex.value < 0) lineId.value = lines.value[0]?.id ?? null;
});

function stepLine(delta: number) {
  const n = lines.value.length;
  if (n === 0) return;
  const i = lineIndex.value < 0 ? 0 : Math.min(n - 1, Math.max(0, lineIndex.value + delta));
  lineId.value = lines.value[i].id;
  root.value?.querySelector(`[data-line="${lineId.value}"]`)?.scrollIntoView({ block: 'nearest' });
}

function stepFile(delta: number) {
  const n = props.report.attachments.length;
  if (n > 1) fileIndex.value = (fileIndex.value + delta + n) % n;
}

// Arrow keys only while focus is in this dialog: a dialog opened on top (lower amount, approve) keeps its keys.
function onKey(e: KeyboardEvent) {
  const target = e.target as HTMLElement | null;
  if (!open.value || !root.value || (target && target !== document.body && !root.value.contains(target))) return;
  if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return;
  const moves: Record<string, () => void> = {
    ArrowUp: () => stepLine(-1),
    ArrowDown: () => stepLine(1),
    ArrowLeft: () => stepFile(-1),
    ArrowRight: () => stepFile(1),
  };
  const move = moves[e.key];
  if (move) {
    e.preventDefault();
    move();
  }
}

window.addEventListener('keydown', onKey);
onBeforeUnmount(() => window.removeEventListener('keydown', onKey));

function detail(l: ExpenseLine): string {
  const category = categoryLabel[l.category];
  return [
    (l.particulars ?? '').trim().toLowerCase() === category.toLowerCase() ? null : category,
    l.meals.length ? l.meals.map((m) => m.mealType).join(', ') : null,
    l.location,
    l.workWith ? `w/ ${l.workWith}` : null,
    l.computation ?? (l.multiplier > 1 ? `${formatMoney(l.amount)} × ${l.multiplier}` : null),
  ]
    .filter(Boolean)
    .join(' · ');
}

function refs(l: ExpenseLine): string {
  return [
    l.invoiceOrNo ? `OR ${l.invoiceOrNo}` : null,
    l.vatAmount ? `VAT ${formatMoney(l.vatAmount)}` : null,
    l.serviceNumber ? `SN ${l.serviceNumber}` : null,
    l.instrument,
    l.serialNumber ? `S/N ${l.serialNumber}` : null,
  ]
    .filter(Boolean)
    .join(' · ');
}

function adjustmentNote(l: ExpenseLine): string | null {
  const a = l.adjustment;
  if (!a) return null;
  return `Lowered from ${formatMoney(a.originalTotal)}${a.adjustedByName ? ` by ${a.adjustedByName}` : ''}${a.reason ? `: ${a.reason}` : ''}`;
}
</script>

<template>
  <v-dialog v-model="open" fullscreen :scrim="false" transition="dialog-bottom-transition">
    <div ref="root" class="check">
      <header class="check__head">
        <v-btn class="press" icon="mdi-close" variant="text" aria-label="Close" @click="open = false" />
        <div class="check__title">
          <div class="text-body-1 font-weight-medium text-truncate">Check receipts</div>
          <div class="text-caption text-medium-emphasis text-truncate">
            <span class="mono">{{ report.referenceNo || 'Draft' }}</span> · {{ report.employeeName }}
          </div>
        </div>
        <template v-if="!stacked">
          <span v-if="!compact" class="check__keys text-caption text-medium-emphasis">↑ ↓ line · ← → receipt</span>
          <template v-if="review">
            <v-btn v-if="review.canReturn" class="press" variant="outlined" color="warning" @click="emit('return')">Return</v-btn>
            <v-btn v-if="review.canApprove" class="press" variant="flat" color="success" prepend-icon="mdi-check" @click="emit('approve')">Approve</v-btn>
          </template>
        </template>
      </header>

      <div class="check__body" :class="{ 'check__body--stacked': stacked }">
        <section class="lines" aria-label="Expense lines">
          <div class="lines__head">
            <span>{{ lines.length }} line{{ lines.length === 1 ? '' : 's' }}</span>
            <span class="mono">₱ {{ formatMoney(total) }}</span>
          </div>
          <div v-if="lines.length === 0" class="lines__none text-body-2 text-medium-emphasis">This report has no expense lines.</div>
          <ol class="lines__list">
            <li v-for="l in lines" :key="l.id" :class="{ 'is-on': l.id === lineId }">
              <button type="button" class="row" :data-line="l.id" :aria-current="l.id === lineId ? 'true' : undefined" @click="lineId = l.id">
                <span class="row__top">
                  <span class="row__date">{{ formatDate(l.transactionDate) }}</span>
                  <span class="row__part">{{ l.particulars || categoryLabel[l.category] }}</span>
                  <span class="row__amt mono">
                    <span v-if="l.adjustment" class="row__was">{{ formatMoney(l.adjustment.originalTotal) }}</span>
                    {{ formatMoney(l.totalAmount) }}
                  </span>
                </span>
                <span v-if="detail(l)" class="row__sub">{{ detail(l) }}</span>
                <span class="row__refs">
                  <span v-if="!l.invoiceOrNo" class="pill pill--warning">No OR no.</span>
                  <span v-if="refs(l)" class="mono">{{ refs(l) }}</span>
                </span>
                <span v-if="l.remarks" class="row__sub">{{ l.remarks }}</span>
                <span v-if="adjustmentNote(l)" class="warn">{{ adjustmentNote(l) }}</span>
                <span v-for="(w, wi) in l.workWithWarnings ?? []" :key="wi" class="warn"><v-icon icon="mdi-alert-outline" size="14" /> {{ w }}</span>
              </button>
              <div v-if="l.id === lineId && review?.canAdjustLines" class="row__act">
                <v-btn class="press" size="small" variant="tonal" color="primary" prepend-icon="mdi-arrow-down-bold-circle-outline" @click="emit('adjust', l)">Lower this amount</v-btn>
              </div>
            </li>
          </ol>
        </section>
        <ReceiptViewer v-model:index="fileIndex" class="check__viewer" :report-id="report.id" :attachments="report.attachments" />
      </div>

      <footer v-if="hasBottomBar" class="actions">
        <v-btn v-if="review?.canReturn" class="press" variant="outlined" color="warning" prepend-icon="mdi-undo-variant" @click="emit('return')">Return</v-btn>
        <v-btn v-if="review?.canApprove" class="press actions__main" variant="flat" color="success" prepend-icon="mdi-check" @click="emit('approve')">Approve</v-btn>
      </footer>
    </div>
  </v-dialog>
</template>

<style scoped>
.check {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: rgb(var(--v-theme-background));
}
.check__head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px 6px 4px;
  padding-top: max(6px, env(safe-area-inset-top));
  border-bottom: 1px solid var(--line);
  background: rgb(var(--v-theme-surface));
}
.check__title {
  flex: 1;
  min-width: 0;
}
.check__keys {
  margin-right: 8px;
}
.check__body {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: minmax(300px, 40%) minmax(0, 1fr);
}
.check__viewer {
  min-height: 0;
  min-width: 0;
}

/* Phone / upright tablet: receipt on top, all lines below. */
.check__body--stacked {
  display: flex;
  flex-direction: column;
}
.check__body--stacked .check__viewer {
  order: -1;
  flex: 0 0 46%;
}
.check__body--stacked .lines {
  flex: 1 1 0;
  border-right: 0;
}

.lines {
  display: flex;
  flex-direction: column;
  min-height: 0;
  border-right: 1px solid var(--line);
  background: rgb(var(--v-theme-surface));
}
.lines__head {
  display: flex;
  justify-content: space-between;
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 600;
  color: var(--ink-2);
  border-bottom: 1px solid var(--line);
}
.lines__none {
  padding: 20px 16px;
}
.lines__list {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  list-style: none;
  margin: 0;
  padding: 0;
}
.lines__list > li {
  border-bottom: 1px solid var(--line);
  border-left: 3px solid transparent;
}
.lines__list > li.is-on {
  border-left-color: var(--navy);
  background: var(--navy-soft);
}
.row {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 16px 10px 13px;
  min-height: 48px;
  background: none;
  border: 0;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
}
@media (hover: hover) and (pointer: fine) {
  .lines__list > li:not(.is-on) .row:hover {
    background: rgba(31, 56, 100, 0.04);
  }
}
.row:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
.row__top {
  display: flex;
  align-items: baseline;
  gap: 10px;
}
.row__date {
  flex: none;
  font-size: 12px;
  color: var(--ink-3);
}
.row__part {
  flex: 1;
  min-width: 0;
  font-size: 14px;
  font-weight: 500;
}
.row__amt {
  flex: none;
  font-size: 15px;
  font-weight: 600;
}
.row__was {
  font-size: 11px;
  font-weight: 400;
  color: var(--ink-3);
  text-decoration: line-through;
  margin-right: 4px;
}
.row__sub {
  font-size: 12px;
  color: var(--ink-2);
}
.row__refs {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 4px 8px;
  font-size: 12px;
  color: var(--ink-3);
}
.row__act {
  padding: 0 13px 10px;
}
.warn {
  display: block;
  font-size: 12px;
  color: rgb(var(--v-theme-warning));
}
.actions {
  display: flex;
  gap: 8px;
  padding: 8px 12px;
  padding-bottom: max(8px, env(safe-area-inset-bottom));
  border-top: 1px solid var(--line);
  background: rgb(var(--v-theme-surface));
}
.actions > * {
  flex: 1;
  min-height: 48px;
}
.actions__main {
  flex: 2;
}
</style>
