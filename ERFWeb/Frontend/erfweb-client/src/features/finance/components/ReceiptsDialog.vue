<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { formatMoney } from '@/shared/format';
import { financeStatusLabel } from '@/features/expenses/labels';
import type { FinanceQueueItem, ReceiptStatus } from '../types';

// Records what physically arrived. "Received" is one-time and cannot be undone, so the dialog
// says so; "incomplete" needs remarks naming what is missing.
const props = withDefaults(defineProps<{ item: FinanceQueueItem | null; saving?: boolean; error?: string | null }>(), {
  saving: false,
  error: null,
});

const emit = defineEmits<{
  close: [];
  save: [status: ReceiptStatus, remarks: string | null];
}>();

const status = ref<ReceiptStatus>('ReceiptsReceived');
const remarks = ref('');
const touched = ref(false);

watch(
  () => props.item,
  (item) => {
    status.value = 'ReceiptsReceived';
    remarks.value = item?.status === 'ReceiptsIncomplete' ? (item.remarks ?? '') : '';
    touched.value = false;
  },
);

const options = computed(() =>
  (['Pending', 'ReceiptsIncomplete', 'ReceiptsReceived'] as ReceiptStatus[])
    .filter((s) => s !== props.item?.status)
    .map((value) => ({ value, title: financeStatusLabel[value] })),
);
const remarksError = computed(() =>
  status.value === 'ReceiptsIncomplete' && !remarks.value.trim() ? 'Say which receipts are missing.' : null,
);

function save() {
  touched.value = true;
  if (remarksError.value) return;
  emit('save', status.value, remarks.value.trim() || null);
}
</script>

<template>
  <v-dialog :model-value="item !== null" max-width="460" :persistent="saving" @update:model-value="(v: boolean) => !v && emit('close')">
    <v-card v-if="item" class="pa-2">
      <v-card-title class="text-subtitle-1 font-weight-bold">Record receipts</v-card-title>
      <v-card-text class="d-flex flex-column ga-3">
        <div class="text-body-2">
          <span class="mono font-weight-medium">{{ item.referenceNo }}</span> · {{ item.employeeName }} · {{ formatMoney(item.totalAmount) }}
          <div class="text-medium-emphasis">{{ item.attachmentCount }} scanned attachment{{ item.attachmentCount === 1 ? '' : 's' }} on file</div>
        </div>
        <v-radio-group v-model="status" hide-details>
          <v-radio v-for="o in options" :key="o.value" :value="o.value" :label="o.title" />
        </v-radio-group>
        <v-alert v-if="status === 'ReceiptsReceived'" type="info" density="compact" variant="tonal">
          Confirming the receipts as received is one-time. It cannot be changed afterwards.
        </v-alert>
        <v-textarea
          v-model="remarks"
          :label="status === 'ReceiptsIncomplete' ? 'Which receipts are missing?' : 'Remarks (optional)'"
          rows="2"
          auto-grow
          counter="1000"
          maxlength="1000"
          :error-messages="touched && remarksError ? remarksError : (error ?? undefined)"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="emit('close')">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" @click="save">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
