<script setup lang="ts">
import { computed } from 'vue';
import { formatDateTime } from '@/shared/format';
import type { ReminderStatus } from '../types';

/**
 * The daily SLA reminder job: whether the server runs it, when it runs next, and what its last run did.
 * On/off and the run time are server settings (appsettings.json), not editable here.
 */
const props = withDefaults(
  defineProps<{
    status: ReminderStatus | null;
    loading?: boolean;
    error?: string | null;
  }>(),
  { loading: false, error: null },
);

const emit = defineEmits<{ refresh: [] }>();

const last = computed(() => props.status?.lastRun ?? null);

const lastResult = computed(() => {
  const run = last.value;
  if (!run) return '';
  if (!run.succeeded) return 'Stopped on an error. The server log has the details; the next run goes ahead as scheduled.';
  const parts = [`${run.stepsWaiting ?? 0} approval(s) waiting`, `${run.sent ?? 0} sent`, `${run.failed ?? 0} failed`];
  if (run.skipped) parts.push(`${run.skipped} skipped (no email or mobile on file)`);
  return parts.join(' · ');
});

const lastTone = computed(() => {
  const run = last.value;
  if (!run) return 'grey';
  return !run.succeeded || (run.failed ?? 0) > 0 ? 'error' : 'success';
});
</script>

<template>
  <v-card class="mb-4">
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">SLA reminders</span>
        <v-chip v-if="status" size="small" label :color="status.enabled ? 'success' : 'grey'">
          {{ status.enabled ? 'On' : 'Off' }}
        </v-chip>
      </div>
      <v-btn class="press" size="small" variant="text" prepend-icon="mdi-refresh" :loading="loading" @click="emit('refresh')">
        Refresh
      </v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" density="compact" class="ma-4">{{ error }}</v-alert>

    <div v-else-if="status" class="pa-4 text-body-2">
      <p v-if="!status.enabled" class="text-medium-emphasis mb-3">
        The server does not send reminders. Set <code>Reminders:Enabled</code> to <code>true</code> in the server's
        appsettings.json and restart the ERFWeb.Api service.
      </p>
      <dl class="reminder-facts">
        <dt>Runs</dt>
        <dd>Working days at {{ status.runAt }}</dd>
        <dt>Next run</dt>
        <dd>{{ status.nextRunAt ? formatDateTime(status.nextRunAt) : '—' }}</dd>
        <dt>Channels</dt>
        <dd>
          Email {{ status.emailConfigured ? 'set up' : 'not set up' }} · SMS {{ status.smsConfigured ? 'set up' : 'not set up' }}
        </dd>
        <dt>Last run</dt>
        <dd v-if="last">
          <v-icon size="small" :color="lastTone" class="mr-1">
            {{ lastTone === 'error' ? 'mdi-alert-circle' : 'mdi-check-circle' }}
          </v-icon>
          {{ formatDateTime(last.at) }}
          <div class="text-medium-emphasis">{{ lastResult }}</div>
        </dd>
        <dd v-else class="text-medium-emphasis">No run recorded yet.</dd>
      </dl>
    </div>
  </v-card>
</template>

<style scoped>
.reminder-facts {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 6px 16px;
  margin: 0;
}
.reminder-facts dt {
  color: rgb(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}
.reminder-facts dd {
  margin: 0;
}
</style>
