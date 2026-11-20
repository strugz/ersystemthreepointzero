<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { formatMoney } from '@/shared/format';
import type { ExpenseLine } from '@/features/expenses/types';

// The current Level 1 may only lower a line amount, with a reason, before the first approval.
const props = withDefaults(defineProps<{ line: ExpenseLine | null; saving?: boolean; error?: string | null }>(), {
  saving: false,
  error: null,
});

const emit = defineEmits<{
  close: [];
  save: [amount: number, reason: string];
}>();

const amount = ref<number | null>(null);
const reason = ref('');
const touched = ref(false);

watch(
  () => props.line,
  (line) => {
    amount.value = line ? line.amount : null;
    reason.value = '';
    touched.value = false;
  },
);

const amountError = computed(() => {
  if (!props.line || amount.value === null || Number.isNaN(amount.value)) return 'Enter the new amount.';
  if (amount.value < 0) return 'The amount cannot be negative.';
  if (amount.value >= props.line.amount) return `Enter less than ${formatMoney(props.line.amount)}. Amounts can only be lowered.`;
  return null;
});
const reasonError = computed(() => (reason.value.trim() ? null : 'Say why the amount is lowered.'));

// Common reasons; a pick fills the reason, which the approver can still edit. When a Work With colleague paid for this
// meal, that choice comes first and names them, the meal and their report (the start of the line's warning).
const COMMON_REASONS = [
  'Above the allowed rate.',
  'No receipt, or the receipt does not match.',
  'Not a company expense.',
  'Already claimed on another line or report.',
  'Wrong amount entered.',
];
const reasonChoices = computed(() => {
  const paidBy = props.line?.workWithWarnings?.find((w) => w.includes(' paid for '));
  const end = paidBy?.indexOf('). ') ?? -1;
  const workWith = paidBy && end > 0 ? `${paidBy.slice(0, end + 1)}.` : 'Paid by a Work With colleague.';
  return [{ label: 'Paid by Work With', text: workWith }, ...COMMON_REASONS.map((text) => ({ label: text.slice(0, -1), text }))];
});

function save() {
  touched.value = true;
  if (amountError.value || reasonError.value || amount.value === null) return;
  emit('save', amount.value, reason.value.trim());
}
</script>

<template>
  <v-dialog :model-value="line !== null" max-width="460" :persistent="saving" @update:model-value="(v: boolean) => !v && emit('close')">
    <v-card v-if="line" class="pa-2">
      <v-card-title class="text-subtitle-1 font-weight-bold">Lower this amount</v-card-title>
      <v-card-text class="d-flex flex-column ga-3">
        <div class="text-body-2">
          <div class="font-weight-medium">{{ line.particulars }}</div>
          <div class="text-medium-emphasis">
            Filed amount {{ formatMoney(line.amount) }}<span v-if="line.multiplier > 1"> × {{ line.multiplier }}</span>
          </div>
        </div>
        <v-text-field
          v-model.number="amount"
          type="number"
          min="0"
          step="0.01"
          label="New amount"
          prefix="₱"
          autofocus
          :error-messages="touched && amountError ? amountError : undefined"
        />
        <div class="d-flex flex-wrap ga-2" role="group" aria-label="Common reasons">
          <v-chip
            v-for="c in reasonChoices"
            :key="c.label"
            size="small"
            :variant="reason === c.text ? 'flat' : 'outlined'"
            :color="reason === c.text ? 'primary' : undefined"
            class="press"
            @click="reason = c.text"
          >
            {{ c.label }}
          </v-chip>
        </div>
        <v-textarea
          v-model="reason"
          label="Reason (the employee sees this)"
          rows="2"
          auto-grow
          counter="1000"
          maxlength="1000"
          :error-messages="touched && reasonError ? reasonError : (error ?? undefined)"
        />
        <p class="text-caption text-medium-emphasis mb-0">The filed amount stays on record. Amounts lock after the first approval.</p>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="emit('close')">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" @click="save">Lower amount</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
