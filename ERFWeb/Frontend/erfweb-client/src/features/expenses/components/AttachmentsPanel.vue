<script setup lang="ts">
import { ref } from 'vue';
import { formatBytes } from '@/shared/format';
import { expensesApi } from '../api';
import type { Attachment } from '../types';

const props = withDefaults(
  defineProps<{ reportId: string; attachments: Attachment[]; editable: boolean; uploading?: boolean; busyId?: number | null }>(),
  { uploading: false, busyId: null },
);

const emit = defineEmits<{
  upload: [file: File];
  remove: [attachment: Attachment];
}>();

const picker = ref<HTMLInputElement | null>(null);
const dragging = ref(false);

function pick() {
  picker.value?.click();
}

function onPicked(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (file) {
    emit('upload', file);
  }
  input.value = '';
}

function onDrop(event: DragEvent) {
  dragging.value = false;
  const file = event.dataTransfer?.files?.[0];
  if (file && props.editable) {
    emit('upload', file);
  }
}

function icon(contentType: string): string {
  return contentType === 'application/pdf' ? 'mdi-file-pdf-box' : 'mdi-file-image-outline';
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <span class="card-t">Receipts</span>
      <span class="card-meta">{{ attachments.length }} file{{ attachments.length === 1 ? '' : 's' }}</span>
    </div>

    <ul class="files" :class="{ 'files--empty': attachments.length === 0 }">
      <li v-if="attachments.length === 0" class="files__none text-body-2 text-medium-emphasis">No scans attached yet.</li>
      <li v-for="(a, i) in attachments" :key="a.id" class="file stagger-in" :style="{ '--i': Math.min(i, 8) }">
        <v-icon :icon="icon(a.contentType)" size="18" class="file__icon" />
        <a class="file__name" :href="expensesApi.attachmentUrl(props.reportId, a.id)" target="_blank" rel="noopener" :title="`${a.uploadedBy}`">
          {{ a.fileName }}
        </a>
        <span class="file__size mono">{{ formatBytes(a.sizeBytes) }}</span>
        <v-btn
          v-if="editable"
          class="press file__del"
          icon="mdi-close"
          size="x-small"
          variant="text"
          aria-label="Delete attachment"
          :loading="busyId === a.id"
          @click="emit('remove', a)"
        />
      </li>
    </ul>

    <div v-if="editable" class="dropzone-wrap">
      <button
        type="button"
        class="dropzone press"
        :class="{ 'dropzone--active': dragging }"
        :disabled="uploading"
        @click="pick"
        @dragover.prevent="dragging = true"
        @dragleave="dragging = false"
        @drop.prevent="onDrop"
      >
        <v-progress-circular v-if="uploading" indeterminate size="16" width="2" color="primary" />
        <v-icon v-else icon="mdi-tray-arrow-up" size="16" />
        <span>{{ uploading ? 'Uploading…' : 'Drop JPEG, PNG or PDF here, or tap to choose' }}</span>
      </button>
      <input ref="picker" type="file" accept="image/jpeg,image/png,application/pdf" hidden @change="onPicked" />
    </div>
    <p class="files__hint text-caption text-medium-emphasis">Photos are shrunk before upload to save space. Finance still needs the original receipts.</p>
  </v-card>
</template>

<style scoped>
.files {
  list-style: none;
  margin: 0;
  padding: 6px 20px 0;
  display: flex;
  flex-direction: column;
}
.files--empty {
  padding-top: 12px;
}
.file {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 40px;
  font-size: 13px;
  border-bottom: 1px solid var(--line);
}
.file:last-child {
  border-bottom: 0;
}
.file__icon {
  color: var(--ink-2);
}
.file__name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--ink);
  text-decoration: none;
}
.file__name:hover {
  text-decoration: underline;
}
.file__size {
  color: var(--ink-3);
  font-size: 12px;
}
.dropzone-wrap {
  padding: 12px 20px 0;
}
.dropzone {
  width: 100%;
  min-height: 44px;
  border: 1px dashed var(--line-2);
  border-radius: 8px;
  padding: 12px 14px;
  background: transparent;
  color: var(--ink-3);
  font: inherit;
  font-size: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  cursor: pointer;
  transition: border-color 120ms ease, background-color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.dropzone--active {
  border-color: var(--navy);
  background: var(--navy-soft);
  color: var(--navy);
}
.dropzone:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.files__hint {
  margin: 0;
  padding: 10px 20px 16px;
}
</style>
