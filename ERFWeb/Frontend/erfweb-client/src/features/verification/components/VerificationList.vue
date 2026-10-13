<script setup lang="ts">
import { formatDate, formatDateTime, formatMoney } from '@/shared/format';
import { dispositionLabel, findingLabel, findingTone, verificationStatusLabel, verificationStatusTone } from '../labels';
import type { VerificationQueueItem } from '../types';

// Rows for the verifier queue and for Level 2's queries. A row opens the report only while the
// viewer still has access to it (`openable`); closed requests are history.
const props = defineProps<{ items: VerificationQueueItem[]; showQuery?: boolean }>();

const emit = defineEmits<{ open: [item: VerificationQueueItem] }>();

function openable(item: VerificationQueueItem): boolean {
  return item.status !== 'Closed' || !!props.showQuery;
}

function open(item: VerificationQueueItem) {
  if (openable(item)) emit('open', item);
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
          <span class="pill" :class="verificationStatusTone[item.status]">{{ verificationStatusLabel[item.status] }}</span>
          <span v-if="item.finding" class="pill" :class="findingTone[item.finding]">{{ findingLabel[item.finding] }}</span>
          <span v-if="item.disposition" class="pill pill--neutral">{{ dispositionLabel[item.disposition] }}</span>
        </div>
        <div class="row__who">
          {{ item.employeeName }}<span v-if="item.departmentName" class="text-medium-emphasis"> · {{ item.departmentName }}</span>
        </div>
        <div class="row__line">
          {{ formatDate(item.lineDate) }} · {{ item.lineParticulars || 'Expense line' }}
        </div>
        <div class="row__text"><span class="row__k">Finance:</span> {{ item.reason }}</div>
        <div v-if="showQuery && item.findingNote" class="row__text"><span class="row__k">Verifier:</span> {{ item.findingNote }}</div>
        <div v-if="showQuery && item.queryNote" class="row__text"><span class="row__k">Finance asks:</span> {{ item.queryNote }}</div>
        <div class="row__meta">
          Asked by {{ item.requestedByName }} {{ formatDateTime(item.requestedAt) }}
          <span v-if="item.status !== 'Closed' && item.daysWaiting > 0"> · waiting {{ item.daysWaiting }} day{{ item.daysWaiting === 1 ? '' : 's' }}</span>
          <span v-if="item.decidedAt"> · decided {{ formatDateTime(item.decidedAt) }}</span>
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
</style>
