<script setup lang="ts">
import { computed } from 'vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';

// Shows the oldest queued message; the next one appears as soon as it is dismissed.
const snack = useSnackbar();
const current = computed(() => snack.messages.value[0] ?? null);

const color: Record<string, string> = { success: 'success', error: 'error', info: 'primary' };
const icon: Record<string, string> = {
  success: 'mdi-check-circle-outline',
  error: 'mdi-alert-circle-outline',
  info: 'mdi-information-outline',
};

function close() {
  if (current.value) {
    snack.dismiss(current.value.id);
  }
}
</script>

<template>
  <v-snackbar
    :model-value="current !== null"
    :color="current ? color[current.kind] : undefined"
    :timeout="current?.kind === 'error' ? 6000 : 3200"
    location="bottom right"
    transition="app-toast"
    @update:model-value="(open: boolean) => !open && close()"
  >
    <div class="d-flex align-center">
      <v-icon v-if="current" :icon="icon[current.kind]" class="mr-2" size="20" />
      <span>{{ current?.text }}</span>
    </div>
    <template #actions>
      <v-btn class="press" variant="text" icon="mdi-close" size="small" aria-label="Dismiss" @click="close" />
    </template>
  </v-snackbar>
</template>
