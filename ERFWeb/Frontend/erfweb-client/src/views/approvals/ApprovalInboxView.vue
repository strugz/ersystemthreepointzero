<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { formatDateTime, formatMoney } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { approvalsApi } from '@/features/approvals/api';
import type { ApprovalInboxItem, ApprovalInboxView } from '@/features/approvals/types';
import { approvalStatusLabel, reportTypeLabel } from '@/features/expenses/labels';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import type { PagedResult } from '@/features/expenses/types';
import { useSessionStore } from '@/app/stores/session';
import { verificationApi } from '@/features/verification/api';
import VerificationList from '@/features/verification/components/VerificationList.vue';
import type { VerificationQueueItem } from '@/features/verification/types';

const router = useRouter();
const route = useRoute();
const snack = useSnackbar();
const { xs } = useDisplay();

// "Queries" is the Level 2's list of Not verified findings Finance referred to them (plan Flow 6).
type InboxTab = ApprovalInboxView | 'Queries';
const session = useSessionStore();
const isLevel2 = session.hasRole('SignatoryLevel2');
const initialTab: InboxTab = route.query.view === 'Acted' ? 'Acted' : route.query.view === 'Queries' && isLevel2 ? 'Queries' : 'Pending';
const view = ref<InboxTab>(initialTab);
const page = ref(1);
const pageSize = 15;
const search = ref('');
const loading = ref(true);
const error = ref<string | null>(null);
const result = ref<PagedResult<ApprovalInboxItem> | null>(null);
const queries = ref<PagedResult<VerificationQueueItem> | null>(null);

let searchTimer: ReturnType<typeof setTimeout> | undefined;

async function load() {
  loading.value = true;
  error.value = null;
  try {
    if (view.value === 'Queries') {
      result.value = null;
      queries.value = await verificationApi.queries('Pending', page.value, pageSize);
    } else {
      queries.value = null;
      result.value = await approvalsApi.inbox({ view: view.value, page: page.value, pageSize, search: search.value.trim() || undefined });
    }
  } catch (err) {
    error.value = 'Unable to load your approvals.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(page, load);
watch(view, () => {
  page.value = 1;
  void router.replace({ query: view.value === 'Pending' ? {} : { view: view.value } });
  void load();
});
watch(search, () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    page.value = 1;
    void load();
  }, 250);
});

function openQuery(item: VerificationQueueItem) {
  router.push({ name: 'report-review', params: { id: item.reportId }, query: { from: 'queries' } });
}

function open(item: ApprovalInboxItem) {
  router.push({ name: 'report-review', params: { id: item.reportId } });
}

/** What the row is waiting for, from my point of view. */
function waitingFor(item: ApprovalInboxItem): string {
  if (view.value === 'Acted') return `${approvalStatusLabel[item.myStepStatus]}${item.myActedAt ? ` ${formatDateTime(item.myActedAt)}` : ''}`;
  if (item.isMyTurn) return item.onBehalfOfName ? `Your turn, for ${item.onBehalfOfName}` : 'Your turn';
  return item.currentApproverName ? `Waiting for ${item.currentApproverName}` : 'Waiting for an earlier step';
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Approvals' }]">
    <div v-if="!xs" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Approvals</h1>
      <span class="text-body-2 text-medium-emphasis">Reports routed to you. Steps go in order; a Level 2 may override a pending Level 1 with a reason.</span>
    </div>

    <v-card>
      <div class="filters">
        <v-btn-toggle v-model="view" mandatory density="comfortable" variant="outlined" divided color="primary">
          <v-btn value="Pending" class="press">To review</v-btn>
          <v-btn value="Acted" class="press">Acted on</v-btn>
          <v-btn v-if="isLevel2" value="Queries" class="press">Queries</v-btn>
        </v-btn-toggle>
        <v-text-field
          v-if="view !== 'Queries'"
          v-model="search"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
          placeholder="Search employee, purpose or reference no."
          class="filters__search"
        />
        <v-spacer class="d-none d-sm-block" />
        <span v-if="result" class="card-meta d-none d-sm-inline">{{ result.total }} report{{ result.total === 1 ? '' : 's' }}</span>
      </div>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <template v-else-if="view === 'Queries'">
        <AppEmptyState
          v-if="queries && queries.items.length === 0 && !loading"
          icon="mdi-magnify-scan"
          title="No queries for you"
          hint="When a verifier cannot verify a line, Finance asks the Level 2 Signatory to decide. Those queries appear here."
        />
        <VerificationList v-else-if="queries" :items="queries.items" show-query @open="openQuery" />
        <div v-if="queries && queries.totalPages > 1" class="d-flex justify-center py-2">
          <v-pagination v-model="page" :length="queries.totalPages" density="comfortable" :total-visible="xs ? 5 : 7" />
        </div>
      </template>

      <AppEmptyState
        v-else-if="result && result.items.length === 0 && !loading"
        icon="mdi-check-decagram-outline"
        :title="search ? 'No reports match your search' : view === 'Pending' ? 'Nothing waiting for you' : 'You have not acted on any report yet'"
        :hint="search ? 'Try a different name or reference number.' : view === 'Pending' ? 'Reports filed by the people you approve for appear here.' : 'Reports you approve, return or reject are listed here.'"
      />

      <!-- Phone: list rows -->
      <div v-else-if="result && xs" class="rows">
        <button
          v-for="(item, i) in result.items"
          :key="item.reportId"
          type="button"
          class="row press stagger-in"
          :style="{ '--i': Math.min(i, 10) }"
          @click="open(item)"
        >
          <div class="row__main">
            <div class="row__top">
              <span class="mono row__ref">{{ item.referenceNo }}</span>
              <ReportStatusChip :status="item.status" />
            </div>
            <div class="row__purpose">{{ item.employeeName }} · {{ item.purpose || reportTypeLabel[item.reportType] }}</div>
            <div class="row__meta" :class="{ 'row__meta--turn': item.isMyTurn && view === 'Pending' }">{{ waitingFor(item) }}</div>
            <div v-if="item.onBehalfOfName && view === 'Acted'" class="row__meta">On behalf of {{ item.onBehalfOfName }}</div>
          </div>
          <span class="mono row__amt">{{ formatMoney(item.totalAmount) }}</span>
        </button>
      </div>

      <!-- Tablet / desktop: ledger -->
      <div v-else-if="result" class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th>Reference</th>
              <th>Employee</th>
              <th class="d-none d-md-table-cell">Purpose</th>
              <th>Filed</th>
              <th class="r">Total</th>
              <th>{{ view === 'Pending' ? 'Waiting for' : 'Your action' }}</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(item, i) in result.items"
              :key="item.reportId"
              class="row-link stagger-in"
              :style="{ '--i': Math.min(i, 10) }"
              tabindex="0"
              @click="open(item)"
              @keydown.enter="open(item)"
            >
              <td class="mono font-weight-medium">{{ item.referenceNo }}</td>
              <td>
                <div>{{ item.employeeName }}</div>
                <div v-if="item.departmentName" class="text-caption text-medium-emphasis">{{ item.departmentName }}</div>
              </td>
              <td class="text-truncate d-none d-md-table-cell" style="max-width: 280px">{{ item.purpose || reportTypeLabel[item.reportType] }}</td>
              <td class="text-no-wrap text-medium-emphasis">
                {{ formatDateTime(item.filedAt) }}
                <div v-if="view === 'Pending' && item.daysWaiting > 0" class="text-caption">{{ item.daysWaiting }} day{{ item.daysWaiting === 1 ? '' : 's' }} waiting</div>
              </td>
              <td class="r mono font-weight-medium">{{ formatMoney(item.totalAmount) }}</td>
              <td :class="{ 'turn': item.isMyTurn && view === 'Pending' }">
                {{ waitingFor(item) }}
                <div v-if="view === 'Pending' && item.canOverride && !item.isMyTurn" class="text-caption text-medium-emphasis">You can override</div>
                <div v-if="item.onBehalfOfName && (view === 'Acted' || !item.isMyTurn)" class="text-caption text-medium-emphasis">On behalf of {{ item.onBehalfOfName }}</div>
              </td>
              <td><ReportStatusChip :status="item.status" /></td>
            </tr>
          </tbody>
        </table>
      </div>

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
.filters__search {
  flex: 1 1 240px;
  max-width: 360px;
}
@media (max-width: 599.98px) {
  .filters__search {
    flex: 1 1 100%;
    max-width: none;
  }
}
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
.turn,
.row__meta--turn {
  color: rgb(var(--v-theme-primary));
  font-weight: 500;
}
.rows {
  display: flex;
  flex-direction: column;
}
.row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 16px;
  min-height: 64px;
  border: 0;
  border-bottom: 1px solid var(--line);
  background: none;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
}
.row:last-child {
  border-bottom: 0;
}
.row__main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.row__top {
  display: flex;
  align-items: center;
  gap: 8px;
}
.row__ref {
  font-size: 13px;
  font-weight: 500;
}
.row__purpose {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.row__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.row__amt {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}
</style>
