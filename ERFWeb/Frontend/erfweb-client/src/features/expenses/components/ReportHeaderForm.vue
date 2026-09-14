<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { reportTypeLabel, usesCashAdvance, usesRevolvingFund } from '../labels';
import ReportTypePicker from './ReportTypePicker.vue';
import type { ReportHeaderInput } from '../types';

// Header + cash-advance form. Emits the typed input; the parent owns save/create.
// Which money block is shown follows the report type, exactly as the desktop form does:
//   Liquidation of Cash Advance      -> CASH ADVANCE RECEIVED block
//   Replenishment of Revolving Fund  -> revolving fund amount
//   Reimbursement                    -> neither
const props = withDefaults(
  defineProps<{
    initial?: ReportHeaderInput | null;
    disabled?: boolean;
    saving?: boolean;
    submitText?: string;
    dirtyHint?: boolean;
  }>(),
  { initial: null, disabled: false, saving: false, submitText: 'Save changes', dirtyHint: true },
);

const emit = defineEmits<{ submit: [input: ReportHeaderInput] }>();

const blank = (): ReportHeaderInput => ({
  reportType: 'Reimbursement',
  purpose: '',
  dateFrom: null,
  dateTo: null,
  revolvingFundAmount: 0,
});

// Deep copy that works on Vue reactive proxies (structuredClone throws DataCloneError on a Proxy).
const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value)) as T;

const form = reactive<ReportHeaderInput>(blank());
let snapshot = JSON.stringify(form);

function load(value: ReportHeaderInput | null) {
  const next = value ? clone(value) : blank();
  Object.assign(form, next);
  snapshot = JSON.stringify(form);
}

watch(() => props.initial, load, { immediate: true, deep: true });

// Purpose defaults to "<type> MM/dd/yyyy - MM/dd/yyyy", as the desktop ERF filled it. It is only
// written while the field is empty or still holds the previous suggestion, so typed text is kept.
const mdy = (iso: string) => {
  const [y, m, d] = iso.split('-');
  return `${m}/${d}/${y}`;
};
function suggestedPurpose(): string {
  const label = reportTypeLabel[form.reportType];
  return form.dateFrom && form.dateTo ? `${label} ${mdy(form.dateFrom)} - ${mdy(form.dateTo)}` : label;
}
let lastSuggestion = '';
watch(
  () => [form.reportType, form.dateFrom, form.dateTo] as const,
  () => {
    const current = (form.purpose ?? '').trim();
    if (current === '' || current === lastSuggestion) {
      lastSuggestion = suggestedPurpose();
      form.purpose = lastSuggestion;
    }
  },
  { immediate: true },
);

const isDirty = computed(() => JSON.stringify(form) !== snapshot);
const showRevolving = computed(() => usesRevolvingFund(form.reportType));
const showCash = computed(() => usesCashAdvance(form.reportType));

const dateError = computed(() =>
  form.dateFrom && form.dateTo && form.dateTo < form.dateFrom ? 'Date To must be on or after Date From.' : undefined,
);

function submit() {
  if (dateError.value) {
    return;
  }
  const out = clone(form);
  out.revolvingFundAmount = showRevolving.value ? Number(form.revolvingFundAmount) || 0 : 0;
  emit('submit', out);
}

defineExpose({ isDirty });
</script>

<template>
  <v-form :disabled="disabled || saving" @submit.prevent="submit">
    <div class="lbl mb-2">Report type</div>
    <ReportTypePicker v-model="form.reportType" :disabled="disabled || saving" class="mb-1" />
    <p class="text-caption text-medium-emphasis mb-4">The ERF reference no. is assigned automatically when the report is filed.</p>

    <v-row dense>
      <v-col cols="12">
        <v-textarea
          v-model="form.purpose"
          label="Purpose"
          rows="2"
          auto-grow
          maxlength="300"
          counter
          hint="Printed on the R47 as the report purpose."
          persistent-hint
        />
      </v-col>
      <v-col cols="12" sm="6" md="4">
        <v-text-field v-model="form.dateFrom" type="date" label="Date from" hide-details="auto" />
      </v-col>
      <v-col cols="12" sm="6" md="4">
        <v-text-field v-model="form.dateTo" type="date" label="Date to" :error-messages="dateError" hide-details="auto" />
      </v-col>
      <v-col v-if="showRevolving" cols="12" md="4">
        <v-text-field
          v-model.number="form.revolvingFundAmount"
          type="number"
          min="0"
          step="0.01"
          label="Revolving fund amount"
          prefix="₱"
          hide-details="auto"
        />
      </v-col>
    </v-row>

    <v-expand-transition>
      <p v-if="showCash" class="text-caption text-medium-emphasis mt-3 mb-0">
        The advances you received are entered as rows in the <strong>Cash advance received</strong> card of the report.
        Who the balance is due to is worked out from the figures: unspent advance is due to MDMPI, spending beyond it is due to you.
      </p>
    </v-expand-transition>

    <div class="d-flex align-center mt-4">
      <span v-if="dirtyHint && isDirty && !disabled" class="text-caption text-medium-emphasis">Unsaved changes</span>
      <v-spacer />
      <v-btn
        class="press"
        color="primary"
        type="submit"
        variant="flat"
        :loading="saving"
        :disabled="disabled || (dirtyHint && !isDirty) || !!dateError"
      >
        {{ submitText }}
      </v-btn>
    </div>
  </v-form>
</template>
