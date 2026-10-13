<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import type { PagedResult } from '@/features/expenses/types';
import { verificationApi } from '@/features/verification/api';
import VerificationList from '@/features/verification/components/VerificationList.vue';
import type { VerificationQueueItem, VerificationQueueView } from '@/features/verification/types';

// The Expense Verifier's queue (plan Flow 6): the open pool, requests I picked up, and my findings.
const router = useRouter();
const route = useRoute();
const snack = useSnackbar();
const { xs } = useDisplay();

const views: VerificationQueueView[] = ['Open', 'Mine', 'Done'];
const view = ref<VerificationQueueView>(views.includes(route.query.view as VerificationQueueView) ? (route.query.view as VerificationQueueView) : 'Open');
const page = ref(1);
const pageSize = 20;
const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<VerificationQueueItem> | null>(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    result.value = await verificationApi.queue(view.value, page.value, pageSize);
  } catch (err) {
    error.value = 'Unable to load the verification queue.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(page, load);
watch(view, () => {
  page.value = 1;
  void router.replace({ query: view.value === 'Open' ? {} : { view: view.value } });
  void load();
});

function open(item: VerificationQueueItem) {
  router.push({ name: 'report-review', params: { id: item.reportId }, query: { from: 'verifications' } });
}

const empty: Record<VerificationQueueView, { title: string; hint: string }> = {
  Open: { title: 'Nothing waiting', hint: 'Requests from Finance appear here. Requests on your own reports, or on ones you approved, are left out.' },
  Mine: { title: 'Nothing in progress', hint: 'Pick up a request from the open pool to start checking the facts.' },
  Done: { title: 'No findings yet', hint: 'Requests you record a finding on are listed here.' },
};
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Verifications' }]">
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Verifications</h1>
      <span class="text-body-2 text-medium-emphasis">Check the facts of a line Finance doubts, then report what you found. You cannot change amounts or approve.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-btn-toggle v-model="view" mandatory density="comfortable" variant="outlined" divided color="primary">
          <v-btn value="Open" class="press">Open</v-btn>
          <v-btn value="Mine" class="press">In progress</v-btn>
          <v-btn value="Done" class="press">My findings</v-btn>
        </v-btn-toggle>
        <v-spacer />
        <span v-if="result" class="card-meta">{{ result.total }} request{{ result.total === 1 ? '' : 's' }}</span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="result && result.items.length === 0 && !loading"
        icon="mdi-magnify-scan"
        :title="empty[view].title"
        :hint="empty[view].hint"
      />

      <VerificationList v-else-if="result" :items="result.items" @open="open" />

      <div v-if="result && result.totalPages > 1" class="d-flex justify-center py-2">
        <v-pagination v-model="page" :length="result.totalPages" density="comfortable" :total-visible="xs ? 5 : 7" />
      </div>
    </v-card>
  </AuthenticatedLayout>
</template>

<style scoped>
.filters {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  padding: 12px;
}
</style>
