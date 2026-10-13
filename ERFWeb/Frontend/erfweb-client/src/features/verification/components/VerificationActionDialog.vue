<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { formatDate, formatMoney } from '@/shared/format';
import type { ExpenseLine } from '@/features/expenses/types';
import { dispositionOptions, findingOptions } from '../labels';
import type { QueryDisposition, Verification, VerificationFinding } from '../types';

/**
 * One dialog for the four verification steps:
 *  - raise (Finance): pick a line and say what is doubtful;
 *  - finding (Verifier): Verified / Verified with note / Not verified, written once;
 *  - query (Finance): refer a Not verified finding to Level 2;
 *  - decide (Level 2): let it through, return, or reject.
 * The API applies the same rules; its 400 message comes back through `error`.
 */
export type VerificationActionMode = 'raise' | 'finding' | 'query' | 'decide';

export interface VerificationActionPayload {
  lineId?: number;
  finding?: VerificationFinding;
  disposition?: QueryDisposition;
  note: string | null;
}

const props = withDefaults(
  defineProps<{
    mode: VerificationActionMode | null;
    request?: Verification | null;
    lines?: ExpenseLine[];
    saving?: boolean;
    error?: string | null;
  }>(),
  { request: null, lines: () => [], saving: false, error: null },
);

const emit = defineEmits<{
  close: [];
  save: [payload: VerificationActionPayload];
}>();

const lineId = ref<number | null>(null);
const finding = ref<VerificationFinding>('Verified');
const disposition = ref<QueryDisposition>('Resume');
const note = ref('');
const touched = ref(false);

watch(
  () => props.mode,
  () => {
    lineId.value = null;
    finding.value = 'Verified';
    disposition.value = 'Resume';
    note.value = '';
    touched.value = false;
  },
);

const copy = computed(() => {
  switch (props.mode) {
    case 'raise':
      return { title: 'Ask for a line to be verified', confirm: 'Send to verifiers', noteLabel: 'What does Finance doubt?' };
    case 'finding':
      return { title: 'Record your finding', confirm: 'Record finding', noteLabel: 'What did you find?' };
    case 'query':
      return { title: 'Query the Level 2 Signatory', confirm: 'Send query', noteLabel: 'What should Level 2 decide?' };
    case 'decide':
      return { title: 'Decide on the query', confirm: 'Save decision', noteLabel: 'Reason' };
    default:
      return { title: '', confirm: '', noteLabel: '' };
  }
});


const noteRequired = computed(() => {
  switch (props.mode) {
    case 'finding':
      return finding.value !== 'Verified';
    case 'decide':
      return disposition.value !== 'Resume';
    default:
      return true;
  }
});

const lineError = computed(() => (props.mode === 'raise' && lineId.value === null ? 'Choose the line to verify.' : null));
const noteError = computed(() => (noteRequired.value && !note.value.trim() ? 'This is required.' : null));

function save() {
  touched.value = true;
  if (lineError.value || noteError.value) return;
  emit('save', {
    lineId: lineId.value ?? undefined,
    finding: props.mode === 'finding' ? finding.value : undefined,
    disposition: props.mode === 'decide' ? disposition.value : undefined,
    note: note.value.trim() || null,
  });
}
</script>

<template>
  <v-dialog :model-value="mode !== null" max-width="500" :persistent="saving" @update:model-value="(v: boolean) => !v && emit('close')">
    <v-card v-if="mode" class="pa-2">
      <v-card-title class="text-subtitle-1 font-weight-bold">{{ copy.title }}</v-card-title>
      <v-card-text class="d-flex flex-column ga-3">
        <template v-if="request">
          <div class="text-body-2">
            <span class="font-weight-medium">{{ request.lineParticulars || 'Expense line' }}</span> · <span class="mono">{{ formatMoney(request.lineTotal) }}</span>
          </div>
          <div v-if="request.reason" class="text-body-2"><span class="text-medium-emphasis">Finance:</span> {{ request.reason }}</div>
          <div v-if="request.findingNote" class="text-body-2"><span class="text-medium-emphasis">Verifier:</span> {{ request.findingNote }}</div>
          <div v-if="request.queryNote" class="text-body-2"><span class="text-medium-emphasis">Finance asks:</span> {{ request.queryNote }}</div>
        </template>

        <div v-if="mode === 'raise'" class="lines">
          <div class="text-caption text-medium-emphasis mb-1">Which line?</div>
          <v-radio-group v-model="lineId" hide-details class="lines__list">
            <v-radio v-for="l in lines" :key="l.id" :value="l.id">
              <template #label>
                <div class="lines__row">
                  <span>{{ formatDate(l.transactionDate) }} · {{ l.particulars || 'Expense line' }}</span>
                  <span class="mono">{{ formatMoney(l.totalAmount) }}</span>
                </div>
              </template>
            </v-radio>
          </v-radio-group>
          <div v-if="touched && lineError" class="text-caption text-error mt-1">{{ lineError }}</div>
        </div>

        <v-radio-group v-if="mode === 'finding'" v-model="finding" hide-details>
          <v-radio v-for="o in findingOptions" :key="o.value" :value="o.value">
            <template #label>
              <div>
                <div>{{ o.title }}</div>
                <div class="text-caption text-medium-emphasis">{{ o.hint }}</div>
              </div>
            </template>
          </v-radio>
        </v-radio-group>

        <v-radio-group v-if="mode === 'decide'" v-model="disposition" hide-details>
          <v-radio v-for="o in dispositionOptions" :key="o.value" :value="o.value">
            <template #label>
              <div>
                <div>{{ o.title }}</div>
                <div class="text-caption text-medium-emphasis">{{ o.hint }}</div>
              </div>
            </template>
          </v-radio>
        </v-radio-group>

        <v-alert v-if="mode === 'finding'" type="info" variant="tonal" density="compact">
          A finding is recorded once and cannot be changed. You only report; you cannot change amounts or approve.
        </v-alert>

        <v-textarea
          v-model="note"
          :label="noteRequired ? copy.noteLabel : `${copy.noteLabel} (optional)`"
          rows="3"
          auto-grow
          counter="2000"
          maxlength="2000"
          :error-messages="touched && noteError ? noteError : (error ?? undefined)"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="emit('close')">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" @click="save">{{ copy.confirm }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.lines__list {
  max-height: 240px;
  overflow-y: auto;
  border: 1px solid var(--line);
  border-radius: 8px;
  padding: 4px 8px;
}
.lines__row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  width: 100%;
  font-size: 13px;
}
.lines__list :deep(.v-label) {
  width: 100%;
  opacity: 1;
}
</style>
