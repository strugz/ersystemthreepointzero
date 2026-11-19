<script setup lang="ts">
import { formatDateTime, formatMoney } from '@/shared/format';
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
import type { Verification, VerificationPanel } from '../types';

// Verification requests on a report, shown as a thread so everyone involved sees who said what and
// whose turn it is. What each viewer may do comes from the API (can* flags); the claimant gets
// reason, notes and names as null, so every name falls back to the role.
withDefaults(defineProps<{ panel: VerificationPanel; busyId?: number | null }>(), { busyId: null });

const emit = defineEmits<{
  request: [];
  pickUp: [request: Verification];
  finding: [request: Verification];
  query: [request: Verification];
  decide: [request: Verification];
}>();

type Tone = 'navy' | 'brass' | 'green' | 'neutral';
interface Entry {
  key: string;
  kind: 'system' | 'message';
  icon?: string;
  author?: string;
  role?: string;
  tone?: Tone;
  to?: string;
  at: string | null;
  pill?: { label: string; tone: string };
  text?: string | null;
}
interface Step {
  label: string;
  done: boolean;
  bad: boolean;
}

function initials(name: string): string {
  const parts = name.replace(/[^\p{L}\s]/gu, '').split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
}

function isAutomatic(v: Verification): boolean {
  return !!v.reason?.startsWith(AUTOMATIC_PREFIX);
}

function needsLevel2(v: Verification): boolean {
  return v.finding === 'NotVerified' || !!v.queriedAt;
}

function thread(v: Verification): Entry[] {
  const level2 = v.queryToName ?? 'Level 2';
  const entries: Entry[] = [];

  if (isAutomatic(v)) {
    entries.push({
      key: 'opened',
      kind: 'system',
      icon: 'mdi-robot-outline',
      at: v.requestedAt,
      text: v.requestedByName ? `Opened automatically when ${v.requestedByName} gave final approval` : 'Opened automatically on final approval',
    });
    entries.push({ key: 'rule', kind: 'message', author: 'Finance rule', tone: 'neutral', icon: 'mdi-gavel', at: null, text: v.reason!.slice(AUTOMATIC_PREFIX.length) });
  } else if (v.reason) {
    entries.push({ key: 'asked', kind: 'message', author: v.requestedByName || 'Finance', role: 'Finance', tone: 'brass', at: v.requestedAt, text: v.reason });
  } else {
    entries.push({ key: 'asked', kind: 'system', icon: 'mdi-magnify-scan', at: v.requestedAt, text: 'Verification requested' });
  }

  if (v.pickedUpAt) {
    entries.push({
      key: 'picked',
      kind: 'system',
      icon: 'mdi-hand-back-right-outline',
      at: v.pickedUpAt,
      text: `${v.verifierName ?? 'An Expense Verifier'} picked it up`,
    });
  }
  if (v.finding) {
    entries.push({
      key: 'finding',
      kind: 'message',
      author: v.verifierName ?? 'Expense Verifier',
      role: v.verifierName ? 'Expense Verifier' : undefined,
      tone: 'navy',
      at: v.foundAt,
      pill: { label: findingLabel[v.finding], tone: findingTone[v.finding] },
      text: v.findingNote,
    });
  }
  if (v.queriedAt) {
    entries.push({ key: 'query', kind: 'message', author: 'Finance', tone: 'brass', to: `${level2} (Level 2)`, at: v.queriedAt, text: v.queryNote });
  }
  if (v.disposition) {
    entries.push({
      key: 'decision',
      kind: 'message',
      author: level2,
      role: v.queryToName ? 'Level 2' : undefined,
      tone: 'green',
      at: v.decidedAt,
      pill: { label: dispositionLabel[v.disposition], tone: dispositionTone[v.disposition] },
      text: v.dispositionNote,
    });
  }
  return entries;
}

function steps(v: Verification): Step[] {
  const list: Step[] = [
    { label: 'Asked', done: true, bad: false },
    { label: 'Picked up', done: !!v.pickedUpAt, bad: false },
    { label: v.finding ? findingLabel[v.finding] : 'Finding', done: !!v.finding, bad: v.finding === 'NotVerified' },
  ];
  if (needsLevel2(v)) {
    list.push({ label: 'Sent to Level 2', done: !!v.queriedAt, bad: false });
    list.push({ label: v.disposition ? dispositionLabel[v.disposition] : 'Decided', done: !!v.disposition, bad: v.disposition === 'Reject' });
  }
  return list;
}

function currentStep(v: Verification): number {
  return v.status === 'Closed' ? -1 : steps(v).findIndex((s) => !s.done);
}

function headPill(v: Verification): { label: string; tone: string } {
  if (v.status !== 'Closed') return { label: verificationStatusLabel[v.status], tone: verificationStatusTone[v.status] };
  if (v.disposition) return { label: dispositionLabel[v.disposition], tone: dispositionTone[v.disposition] };
  if (v.finding) return { label: findingLabel[v.finding], tone: findingTone[v.finding] };
  return { label: verificationStatusLabel.Closed, tone: verificationStatusTone.Closed };
}

/** Whose turn it is, or how it ended. */
function nextStep(v: Verification): { text: string; since: string | null; open: boolean } {
  switch (v.status) {
    case 'Open':
      return { text: 'Waiting for an Expense Verifier to pick it up', since: v.requestedAt, open: true };
    case 'InProgress':
      return { text: `Waiting on ${v.verifierName ?? 'the Expense Verifier'} for a finding`, since: v.pickedUpAt, open: true };
    case 'Found':
      return { text: 'Waiting on Finance to send it to Level 2', since: v.foundAt, open: true };
    case 'Queried':
      return { text: `Waiting on ${v.queryToName ?? 'Level 2'} to decide`, since: v.queriedAt, open: true };
    default:
      return { text: closedOutcome(v.disposition), since: null, open: false };
  }
}
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
            <div class="req__title">
              <span class="req__line">{{ v.lineParticulars || 'Expense line' }}</span>
              <span class="req__amount mono">{{ formatMoney(v.lineTotal) }}</span>
            </div>
            <span class="pill" :class="headPill(v).tone">{{ headPill(v).label }}</span>
          </div>

          <ol class="steps" :aria-label="`Progress: ${headPill(v).label}`">
            <li
              v-for="(s, i) in steps(v)"
              :key="s.label + i"
              class="step"
              :class="{ 'step--done': s.done, 'step--bad': s.bad, 'step--now': i === currentStep(v) }"
            >
              <span class="step__bar" />
              <span class="step__label">{{ s.label }}</span>
            </li>
          </ol>

          <TransitionGroup tag="ol" name="msg" class="thread">
            <li v-for="e in thread(v)" :key="e.key" :class="e.kind === 'system' ? 'sys' : 'msg'">
              <template v-if="e.kind === 'system'">
                <v-icon :icon="e.icon" size="16" />
                <span>{{ e.text }}<span v-if="e.at" class="meta"> · {{ formatDateTime(e.at) }}</span></span>
              </template>
              <template v-else>
                <span class="avatar" :class="`avatar--${e.tone}`" aria-hidden="true">
                  <v-icon v-if="e.icon" :icon="e.icon" size="15" />
                  <template v-else>{{ initials(e.author!) }}</template>
                </span>
                <div class="msg__body">
                  <div class="msg__who">
                    <span class="msg__name">{{ e.author }}</span>
                    <span v-if="e.role" class="msg__role">{{ e.role }}</span>
                    <span v-if="e.at" class="meta">{{ formatDateTime(e.at) }}</span>
                  </div>
                  <div v-if="e.to" class="msg__to"><v-icon icon="mdi-arrow-right" size="13" /> to {{ e.to }}</div>
                  <span v-if="e.pill" class="pill pill--sm" :class="e.pill.tone">{{ e.pill.label }}</span>
                  <div v-if="e.text" class="bubble">{{ e.text }}</div>
                </div>
              </template>
            </li>
          </TransitionGroup>

          <div class="next" :class="{ 'next--open': nextStep(v).open }">
            <v-icon :icon="nextStep(v).open ? 'mdi-clock-outline' : 'mdi-check-circle-outline'" size="16" />
            <span class="next__text">
              {{ nextStep(v).text }}<span v-if="nextStep(v).since" class="meta"> · since {{ formatDateTime(nextStep(v).since) }}</span>
            </span>
            <div v-if="v.canPickUp || v.canRecordFinding || v.canQuery || v.canDecide" class="next__actions">
              <v-btn v-if="v.canPickUp" class="press" size="small" color="primary" variant="flat" :loading="busyId === v.id" @click="emit('pickUp', v)">Pick up</v-btn>
              <v-btn v-if="v.canRecordFinding" class="press" size="small" color="primary" variant="flat" @click="emit('finding', v)">Record finding</v-btn>
              <v-btn v-if="v.canQuery" class="press" size="small" color="warning" variant="flat" @click="emit('query', v)">Query Level 2</v-btn>
              <v-btn v-if="v.canDecide" class="press" size="small" color="primary" variant="flat" @click="emit('decide', v)">Decide</v-btn>
            </div>
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
.reqs,
.thread,
.steps {
  list-style: none;
  margin: 0;
  padding: 0;
}
.reqs {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.req {
  border: 1px solid var(--line);
  border-radius: 10px;
  overflow: hidden;
  transition: opacity var(--dur-fast) var(--ease-out);
}
.req--busy {
  opacity: 0.6;
}
.req__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-wrap: wrap;
  padding: 12px 14px;
  border-bottom: 1px solid var(--line);
}
.req__title {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.req__line {
  font-size: 13px;
  font-weight: 500;
}
.req__amount {
  font-size: 12px;
  color: var(--ink-2);
}

.steps {
  display: flex;
  gap: 4px;
  padding: 10px 14px 0;
}
.step {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 11px;
  color: var(--ink-3);
}
.step__bar {
  height: 4px;
  border-radius: 2px;
  background: var(--line);
}
.step__label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.step--done .step__bar {
  background: #2f7a4a;
}
.step--done {
  color: var(--ink-2);
}
.step--bad .step__bar {
  background: #b23a2e;
}
.step--now .step__bar {
  background: #b7791f;
}
.step--now {
  color: var(--ink);
  font-weight: 500;
}

.thread {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 14px;
}
.sys {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding-left: 6px;
  font-size: 12px;
  color: var(--ink-2);
}
.msg {
  display: flex;
  gap: 10px;
}
.avatar {
  flex: none;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 600;
}
.avatar--navy {
  background: var(--navy-soft);
  color: var(--navy);
}
.avatar--brass {
  background: var(--brass-soft);
  color: var(--brass);
}
.avatar--green {
  background: rgba(47, 122, 74, 0.14);
  color: #226038;
}
.v-theme--erDark .avatar--green {
  color: #7fcf9a;
}
.avatar--neutral {
  background: var(--line);
  color: var(--ink-2);
}
.msg__body {
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
}
.msg__who {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 6px;
}
.msg__name {
  font-size: 13px;
  font-weight: 500;
}
.msg__role {
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 4px;
  background: var(--line);
  color: var(--ink-2);
}
.msg__to {
  font-size: 12px;
  color: var(--ink-2);
}
.pill--sm {
  height: 20px;
  padding: 0 8px;
  font-size: 11px;
}
.bubble {
  font-size: 13px;
  white-space: pre-line;
  padding: 8px 10px;
  border-radius: 4px 10px 10px 10px;
  background: var(--paper);
  border: 1px solid var(--line);
  max-width: 100%;
  overflow-wrap: anywhere;
}
.meta {
  font-size: 12px;
  color: var(--ink-3);
}

.next {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  padding: 10px 14px;
  border-top: 1px solid var(--line);
  background: var(--paper);
  font-size: 13px;
  color: var(--ink-2);
}
.next--open {
  background: var(--brass-soft);
  color: var(--ink);
}
.next__text {
  flex: 1;
  min-width: 180px;
}
.next__actions {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

/* Only messages added after an action animate in; the first render does not. */
.msg-enter-active {
  transition:
    opacity var(--dur-fast) var(--ease-out),
    transform var(--dur-fast) var(--ease-out);
}
.msg-enter-from {
  opacity: 0;
  transform: translateY(8px);
}
@media (prefers-reduced-motion: reduce) {
  .msg-enter-from {
    transform: none;
  }
}
</style>
