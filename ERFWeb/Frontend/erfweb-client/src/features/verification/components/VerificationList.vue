<script setup lang="ts">
import { formatDate, formatDateTime, formatMoney } from '@/shared/format';
import {
  AUTOMATIC_PREFIX,
  closedOutcome,
  dispositionLabel,
  dispositionTone,
  findingLabel,
  findingTone,
  verificationStatusLabel,
  verificationStatusTone,
} from '../labels';
import type { VerificationQueueItem } from '../types';

// Rows for the verifier queue and for Level 2's queries. A row opens the report only while the
// viewer still has access to it (`openable`); closed requests are history. Once there is a finding,
// the row shows what happened after it, so the verifier sees how their finding was used.
const props = defineProps<{ items: VerificationQueueItem[]; showQuery?: boolean }>();

const emit = defineEmits<{ open: [item: VerificationQueueItem] }>();

interface TrailStep {
  key: string;
  text: string;
  at: string | null;
  pill?: { label: string; tone: string };
}

function openable(item: VerificationQueueItem): boolean {
  return item.status !== 'Closed' || !!props.showQuery;
}

function open(item: VerificationQueueItem) {
  if (openable(item)) emit('open', item);
}

function isAutomatic(item: VerificationQueueItem): boolean {
  return item.reason.startsWith(AUTOMATIC_PREFIX);
}

/** One pill: the outcome once closed, otherwise the status. */
function headPill(item: VerificationQueueItem): { label: string; tone: string } {
  if (item.status !== 'Closed') return { label: verificationStatusLabel[item.status], tone: verificationStatusTone[item.status] };
  if (item.disposition) return { label: dispositionLabel[item.disposition], tone: dispositionTone[item.disposition] };
  if (item.finding) return { label: findingLabel[item.finding], tone: findingTone[item.finding] };
  return { label: verificationStatusLabel.Closed, tone: verificationStatusTone.Closed };
}

/** What happened from the finding on; the verifier's own steps say "You", Level 2's say "You" in their view. */
function trail(item: VerificationQueueItem): TrailStep[] {
  if (!item.finding) return [];
  const steps: TrailStep[] = [
    {
      key: 'finding',
      text: props.showQuery ? 'Verifier found' : 'You found',
      at: item.foundAt,
      pill: { label: findingLabel[item.finding], tone: findingTone[item.finding] },
    },
  ];
  if (item.queriedAt) steps.push({ key: 'query', text: 'Finance sent it to Level 2', at: item.queriedAt });
  else if (item.status === 'Found') steps.push({ key: 'query', text: 'Waiting for Finance to send it to Level 2', at: null });
  if (item.disposition) {
    steps.push({
      key: 'decision',
      text: props.showQuery ? 'You decided' : 'Level 2 decided',
      at: item.decidedAt,
      pill: { label: dispositionLabel[item.disposition], tone: dispositionTone[item.disposition] },
    });
  } else if (item.status === 'Queried') {
    steps.push({ key: 'decision', text: 'Waiting for Level 2 to decide', at: null });
  }
  return steps;
}

function outcomeTone(item: VerificationQueueItem): string {
  if (item.disposition === 'Reject') return 'outcome--bad';
  if (item.disposition === 'Return') return 'outcome--warn';
  return 'outcome--ok';
}
</script>

<template>
  <div class="rows">
    <component
      :is="openable(item) ? 'button' : 'div'"
      v-for="(item, i) in items"
      :key="item.id"
      :type="openable(item) ? 'button' : undefined"
      class="row stagger-in"
      :class="{ 'row--link press': openable(item) }"
      :style="{ '--i': Math.min(i, 10) }"
      @click="open(item)"
    >
      <div class="row__main">
        <div class="row__top">
          <span class="mono row__ref">{{ item.referenceNo }}</span>
          <span class="pill" :class="headPill(item).tone">{{ headPill(item).label }}</span>
        </div>
        <div class="row__who">
          {{ item.employeeName }}<span v-if="item.departmentName" class="text-medium-emphasis"> · {{ item.departmentName }}</span>
        </div>
        <div class="row__line">
          {{ formatDate(item.lineDate) }} · {{ item.lineParticulars || 'Expense line' }}
        </div>
        <div v-if="isAutomatic(item)" class="row__text">
          <span class="row__k">Finance rule:</span> {{ item.reason.slice(AUTOMATIC_PREFIX.length) }}
        </div>
        <div v-else class="row__text"><span class="row__k">Finance:</span> {{ item.reason }}</div>
        <div class="row__meta">
          <template v-if="isAutomatic(item)">Opened automatically on {{ item.requestedByName }}'s final approval</template>
          <template v-else>Asked by {{ item.requestedByName }}</template>
          {{ formatDateTime(item.requestedAt) }}
          <span v-if="item.status !== 'Closed' && item.daysWaiting > 0"> · waiting {{ item.daysWaiting }} day{{ item.daysWaiting === 1 ? '' : 's' }}</span>
        </div>

        <ol v-if="trail(item).length" class="trail">
          <li v-for="s in trail(item)" :key="s.key" class="trail__step" :class="{ 'trail__step--waiting': !s.at }">
            <span class="trail__dot" aria-hidden="true" />
            <span class="trail__text">{{ s.text }}</span>
            <span v-if="s.pill" class="pill pill--sm" :class="s.pill.tone">{{ s.pill.label }}</span>
            <span v-if="s.at" class="row__meta">{{ formatDateTime(s.at) }}</span>
          </li>
        </ol>
        <div v-if="showQuery && item.findingNote" class="row__text"><span class="row__k">Verifier:</span> {{ item.findingNote }}</div>
        <div v-else-if="item.findingNote" class="row__text"><span class="row__k">Your note:</span> {{ item.findingNote }}</div>
        <div v-if="showQuery && item.queryNote" class="row__text"><span class="row__k">Finance asks Level 2:</span> {{ item.queryNote }}</div>

        <div v-if="item.status === 'Closed'" class="outcome" :class="outcomeTone(item)">
          <v-icon :icon="item.disposition === 'Reject' ? 'mdi-close-circle-outline' : 'mdi-check-circle-outline'" size="16" />
          {{ closedOutcome(item.disposition) }}
        </div>
      </div>
      <span class="mono row__amt">{{ formatMoney(item.lineTotal) }}</span>
    </component>
  </div>
</template>

<style scoped>
.rows {
  display: flex;
  flex-direction: column;
}
.row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  width: 100%;
  padding: 14px 16px;
  border: 0;
  border-bottom: 1px solid var(--line);
  background: none;
  font: inherit;
  color: inherit;
  text-align: left;
}
.row:last-child {
  border-bottom: 0;
}
.row--link {
  cursor: pointer;
}
.row--link:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
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
  gap: 8px;
  flex-wrap: wrap;
}
.row__ref {
  font-size: 13px;
  font-weight: 500;
}
.row__who {
  font-size: 14px;
}
.row__line {
  font-size: 13px;
  font-weight: 500;
}
.row__text {
  font-size: 13px;
  white-space: pre-line;
}
.row__k {
  color: var(--ink-3);
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

.trail {
  list-style: none;
  margin: 8px 0 2px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.trail__step {
  position: relative;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  padding-left: 18px;
  font-size: 13px;
}
/* The connector runs from each dot down to the next one. */
.trail__step:not(:last-child)::after {
  content: '';
  position: absolute;
  left: 4px;
  top: 14px;
  bottom: -10px;
  width: 1px;
  background: var(--line-2);
}
.trail__dot {
  position: absolute;
  left: 0;
  top: 50%;
  width: 9px;
  height: 9px;
  margin-top: -4.5px;
  border-radius: 50%;
  background: var(--ink-3);
}
.trail__step--waiting .trail__dot {
  background: var(--card);
  border: 1.5px solid #b7791f;
}
.trail__step--waiting .trail__text {
  color: var(--ink-2);
  font-style: italic;
}
.pill--sm {
  height: 20px;
  padding: 0 8px;
  font-size: 11px;
}

.outcome {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  align-self: flex-start;
  margin-top: 6px;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 500;
}
.outcome--ok {
  background: rgba(47, 122, 74, 0.14);
  color: #226038;
}
.outcome--warn {
  background: rgba(183, 121, 31, 0.14);
  color: #8a5a12;
}
.outcome--bad {
  background: rgba(178, 58, 46, 0.12);
  color: #8f2d23;
}
.v-theme--erDark .outcome--ok { color: #7fcf9a; }
.v-theme--erDark .outcome--warn { color: #e9bc6c; }
.v-theme--erDark .outcome--bad { color: #ef8f84; }
</style>
