<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { expensesApi } from '@/features/expenses/api';
import ReportHeaderForm from '@/features/expenses/components/ReportHeaderForm.vue';
import type { ReportHeaderInput } from '@/features/expenses/types';

const router = useRouter();
const snack = useSnackbar();
const saving = ref(false);

async function create(input: ReportHeaderInput) {
  saving.value = true;
  try {
    const created = await expensesApi.create(input);
    snack.success('Report created. Add your expense lines next.');
    await router.replace({ name: 'report-edit', params: { id: created.id } });
  } catch (err) {
    snack.fromError(err, 'Unable to create the report.');
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'My reports', to: { name: 'reports' } }, { title: 'New report' }]" narrow>
    <div class="mb-5">
      <h1 class="page-title">New expense report</h1>
      <p class="text-body-2 text-medium-emphasis mb-0">Start with the header. Expense lines and receipts come after the report is created.</p>
    </div>

    <v-card>
      <div class="card-h"><span class="card-t">Report details</span><span class="card-meta">Printed on the R47 header</span></div>
      <div class="pa-5 pt-4">
        <ReportHeaderForm :saving="saving" submit-text="Create report" :dirty-hint="false" @submit="create" />
      </div>
    </v-card>
  </AuthenticatedLayout>
</template>
