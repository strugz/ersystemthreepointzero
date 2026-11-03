<script setup lang="ts">
import { ref } from 'vue';
import { isTestMode } from '@/shared/appMode';
import { testDataApi, type TestDataKind } from '@/shared/api/testData';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';

// Test-only hard delete for a record made while testing. Renders nothing unless the build is a test build
// (VITE_APP_MODE=test); the parent decides who sees it (the API allows the System Administrator only).
const props = withDefaults(
  defineProps<{
    kind: TestDataKind;
    id: string | number;
    /** What is deleted, for the confirmation ("report ERF-2026-000142"). */
    label: string;
    /** Text button ("Delete test data") instead of an icon, for page headers. */
    text?: boolean;
  }>(),
  { text: false },
);

const emit = defineEmits<{ deleted: [] }>();

const snackbar = useSnackbar();
const confirmOpen = ref(false);
const deleting = ref(false);

async function remove() {
  deleting.value = true;
  try {
    await testDataApi.delete(props.kind, props.id);
    confirmOpen.value = false;
    snackbar.success(`Deleted ${props.label}.`);
    emit('deleted');
  } catch (err) {
    snackbar.fromError(err, `Could not delete ${props.label}.`);
  } finally {
    deleting.value = false;
  }
}
</script>

<template>
  <template v-if="isTestMode">
    <v-btn
      v-if="props.text"
      class="press"
      color="error"
      variant="outlined"
      prepend-icon="mdi-delete-forever-outline"
      @click.stop="confirmOpen = true"
    >
      Delete test data
    </v-btn>
    <v-btn
      v-else
      class="press"
      color="error"
      variant="text"
      size="small"
      icon="mdi-delete-forever-outline"
      :aria-label="`Delete ${props.label} (test data)`"
      v-tooltip="'Delete (test data)'"
      @click.stop="confirmOpen = true"
    />
    <AppConfirmDialog
      v-model="confirmOpen"
      title="Delete test data?"
      :message="`This permanently deletes ${props.label} and everything under it. It cannot be undone.`"
      confirm-text="Delete"
      confirm-color="error"
      :loading="deleting"
      @confirm="remove"
    />
  </template>
</template>
