<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { adminApi } from '@/features/admin/api';
import AuditTable from '@/features/admin/components/AuditTable.vue';
import type { AuditFilters } from '@/features/admin/components/AuditTable.vue';
import type { AuditEntry, PagedResult } from '@/features/admin/types';

const route = useRoute();
const session = useSessionStore();
const snack = useSnackbar();
const { xs } = useDisplay();

const isAdmin = session.hasRole('SystemAdministrator');
const q = (key: string) => (typeof route.query[key] === 'string' ? (route.query[key] as string) : null);

const page = ref(1);
const pageSize = 25;
// Deep links (e.g. from a user page) may seed the event / actor filters.
const filters = ref<AuditFilters>({
  event: q('event'),
  from: q('from'),
  to: q('to'),
  direction: 'Desc',
  administrationOnly: isAdmin,
});
const actorUserId = q('actorUserId') ? Number(q('actorUserId')) : undefined;

const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<AuditEntry> | null>(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    const f = filters.value;
    result.value = await adminApi.audit({
      page: page.value,
      pageSize,
      direction: f.direction,
      event: f.event ?? undefined,
      actorUserId: Number.isFinite(actorUserId) ? actorUserId : undefined,
      from: f.from ?? undefined,
      to: f.to ?? undefined,
      administrationOnly: f.administrationOnly || undefined,
    });
  } catch (err) {
    error.value = 'Unable to load the audit log.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(page, load);
watch(
  filters,
  () => {
    page.value = 1;
    void load();
  },
  { deep: true },
);
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Audit' }]">
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Audit log</h1>
      <span class="text-body-2 text-medium-emphasis">Who did what and when. Entries are append-only.</span>
    </div>

    <AuditTable
      v-model:filters="filters"
      v-model:page="page"
      :items="result?.items ?? []"
      :total="result?.total ?? 0"
      :total-pages="result?.totalPages ?? 1"
      :loading="loading"
      :error="error"
      :show-admin-only-switch="isAdmin"
      @retry="load"
    />
  </AuthenticatedLayout>
</template>
