<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import UserForm from '@/features/admin/components/UserForm.vue';
import type { CostCenter, CreateUserInput, Department } from '@/features/admin/types';

const router = useRouter();
const snack = useSnackbar();

const departments = ref<Department[]>([]);
const costCenters = ref<CostCenter[]>([]);
const lookupsLoading = ref(true);
const saving = ref(false);
const errors = ref<Record<string, string[]> | null>(null);

onMounted(async () => {
  const [d, c] = await Promise.allSettled([adminApi.departments(false), adminApi.costCenters(false)]);
  if (d.status === 'fulfilled') departments.value = d.value;
  if (c.status === 'fulfilled') costCenters.value = c.value;
  if (d.status === 'rejected' || c.status === 'rejected') {
    snack.error('Some reference lists could not be loaded; you can still create the account.');
  }
  lookupsLoading.value = false;
});

async function create(input: CreateUserInput) {
  saving.value = true;
  errors.value = null;
  try {
    const created = await adminApi.createUser(input);
    snack.success(`Account ${created.username} created.`);
    await router.replace({ name: 'admin-user', params: { id: created.id } });
  } catch (err) {
    if (err instanceof ApiError && err.problem?.errors) {
      errors.value = err.problem.errors;
    }
    snack.fromError(err, 'Unable to create the account.');
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Users', to: { name: 'admin-users' } }, { title: 'New user' }]" narrow>
    <div class="mb-5">
      <h1 class="page-title">New user</h1>
      <p class="text-body-2 text-medium-emphasis mb-0">
        The account starts as an Employee. Grant privileged roles from the account page after it is created.
      </p>
    </div>

    <v-card class="pa-5">
      <v-progress-linear v-if="lookupsLoading" indeterminate color="primary" height="2" class="mb-3" />
      <UserForm
        mode="create"
        :departments="departments"
        :cost-centers="costCenters"
        :saving="saving"
        :errors="errors"
        @submit="create"
        @cancel="router.push({ name: 'admin-users' })"
      />
    </v-card>
  </AuthenticatedLayout>
</template>
