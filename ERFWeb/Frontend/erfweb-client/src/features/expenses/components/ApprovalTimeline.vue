<script setup lang="ts">
import { formatDateTime } from '@/shared/format';
import { approvalStatusLabel } from '../labels';
import type { ApprovalStep, ReportStatus } from '../types';

defineProps<{ steps: ApprovalStep[]; status: ReportStatus; cycle: number }>();

const tone: Record<string, string> = {
  Pending: 'step--pending',
  Approved: 'step--approved',
  Returned: 'step--returned',
  Superseded: 'step--superseded',
  Rejected: 'step--returned',
  Overridden: 'step--superseded',
};
</script>

<template>
  <v-card>
    <div class="approval">
      <div class="d-flex align-center justify-space-between">
        <span class="card-t">Approval</span>
        <span v-if="cycle > 1" class="card-meta">Cycle {{ cycle }}</span>
      </div>

      <p v-if="steps.length === 0" class="text-body-2 text-medium-emphasis mb-0">
        {{ status === 'New' ? 'Approvers are assigned when you file the report.' : 'No approval steps recorded.' }}
      </p>

      <ol v-else class="steps">
        <li v-for="s in steps" :key="s.stepOrder" class="step" :class="tone[s.status]">
          <span class="step__num mono">
            <svg v-if="s.status === 'Approved'" viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5L20 7"></path></svg>
            <template v-else>{{ s.stepOrder }}</template>
          </span>
          <div class="step__body">
            <div class="step__name">{{ s.approverName }}<span v-if="s.level" class="step__level"> · Level {{ s.level }}</span></div>
            <div class="step__meta">
              {{ approvalStatusLabel[s.status] }}<span v-if="s.actedByName"> by {{ s.actedByName }} on behalf of {{ s.approverName }}</span><span v-if="s.actedAt"> · {{ formatDateTime(s.actedAt) }}</span>
            </div>
            <div v-if="s.note" class="step__note">{{ s.note }}</div>
          </div>
        </li>
      </ol>
    </div>
  </v-card>
</template>

<style scoped>
.approval {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.steps {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.step {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  font-size: 13px;
}
.step__num {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 1px solid var(--line-2);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  color: var(--ink-2);
  flex: none;
  margin-top: 1px;
}
.step--approved .step__num {
  background: rgb(var(--v-theme-success));
  border-color: rgb(var(--v-theme-success));
  color: #fff;
}
.step--returned .step__num {
  border-color: rgb(var(--v-theme-error));
  color: rgb(var(--v-theme-error));
}
.step--superseded {
  opacity: 0.55;
}
.step__name {
  font-weight: 500;
}
.step__level {
  font-weight: 400;
  color: var(--ink-3);
  font-size: 12px;
}
.step__meta {
  color: var(--ink-3);
  font-size: 12px;
}
.step__note {
  margin-top: 4px;
  font-size: 12px;
  white-space: pre-line;
}
</style>
