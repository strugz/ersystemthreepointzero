<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import AppConfirmDialog from '@/shared/components/AppConfirmDialog.vue'
import AppFormDialog from '@/shared/components/AppFormDialog.vue'
import { maximumLength, required } from '@/shared/validation/rules'

const props = defineProps<{ approveOpen: boolean; returnOpen: boolean; loading?: boolean }>()
const emit = defineEmits<{
  'update:approveOpen': [value: boolean]
  'update:returnOpen': [value: boolean]
  approve: []
  returnReport: [reason: string]
}>()
const reason = ref('')
const validReason = computed(() => reason.value.trim().length >= 1 && reason.value.trim().length <= 1000)
watch(() => props.returnOpen, open => { if (!open) reason.value = '' })
</script>

<template>
  <AppConfirmDialog
    :model-value="approveOpen"
    title="Approve expense report"
    message="This records your approval and advances the report to the next step."
    confirm-text="Approve"
    color="success"
    :loading="loading"
    @update:model-value="emit('update:approveOpen', $event)"
    @confirm="emit('approve')"
  />
  <AppFormDialog
    :model-value="returnOpen"
    title="Return expense report"
    submit-text="Return report"
    :loading="loading"
    :disabled="!validReason"
    @update:model-value="emit('update:returnOpen', $event)"
    @submit="emit('returnReport', reason.trim())"
  >
    <v-textarea
      v-model="reason"
      label="Reason"
      counter="1000"
      autofocus
      :rules="[required('Return reason'), maximumLength('Return reason', 1000)]"
    />
  </AppFormDialog>
</template>
