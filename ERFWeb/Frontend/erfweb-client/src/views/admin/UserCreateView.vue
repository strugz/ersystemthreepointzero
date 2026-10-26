<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import UserForm from '@/features/admin/components/UserForm.vue';
import type { CreateUserInput, Department } from '@/features/admin/types';

const router = useRouter();
const snack = useSnackbar();

const departments = ref<Department[]>([]);
/** Undefined until the settings load; the form then falls back to its default domain. */
const emailDomains = ref<string[] | undefined>(undefined);
const lookupsLoading = ref(true);
const saving = ref(false);
const errors = ref<Record<string, string[]> | null>(null);

onMounted(async () => {
  const [d, s] = await Promise.allSettled([adminApi.departments(false), adminApi.settings()]);
  if (d.status === 'fulfilled') departments.value = d.value;
  // An API older than the domain list sends none; keep the form's default.
  if (s.status === 'fulfilled' && s.value.emailDomains?.length) emailDomains.value = s.value.emailDomains;
  if (d.status === 'rejected') {
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
        :email-domains="emailDomains"
        :saving="saving"
        :errors="errors"
        @submit="create"
        @cancel="router.push({ name: 'admin-users' })"
      />
    </v-card>
  </AuthenticatedLayout>
</template>
