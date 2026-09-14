<script setup lang="ts">
import { computed } from 'vue';
import { formatDate } from '@/shared/format';
import { balanceDueTo, reportTypeLabel, usesCashAdvance, usesRevolvingFund } from '../labels';
import ReportHeaderForm from './ReportHeaderForm.vue';
import type { ExpenseReportDetail, ReportHeaderInput } from '../types';

// Read view of the header in R47-style definition fields; editing happens in a dialog so the
// document stays calm and the form only appears when asked for.
const props = defineProps<{ report: ExpenseReportDetail; editable: boolean; saving: boolean }>();
const emit = defineEmits<{ save: [input: ReportHeaderInput] }>();
const open = defineModel<boolean>('open', { default: false });

const initial = computed<ReportHeaderInput>(() => ({
  reportType: props.report.reportType,
  purpose: props.report.purpose ?? '',
  dateFrom: props.report.dateFrom,
  dateTo: props.report.dateTo,
  revolvingFundAmount: props.report.revolvingFundAmount,
}));

// `initial` above feeds the edit dialog; the read view below derives everything else from the report.

const cashTotal = computed(() => props.report.cashAdvanceTotal);
const showCash = computed(() => usesCashAdvance(props.report.reportType));
const showRevolving = computed(() => usesRevolvingFund(props.report.reportType));
const dueTo = computed(() => balanceDueTo(cashTotal.value - props.report.totalAmount));
const money = (v: number) => v.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const period = computed(() =>
  props.report.dateFrom || props.report.dateTo
    ? `${formatDate(props.report.dateFrom)} – ${formatDate(props.report.dateTo)}`
    : null,
);
</script>

<template>
  <v-card class="details">
    <div class="card-h">
      <span class="card-t">Report details</span>
      <div class="d-flex align-center ga-3">
        <span class="card-meta d-none d-sm-inline">Printed on the R47 header</span>
        <v-btn v-if="editable" class="press" size="small" variant="text" color="primary" prepend-icon="mdi-pencil-outline" @click="open = true">
          Edit
        </v-btn>
      </div>
    </div>

    <div class="details__grid">
      <div class="field"><span class="lbl">Report type</span><span class="val">{{ reportTypeLabel[report.reportType] }}</span></div>
      <div class="field"><span class="lbl">ERF reference no.</span><span class="val mono" :class="{ muted: !report.referenceNo }">{{ report.referenceNo || 'Assigned when filed' }}</span></div>
      <div v-if="showCash" class="field"><span class="lbl">Cash advance total</span><span class="val mono" :class="{ muted: cashTotal <= 0 }">{{ cashTotal > 0 ? `${money(cashTotal)} · ${report.cashAdvances.length} row${report.cashAdvances.length === 1 ? '' : 's'}` : 'None entered yet' }}</span></div>
      <div v-else-if="showRevolving" class="field"><span class="lbl">Revolving fund</span><span class="val mono" :class="{ muted: report.revolvingFundAmount <= 0 }">{{ report.revolvingFundAmount > 0 ? money(report.revolvingFundAmount) : 'Not entered' }}</span></div>
      <div v-else class="field"><span class="lbl">Cash advance</span><span class="val muted">None (reimbursement)</span></div>
      <div class="field details__wide"><span class="lbl">Purpose</span><span class="val" :class="{ muted: !report.purpose }">{{ report.purpose || 'Not set' }}</span></div>
      <div class="field"><span class="lbl">Period</span><span class="val" :class="{ muted: !period }">{{ period ?? 'Not set' }}</span></div>
      <div class="field"><span class="lbl">Balance due to</span><span class="val">{{ dueTo }}<span class="text-medium-emphasis"> · from the figures</span></span></div>
    </div>

    <v-dialog v-model="open" max-width="920" scrollable :persistent="saving">
      <v-card>
        <div class="card-h">
          <span class="card-t">Edit report details</span>
          <v-btn class="press" icon="mdi-close" variant="text" size="small" aria-label="Close" :disabled="saving" @click="open = false" />
        </div>
        <v-card-text class="pt-4">
          <ReportHeaderForm :initial="initial" :saving="saving" submit-text="Save changes" @submit="emit('save', $event)" />
        </v-card-text>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<style scoped>
.details__grid {
  padding: 8px 20px 20px;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px 28px;
}
.details__wide {
  grid-column: span 3;
}
@media (max-width: 959.98px) {
  .details__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px 24px;
    padding: 6px 18px 18px;
  }
  .details__wide {
    grid-column: span 2;
  }
}
@media (max-width: 599.98px) {
  .details__grid {
    grid-template-columns: 1fr;
    padding: 4px 16px 12px;
    gap: 0;
  }
  .details__wide {
    grid-column: span 1;
  }
  .details__grid .field {
    flex-direction: row;
    justify-content: space-between;
    align-items: baseline;
    gap: 12px;
    padding: 9px 0;
    border-bottom: 1px solid var(--line);
  }
  .details__grid .field:last-child {
    border-bottom: 0;
  }
  .details__grid .field .lbl {
    text-transform: none;
    letter-spacing: 0;
    font-size: 13px;
    font-weight: 400;
    flex: none;
  }
  .details__grid .field .val {
    border-bottom: 0;
    padding: 0;
    text-align: right;
    font-size: 13px;
  }
}
</style>
