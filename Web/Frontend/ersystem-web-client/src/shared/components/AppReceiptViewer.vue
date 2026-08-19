<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ url: string; contentType: string; title: string }>()
const emit = defineEmits<{ openExternal: []; download: [] }>()
const normalizedType = computed(() => props.contentType.toLowerCase().split(';', 1)[0].trim())
const isImage = computed(() => normalizedType.value.startsWith('image/'))
const isPdf = computed(() => normalizedType.value === 'application/pdf')
</script>
<template>
  <div class="receipt-viewer border rounded-lg overflow-hidden">
    <div class="receipt-viewer__content">
      <img
        v-if="isImage"
        :src="url"
        :alt="title"
        class="receipt-viewer__image"
      ><iframe
        v-else-if="isPdf"
        :src="url"
        :title="title"
        class="receipt-viewer__pdf"
      /><v-alert
        v-else
        type="warning"
        variant="tonal"
        icon="mdi-file-alert-outline"
      >
        This receipt type cannot be previewed here. Open it in another tab or download the file.
      </v-alert>
    </div>
    <div class="receipt-viewer__actions">
      <v-btn
        variant="text"
        prepend-icon="mdi-open-in-new"
        @click="emit('openExternal')"
      >
        Open in new tab
      </v-btn>
      <v-btn
        color="primary"
        prepend-icon="mdi-download"
        @click="emit('download')"
      >
        Download
      </v-btn>
    </div>
  </div>
</template>
