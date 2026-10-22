<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDate } from '@/shared/format';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { delegationsApi } from '@/features/delegations/api';
import { delegationStateColor, delegationStateLabel } from '@/features/delegations/labels';
import DelegationDialog from '@/features/delegations/components/DelegationDialog.vue';
import type { CreateDelegationInput, Delegation, DelegationOptions, MyDelegations } from '@/features/delegations/types';

const snack = useSnackbar();
const { xs } = useDisplay();

const mine = ref<MyDelegations | null>(null);
const options = ref<DelegationOptions | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    [mine.value, options.value] = await Promise.all([delegationsApi.mine(), delegationsApi.options()]);
  } catch (err) {
    error.value = 'Unable to load delegations.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const sections = computed(() => {
  const m = mine.value;
  if (!m) return [];
  return [
    { key: 'received', title: 'Standing in for', hint: 'Signatories whose approvals you handle during these dates.', items: m.received, show: m.received.length > 0 },
    { key: 'given', title: 'My delegations', hint: 'Who approves in your place while you are away.', items: m.given, show: true },
    { key: 'assigned', title: 'Assigned for Level 1s', hint: 'Delegates you named for a Level 1 who left none.', items: m.assigned, show: m.assigned.length > 0 },
  ].filter((s) => s.show);
});

// ---- create ---------------------------------------------------------------------
const dialog = ref(false);
const saving = ref(false);
const saveError = ref<string | null>(null);

function openCreate() {
  saveError.value = null;
  dialog.value = true;
}

async function create(input: CreateDelegationInput) {
  saving.value = true;
  saveError.value = null;
  try {
    const d = await delegationsApi.create(input);
    dialog.value = false;
    snack.success(`${d.delegateName} will approve for ${d.signatoryName} from ${formatDate(d.startsOn)} to ${formatDate(d.endsOn)}.`);
    await load();
  } catch (err) {
    const fields = err instanceof ApiError ? Object.values(err.problem?.errors ?? {}).flat() : [];
    saveError.value = fields.length ? fields.join(' ') : describeError(err, 'Unable to save the delegation.');
  } finally {
    saving.value = false;
  }
}

// ---- cancel ---------------------------------------------------------------------
const cancelling = ref<Delegation | null>(null);
const cancelBusy = ref(false);
const cancelOpen = computed({
  get: () => cancelling.value !== null,
  set: (v: boolean) => {
    if (!v) cancelling.value = null;
  },
});

async function confirmCancel() {
  const d = cancelling.value;
  if (!d) return;
  cancelBusy.value = true;
  try {
    await delegationsApi.cancel(d.id, d.rowVersion);
    snack.success('Delegation cancelled.');
    cancelling.value = null;
    await load();
  } catch (err) {
    snack.fromError(err, 'Unable to cancel the delegation.');
    if (err instanceof ApiError && err.status === 409) {
      cancelling.value = null;
      await load();
    }
  } finally {
    cancelBusy.value = false;
  }
}

function who(d: Delegation, section: string): string {
  if (section === 'received') return d.signatoryName;
  if (section === 'given') return d.delegateName;
  return `${d.delegateName} for ${d.signatoryName}`;
}

function byLine(d: Delegation, section: string): string | null {
  if (d.state === 'Cancelled' && d.cancelledByName) return `Cancelled by ${d.cancelledByName}`;
  if (d.isForced && section !== 'assigned') return `Assigned by ${d.createdByName}`;
  return null;
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Delegations' }]">
    <div class="d-flex align-center ga-3 flex-wrap mb-5">
      <h1 v-if="!xs" class="page-title">Delegations</h1>
      <span v-if="!xs" class="text-body-2 text-medium-emphasis">Name a delegate before you leave. Only approving is delegated.</span>
      <v-spacer />
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :disabled="!options" @click="openCreate">New delegation</v-btn>
    </div>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" class="mb-4" />

    <v-alert v-if="error && !loading" type="error" class="mb-4">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
    </v-alert>

    <div v-else class="sections">
      <v-card v-for="s in sections" :key="s.key">
        <div class="section-head">
          <span class="card-t">{{ s.title }}</span>
          <span class="text-caption text-medium-emphasis">{{ s.hint }}</span>
        </div>
        <v-divider />

        <AppEmptyState
          v-if="s.items.length === 0 && !loading"
          icon="mdi-account-arrow-right-outline"
          title="No delegations yet"
          hint="Going on leave? Name a signatory of your level or higher to approve in your place."
        />

        <ul v-else class="list">
          <li v-for="d in s.items" :key="d.id" class="item" :class="{ 'item--muted': d.state === 'Ended' || d.state === 'Cancelled' }">
            <div class="item__main">
              <div class="item__top">
                <span class="item__who">{{ who(d, s.key) }}</span>
                <v-chip size="x-small" label variant="tonal" :color="delegationStateColor[d.state]">{{ delegationStateLabel[d.state] }}</v-chip>
                <v-chip v-if="d.isForced" size="x-small" label variant="outlined">Assigned by Level 2</v-chip>
              </div>
              <div class="item__meta mono">{{ formatDate(d.startsOn) }} – {{ formatDate(d.endsOn) }}</div>
              <div v-if="d.reason" class="item__reason">{{ d.reason }}</div>
              <div v-if="byLine(d, s.key)" class="item__meta">{{ byLine(d, s.key) }}</div>
            </div>
            <v-btn v-if="d.canCancel" class="press" size="small" variant="text" color="error" @click="cancelling = d">Cancel</v-btn>
          </li>
        </ul>
      </v-card>
    </div>

    <DelegationDialog v-if="options" v-model="dialog" :options="options" :saving="saving" :error="saveError" @create="create" />

    <AppConfirmDialog
      v-model="cancelOpen"
      title="Cancel this delegation?"
      :message="cancelling ? `${cancelling.delegateName} will stop approving for ${cancelling.signatoryName}. Actions already taken stay as they are.` : ''"
      confirm-text="Cancel delegation"
      confirm-color="error"
      :loading="cancelBusy"
      @confirm="confirmCancel"
    />
  </AuthenticatedLayout>
</template>

<style scoped>
.sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.section-head {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 4px 12px;
  padding: 14px 20px;
}
.list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 20px;
  border-bottom: 1px solid var(--line);
}
.item:last-child {
  border-bottom: 0;
}
.item--muted {
  opacity: 0.6;
}
.item__main {
  flex: 1;
  min-width: 0;
}
.item__top {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}
.item__who {
  font-weight: 500;
}
.item__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.item__reason {
  font-size: 13px;
  margin-top: 2px;
}
</style>
