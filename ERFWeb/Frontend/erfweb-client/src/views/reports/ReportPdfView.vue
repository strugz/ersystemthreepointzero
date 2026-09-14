<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { reportingApi } from '@/features/reporting/api';
import { ApiError } from '@/shared/api/client';

const route = useRoute();
const { xs } = useDisplay();
const reportId = String(route.params.id);

const pdfUrl = ref<string | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const frame = ref<HTMLIFrameElement | null>(null);

onMounted(async () => {
  try {
    const blob = await reportingApi.fetchPdf(reportId);
    pdfUrl.value = URL.createObjectURL(blob);
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
      <v-btn class="press" variant="outlined" prepend-icon="mdi-download-outline" :href="pdfUrl ?? undefined" :download="`ExpenseReport-${reportId}.pdf`" :disabled="!pdfUrl">Download</v-btn>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-open-in-new" :href="reportingApi.pdfUrl(reportId)" target="_blank" rel="noopener" :disabled="!pdfUrl">Open in new tab</v-btn>
    </template>
    <template #bottom>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-download-outline" :href="pdfUrl ?? undefined" :download="`ExpenseReport-${reportId}.pdf`" :disabled="!pdfUrl">Download</v-btn>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-open-in-new" :href="reportingApi.pdfUrl(reportId)" target="_blank" rel="noopener" :disabled="!pdfUrl">Open</v-btn>
    </template>

    <div v-if="!xs" class="mb-4">
      <h1 class="page-title">Expense report (R47)</h1>
      <p class="text-body-2 text-medium-emphasis mb-0">The controlled form exactly as it prints. Landscape, US Letter.</p>
    </div>

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
