<script setup lang="ts">
// Confirmation modal. Centered (modals are not origin-aware), enter 220ms / exit faster via main.css.
withDefaults(
  defineProps<{
    modelValue: boolean;
    title: string;
    message: string;
    confirmText?: string;
    confirmColor?: string;
    loading?: boolean;
  }>(),
  { confirmText: 'Confirm', confirmColor: 'primary', loading: false },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  confirm: [];
}>();
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="440"
    :persistent="loading"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
  >
    <v-card class="pa-2">
      <v-card-title class="text-subtitle-1 font-weight-bold">{{ title }}</v-card-title>
      <v-card-text class="text-body-2" style="white-space: pre-line">{{ message }}</v-card-text>
      <!-- Optional extra input (e.g. a note) rendered between the message and the actions. -->
      <v-card-text v-if="$slots.default" class="pt-0"><slot /></v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="loading" @click="emit('update:modelValue', false)">Cancel</v-btn>
        <v-btn class="press" :color="confirmColor" variant="flat" :loading="loading" @click="emit('confirm')">
          {{ confirmText }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
