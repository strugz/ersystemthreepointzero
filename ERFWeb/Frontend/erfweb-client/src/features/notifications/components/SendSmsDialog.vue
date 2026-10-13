<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { newIdempotencyKey } from '../api';
import { SMS_MAX_LENGTH } from '../types';

// Compose one notification SMS. The idempotency key is made when the dialog opens and kept while the
// message stays the same, so pressing Send again after a failure retries the same SMS instead of
// sending a second one. Editing the message makes it a new SMS with a new key.
const props = withDefaults(
  defineProps<{
    open: boolean;
    recipientName: string;
    context?: string | null;
    /** Who else receives it, shown under the recipient. */
    recipientNote?: string;
    initialMessage?: string;
    sending?: boolean;
    error?: string | null;
  }>(),
  { context: null, recipientNote: 'Sent to the mobile number on their profile.', initialMessage: '', sending: false, error: null },
);

const emit = defineEmits<{
  close: [];
  send: [message: string, idempotencyKey: string];
}>();

const message = ref('');
const key = ref('');
const touched = ref(false);

watch(
  () => props.open,
  (open) => {
    if (!open) return;
    message.value = props.initialMessage;
    key.value = newIdempotencyKey();
    touched.value = false;
  },
  { immediate: true },
);

watch(message, (next, previous) => {
  if (props.open && next.trim() !== previous.trim()) {
    key.value = newIdempotencyKey();
  }
});

const messageError = computed(() => {
  const text = message.value.trim();
  if (!text) return 'Enter the message to send.';
  if (text.length > SMS_MAX_LENGTH) return `The message must be at most ${SMS_MAX_LENGTH} characters.`;
  return null;
});

function send() {
  touched.value = true;
  if (messageError.value || props.sending) return;
  emit('send', message.value.trim(), key.value);
}
</script>

<template>
  <v-dialog :model-value="open" max-width="480" :persistent="sending" @update:model-value="(v: boolean) => !v && emit('close')">
    <v-card class="pa-2">
      <v-card-title class="text-subtitle-1 font-weight-bold">Send SMS</v-card-title>
      <v-card-text class="d-flex flex-column ga-3">
        <div class="text-body-2">
          To <span class="font-weight-medium">{{ recipientName }}</span>
          <span v-if="context" class="text-medium-emphasis"> · {{ context }}</span>
          <div class="text-medium-emphasis">{{ recipientNote }}</div>
        </div>
        <v-textarea
          v-model="message"
          label="Message"
          rows="3"
          auto-grow
          :counter="SMS_MAX_LENGTH"
          :maxlength="SMS_MAX_LENGTH"
          :disabled="sending"
          :error-messages="touched && messageError ? messageError : undefined"
        />
        <v-alert v-if="error" type="error" density="compact" variant="tonal">{{ error }}</v-alert>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="sending" @click="emit('close')">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-message-text-outline" :loading="sending" @click="send">Send</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
