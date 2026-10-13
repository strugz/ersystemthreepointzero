<script setup lang="ts">
import { formatDateTime } from '@/shared/format';
import { financeStatusLabel, financeStatusTone } from '../labels';
import type { FinanceSummary } from '../types';

// Finance receipt tracking for an approved report, as the employee and approvers see it.
defineProps<{ finance: FinanceSummary }>();
</script>

<template>
  <v-card>
    <div class="fin">
      <div class="d-flex align-center justify-space-between ga-2">
        <span class="card-t">Finance</span>
        <span class="pill" :class="financeStatusTone[finance.status]">{{ financeStatusLabel[finance.status] }}</span>
      </div>
      <p v-if="finance.status === 'Pending'" class="text-body-2 text-medium-emphasis mb-0">
        Submit the original receipts to Finance. The report is paid once Finance confirms them.
      </p>
      <v-alert v-if="finance.underVerification" type="warning" variant="tonal" density="compact" icon="mdi-magnify-scan">
        A line is being verified. Payment waits until the check is done.
      </v-alert>
      <div v-if="finance.remarks" class="fin__remarks">{{ finance.remarks }}</div>
      <div class="fin__meta">
        <span v-if="finance.receivedAt">Received {{ formatDateTime(finance.receivedAt) }}</span>
        <span v-if="finance.clearedAt">Cleared {{ formatDateTime(finance.clearedAt) }}</span>
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.fin {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.fin__remarks {
  font-size: 13px;
  white-space: pre-line;
}
.fin__meta {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 12px;
  color: var(--ink-3);
}
</style>
