<script setup lang="ts">
import { computed, ref, watch } from 'vue';

/** Sets a new temporary password for an account. The parent owns the API call. */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    fullName: string;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
  }>(),
  { saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  submit: [newPassword: string];
}>();

const password = ref('');
const confirm = ref('');
const show = ref(false);

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      password.value = '';
      confirm.value = '';
      show.value = false;
    }
  },
);

const mismatch = computed(() => confirm.value !== '' && password.value !== confirm.value);
const canSubmit = computed(() => password.value.length >= 8 && password.value === confirm.value && !props.saving);

function submit() {
  if (!canSubmit.value) return;
  emit('submit', password.value);
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="460" :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="text-subtitle-1 font-weight-bold">Reset password</v-card-title>
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis">
          Set a temporary password for <strong>{{ fullName }}</strong>. Share it with them directly; it is not emailed.
        </p>
        <v-form :disabled="saving" @submit.prevent="submit">
          <v-text-field
            v-model="password"
            label="New password"
            :type="show ? 'text' : 'password'"
            autocomplete="new-password"
            autofocus
            hint="At least 8 characters"
            persistent-hint
            :append-inner-icon="show ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :error-messages="errors?.newPassword ?? errors?.NewPassword"
            @click:append-inner="show = !show"
          />
          <v-text-field
            v-model="confirm"
            label="Confirm password"
            :type="show ? 'text' : 'password'"
            autocomplete="new-password"
            class="mt-2"
            :error-messages="mismatch ? ['Passwords do not match.'] : undefined"
          />
        </v-form>
      </v-card-text>
      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSubmit" @click="submit">Set password</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
