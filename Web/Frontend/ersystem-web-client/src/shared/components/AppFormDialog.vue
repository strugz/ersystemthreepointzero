<script setup lang="ts">
defineProps<{ modelValue: boolean; title: string; submitText?: string; loading?: boolean; disabled?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; submit: [] }>()
</script>
<template>
  <v-dialog
    :model-value="modelValue"
    max-width="620"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card>
      <v-card-title>{{ title }}</v-card-title><v-card-text><slot /></v-card-text><v-card-actions>
        <v-spacer /><v-btn @click="emit('update:modelValue', false)">
          Cancel
        </v-btn><v-btn
          color="primary"
          :loading="loading"
          :disabled="disabled"
          @click="emit('submit')"
        >
          {{ submitText ?? 'Save' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
