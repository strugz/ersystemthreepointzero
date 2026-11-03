<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { formatDateTime } from '@/shared/format';
import type { SignatureStatus } from '@/features/admin/types';

/**
 * Signature image on file (PNG/JPEG) that the R47 prints for the preparer and, once they approve,
 * for the approver, after Finance approves the image. Shows the image on file; a picked file is
 * previewed beside it and only uploaded when confirmed. The parent owns the upload and remove calls
 * (and the remove confirmation) and bumps `imageUrl` after a successful save.
 */
const props = withDefaults(
  defineProps<{
    hasSignature: boolean;
    status?: SignatureStatus;
    approvedAt?: string | null;
    approvedByName?: string | null;
    imageUrl?: string | null;
    uploading?: boolean;
    removing?: boolean;
    disabled?: boolean;
  }>(),
  {
    status: undefined,
    approvedAt: null,
    approvedByName: null,
    imageUrl: null,
    uploading: false,
    removing: false,
    disabled: false,
  },
);

// An older API sends no status; treat an image on file as approved, as it printed then.
const effectiveStatus = computed<SignatureStatus>(() => props.status ?? (props.hasSignature ? 'Approved' : 'None'));
const pill = computed(() => {
  switch (effectiveStatus.value) {
    case 'Approved':
      return { text: 'Approved', cls: 'pill--success' };
    case 'Pending':
      return { text: 'Waiting for Finance', cls: 'pill--warning' };
    default:
      return { text: 'Not on file', cls: 'pill--warning' };
  }
});

const emit = defineEmits<{ upload: [file: File]; remove: [] }>();

const ACCEPTED = new Set(['image/png', 'image/jpeg']);
const picker = ref<HTMLInputElement | null>(null);
const typeError = ref<string | null>(null);
const pending = ref<{ file: File; url: string } | null>(null);
const imageFailed = ref(false);

function clearPending() {
  if (pending.value) URL.revokeObjectURL(pending.value.url);
  pending.value = null;
}

// A new image URL means the upload was saved (or another account loaded): drop the preview.
watch(
  () => props.imageUrl,
  () => {
    imageFailed.value = false;
    clearPending();
  },
);

onBeforeUnmount(clearPending);

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
  clearPending();
  pending.value = { file, url: URL.createObjectURL(file) };
}

function save() {
  if (pending.value) emit('upload', pending.value.file);
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <span class="card-t">Signature</span>
      <span class="pill" :class="pill.cls">{{ pill.text }}</span>
    </div>
    <div class="pa-4 d-flex flex-column ga-3">
      <p class="text-body-2 text-medium-emphasis mb-0">
        Printed on the R47 as the preparer's signature, and as the approver's signature once they approve, with an ELECTRONIC SIGNATURE
        watermark. A new image prints only after Finance approves it. Optional: without one the account can still file and signs the
        printed R47 instead.
      </p>
      <p v-if="effectiveStatus === 'Approved' && approvedAt" class="text-body-2 mb-0">
        Approved{{ approvedByName ? ` by ${approvedByName}` : '' }} on {{ formatDateTime(approvedAt) }}.
      </p>
      <v-alert v-else-if="effectiveStatus === 'Pending'" type="info" variant="tonal" density="compact">
        Waiting for Finance to approve this signature. Until then the R47 prints "Electronically approved" with the date and time instead.
      </v-alert>

      <div class="sig-grid" :class="{ 'sig-grid--two': pending }">
        <figure class="sig">
          <figcaption class="sig__cap">{{ pending ? 'Current' : 'On file' }}</figcaption>
          <div class="sig__frame">
            <img v-if="hasSignature && imageUrl && !imageFailed" :src="imageUrl" alt="Signature on file" class="sig__img" @error="imageFailed = true" />
            <span v-else class="sig__empty">{{ imageFailed ? 'Unable to load the signature.' : 'No signature on file' }}</span>
          </div>
        </figure>
        <figure v-if="pending" class="sig">
          <figcaption class="sig__cap">New · {{ pending.file.name }}</figcaption>
          <div class="sig__frame sig__frame--new">
            <img :src="pending.url" alt="New signature preview" class="sig__img" />
          </div>
        </figure>
      </div>

      <v-alert v-if="typeError" type="warning" variant="tonal" density="compact">{{ typeError }}</v-alert>

      <div v-if="pending" class="d-flex flex-wrap ga-2">
        <v-btn class="press" color="primary" variant="flat" size="small" prepend-icon="mdi-content-save-outline" :loading="uploading" :disabled="disabled" @click="save">
          Save signature
        </v-btn>
        <v-btn class="press" variant="text" size="small" :disabled="uploading" @click="clearPending">Cancel</v-btn>
      </div>
      <div v-else class="d-flex flex-wrap ga-2">
        <v-btn class="press" variant="outlined" size="small" prepend-icon="mdi-draw-pen" :disabled="disabled || removing" @click="pick">
          {{ hasSignature ? 'Replace signature' : 'Upload signature' }}
        </v-btn>
        <v-btn
          v-if="hasSignature"
          class="press"
          variant="text"
          color="error"
          size="small"
          prepend-icon="mdi-delete-outline"
          :loading="removing"
          :disabled="disabled"
          @click="emit('remove')"
        >
          Remove signature
        </v-btn>
      </div>
      <input ref="picker" type="file" accept="image/png,image/jpeg" class="d-none" aria-label="Signature image" @change="onPicked" />
    </div>
  </v-card>
</template>

<style scoped>
.sig-grid {
  display: grid;
  gap: 12px;
}
.sig-grid--two {
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
}
.sig {
  margin: 0;
  min-width: 0;
}
.sig__cap {
  font-size: 12px;
  color: var(--ink-3);
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sig__frame {
  height: 96px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px;
  border: 1px solid var(--line);
  border-radius: 8px;
  /* White like the printed form, so the preview matches how the R47 shows it. */
  background: #fff;
}
.sig__frame--new {
  border-color: var(--navy);
  border-style: dashed;
}
.sig__img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}
.sig__empty {
  font-size: 13px;
  color: var(--ink-3);
}
</style>
