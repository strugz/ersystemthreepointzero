<script setup lang="ts">
import { ref } from 'vue';

/**
 * Signature image on file (PNG/JPEG) that the R47 prints under "Prepared by". The parent owns the
 * upload call; this only picks a file and validates the type client-side.
 */
withDefaults(defineProps<{ hasSignature: boolean; uploading?: boolean; disabled?: boolean }>(), {
  uploading: false,
  disabled: false,
});

const emit = defineEmits<{ upload: [file: File] }>();

const ACCEPTED = new Set(['image/png', 'image/jpeg']);
const picker = ref<HTMLInputElement | null>(null);
const typeError = ref<string | null>(null);

function pick() {
  picker.value?.click();
}

function onPicked(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;
  if (!ACCEPTED.has(file.type)) {
    typeError.value = 'Choose a PNG or JPEG image.';
    return;
  }
  typeError.value = null;
  emit('upload', file);
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <span class="card-t">Signature</span>
      <span class="pill" :class="hasSignature ? 'pill--success' : 'pill--warning'">{{ hasSignature ? 'On file' : 'Not on file' }}</span>
    </div>
    <div class="pa-4 d-flex flex-column ga-3">
      <p class="text-body-2 text-medium-emphasis mb-0">
        Printed on the R47 as the preparer's signature. The account cannot file an expense report until one is on file.
      </p>
      <v-alert v-if="typeError" type="warning" variant="tonal" density="compact">{{ typeError }}</v-alert>
      <div>
        <v-btn class="press" variant="outlined" size="small" prepend-icon="mdi-draw-pen" :loading="uploading" :disabled="disabled" @click="pick">
          {{ hasSignature ? 'Replace signature' : 'Upload signature' }}
        </v-btn>
        <input ref="picker" type="file" accept="image/png,image/jpeg" class="d-none" aria-label="Signature image" @change="onPicked" />
      </div>
    </div>
  </v-card>
</template>
