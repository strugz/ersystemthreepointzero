<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { reportingApi } from '@/features/reporting/api';
import type { ReportEmailDelivery } from '@/features/reporting/types';
import { newIdempotencyKey } from '@/features/notifications/api';
import { ApiError } from '@/shared/api/client';
import { useSnackbar, describeError } from '@/shared/composables/useSnackbar';
import { formatDateTime } from '@/shared/format';

const route = useRoute();
const { xs } = useDisplay();
const reportId = String(route.params.id);

const pdfUrl = ref<string | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const frame = ref<HTMLIFrameElement | null>(null);
const snack = useSnackbar();

const emails = ref<ReportEmailDelivery[]>([]);
const emailing = ref(false);
// Kept after a failed send so a retry cannot email twice; cleared once the send is settled.
let emailKey: string | null = null;
const KEEP_KEY_CODES = new Set(['email_gateway_failed', 'email_gateway_unconfirmed', 'email_in_progress']);

const lastEmail = computed(() => emails.value[0] ?? null);
const lastEmailText = computed(() => {
  const e = lastEmail.value;
  if (!e) return null;
  const what = e.kind === 'R47' ? `Emailed by ${e.sentByName}` : 'Emailed automatically';
  const status = e.status === 'Sent' ? `to ${e.recipientCount} recipient${e.recipientCount === 1 ? '' : 's'}` : `(${e.status.toLowerCase()})`;
  return `${what} ${status}, ${formatDateTime(e.completedAt ?? e.createdAt)}`;
});

async function loadEmails() {
  try {
    emails.value = await reportingApi.emailHistory(reportId);
  } catch {
    // The sent status is a hint; the PDF stays usable without it.
    emails.value = [];
  }
}

async function emailPdf() {
  emailing.value = true;
  emailKey ??= newIdempotencyKey();
  try {
    const sent = await reportingApi.emailPdf(reportId, emailKey);
    emailKey = null;
    if (sent.replayed) {
      snack.info('This R47 was already emailed.');
    } else {
      snack.success(`R47 emailed to ${sent.recipientCount} recipient${sent.recipientCount === 1 ? '' : 's'}.`);
    }
  } catch (err) {
    // Keep the key when the email may already be on its way (network error, still sending, unconfirmed)
    // or the server may retry it (refused by the mail server); any other refusal sent nothing.
    if (err instanceof ApiError && !KEEP_KEY_CODES.has(err.code ?? '')) {
      emailKey = null;
    }
    snack.error(describeError(err, 'Unable to email the R47.'));
  } finally {
    emailing.value = false;
    await loadEmails();
  }
}

onMounted(async () => {
  try {
    const blob = await reportingApi.fetchPdf(reportId);
    pdfUrl.value = URL.createObjectURL(blob);
    void loadEmails();
  } catch (err) {
    if (err instanceof ApiError) {
      error.value =
        err.status === 403
          ? 'You are not allowed to view this report.'
          : err.status === 404
            ? 'Report not found.'
            : err.message;
    } else {
      error.value = 'Unable to load the report.';
    }
  } finally {
    loading.value = false;
  }
});

onBeforeUnmount(() => {
  if (pdfUrl.value) {
    URL.revokeObjectURL(pdfUrl.value);
  }
});

function print() {
  frame.value?.contentWindow?.print();
}
</script>

<template>
  <AuthenticatedLayout
    :crumbs="[{ title: 'My reports', to: { name: 'reports' } }, { title: 'Expense report', to: { name: 'report-edit', params: { id: reportId } } }, { title: 'R47 PDF' }]"
    :back="{ name: 'report-edit', params: { id: reportId } }"
  >
    <template #actions>
      <v-btn class="press" variant="text" prepend-icon="mdi-printer-outline" :disabled="!pdfUrl" @click="print">Print</v-btn>
      <v-btn class="press" variant="text" prepend-icon="mdi-email-outline" :disabled="!pdfUrl" :loading="emailing" @click="emailPdf">Email</v-btn>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-download-outline" :href="pdfUrl ?? undefined" :download="`ExpenseReport-${reportId}.pdf`" :disabled="!pdfUrl">Download</v-btn>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-open-in-new" :href="reportingApi.pdfUrl(reportId)" target="_blank" rel="noopener" :disabled="!pdfUrl">Open in new tab</v-btn>
    </template>
    <template #bottom>
      <v-btn class="press" variant="text" prepend-icon="mdi-email-outline" :disabled="!pdfUrl" :loading="emailing" @click="emailPdf">Email</v-btn>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-download-outline" :href="pdfUrl ?? undefined" :download="`ExpenseReport-${reportId}.pdf`" :disabled="!pdfUrl">Download</v-btn>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-open-in-new" :href="reportingApi.pdfUrl(reportId)" target="_blank" rel="noopener" :disabled="!pdfUrl">Open</v-btn>
    </template>

    <div v-if="!xs" class="mb-4">
      <h1 class="page-title">Expense report (R47)</h1>
      <p class="text-body-2 text-medium-emphasis mb-0">The controlled form exactly as it prints. Landscape, US Letter.</p>
    </div>
    <p v-if="lastEmailText" class="text-body-2 text-medium-emphasis mb-3" data-testid="email-status">
      <v-icon icon="mdi-email-check-outline" size="small" class="mr-1" />{{ lastEmailText }}
    </p>

    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />
    <v-alert v-else-if="error" type="error">{{ error }}</v-alert>
    <v-card v-else class="pdf-card">
      <iframe ref="frame" :src="pdfUrl ?? undefined" title="Expense Report PDF" class="pdf-frame" />
    </v-card>
  </AuthenticatedLayout>
</template>

<style scoped>
.pdf-card {
  overflow: hidden;
}
.pdf-frame {
  width: 100%;
  height: 78vh;
  border: 0;
  display: block;
  background: #525659;
}
@media (max-width: 599.98px) {
  .pdf-frame {
    height: 70vh;
  }
}
</style>
