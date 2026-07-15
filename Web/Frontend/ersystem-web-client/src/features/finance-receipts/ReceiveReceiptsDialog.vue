<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import AppFormDialog from '@/shared/components/AppFormDialog.vue'
import { maximumLength } from '@/shared/validation/rules'

const props = defineProps<{ modelValue: boolean; loading?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; submit: [remarks: string] }>()
const remarks = ref('')
const valid = computed(() => remarks.value.trim().length <= 1000)
watch(() => props.modelValue, open => { if (!open) remarks.value = '' })
</script>

<template>
  <AppFormDialog
    :model-value="modelValue"
    title="Confirm physical receipts"
    submit-text="Mark as received"
    :loading="loading"
    :disabled="!valid"
    @update:model-value="emit('update:modelValue', $event)"
    @submit="emit('submit', remarks.trim())"
  >
    <v-alert
      type="info"
      variant="tonal"
      class="mb-4"
    >
      This action is permanent. Confirm that Finance has the physical documents.
    </v-alert>
    <v-textarea
      v-model="remarks"
      label="Remarks (optional)"
      counter="1000"
      :rules="[maximumLength('Remarks', 1000)]"
    />
  </AppFormDialog>
</template>
