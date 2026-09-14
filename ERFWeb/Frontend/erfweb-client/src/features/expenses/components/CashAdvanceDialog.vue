<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { formatMoney, todayIso } from '@/shared/format';
import type { CashAdvanceLine, SaveCashAdvanceLineInput } from '../types';

// Add/edit one row of the CASH ADVANCE RECEIVED block. On add, the parent keeps the dialog open
// and calls reset() so several advances can be entered in a row.
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    line?: CashAdvanceLine | null;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
  }>(),
  { line: null, saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: SaveCashAdvanceLineInput];
}>();

const blank = (): SaveCashAdvanceLineInput => ({
  advanceDate: todayIso(),
  referenceDocument: '',
  referenceNo: '',
  amount: 0,
  rowVersion: null,
});

const form = reactive<SaveCashAdvanceLineInput>(blank());
const dateInput = ref<HTMLElement | null>(null);

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    Object.assign(form, blank());
    if (props.line) {
      Object.assign(form, {
        advanceDate: props.line.advanceDate,
        referenceDocument: props.line.referenceDocument ?? '',
        referenceNo: props.line.referenceNo ?? '',
        amount: props.line.amount,
        rowVersion: props.line.rowVersion,
      } satisfies SaveCashAdvanceLineInput);
    }
  },
);

/** Clears the form for the next advance and puts the cursor back on Date. */
function reset() {
  Object.assign(form, blank());
  requestAnimationFrame(() => dateInput.value?.querySelector('input')?.focus());
}
defineExpose({ reset });

const isEdit = computed(() => props.line !== null);
const canSave = computed(() => Number(form.amount) > 0 && !props.saving);

function fieldErrors(field: string): string[] | undefined {
  return props.errors?.[field];
}

function save() {
  if (!canSave.value) return;
  emit('save', {
    advanceDate: form.advanceDate || null,
    referenceDocument: form.referenceDocument || null,
    referenceNo: form.referenceNo || null,
    amount: Number(form.amount),
    rowVersion: form.rowVersion,
  });
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="640" :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="d-flex align-center">
        <span class="text-subtitle-1 font-weight-bold">{{ isEdit ? 'Edit cash advance' : 'Add cash advance' }}</span>
        <v-spacer />
        <span class="card-meta">CASH ADVANCE RECEIVED block</span>
      </v-card-title>

      <v-card-text>
        <v-form :disabled="saving" @submit.prevent="save">
          <v-row dense>
            <v-col cols="12" sm="6">
              <v-text-field ref="dateInput" v-model="form.advanceDate" type="date" label="Date" autofocus :error-messages="fieldErrors('advanceDate')" />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model.number="form.amount"
                type="number"
                min="0"
                step="0.01"
                label="Amount"
                prefix="₱"
                :error-messages="fieldErrors('amount')"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field v-model="form.referenceDocument" label="Reference document" maxlength="100" hint="e.g. Check Voucher, Petty Cash Voucher" persistent-hint :error-messages="fieldErrors('referenceDocument')" />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field v-model="form.referenceNo" label="Ref. no." maxlength="50" :error-messages="fieldErrors('referenceNo')" />
            </v-col>
          </v-row>
          <div class="d-flex align-center ga-2 mt-1">
            <v-chip size="small" label variant="tonal" color="primary" class="font-weight-bold mono">{{ formatMoney(Number(form.amount) || 0) }}</v-chip>
            <span class="text-caption text-medium-emphasis">Printed as one row; the block's TOTAL is what the R47 reconciles against.</span>
          </div>
        </v-form>
      </v-card-text>

      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">{{ isEdit ? 'Cancel' : 'Done' }}</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">
          {{ isEdit ? 'Save' : 'Add advance' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
