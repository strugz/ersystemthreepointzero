<script setup lang="ts">
// The R47 designer: edit the layout both PDF engines print the R47 with, preview it on the sample report
// or a picked report, and save it. Saving changes every R47 printed or emailed from then on.
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { onBeforeRouteLeave } from 'vue-router';
import { useDisplay } from 'vuetify';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDateTime } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import R47DesignCanvas from '@/features/admin/components/R47DesignCanvas.vue';
import R47DesignPanel from '@/features/admin/components/R47DesignPanel.vue';
import { designProblems, type R47Section } from '@/features/admin/r47Design';
import type { R47Design, R47DesignState, R47PdfEngine, R47PreviewReport } from '@/features/admin/types';
import { reportStatusLabel } from '@/features/expenses/labels';
import type { ReportStatus } from '@/features/expenses/types';

const snack = useSnackbar();
const { mdAndUp } = useDisplay();

const state = ref<R47DesignState | null>(null);
const design = ref<R47Design | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const section = ref<R47Section | null>('grid');
const errors = ref<Record<string, string[]> | null>(null);

const undoStack = ref<string[]>([]);
const MAX_UNDO = 100;

async function load() {
  loading.value = true;
  error.value = null;
  try {
    state.value = await adminApi.r47Design();
    design.value = clone(state.value.design);
    undoStack.value = [];
    errors.value = null;
  } catch (err) {
    error.value = 'Unable to load the R47 layout.';
    snack.fromError(err, error.value);
  } finally {
    loading.value = false;
  }
}
onMounted(load);

const clone = (d: R47Design): R47Design => JSON.parse(JSON.stringify(d)) as R47Design;
const same = (a: R47Design, b: R47Design) => JSON.stringify(a) === JSON.stringify(b);

const dirty = computed(() => !!state.value && !!design.value && !same(state.value.design, design.value));
const isStandard = computed(() => !!state.value && !!design.value && same(state.value.standard, design.value));
const problems = computed(() => (design.value ? designProblems(design.value) : []));

/** Records the layout as it is now, so Undo can return to it. */
function checkpoint() {
  if (!design.value) return;
  const snapshot = JSON.stringify(design.value);
  if (undoStack.value[undoStack.value.length - 1] !== snapshot) {
    undoStack.value = [...undoStack.value.slice(-(MAX_UNDO - 1)), snapshot];
  }
}

function update(next: R47Design) {
  design.value = next;
}

function undo() {
  const last = undoStack.value[undoStack.value.length - 1];
  if (!last) return;
  undoStack.value = undoStack.value.slice(0, -1);
  design.value = JSON.parse(last) as R47Design;
}

function resetToStandard() {
  if (!state.value) return;
  checkpoint();
  design.value = clone(state.value.standard);
}

function discard() {
  if (!state.value) return;
  checkpoint();
  design.value = clone(state.value.design);
}

function onKey(event: KeyboardEvent) {
  const target = event.target as HTMLElement | null;
  const typing = target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA');
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z' && !event.shiftKey && !typing) {
    event.preventDefault();
    undo();
  }
}
onMounted(() => window.addEventListener('keydown', onKey));
onBeforeUnmount(() => window.removeEventListener('keydown', onKey));

onBeforeRouteLeave(() => !dirty.value || window.confirm('Leave the R47 designer? Your layout changes are not saved.'));

// ---- save ------------------------------------------------------------------

const confirmSave = ref(false);
const saving = ref(false);

async function save() {
  if (!design.value) return;
  saving.value = true;
  errors.value = null;
  try {
    state.value = await adminApi.saveR47Design(design.value);
    design.value = clone(state.value.design);
    confirmSave.value = false;
    snack.success('R47 layout saved. The next R47 printed uses it.');
  } catch (err) {
    confirmSave.value = false;
    snack.fromError(err, 'Unable to save the R47 layout.');
    if (err instanceof ApiError && err.problem?.errors) errors.value = err.problem.errors;
  } finally {
    saving.value = false;
  }
}

// ---- preview ---------------------------------------------------------------

// The sample report needs no real report; 'report' previews one picked from the search (drafts included).
const source = ref<'sample' | 'report'>('sample');
const picked = ref<R47PreviewReport | null>(null);
const reportSearch = ref('');
const reports = ref<R47PreviewReport[]>([]);
const reportsLoading = ref(false);
let searchTimer: ReturnType<typeof setTimeout> | undefined;
let searchSeq = 0;

async function searchReports(text: string) {
  const seq = ++searchSeq;
  reportsLoading.value = true;
  try {
    const found = await adminApi.r47PreviewReports(text.trim());
    // Keep the picked report in the list so the field still shows it after a new search.
    if (seq === searchSeq) reports.value = picked.value && !found.some((r) => r.id === picked.value!.id) ? [picked.value, ...found] : found;
  } catch (err) {
    if (seq === searchSeq) snack.fromError(err, 'Unable to search the reports.');
  } finally {
    if (seq === searchSeq) reportsLoading.value = false;
  }
}

watch(reportSearch, (text) => {
  if (picked.value && text === reportTitle(picked.value)) return;
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => void searchReports(text ?? ''), 300);
});
watch(source, (s) => {
  if (s === 'report' && !reports.value.length) void searchReports('');
});
onBeforeUnmount(() => clearTimeout(searchTimer));

function reportTitle(r: R47PreviewReport): string {
  return `${r.referenceNo ?? 'Not filed yet'} · ${r.employeeName}`;
}

function reportSubtitle(r: R47PreviewReport): string {
  const status = reportStatusLabel[r.status as ReportStatus] ?? r.status;
  return [status, r.purpose, `updated ${formatDateTime(r.updatedAt)}`].filter(Boolean).join(' · ');
}

// 'Rules' prints with the engine set on the Rules page.
const engine = ref<R47PdfEngine | 'Rules'>('Rules');
const previewing = ref(false);
const previewError = ref<string | null>(null);
const previewUrl = ref<string | null>(null);
const previewOpen = ref(false);
const canPreview = computed(() => source.value === 'sample' || !!picked.value);
const previewTitle = computed(() =>
  source.value === 'sample' ? 'Sample report' : picked.value ? reportTitle(picked.value) : '',
);

async function preview() {
  if (!design.value || !canPreview.value) return;
  previewing.value = true;
  previewError.value = null;
  try {
    const blob = await adminApi.previewR47Design({
      sample: source.value === 'sample',
      reportId: source.value === 'report' ? (picked.value?.id ?? null) : null,
      design: design.value,
      engine: engine.value === 'Rules' ? null : engine.value,
    });
    closePreviewUrl();
    previewUrl.value = URL.createObjectURL(blob);
    previewOpen.value = true;
  } catch (err) {
    if (err instanceof ApiError) {
      if (err.problem?.errors) errors.value = err.problem.errors;
      previewError.value =
        err.status === 404
          ? 'That report no longer exists.'
          : err.status === 403
            ? 'You are not allowed to view that report.'
            : err.status === 400
              ? 'The layout is not valid; see the messages.'
              : 'Unable to build the preview.';
    } else {
      previewError.value = 'Unable to build the preview.';
    }
  } finally {
    previewing.value = false;
  }
}

function closePreviewUrl() {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
  previewUrl.value = null;
}
onBeforeUnmount(closePreviewUrl);

const serverMessages = computed(() => (errors.value ? Object.values(errors.value).flat() : []));
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Rules', to: { name: 'admin-settings' } }, { title: 'R47 designer' }]">
    <div class="d-flex align-baseline ga-3 flex-wrap mb-4">
      <h1 class="page-title">R47 designer</h1>
      <span class="text-body-2 text-medium-emphasis">
        Sizes, column widths, type and fixed texts of the R47. Both PDF engines print this layout. Nothing changes until you save.
      </span>
    </div>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-else-if="error" type="error">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
    </v-alert>

    <template v-else-if="state && design">
      <div class="d-flex align-center flex-wrap ga-2 mb-3">
        <v-chip size="small" :color="state.isSaved ? 'primary' : undefined" variant="tonal">
          {{ state.isSaved ? 'Saved layout' : 'Standard form' }}
        </v-chip>
        <span v-if="state.isSaved && state.updatedAt" class="text-caption text-medium-emphasis">
          Saved {{ formatDateTime(state.updatedAt) }}<template v-if="state.updatedByName"> by {{ state.updatedByName }}</template>
        </span>
        <v-chip v-if="dirty" size="small" color="warning" variant="tonal">Unsaved changes</v-chip>
        <v-spacer />
        <v-btn class="press" variant="text" prepend-icon="mdi-undo" :disabled="!undoStack.length || saving" @click="undo">Undo</v-btn>
        <v-btn class="press" variant="text" :disabled="!dirty || saving" @click="discard">Discard changes</v-btn>
        <v-btn class="press" variant="text" :disabled="isStandard || saving" @click="resetToStandard">Reset to standard</v-btn>
        <v-btn class="press" color="primary" variant="flat" :disabled="!dirty || problems.length > 0" :loading="saving" @click="confirmSave = true">Save layout</v-btn>
      </div>

      <v-alert v-if="problems.length" type="warning" variant="tonal" density="compact" class="mb-3">
        <div class="font-weight-medium mb-1">Fix these before saving or previewing:</div>
        <ul class="pl-4"><li v-for="p in problems" :key="p">{{ p }}</li></ul>
      </v-alert>
      <v-alert v-else-if="serverMessages.length" type="error" variant="tonal" density="compact" class="mb-3" closable @click:close="errors = null">
        <ul class="pl-4"><li v-for="m in serverMessages" :key="m">{{ m }}</li></ul>
      </v-alert>

      <v-row>
        <v-col cols="12" :md="8">
          <v-card variant="outlined" class="pa-2 canvas-card">
            <R47DesignCanvas
              :model-value="design"
              :selected="section"
              @update:model-value="update"
              @select="(s) => (section = s)"
              @checkpoint="checkpoint"
            />
          </v-card>
          <p class="text-caption text-medium-emphasis mt-2">
            Drag a band edge or column divider, or select one with Tab and move it with the arrow keys (Shift: 5 pt). Click a band to open its values.
            The drawing is a guide; Preview shows the real print.
          </p>

          <v-card variant="outlined" class="mt-4">
            <div class="card-h"><span class="card-t">Preview</span></div>
            <v-card-text>
              <v-btn-toggle v-model="source" density="compact" variant="outlined" divided mandatory class="mb-3">
                <v-btn value="sample" prepend-icon="mdi-file-document-outline">Sample report</v-btn>
                <v-btn value="report" prepend-icon="mdi-file-search-outline">A report</v-btn>
              </v-btn-toggle>
              <v-autocomplete
                v-if="source === 'report'"
                v-model="picked"
                v-model:search="reportSearch"
                :items="reports"
                :item-title="reportTitle"
                item-value="id"
                return-object
                no-filter
                clearable
                :loading="reportsLoading"
                label="Report"
                placeholder="Employee name, purpose or ER reference"
                density="compact"
                class="mb-3"
                hide-details="auto"
                :error-messages="errors?.referenceNo"
                no-data-text="No report found that you may view"
              >
                <template #item="{ props: itemProps, item }">
                  <v-list-item v-bind="itemProps" :subtitle="reportSubtitle(item.raw)" />
                </template>
              </v-autocomplete>
              <div class="d-flex flex-wrap align-center ga-3">
                <v-btn-toggle v-model="engine" density="compact" variant="outlined" divided mandatory>
                  <v-btn value="Rules">Rules page engine</v-btn>
                  <v-btn value="QuestPdf">QuestPDF</v-btn>
                  <v-btn value="PdfSharp">PDFsharp</v-btn>
                </v-btn-toggle>
                <v-btn
                  class="press"
                  color="primary"
                  variant="tonal"
                  prepend-icon="mdi-file-pdf-box"
                  :loading="previewing"
                  :disabled="!canPreview || problems.length > 0"
                  @click="preview"
                >Preview PDF</v-btn>
              </div>
              <p class="text-caption text-medium-emphasis mt-2">
                <template v-if="source === 'sample'">
                  Made-up data, no real employee: several dates and Work With groups, long particulars, a cash advance, signature stamps and a second page.
                </template>
                <template v-else>
                  Any report you may view, drafts included (a report gets its ER reference when it is filed).
                </template>
                Prints with this layout, saved or not; nothing is saved or sent.
              </p>
              <v-alert v-if="previewError" type="error" variant="tonal" density="compact" class="mt-2">{{ previewError }}</v-alert>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="12" :md="4">
          <div :class="{ 'panel-sticky': mdAndUp }">
            <R47DesignPanel
              :model-value="design"
              :section="section"
              :errors="errors"
              :disabled="saving"
              @update:model-value="update"
              @update:section="(s) => (section = s)"
              @checkpoint="checkpoint"
            />
          </div>
        </v-col>
      </v-row>
    </template>

    <AppConfirmDialog
      v-model="confirmSave"
      title="Save the R47 layout?"
      message="Every R47 printed or emailed from now on uses this layout, with either PDF engine. Reports already printed keep their old copies."
      confirm-text="Save layout"
      :loading="saving"
      @confirm="save"
    />

    <v-dialog v-model="previewOpen" fullscreen @after-leave="closePreviewUrl">
      <v-card>
        <v-toolbar density="compact" color="surface">
          <v-toolbar-title class="text-subtitle-1">R47 preview · {{ previewTitle }}</v-toolbar-title>
          <v-btn icon="mdi-close" aria-label="Close preview" @click="previewOpen = false" />
        </v-toolbar>
        <iframe v-if="previewUrl" :src="previewUrl" title="R47 preview" class="preview-frame" />
      </v-card>
    </v-dialog>
  </AuthenticatedLayout>
</template>

<style scoped>
.canvas-card {
  background: rgba(var(--v-theme-on-surface), 0.04);
}
.panel-sticky {
  position: sticky;
  top: 80px;
  max-height: calc(100vh - 100px);
  overflow-y: auto;
}
.preview-frame {
  border: 0;
  width: 100%;
  height: calc(100vh - 48px);
  display: block;
}
</style>
