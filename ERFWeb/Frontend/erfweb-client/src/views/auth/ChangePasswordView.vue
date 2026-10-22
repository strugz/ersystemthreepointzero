<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { useSessionStore } from '@/app/stores/session';
import { MIN_PASSWORD_LENGTH } from '@/features/auth/types';
import { ApiError } from '@/shared/api/client';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';

const session = useSessionStore();
const router = useRouter();
const route = useRoute();
const snack = useSnackbar();

// Arriving here with an administrator-set password: the router sends the user back here until it changes.
const forced = computed(() => session.user?.mustChangePassword ?? false);

const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
// The confirm field follows the new-password toggle, as in the administrator's reset dialog.
const showCurrent = ref(false);
const showNew = ref(false);
const saving = ref(false);
const error = ref<string | null>(null);
const fieldErrors = ref<Record<string, string>>({});

const rules = {
  required: (v: string) => !!v || 'Required.',
  length: (v: string) => v.length >= MIN_PASSWORD_LENGTH || `At least ${MIN_PASSWORD_LENGTH} characters.`,
  matches: (v: string) => v === newPassword.value || 'Does not match the new password.',
};

const canSave = computed(
  () =>
    !!currentPassword.value &&
    newPassword.value.length >= MIN_PASSWORD_LENGTH &&
    confirmPassword.value === newPassword.value &&
    !saving.value,
);

async function save() {
  if (!canSave.value) return;
  saving.value = true;
  error.value = null;
  fieldErrors.value = {};
  try {
    await session.changePassword({ currentPassword: currentPassword.value, newPassword: newPassword.value });
    snack.success('Your password has been changed.');
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
    await router.push(redirect);
  } catch (err) {
    // The API names the field: a wrong current password, or a new one it does not accept.
    const errors = err instanceof ApiError ? err.problem?.errors : undefined;
    if (errors) {
      fieldErrors.value = Object.fromEntries(Object.entries(errors).map(([field, messages]) => [field, messages[0] ?? '']));
    } else {
      error.value = describeError(err, 'Unable to change the password.');
    }
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Change password' }]" narrow>
    <h1 class="page-title mb-1">Change password</h1>
    <p class="text-body-2 text-medium-emphasis mb-5">
      <template v-if="forced">Your password was set by an administrator. Choose your own before you continue.</template>
      <template v-else>Use at least {{ MIN_PASSWORD_LENGTH }} characters, different from your username.</template>
    </p>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-card class="pa-5">
      <v-form @submit.prevent="save">
        <v-text-field
          v-model="currentPassword"
          label="Current password"
          :type="showCurrent ? 'text' : 'password'"
          autocomplete="current-password"
          prepend-inner-icon="mdi-lock-outline"
          :append-inner-icon="showCurrent ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
          @click:append-inner="showCurrent = !showCurrent"
          :rules="[rules.required]"
          :error-messages="fieldErrors.currentPassword"
          :disabled="saving"
          class="mb-2"
          autofocus
          @update:model-value="fieldErrors.currentPassword = ''"
        />
        <v-text-field
          v-model="newPassword"
          label="New password"
          :type="showNew ? 'text' : 'password'"
          autocomplete="new-password"
          prepend-inner-icon="mdi-lock-plus-outline"
          :append-inner-icon="showNew ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
          @click:append-inner="showNew = !showNew"
          :rules="[rules.required, rules.length]"
          :error-messages="fieldErrors.newPassword"
          :disabled="saving"
          class="mb-2"
          @update:model-value="fieldErrors.newPassword = ''"
        />
        <v-text-field
          v-model="confirmPassword"
          label="Confirm new password"
          :type="showNew ? 'text' : 'password'"
          autocomplete="new-password"
          prepend-inner-icon="mdi-lock-check-outline"
          :rules="[rules.required, rules.matches]"
          :disabled="saving"
          class="mb-4"
        />
        <div class="d-flex ga-2">
          <v-spacer />
          <v-btn v-if="!forced" class="press" variant="text" :disabled="saving" @click="router.back()">Cancel</v-btn>
          <v-btn type="submit" class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave">Change password</v-btn>
        </div>
      </v-form>
    </v-card>
  </AuthenticatedLayout>
</template>
