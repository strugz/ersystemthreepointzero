<script setup lang="ts">
import { formatDateTime, formatMoney } from '@/shared/format';
import { dispositionLabel, findingLabel, findingTone, verificationStatusLabel, verificationStatusTone } from '../labels';
import type { Verification, VerificationPanel } from '../types';

// Verification requests on a report. What each viewer may do comes from the API (can* flags);
// the claimant sees only the line, the status and the outcome.
withDefaults(defineProps<{ panel: VerificationPanel; busyId?: number | null }>(), { busyId: null });

const emit = defineEmits<{
  request: [];
  pickUp: [request: Verification];
  finding: [request: Verification];
  query: [request: Verification];
  decide: [request: Verification];
}>();
</script>

<template>
  <v-card>
    <div class="ver">
      <div class="d-flex align-center justify-space-between ga-2">
        <span class="card-t">Verification</span>
        <v-btn v-if="panel.canRequest" class="press" size="small" variant="outlined" prepend-icon="mdi-magnify-scan" @click="emit('request')">
          Verify a line
        </v-btn>
      </div>

      <p v-if="panel.requests.length === 0" class="text-body-2 text-medium-emphasis mb-0">
        If a line looks doubtful, ask an Expense Verifier to check the facts. Clearance waits until it is resolved.
      </p>

      <ol v-else class="reqs">
        <li v-for="v in panel.requests" :key="v.id" class="req" :class="{ 'req--busy': busyId === v.id }">
          <div class="req__head">
            <span class="req__line">{{ v.lineParticulars || 'Expense line' }} · <span class="mono">{{ formatMoney(v.lineTotal) }}</span></span>
            <span class="pill" :class="verificationStatusTone[v.status]">{{ verificationStatusLabel[v.status] }}</span>
          </div>

          <div v-if="v.reason" class="req__text"><span class="req__k">Finance:</span> {{ v.reason }}</div>
          <div class="req__meta">
            Asked by {{ v.requestedByName }} {{ formatDateTime(v.requestedAt) }}<span v-if="v.verifierName"> · picked up by {{ v.verifierName }}</span>
          </div>

          <div v-if="v.finding" class="req__row">
            <span class="pill" :class="findingTone[v.finding]">{{ findingLabel[v.finding] }}</span>
            <span class="req__meta">{{ formatDateTime(v.foundAt) }}</span>
          </div>
          <div v-if="v.findingNote" class="req__text"><span class="req__k">Verifier:</span> {{ v.findingNote }}</div>
          <div v-if="v.queryNote" class="req__text">
            <span class="req__k">Query to {{ v.queryToName }}:</span> {{ v.queryNote }}
          </div>
          <div v-if="v.disposition" class="req__text">
            <span class="req__k">{{ dispositionLabel[v.disposition] }}</span><span v-if="v.dispositionNote">: {{ v.dispositionNote }}</span>
            <span class="req__meta"> · {{ formatDateTime(v.decidedAt) }}</span>
          </div>

          <div v-if="v.canPickUp || v.canRecordFinding || v.canQuery || v.canDecide" class="req__actions">
            <v-btn v-if="v.canPickUp" class="press" size="small" color="primary" variant="flat" :loading="busyId === v.id" @click="emit('pickUp', v)">Pick up</v-btn>
            <v-btn v-if="v.canRecordFinding" class="press" size="small" color="primary" variant="flat" @click="emit('finding', v)">Record finding</v-btn>
            <v-btn v-if="v.canQuery" class="press" size="small" color="warning" variant="flat" @click="emit('query', v)">Query Level 2</v-btn>
            <v-btn v-if="v.canDecide" class="press" size="small" color="primary" variant="flat" @click="emit('decide', v)">Decide</v-btn>
          </div>
        </li>
      </ol>
    </div>
  </v-card>
</template>

<style scoped>
.ver {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.reqs {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.req {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-top: 12px;
  border-top: 1px solid var(--line);
  transition: opacity var(--dur-fast) var(--ease-out);
}
.req:first-child {
  border-top: 0;
  padding-top: 0;
}
.req--busy {
  opacity: 0.6;
}
.req__head,
.req__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-wrap: wrap;
}
.req__row {
  justify-content: flex-start;
}
.req__line {
  font-size: 13px;
  font-weight: 500;
}
.req__text {
  font-size: 13px;
  white-space: pre-line;
}
.req__k {
  color: var(--ink-3);
}
.req__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.req__actions {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-top: 4px;
}
</style>
