<script setup lang="ts">
defineProps<{ modelValue: boolean; title: string; message: string; confirmText?: string; loading?: boolean; color?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; confirm: [] }>()
</script>
<template>
  <v-dialog
    :model-value="modelValue"
    max-width="480"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card>
      <v-card-title>{{ title }}</v-card-title><v-card-text>{{ message }}</v-card-text><v-card-actions>
        <v-spacer /><v-btn @click="emit('update:modelValue', false)">
          Cancel
        </v-btn><v-btn
          :color="color ?? 'primary'"
          :loading="loading"
          @click="emit('confirm')"
        >
          {{ confirmText ?? 'Confirm' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
