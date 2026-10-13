<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDateTime, formatMoney } from '@/shared/format';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { financeStatusLabel, financeStatusTone } from '@/features/expenses/labels';
import type { FinanceStatus, PagedResult } from '@/features/expenses/types';
import { financeApi } from '@/features/finance/api';
import ReceiptsDialog from '@/features/finance/components/ReceiptsDialog.vue';
import type { FinanceQueueItem, ReceiptStatus } from '@/features/finance/types';

// The physical-receipt queue: approved reports wait here until Finance confirms the receipts and
// clears them for payment. The API refuses out-of-order steps (409) and Finance's own reports (403).
const router = useRouter();
const snack = useSnackbar();
const { xs } = useDisplay();

const page = ref(1);
const pageSize = 20;
const status = ref<FinanceStatus | null>(null);
const showCleared = ref(false);
const search = ref('');
const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<FinanceQueueItem> | null>(null);

const statusOptions = (Object.keys(financeStatusLabel) as FinanceStatus[]).map((value) => ({ value, title: financeStatusLabel[value] }));

let searchTimer: ReturnType<typeof setTimeout> | undefined;

async function load() {
  loading.value = true;
  error.value = null;
  try {
    result.value = await financeApi.queue({
      page: page.value,
      pageSize,
      status: status.value ?? undefined,
      search: search.value.trim() || undefined,
      openOnly: !showCleared.value && status.value !== 'ClearedForPayment',
    });
  } catch (err) {
    error.value = 'Unable to load the Finance queue.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(page, load);
watch([status, showCleared], () => {
  page.value = 1;
  void load();
});
watch(search, () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    page.value = 1;
    void load();
  }, 250);
});

function open(item: FinanceQueueItem) {
  router.push({ name: 'report-review', params: { id: item.reportId }, query: { from: 'finance' } });
}

const awaitsReceipts = (item: FinanceQueueItem) => item.status === 'Pending' || item.status === 'ReceiptsIncomplete';

// A 409 means the row moved on (someone else recorded it); reload so the queue is current.
async function onActionError(err: unknown, fallback: string) {
  snack.fromError(err, fallback);
  if (err instanceof ApiError && (err.status === 409 || err.status === 404)) {
    await load();
  }
}

// ---- receipts ------------------------------------------------------------------
const recording = ref<FinanceQueueItem | null>(null);
const savingReceipts = ref(false);
const receiptsError = ref<string | null>(null);

function startReceipts(item: FinanceQueueItem) {
  receiptsError.value = null;
  recording.value = item;
}

async function saveReceipts(next: ReceiptStatus, remarks: string | null) {
  const item = recording.value;
  if (!item) return;
  savingReceipts.value = true;
  receiptsError.value = null;
  try {
    await financeApi.recordReceipts(item.reportId, next, remarks, item.rowVersion);
    recording.value = null;
    snack.success(`${item.referenceNo}: ${financeStatusLabel[next].toLowerCase()}.`);
    await load();
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      receiptsError.value = describeError(err);
    } else {
      recording.value = null;
      await onActionError(err, 'Unable to record the receipts.');
    }
  } finally {
    savingReceipts.value = false;
  }
}

// ---- clear for payment -----------------------------------------------------------
const clearing = ref<FinanceQueueItem | null>(null);
const clearRemarks = ref('');
const busyId = ref<string | null>(null);

function startClear(item: FinanceQueueItem) {
  clearRemarks.value = '';
  clearing.value = item;
}

async function confirmClear() {
  const item = clearing.value;
  if (!item) return;
  busyId.value = item.reportId;
  try {
    await financeApi.clear(item.reportId, clearRemarks.value.trim() || null, item.rowVersion);
    clearing.value = null;
    snack.success(`${item.referenceNo} cleared for payment.`);
    await load();
  } catch (err) {
    clearing.value = null;
    await onActionError(err, 'Unable to clear the report for payment.');
  } finally {
    busyId.value = null;
  }
}

// ---- follow-up ----------------------------------------------------------------------
async function followUp(item: FinanceQueueItem) {
  busyId.value = item.reportId;
  try {
    const updated = await financeApi.followUp(item.reportId, null);
    snack.info(`Reminder sent to ${item.employeeName} (${updated.followUpCount} so far).`);
    await load();
  } catch (err) {
    await onActionError(err, 'Unable to send the follow-up.');
  } finally {
    busyId.value = null;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Finance queue' }]">
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Finance queue</h1>
      <span class="text-body-2 text-medium-emphasis">Approved reports waiting for their original receipts. Confirm the receipts, then clear the report for payment.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-text-field
          v-model="search"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
          placeholder="Search employee, purpose or reference no."
          class="filters__search"
        />
        <v-select v-model="status" :items="statusOptions" density="compact" hide-details clearable label="Receipts" class="filters__status" />
        <v-switch v-model="showCleared" density="compact" hide-details color="primary" label="Include cleared" class="flex-grow-0" />
        <v-spacer class="d-none d-sm-block" />
        <span v-if="result" class="card-meta d-none d-sm-inline">{{ result.total }} report{{ result.total === 1 ? '' : 's' }}</span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="result && result.items.length === 0 && !loading"
        icon="mdi-receipt-text-check-outline"
        :title="search || status ? 'No reports match your filters' : 'The queue is clear'"
        :hint="search || status ? 'Try clearing the search or status filter.' : 'Reports appear here once they are fully approved.'"
      />

      <div v-else-if="result" class="items">
        <div v-for="(item, i) in result.items" :key="item.reportId" class="item stagger-in" :class="{ 'item--busy': busyId === item.reportId }" :style="{ '--i': Math.min(i, 10) }">
          <button type="button" class="item__main press" @click="open(item)">
            <div class="item__top">
              <span class="mono item__ref">{{ item.referenceNo }}</span>
              <span class="pill" :class="financeStatusTone[item.status]">{{ financeStatusLabel[item.status] }}</span>
              <span v-if="item.needsQuery" class="pill pill--error">Not verified: query Level 2</span>
              <span v-else-if="item.openVerifications" class="pill pill--warning">Under verification</span>
              <span v-if="item.isOwnReport" class="pill pill--neutral">Your report</span>
            </div>
            <div class="item__who">
              {{ item.employeeName }}<span v-if="item.departmentName" class="text-medium-emphasis"> · {{ item.departmentName }}</span>
            </div>
            <div class="item__meta">
              Approved {{ formatDateTime(item.approvedAt) }} · {{ item.daysSinceApproval }} day{{ item.daysSinceApproval === 1 ? '' : 's' }} ago ·
              {{ item.attachmentCount }} attachment{{ item.attachmentCount === 1 ? '' : 's' }}
              <span v-if="item.followUpCount"> · {{ item.followUpCount }} follow-up{{ item.followUpCount === 1 ? '' : 's' }}, last {{ formatDateTime(item.lastFollowUpAt) }}</span>
            </div>
            <div v-if="item.remarks" class="item__remarks">{{ item.remarks }}</div>
            <div v-if="item.receivedAt || item.clearedAt" class="item__meta">
              <span v-if="item.receivedAt">Received by {{ item.receivedByName }} {{ formatDateTime(item.receivedAt) }}</span>
              <span v-if="item.clearedAt"> · Cleared by {{ item.clearedByName }} {{ formatDateTime(item.clearedAt) }}</span>
            </div>
          </button>
          <div class="item__side">
            <span class="mono item__amt">{{ formatMoney(item.totalAmount) }}</span>
            <div v-if="!item.isOwnReport" class="item__actions">
              <v-btn v-if="awaitsReceipts(item)" class="press" size="small" variant="text" prepend-icon="mdi-bell-ring-outline" :disabled="busyId !== null" @click="followUp(item)">Follow up</v-btn>
              <v-btn v-if="awaitsReceipts(item)" class="press" size="small" variant="outlined" color="primary" :disabled="busyId !== null" @click="startReceipts(item)">Receipts</v-btn>
              <v-btn v-if="item.status === 'ReceiptsReceived' && !item.openVerifications" class="press" size="small" variant="flat" color="success" prepend-icon="mdi-cash-check" :loading="busyId === item.reportId" @click="startClear(item)">Clear for payment</v-btn>
              <span v-else-if="item.status === 'ReceiptsReceived'" class="text-caption text-medium-emphasis">Clear after the verification is resolved</span>
            </div>
          </div>
        </div>
      </div>

      <div v-if="result && result.totalPages > 1" class="d-flex justify-center py-2">
        <v-pagination v-model="page" :length="result.totalPages" density="comfortable" :total-visible="xs ? 5 : 7" />
      </div>
    </v-card>

    <ReceiptsDialog :item="recording" :saving="savingReceipts" :error="receiptsError" @close="recording = null" @save="saveReceipts" />

    <AppConfirmDialog
      :model-value="clearing !== null"
      title="Clear this report for payment?"
      :message="clearing ? `${clearing.referenceNo} · ${clearing.employeeName} · ${formatMoney(clearing.totalAmount)}\n\nClearing is final.` : ''"
      confirm-text="Clear for payment"
      confirm-color="success"
      :loading="busyId !== null"
      @update:model-value="(v: boolean) => !v && (clearing = null)"
      @confirm="confirmClear"
    >
      <v-textarea v-model="clearRemarks" label="Remarks (optional)" rows="2" auto-grow counter="1000" maxlength="1000" />
    </AppConfirmDialog>
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
.filters__search {
  flex: 1 1 240px;
  max-width: 360px;
}
.filters__status {
  flex: 0 1 220px;
  min-width: 180px;
}
@media (max-width: 599.98px) {
  .filters__search,
  .filters__status {
    flex: 1 1 100%;
    max-width: none;
  }
}
.items {
  display: flex;
  flex-direction: column;
}
.item {
  display: flex;
  gap: 16px;
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
  transition: opacity var(--dur-fast) var(--ease-out);
}
.item:last-child {
  border-bottom: 0;
}
.item--busy {
  opacity: 0.6;
}
.item__main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
  border: 0;
  background: none;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
  padding: 0;
}
.item__main:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.item__top {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.item__ref {
  font-size: 13px;
  font-weight: 500;
}
.item__who {
  font-size: 14px;
}
.item__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.item__remarks {
  font-size: 13px;
  white-space: pre-line;
}
.item__side {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
}
.item__amt {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}
.item__actions {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  justify-content: flex-end;
}
@media (max-width: 599.98px) {
  .item {
    flex-direction: column;
    gap: 10px;
  }
  .item__side {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }
}
</style>
