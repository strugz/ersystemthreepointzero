<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import type { Crumb } from '@/layouts/AuthenticatedLayout.vue';
import { formatDate, formatDateTime, formatMoney } from '@/shared/format';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { useSessionStore } from '@/app/stores/session';
import { expensesApi } from '@/features/expenses/api';
import { reportTypeLabel, usesCashAdvance } from '@/features/expenses/labels';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import ReportTotalsCard from '@/features/expenses/components/ReportTotalsCard.vue';
import ReportDetailsCard from '@/features/expenses/components/ReportDetailsCard.vue';
import ExpenseLinesTable from '@/features/expenses/components/ExpenseLinesTable.vue';
import CashAdvancesCard from '@/features/expenses/components/CashAdvancesCard.vue';
import AttachmentsPanel from '@/features/expenses/components/AttachmentsPanel.vue';
import ApprovalTimeline from '@/features/expenses/components/ApprovalTimeline.vue';
import FinanceSummaryCard from '@/features/expenses/components/FinanceSummaryCard.vue';
import type { ExpenseLine, ExpenseReportDetail } from '@/features/expenses/types';
import { approvalsApi } from '@/features/approvals/api';
import { approvalActionCopy } from '@/features/approvals/labels';
import ApprovalActionDialog from '@/features/approvals/components/ApprovalActionDialog.vue';
import AdjustLineDialog from '@/features/approvals/components/AdjustLineDialog.vue';
import type { ApprovalActionKind } from '@/features/approvals/types';

// Read-only review of someone else's report, for approvers and Finance. The API decides what the
// viewer may do (report.review); these buttons only mirror it, and every refusal is surfaced.
const route = useRoute();
const router = useRouter();
const snack = useSnackbar();
const session = useSessionStore();
const { xs, lgAndUp } = useDisplay();
const reportId = String(route.params.id);

const report = ref<ExpenseReportDetail | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);
const detailsOpen = ref(false);

async function load() {
  loading.value = true;
  loadError.value = null;
  try {
    report.value = await expensesApi.get(reportId);
  } catch (err) {
    loadError.value =
      err instanceof ApiError && err.status === 404
        ? 'Report not found.'
        : err instanceof ApiError && err.status === 403
          ? 'You are not allowed to view this report.'
          : 'Unable to load the report.';
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const fromFinance = computed(() => route.query.from === 'finance');
const crumbs = computed<Crumb[]>(() => [
  fromFinance.value ? { title: 'Finance queue', to: { name: 'finance-queue' } } : { title: 'Approvals', to: { name: 'approvals' } },
  { title: report.value?.referenceNo || 'Expense report', mono: !!report.value?.referenceNo },
]);

const subtitle = computed(() => {
  const r = report.value;
  if (!r) return '';
  const parts = [r.employeeName, reportTypeLabel[r.reportType], r.dateFrom || r.dateTo ? `${formatDate(r.dateFrom)} – ${formatDate(r.dateTo)}` : null];
  return parts.filter(Boolean).join(' · ');
});

const review = computed(() => report.value?.review ?? null);
const isOwnReport = computed(() => !!report.value && report.value.employeeUserId === session.user?.userId);
const hasActions = computed(() => {
  const r = review.value;
  return !!r && (r.canApprove || r.canReturn || r.canReject || r.canOverride);
});

// ---- approve / return / reject / override -----------------------------------
const action = ref<ApprovalActionKind | null>(null);
const acting = ref(false);
const actionError = ref<string | null>(null);

function startAction(kind: ApprovalActionKind) {
  actionError.value = null;
  action.value = kind;
}

async function confirmAction(note: string | null) {
  if (!report.value || !action.value) return;
  const kind = action.value;
  acting.value = true;
  actionError.value = null;
  try {
    const result = await approvalsApi.act(reportId, kind, note, report.value.rowVersion);
    report.value = result.report;
    action.value = null;
    snack.success(
      kind === 'approve' && result.status === 'ForApproval' && result.nextApproverName
        ? `Approved. Next: ${result.nextApproverName}.`
        : approvalActionCopy[kind].done,
    );
    if (result.status !== 'ForApproval') {
      await router.push({ name: 'approvals' });
    }
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      actionError.value = describeError(err);
    } else {
      // 403 / 409: the report moved on or is not ours to act on; show the current state.
      action.value = null;
      snack.fromError(err, 'Unable to complete the action.');
      await load();
    }
  } finally {
    acting.value = false;
  }
}

// ---- lower a line ------------------------------------------------------------
const adjusting = ref<ExpenseLine | null>(null);
const savingAdjust = ref(false);
const adjustError = ref<string | null>(null);

function startAdjust(line: ExpenseLine) {
  adjustError.value = null;
  adjusting.value = line;
}

async function saveAdjust(amount: number, reason: string) {
  const line = adjusting.value;
  if (!line) return;
  savingAdjust.value = true;
  adjustError.value = null;
  try {
    report.value = await approvalsApi.adjustLine(reportId, line.id, amount, reason, line.rowVersion);
    adjusting.value = null;
    snack.success('Line amount lowered.');
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      adjustError.value = describeError(err);
    } else {
      adjusting.value = null;
      snack.fromError(err, 'Unable to lower the amount.');
      await load();
    }
  } finally {
    savingAdjust.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="crumbs">
    <template #actions>
      <template v-if="report">
        <v-btn class="press" variant="text" prepend-icon="mdi-printer-outline" :to="{ name: 'report-pdf', params: { id: report.id } }">R47 PDF</v-btn>
        <template v-if="review">
          <v-btn v-if="review.canReject" class="press" variant="text" color="error" @click="startAction('reject')">Reject</v-btn>
          <v-btn v-if="review.canReturn" class="press" variant="outlined" color="warning" prepend-icon="mdi-undo-variant" @click="startAction('return')">Return</v-btn>
          <v-btn v-if="review.canOverride" class="press" variant="outlined" color="primary" @click="startAction('override')">Override</v-btn>
          <v-btn v-if="review.canApprove" class="press" variant="flat" color="success" prepend-icon="mdi-check" @click="startAction('approve')">Approve</v-btn>
        </template>
      </template>
    </template>

    <template #title-mobile>
      <div v-if="report" class="text-body-1 font-weight-medium text-truncate mono">{{ report.referenceNo || 'Expense report' }}</div>
      <div v-if="report" class="text-caption text-medium-emphasis text-truncate">{{ subtitle }}</div>
    </template>
    <template #actions-mobile>
      <template v-if="report">
        <ReportStatusChip :status="report.status" />
        <v-menu location="bottom end" :offset="6">
          <template #activator="{ props: menu }">
            <v-btn v-bind="menu" class="press" icon="mdi-dots-vertical" variant="text" aria-label="More actions" />
          </template>
          <v-card min-width="220" class="pa-1">
            <v-list density="compact" class="py-0">
              <v-list-item prepend-icon="mdi-printer-outline" title="R47 PDF" :to="{ name: 'report-pdf', params: { id: report.id } }" />
              <v-list-item v-if="review?.canOverride" prepend-icon="mdi-debug-step-over" title="Override Level 1" @click="startAction('override')" />
              <v-list-item v-if="review?.canReject" prepend-icon="mdi-close-octagon-outline" title="Reject" base-color="error" @click="startAction('reject')" />
            </v-list>
          </v-card>
        </v-menu>
      </template>
    </template>

    <template v-if="report && review && (review.canApprove || review.canReturn)" #bottom>
      <v-btn v-if="review.canReturn" class="press" variant="outlined" color="warning" prepend-icon="mdi-undo-variant" @click="startAction('return')">Return</v-btn>
      <v-btn v-if="review.canApprove" class="press" variant="flat" color="success" prepend-icon="mdi-check" @click="startAction('approve')">Approve</v-btn>
    </template>

    <v-progress-linear v-if="loading && !report" indeterminate color="primary" class="mb-4" />
    <v-alert v-else-if="loadError" type="error">{{ loadError }}</v-alert>

    <div v-else-if="report" class="review" :class="{ 'review--wide': lgAndUp }">
      <div class="review__main">
        <div v-if="!xs" class="title-row">
          <h1 class="page-title">{{ report.employeeName }}</h1>
          <ReportStatusChip :status="report.status" />
          <span class="text-body-2 text-medium-emphasis">
            {{ reportTypeLabel[report.reportType] }}<span v-if="report.filedAt"> · filed {{ formatDateTime(report.filedAt) }}</span>
          </span>
        </div>

        <v-alert v-if="isOwnReport" type="info" density="compact">This is your own report. Nobody approves their own report.</v-alert>
        <v-alert v-else-if="report.status === 'ForApproval' && !hasActions" type="info" density="compact">
          You have no action on this report right now. It is waiting for another step, or your step is done.
        </v-alert>
        <v-alert v-if="review?.canAdjustLines" type="info" variant="tonal" density="compact" icon="mdi-arrow-down-bold-circle-outline">
          You can lower a line amount, with a reason, before you approve. Amounts lock after the first approval.
        </v-alert>
        <v-alert v-if="report.status === 'Rejected' || report.status === 'Returned'" :type="report.status === 'Rejected' ? 'error' : 'warning'" density="compact">
          <div class="font-weight-medium">{{ report.status === 'Rejected' ? 'Rejected' : 'Returned to the employee' }}</div>
          <div v-if="report.cancelNote" class="text-body-2" style="white-space: pre-line">{{ report.cancelNote }}</div>
        </v-alert>

        <ReportTotalsCard v-if="!lgAndUp" :total="report.totalAmount" :cash-advance="report.cashAdvanceTotal" :variant="xs ? 'card' : 'strip'" />

        <ReportDetailsCard v-model:open="detailsOpen" :report="report" :editable="false" :saving="false" />

        <CashAdvancesCard v-if="usesCashAdvance(report.reportType)" :lines="report.cashAdvances" :editable="false" />

        <v-card>
          <div class="card-h">
            <div class="d-flex align-baseline ga-3 flex-wrap">
              <span class="card-t">Expense lines</span>
              <span class="card-meta">{{ report.lines.length }} line{{ report.lines.length === 1 ? '' : 's' }}</span>
            </div>
            <span v-if="xs" class="mono card-t">{{ formatMoney(report.totalAmount) }}</span>
          </div>
          <ExpenseLinesTable
            :lines="report.lines"
            :editable="false"
            :adjustable="!!review?.canAdjustLines"
            :busy-line-id="savingAdjust ? (adjusting?.id ?? null) : null"
            @adjust="startAdjust"
          />
        </v-card>

        <div v-if="!lgAndUp" class="review__two-up">
          <ApprovalTimeline :steps="report.approvalSteps" :status="report.status" :cycle="report.currentCycle" />
          <FinanceSummaryCard v-if="report.finance" :finance="report.finance" />
          <AttachmentsPanel class="review__span" :report-id="report.id" :attachments="report.attachments" :editable="false" />
        </div>
      </div>

      <aside v-if="lgAndUp" class="review__side">
        <ReportTotalsCard :total="report.totalAmount" :cash-advance="report.cashAdvanceTotal" />
        <ApprovalTimeline :steps="report.approvalSteps" :status="report.status" :cycle="report.currentCycle" />
        <FinanceSummaryCard v-if="report.finance" :finance="report.finance" />
        <AttachmentsPanel :report-id="report.id" :attachments="report.attachments" :editable="false" />
      </aside>

      <ApprovalActionDialog :action="action" :loading="acting" :error="actionError" @close="action = null" @confirm="confirmAction" />
      <AdjustLineDialog :line="adjusting" :saving="savingAdjust" :error="adjustError" @close="adjusting = null" @save="saveAdjust" />
    </div>
  </AuthenticatedLayout>
</template>

<style scoped>
.review {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 32px;
  align-items: start;
}
.review--wide {
  grid-template-columns: minmax(0, 1fr) 340px;
}
.review__main,
.review__side {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
}
.review__side {
  gap: 16px;
  position: sticky;
  top: calc(var(--shell-header) + 28px);
}
.title-row {
  display: flex;
  align-items: baseline;
  gap: 14px;
  flex-wrap: wrap;
}
.review__two-up {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}
.review__span {
  grid-column: span 2;
}
@media (max-width: 599.98px) {
  .review__main {
    gap: 14px;
  }
  .review__two-up {
    grid-template-columns: 1fr;
    gap: 14px;
  }
  .review__span {
    grid-column: span 1;
  }
}
</style>
