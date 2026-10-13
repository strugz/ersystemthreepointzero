<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { ratesApi } from '@/features/entitlements/api';
import { hasAnyRole, rateWriterRoles } from '@/features/entitlements/access';
import { fieldErrors } from '@/features/entitlements/forms';
import type { RateProfileListItem } from '@/features/entitlements/types';

const router = useRouter();
const session = useSessionStore();
const snack = useSnackbar();
const { xs } = useDisplay();

const canWrite = computed(() => hasAnyRole(session.user?.roles, rateWriterRoles));

const profiles = ref<RateProfileListItem[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const showInactive = ref(false);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    profiles.value = await ratesApi.profiles(true);
  } catch (err) {
    error.value = 'Unable to load rate profiles.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}
onMounted(load);

const inactiveCount = computed(() => profiles.value.filter((p) => !p.isActive).length);
const visible = computed(() =>
  [...profiles.value].filter((p) => showInactive.value || p.isActive).sort((a, b) => a.name.localeCompare(b.name)),
);

function open(p: RateProfileListItem) {
  router.push({ name: 'rate-profile', params: { id: p.id } });
}

// ---- create ------------------------------------------------------------------
const createOpen = ref(false);
const name = ref('');
const saving = ref(false);
const errors = ref<Record<string, string[]> | null>(null);

function openCreate() {
  name.value = '';
  errors.value = null;
  createOpen.value = true;
}

async function create() {
  const n = name.value.trim();
  if (!n) return;
  saving.value = true;
  errors.value = null;
  try {
    const created = await ratesApi.createProfile(n);
    createOpen.value = false;
    snack.success(`${created.name} created. Add its rate lines next.`);
    await router.push({ name: 'rate-profile', params: { id: created.id } });
  } catch (err) {
    snack.fromError(err, 'Unable to create the profile.');
    errors.value = err instanceof ApiError && err.problem?.errors ? err.problem.errors : null;
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Rates' }]">
    <template v-if="canWrite" #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="openCreate">New profile</v-btn>
    </template>
    <template v-if="canWrite" #actions-mobile>
      <v-btn class="press" icon="mdi-plus" variant="text" aria-label="New profile" @click="openCreate" />
    </template>

    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Rate profiles</h1>
      <span class="text-body-2 text-medium-emphasis">Meal, transport and other rates, assigned by person, job grade, department or company.</span>
    </div>

    <v-card>
      <div class="card-h">
        <div class="d-flex align-baseline ga-3 flex-wrap">
          <span class="card-t">Profiles</span>
          <span class="card-meta">{{ visible.length }} of {{ profiles.length }}</span>
        </div>
        <v-switch
          v-if="inactiveCount > 0"
          v-model="showInactive"
          density="compact"
          hide-details
          color="primary"
          :label="xs ? 'Inactive' : `Show inactive (${inactiveCount})`"
        />
      </div>

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="!loading && visible.length === 0"
        icon="mdi-cash-multiple"
        :title="profiles.length === 0 ? 'No rate profiles yet' : 'All profiles are inactive'"
        :hint="
          profiles.length === 0
            ? canWrite
              ? 'Create a profile, add its rates, then assign it.'
              : 'Nobody has set up rate profiles yet.'
            : 'Turn on “Show inactive” to see them.'
        "
      >
        <v-btn v-if="profiles.length === 0 && canWrite" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="openCreate">New profile</v-btn>
      </AppEmptyState>

      <div v-else-if="!loading" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th>Name</th>
              <th class="r">Lines in force</th>
              <th class="r">Assignments</th>
              <th>Status</th>
              <th class="r"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(p, i) in visible"
              :key="p.id"
              class="row-link stagger-in"
              :class="{ 'text-medium-emphasis': !p.isActive }"
              :style="{ '--i': Math.min(i, 10) }"
              tabindex="0"
              @click="open(p)"
              @keydown.enter="open(p)"
            >
              <td class="font-weight-medium">{{ p.name }}</td>
              <td class="r mono">{{ p.currentLineCount }}</td>
              <td class="r mono">{{ p.activeAssignmentCount }}</td>
              <td><span class="pill" :class="p.isActive ? 'pill--success' : 'pill--neutral'">{{ p.isActive ? 'Active' : 'Inactive' }}</span></td>
              <td class="r"><v-icon icon="mdi-chevron-right" size="18" class="text-medium-emphasis" /></td>
            </tr>
          </tbody>
        </table>
      </div>
    </v-card>

    <v-dialog v-model="createOpen" max-width="460" :persistent="saving">
      <v-card>
        <v-card-title class="text-subtitle-1 font-weight-bold">New rate profile</v-card-title>
        <v-card-text>
          <v-form :disabled="saving" @submit.prevent="create">
            <v-text-field v-model="name" label="Name *" maxlength="100" autofocus :error-messages="fieldErrors(errors, 'name')" />
          </v-form>
        </v-card-text>
        <v-divider />
        <v-card-actions class="px-4 py-3">
          <v-spacer />
          <v-btn class="press" variant="text" :disabled="saving" @click="createOpen = false">Cancel</v-btn>
          <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!name.trim()" @click="create">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </AuthenticatedLayout>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.row-link {
  cursor: pointer;
}
.row-link:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
</style>
