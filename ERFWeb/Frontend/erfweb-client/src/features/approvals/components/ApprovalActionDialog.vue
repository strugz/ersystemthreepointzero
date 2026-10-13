<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { approvalActionCopy } from '../labels';
import type { ApprovalActionKind } from '../types';

// One dialog for all four actions; the copy and the reason rule come from approvalActionCopy.
const props = withDefaults(defineProps<{ action: ApprovalActionKind | null; loading?: boolean; error?: string | null }>(), {
  loading: false,
  error: null,
});

const emit = defineEmits<{
  close: [];
  confirm: [note: string | null];
}>();

const note = ref('');
const touched = ref(false);
watch(
  () => props.action,
  () => {
    note.value = '';
    touched.value = false;
  },
);

const copy = computed(() => (props.action ? approvalActionCopy[props.action] : null));
const missingReason = computed(() => !!copy.value?.reasonRequired && !note.value.trim());

function confirm() {
  touched.value = true;
  if (missingReason.value) return;
  emit('confirm', note.value.trim() || null);
}
</script>

<template>
  <AppConfirmDialog
    :model-value="action !== null"
    :title="copy?.title ?? ''"
    :message="copy?.message ?? ''"
    :confirm-text="copy?.confirm"
    :confirm-color="copy?.color"
    :loading="loading"
    @update:model-value="(v: boolean) => !v && emit('close')"
    @confirm="confirm"
  >
    <v-textarea
      v-model="note"
      :label="copy?.noteLabel"
      rows="3"
      auto-grow
      counter="1000"
      maxlength="1000"
      autofocus
      :error-messages="touched && missingReason ? 'A reason is required.' : (error ?? undefined)"
    />
  </AppConfirmDialog>
</template>
