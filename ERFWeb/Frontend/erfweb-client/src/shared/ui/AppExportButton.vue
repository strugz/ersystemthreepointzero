<script setup lang="ts">
import type { ExportFormat } from '@/shared/download';

// Export button: one format downloads straight away; several open a small menu. The parent does the
// download (it knows the endpoint and filters) and reports progress through `loading`.
const props = withDefaults(
  defineProps<{
    formats?: ExportFormat[];
    loading?: boolean;
    disabled?: boolean;
    label?: string;
  }>(),
  { formats: () => ['Csv'], loading: false, disabled: false, label: 'Export' },
);

const emit = defineEmits<{ export: [format: ExportFormat] }>();

const formatTitle: Record<ExportFormat, string> = { Csv: 'CSV (Excel)', Pdf: 'PDF' };
const formatIcon: Record<ExportFormat, string> = { Csv: 'mdi-file-delimited-outline', Pdf: 'mdi-file-pdf-box' };
</script>

<template>
  <v-btn
    v-if="props.formats.length === 1"
    class="press"
    variant="outlined"
    prepend-icon="mdi-download-outline"
    :loading="props.loading"
    :disabled="props.disabled"
    @click="emit('export', props.formats[0])"
  >
    {{ props.label }}
  </v-btn>
  <v-menu v-else location="bottom end">
    <template #activator="{ props: activator }">
      <v-btn
        v-bind="activator"
        class="press"
        variant="outlined"
        prepend-icon="mdi-download-outline"
        append-icon="mdi-menu-down"
        :loading="props.loading"
        :disabled="props.disabled"
      >
        {{ props.label }}
      </v-btn>
    </template>
    <v-list density="compact">
      <v-list-item v-for="f in props.formats" :key="f" :prepend-icon="formatIcon[f]" :title="formatTitle[f]" @click="emit('export', f)" />
    </v-list>
  </v-menu>
</template>
