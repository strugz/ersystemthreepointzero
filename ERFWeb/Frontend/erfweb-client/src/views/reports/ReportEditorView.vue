<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import type { Crumb } from '@/layouts/AuthenticatedLayout.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatBytes, formatDate, formatDateTime, formatMoney } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { expensesApi } from '@/features/expenses/api';
import { reportTypeLabel, usesCashAdvance, usesRevolvingFund } from '@/features/expenses/labels';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import ReportDetailsCard from '@/features/expenses/components/ReportDetailsCard.vue';
import ReportTotalsCard from '@/features/expenses/components/ReportTotalsCard.vue';
import FilingChecklist from '@/features/expenses/components/FilingChecklist.vue';
import type { ChecklistItem } from '@/features/expenses/components/FilingChecklist.vue';
import ExpenseLinesTable from '@/features/expenses/components/ExpenseLinesTable.vue';
import ExpenseLineDialog from '@/features/expenses/components/ExpenseLineDialog.vue';
import CashAdvancesCard from '@/features/expenses/components/CashAdvancesCard.vue';
import CashAdvanceDialog from '@/features/expenses/components/CashAdvanceDialog.vue';
import AttachmentsPanel from '@/features/expenses/components/AttachmentsPanel.vue';
import { compressImage } from '@/shared/images';
import ApprovalTimeline from '@/features/expenses/components/ApprovalTimeline.vue';
import type {
  Attachment,
  CashAdvanceLine,
  EmployeeRates,
  ExpenseLine,
  ExpenseReportDetail,
  FilingReadiness,
  ReportHeaderInput,
  SaveCashAdvanceLineInput,
  SaveExpenseLineInput,
  WorkWithOption,
} from '@/features/expenses/types';

const route = useRoute();
const router = useRouter();
const snack = useSnackbar();
const { xs, lgAndUp } = useDisplay();
const reportId = String(route.params.id);

const report = ref<ExpenseReportDetail | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);

const workWith = ref<WorkWithOption[]>([]);
const rates = ref<EmployeeRates | null>(null);
const readiness = ref<FilingReadiness | null>(null);

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

async function loadLookups() {
  const [ww, r, fr] = await Promise.allSettled([expensesApi.workWith(), expensesApi.myRates(), expensesApi.filingReadiness()]);
  if (ww.status === 'fulfilled') workWith.value = ww.value;
  if (r.status === 'fulfilled') rates.value = r.value;
  if (fr.status === 'fulfilled') readiness.value = fr.value;
}

onMounted(async () => {
  await load();
  if (report.value?.canEdit) {
    void loadLookups();
  } else if (report.value) {
    void expensesApi.filingReadiness().then((v) => (readiness.value = v)).catch(() => undefined);
  }
});

// A 409 means someone else changed the report; reload so the user sees the current state.
async function handleMutationError(err: unknown, fallback: string) {
  snack.fromError(err, fallback);
  if (err instanceof ApiError && err.status === 409) {
    await load();
  }
}

const crumbs = computed<Crumb[]>(() => [
  { title: 'My reports', to: { name: 'reports' } },
  { title: report.value?.referenceNo || 'Expense report', mono: !!report.value?.referenceNo },
]);

const subtitle = computed(() => {
  const r = report.value;
  if (!r) return '';
  const parts = [reportTypeLabel[r.reportType], r.dateFrom || r.dateTo ? `${formatDate(r.dateFrom)} – ${formatDate(r.dateTo)}` : null];
  return parts.filter(Boolean).join(' · ');
});

// ---- header ---------------------------------------------------------------
const detailsOpen = ref(false);
const savingHeader = ref(false);

async function saveHeader(input: ReportHeaderInput) {
  if (!report.value) return;
  savingHeader.value = true;
  try {
    report.value = await expensesApi.update(reportId, { ...input, rowVersion: report.value.rowVersion });
    detailsOpen.value = false;
    snack.success('Report details saved.');
  } catch (err) {
    await handleMutationError(err, 'Unable to save the report details.');
  } finally {
    savingHeader.value = false;
  }
}

// ---- lines ----------------------------------------------------------------
const lineDialog = ref(false);
const lineDialogRef = ref<InstanceType<typeof ExpenseLineDialog> | null>(null);
const editingLine = ref<ExpenseLine | null>(null);
const savingLine = ref(false);
const lineErrors = ref<Record<string, string[]> | null>(null);
const busyLineId = ref<number | null>(null);
const removingLine = ref<ExpenseLine | null>(null);

const lastLineDate = computed(() => {
  const lines = report.value?.lines ?? [];
  return lines.length ? lines[lines.length - 1]?.transactionDate ?? null : report.value?.dateFrom ?? null;
});

function openAddLine() {
  editingLine.value = null;
  lineErrors.value = null;
  lineDialog.value = true;
}

function openEditLine(line: ExpenseLine) {
  editingLine.value = line;
  lineErrors.value = null;
  lineDialog.value = true;
}

async function saveLine(input: SaveExpenseLineInput) {
  if (!report.value) return;
  savingLine.value = true;
  lineErrors.value = null;
  try {
    if (editingLine.value) {
      await expensesApi.updateLine(reportId, editingLine.value.id, input);
      snack.success('Expense line updated.');
      lineDialog.value = false;
      await load();
    } else {
      await expensesApi.addLine(reportId, input);
      snack.success('Expense line added.');
      // Stay open for the next line: reload first so the default date follows the line just added.
      await load();
      lineDialogRef.value?.reset();
    }
  } catch (err) {
    if (err instanceof ApiError && err.status === 400 && err.problem?.errors) {
      lineErrors.value = err.problem.errors;
    }
    await handleMutationError(err, 'Unable to save the expense line.');
  } finally {
    savingLine.value = false;
  }
}

async function confirmRemoveLine() {
  const line = removingLine.value;
  if (!line) return;
  busyLineId.value = line.id;
  try {
    await expensesApi.removeLine(reportId, line.id);
    removingLine.value = null;
    snack.success('Expense line removed.');
    await load();
  } catch (err) {
    await handleMutationError(err, 'Unable to remove the expense line.');
  } finally {
    busyLineId.value = null;
  }
}

// ---- cash advances --------------------------------------------------------
const cashDialog = ref(false);
const cashDialogRef = ref<InstanceType<typeof CashAdvanceDialog> | null>(null);
const editingCash = ref<CashAdvanceLine | null>(null);
const savingCash = ref(false);
const cashErrors = ref<Record<string, string[]> | null>(null);
const busyCashId = ref<number | null>(null);
const removingCash = ref<CashAdvanceLine | null>(null);

function openAddCash() {
  editingCash.value = null;
  cashErrors.value = null;
  cashDialog.value = true;
}

function openEditCash(line: CashAdvanceLine) {
  editingCash.value = line;
  cashErrors.value = null;
  cashDialog.value = true;
}

async function saveCash(input: SaveCashAdvanceLineInput) {
  if (!report.value) return;
  savingCash.value = true;
  cashErrors.value = null;
  try {
    if (editingCash.value) {
      await expensesApi.updateCashAdvance(reportId, editingCash.value.id, input);
      snack.success('Cash advance updated.');
      cashDialog.value = false;
      await load();
    } else {
      await expensesApi.addCashAdvance(reportId, input);
      snack.success('Cash advance added.');
      // Stay open for the next advance.
      await load();
      cashDialogRef.value?.reset();
    }
  } catch (err) {
    if (err instanceof ApiError && err.status === 400 && err.problem?.errors) {
      cashErrors.value = err.problem.errors;
    }
    await handleMutationError(err, 'Unable to save the cash advance.');
  } finally {
    savingCash.value = false;
  }
}

async function confirmRemoveCash() {
  const line = removingCash.value;
  if (!line) return;
  busyCashId.value = line.id;
  try {
    await expensesApi.removeCashAdvance(reportId, line.id);
    removingCash.value = null;
    snack.success('Cash advance removed.');
    await load();
  } catch (err) {
    await handleMutationError(err, 'Unable to remove the cash advance.');
  } finally {
    busyCashId.value = null;
  }
}

// ---- attachments ----------------------------------------------------------
const uploading = ref(false);
const busyAttachmentId = ref<number | null>(null);

async function upload(picked: File) {
  uploading.value = true;
  try {
    // Shrink phone photos in the browser before they leave the device; PDFs pass through.
    const { file, originalBytes, compressed } = await compressImage(picked);
    await expensesApi.upload(reportId, file);
    snack.success(
      compressed
        ? `Uploaded ${file.name} (${formatBytes(originalBytes)} → ${formatBytes(file.size)}).`
        : `Uploaded ${file.name}.`,
    );
    await load();
  } catch (err) {
    await handleMutationError(err, 'Unable to upload the file.');
  } finally {
    uploading.value = false;
  }
}

async function removeAttachment(a: Attachment) {
  busyAttachmentId.value = a.id;
  try {
    await expensesApi.removeAttachment(reportId, a.id);
    snack.success('Attachment deleted.');
    await load();
  } catch (err) {
    await handleMutationError(err, 'Unable to delete the attachment.');
  } finally {
    busyAttachmentId.value = null;
  }
}

// ---- workflow -------------------------------------------------------------
const fileDialog = ref(false);
const reopenDialog = ref(false);
const deleteDialog = ref(false);
const acting = ref(false);

const checklist = computed<ChecklistItem[]>(() => {
  const r = report.value;
  if (!r) return [];
  const items: ChecklistItem[] = [
    { ok: r.lines.length > 0, text: r.lines.length > 0 ? `${r.lines.length} expense line${r.lines.length === 1 ? '' : 's'} added` : 'Add at least one expense line' },
    { ok: !!r.purpose?.trim() && !!r.dateFrom && !!r.dateTo, text: r.purpose?.trim() && r.dateFrom && r.dateTo ? 'Purpose and period set' : 'Set the purpose and period' },
  ];
  if (usesCashAdvance(r.reportType)) {
    items.push({ ok: r.cashAdvanceTotal > 0, text: r.cashAdvanceTotal > 0 ? 'Cash advance recorded' : 'Add at least one cash advance' });
  }
  if (usesRevolvingFund(r.reportType)) {
    items.push({ ok: r.revolvingFundAmount > 0, text: r.revolvingFundAmount > 0 ? 'Revolving fund amount recorded' : 'Enter the revolving fund amount' });
  }
  if (readiness.value) {
    items.push({ ok: readiness.value.hasSignature, text: readiness.value.hasSignature ? 'Signature on file' : 'Signature not on file (Account settings)' });
    items.push({ ok: readiness.value.approverCount > 0, text: readiness.value.approverCount > 0 ? `Approval route: ${readiness.value.approverCount} approver${readiness.value.approverCount === 1 ? '' : 's'}` : 'No approval route is configured for you' });
  }
  return items;
});
const canFileNow = computed(() => !!report.value?.canFile && checklist.value.every((i) => i.ok));

async function fileReport() {
  acting.value = true;
  try {
    const result = await expensesApi.file(reportId);
    fileDialog.value = false;
    snack.success(result.nextApproverName ? `Filed. Awaiting approval by ${result.nextApproverName}.` : 'Report filed for approval.');
    await load();
  } catch (err) {
    await handleMutationError(err, 'Unable to file the report.');
  } finally {
    acting.value = false;
  }
}

async function reopenReport() {
  acting.value = true;
  try {
    report.value = await expensesApi.reopen(reportId);
    reopenDialog.value = false;
    snack.info('Report reopened. Approval will restart when you file it again.');
    void loadLookups();
  } catch (err) {
    await handleMutationError(err, 'Unable to reopen the report.');
  } finally {
    acting.value = false;
  }
}

async function deleteReport() {
  acting.value = true;
  try {
    await expensesApi.remove(reportId);
    snack.success('Report deleted.');
    await router.replace({ name: 'reports' });
  } catch (err) {
    await handleMutationError(err, 'Unable to delete the report.');
  } finally {
    acting.value = false;
  }
}

const fileLabel = computed(() => (report.value?.status === 'Returned' ? 'Re-file report' : 'File report'));
</script>

<template>
  <AuthenticatedLayout :crumbs="crumbs">
    <!-- Desktop / tablet header actions -->
    <template #actions>
      <template v-if="report">
        <v-btn class="press" variant="text" prepend-icon="mdi-printer-outline" :to="{ name: 'report-pdf', params: { id: report.id } }">R47 PDF</v-btn>
        <v-btn v-if="report.canDelete" class="press" variant="text" color="error" @click="deleteDialog = true">Delete</v-btn>
        <v-btn v-if="report.canReopen" class="press" variant="outlined" color="warning" prepend-icon="mdi-lock-open-variant-outline" @click="reopenDialog = true">Reopen</v-btn>
        <v-btn v-if="report.canFile" class="press" color="primary" variant="flat" prepend-icon="mdi-send-outline" :disabled="!canFileNow" @click="fileDialog = true">{{ fileLabel }}</v-btn>
      </template>
    </template>

    <!-- Phone header -->
    <template #title-mobile>
      <div v-if="report" class="text-body-1 font-weight-medium text-truncate" :class="{ mono: !!report.referenceNo }">{{ report.referenceNo || 'Expense report' }}</div>
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
              <v-list-item v-if="report.canReopen" prepend-icon="mdi-lock-open-variant-outline" title="Reopen for editing" @click="reopenDialog = true" />
              <v-list-item v-if="report.canDelete" prepend-icon="mdi-delete-outline" title="Delete report" base-color="error" @click="deleteDialog = true" />
            </v-list>
          </v-card>
        </v-menu>
      </template>
    </template>

    <!-- Phone sticky actions -->
    <template v-if="report && (report.canEdit || report.canFile)" #bottom>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-plus" :disabled="!report.canEdit" @click="openAddLine">Add line</v-btn>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-send-outline" :disabled="!canFileNow" @click="fileDialog = true">{{ fileLabel }}</v-btn>
    </template>

    <v-progress-linear v-if="loading && !report" indeterminate color="primary" class="mb-4" />
    <v-alert v-else-if="loadError" type="error">{{ loadError }}</v-alert>

    <div v-else-if="report" class="editor" :class="{ 'editor--wide': lgAndUp }">
      <!-- Main column -->
      <div class="editor__main">
        <div v-if="!xs" class="title-row">
          <h1 class="page-title">Expense report</h1>
          <ReportStatusChip :status="report.status" />
          <span class="text-body-2 text-medium-emphasis">
            {{ subtitle }}<span v-if="report.filedAt"> · filed {{ formatDateTime(report.filedAt) }}</span>
          </span>
        </div>

        <v-alert v-if="report.status === 'Returned'" type="error" icon="mdi-undo-variant">
          <div class="font-weight-medium">Returned by your approver</div>
          <div class="text-body-2" style="white-space: pre-line">{{ report.cancelNote || 'No reason was given.' }}</div>
          <div class="text-caption mt-1">Correct the report and re-file it. Approval restarts from the first approver.</div>
        </v-alert>
        <v-alert v-else-if="!report.canEdit && report.canReopen" type="info" density="compact">
          This report is {{ report.status === 'Approved' ? 'approved' : 'awaiting approval' }} and is read-only. Reopen it to make changes — all approvals so far will be cleared.
        </v-alert>

        <!-- Totals move above the document when there is no side column -->
        <ReportTotalsCard v-if="!lgAndUp" :total="report.totalAmount" :cash-advance="report.cashAdvanceTotal" :variant="xs ? 'card' : 'strip'" />

        <ReportDetailsCard v-model:open="detailsOpen" :report="report" :editable="report.canEdit" :saving="savingHeader" @save="saveHeader" />

        <CashAdvancesCard
          v-if="usesCashAdvance(report.reportType)"
          :lines="report.cashAdvances"
          :editable="report.canEdit"
          :busy-id="busyCashId"
          @add="openAddCash"
          @edit="openEditCash"
          @remove="removingCash = $event"
        />

        <v-card>
          <div class="card-h">
            <div class="d-flex align-baseline ga-3 flex-wrap">
              <span class="card-t">Expense lines</span>
              <span class="card-meta">{{ report.lines.length }} line{{ report.lines.length === 1 ? '' : 's' }}<span class="d-none d-md-inline"> · grouped by date and Work With, as printed</span></span>
            </div>
            <v-btn v-if="report.canEdit && report.lines.length && !xs" class="press" size="small" variant="outlined" prepend-icon="mdi-plus" @click="openAddLine">Add line</v-btn>
            <span v-else-if="xs" class="mono card-t">{{ formatMoney(report.totalAmount) }}</span>
          </div>
          <ExpenseLinesTable :lines="report.lines" :editable="report.canEdit" :busy-line-id="busyLineId" @add="openAddLine" @edit="openEditLine" @remove="removingLine = $event" />
        </v-card>

        <!-- Below-the-document cards on tablet / small desktop -->
        <div v-if="!lgAndUp" class="editor__two-up">
          <FilingChecklist v-if="report.canFile" :items="checklist" :approver-names="readiness?.approverNames ?? []" />
          <ApprovalTimeline :steps="report.approvalSteps" :status="report.status" :cycle="report.currentCycle" />
          <AttachmentsPanel
            class="editor__span"
            :report-id="report.id"
            :attachments="report.attachments"
            :editable="report.canEdit"
            :uploading="uploading"
            :busy-id="busyAttachmentId"
            @upload="upload"
            @remove="removeAttachment"
          />
        </div>
      </div>

      <!-- Side column (desktop) -->
      <aside v-if="lgAndUp" class="editor__side">
        <ReportTotalsCard :total="report.totalAmount" :cash-advance="report.cashAdvanceTotal" />
        <FilingChecklist v-if="report.canFile" :items="checklist" :approver-names="readiness?.approverNames ?? []" />
        <ApprovalTimeline :steps="report.approvalSteps" :status="report.status" :cycle="report.currentCycle" />
        <AttachmentsPanel
          :report-id="report.id"
          :attachments="report.attachments"
          :editable="report.canEdit"
          :uploading="uploading"
          :busy-id="busyAttachmentId"
          @upload="upload"
          @remove="removeAttachment"
        />
      </aside>

      <ExpenseLineDialog
        ref="lineDialogRef"
        v-model="lineDialog"
        :line="editingLine"
        :default-date="lastLineDate"
        :work-with-options="workWith"
        :rates="rates"
        :saving="savingLine"
        :errors="lineErrors"
        @save="saveLine"
      />

      <CashAdvanceDialog
        ref="cashDialogRef"
        v-model="cashDialog"
        :line="editingCash"
        :saving="savingCash"
        :errors="cashErrors"
        @save="saveCash"
      />

      <AppConfirmDialog
        :model-value="removingCash !== null"
        title="Remove cash advance?"
        :message="removingCash ? `The ${formatMoney(removingCash.amount)} advance${removingCash.referenceNo ? ` (${removingCash.referenceNo})` : ''} will be removed from this report.` : ''"
        confirm-text="Remove"
        confirm-color="error"
        :loading="busyCashId !== null"
        @update:model-value="(v: boolean) => !v && (removingCash = null)"
        @confirm="confirmRemoveCash"
      />

      <AppConfirmDialog
        :model-value="removingLine !== null"
        title="Remove expense line?"
        :message="removingLine ? `“${removingLine.particulars}” (${formatMoney(removingLine.totalAmount)}) will be removed from this report.` : ''"
        confirm-text="Remove"
        confirm-color="error"
        :loading="busyLineId !== null"
        @update:model-value="(v: boolean) => !v && (removingLine = null)"
        @confirm="confirmRemoveLine"
      />

      <AppConfirmDialog
        v-model="fileDialog"
        :title="report.status === 'Returned' ? 'Re-file this expense report?' : 'File this expense report?'"
        :message="`Total ${formatMoney(report.totalAmount)} across ${report.lines.length} line(s).\n\nYou cannot update the expenses once you file. The report goes to ${readiness?.approverNames[0] ?? 'your first approver'} for approval.`"
        :confirm-text="report.status === 'Returned' ? 'Re-file' : 'File report'"
        :loading="acting"
        @confirm="fileReport"
      />

      <AppConfirmDialog
        v-model="reopenDialog"
        title="Reopen this report for editing?"
        message="All manager approvals so far (and Finance receipt tracking, if any) will be cleared. The report must complete approval again after you re-file it."
        confirm-text="Reopen"
        confirm-color="warning"
        :loading="acting"
        @confirm="reopenReport"
      />

      <AppConfirmDialog
        v-model="deleteDialog"
        title="Delete this report?"
        message="The report, its expense lines and attachments will be deleted. This cannot be undone."
        confirm-text="Delete"
        confirm-color="error"
        :loading="acting"
        @confirm="deleteReport"
      />
    </div>
  </AuthenticatedLayout>
</template>

<style scoped>
.editor {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 32px;
  align-items: start;
}
.editor--wide {
  grid-template-columns: minmax(0, 1fr) 340px;
}
.editor__main,
.editor__side {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
}
.editor__side {
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
.editor__two-up {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}
.editor__span {
  grid-column: span 2;
}
@media (max-width: 599.98px) {
  .editor__main {
    gap: 14px;
  }
  .editor__two-up {
    grid-template-columns: 1fr;
    gap: 14px;
  }
  .editor__span {
    grid-column: span 1;
  }
}
</style>
