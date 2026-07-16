<script setup lang="ts">
import type { ReceiptAttachment } from '@/shared/types/reportReview'
import { formatReceiptSize, receiptDisplayName } from '@/shared/utils/reportReview'

defineProps<{
  attachments: ReceiptAttachment[]
  openingAttachmentId: number | null
  desktop: boolean
}>()

const emit = defineEmits<{
  open: [attachment: ReceiptAttachment, index: number]
}>()
</script>

<template>
  <v-card
    v-if="attachments.length"
    title="Scanned receipts"
    class="mb-4 border"
    variant="flat"
  >
    <v-list>
      <v-list-item
        v-for="(attachment, index) in attachments"
        :key="attachment.id"
        class="receipt-list-item"
        :title="receiptDisplayName(attachment, index)"
        :subtitle="[formatReceiptSize(attachment.fileSizeBytes), !desktop ? 'Tap to open' : ''].filter(Boolean).join(' · ')"
        prepend-icon="mdi-paperclip"
        :aria-label="`Open ${receiptDisplayName(attachment, index)}`"
        :disabled="openingAttachmentId != null"
        tag="button"
        type="button"
        link
        @click="emit('open', attachment, index)"
      >
        <template #append>
          <v-progress-circular
            v-if="openingAttachmentId === attachment.id"
            indeterminate
            color="primary"
            size="22"
            width="2"
            :aria-label="`Opening ${receiptDisplayName(attachment, index)}`"
          />
          <span
            v-else
            class="receipt-list-item__action"
            aria-hidden="true"
          >
            <span v-if="desktop">Open</span>
            <v-icon icon="mdi-chevron-right" />
          </span>
        </template>
      </v-list-item>
    </v-list>
  </v-card>
</template>
