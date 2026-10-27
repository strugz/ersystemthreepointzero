<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { formatDateTime } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import SettingsForm from '@/features/admin/components/SettingsForm.vue';
import ReminderStatusCard from '@/features/admin/components/ReminderStatusCard.vue';
import type { ReminderStatus, SystemSettings, SystemSettingsInput } from '@/features/admin/types';

const snack = useSnackbar();
const { xs } = useDisplay();

const settings = ref<SystemSettings | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const saving = ref(false);
const errors = ref<Record<string, string[]> | null>(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    settings.value = await adminApi.settings();
  } catch (err) {
    error.value = 'Unable to load the workflow rules.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}
onMounted(load);

// Loaded on its own so the rules stay editable when the reminder status cannot be read.
const reminders = ref<ReminderStatus | null>(null);
const remindersLoading = ref(false);
const remindersError = ref<string | null>(null);

async function loadReminders() {
  remindersLoading.value = true;
  remindersError.value = null;
  try {
    reminders.value = await adminApi.reminderStatus();
  } catch {
    remindersError.value = 'Unable to load the SLA reminder status.';
  } finally {
    remindersLoading.value = false;
  }
}
onMounted(loadReminders);

async function save(input: SystemSettingsInput) {
  saving.value = true;
  errors.value = null;
  try {
    settings.value = await adminApi.updateSettings(input);
    snack.success('Rules saved.');
  } catch (err) {
    snack.fromError(err, 'Unable to save the rules.');
    if (err instanceof ApiError) {
      // The settings record has no row version; a 409 still means "someone else saved first".
      if (err.status === 409) await load();
      if (err.problem?.errors) errors.value = err.problem.errors;
    }
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Rules' }]" narrow>
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Workflow rules</h1>
      <span class="text-body-2 text-medium-emphasis">Approval thresholds, service levels, filing limits, the R47 reviewer and notifications. Changes apply to reports filed from now on.</span>
    </div>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-else-if="error" type="error">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
    </v-alert>

    <template v-else-if="settings">
      <p v-if="settings.updatedAt" class="text-caption text-medium-emphasis mb-4">
        Last updated {{ formatDateTime(settings.updatedAt) }}<template v-if="settings.updatedByName"> by {{ settings.updatedByName }}</template>
      </p>
      <SettingsForm :initial="settings" :saving="saving" :errors="errors" @submit="save" />
    </template>

    <ReminderStatusCard class="mt-4" :status="reminders" :loading="remindersLoading" :error="remindersError" @refresh="loadReminders" />
  </AuthenticatedLayout>
</template>
