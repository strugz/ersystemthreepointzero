<script setup lang="ts">
import { computed } from 'vue';
import { formatDate, formatMoney } from '@/shared/format';
import type { CashAdvanceLine } from '../types';

// The CASH ADVANCE RECEIVED block as a small ledger: one row per advance, TOTAL at the foot.
const props = withDefaults(defineProps<{ lines: CashAdvanceLine[]; editable: boolean; busyId?: number | null }>(), { busyId: null });

const emit = defineEmits<{
  add: [];
  edit: [line: CashAdvanceLine];
  remove: [line: CashAdvanceLine];
}>();

const total = computed(() => props.lines.reduce((s, l) => s + l.amount, 0));
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">Cash advance received</span>
        <span class="card-meta">{{ lines.length }} row{{ lines.length === 1 ? '' : 's' }}</span>
      </div>
      <v-btn v-if="editable" class="press" size="small" variant="outlined" prepend-icon="mdi-plus" @click="emit('add')">Add advance</v-btn>
    </div>

    <div v-if="lines.length === 0" class="pa-5 text-body-2 text-medium-emphasis">
      No cash advance recorded yet. A Liquidation of Cash Advance needs at least one before filing.
    </div>

    <div v-else class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th style="width: 120px">Date</th>
            <th>Reference document</th>
            <th style="width: 160px">Ref. no.</th>
            <th class="r" style="width: 130px">Amount</th>
            <th v-if="editable" style="width: 84px"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(l, i) in lines" :key="l.id" class="stagger-in" :style="{ '--i': Math.min(i, 8) }" :class="{ 'opacity-50': busyId === l.id }">
            <td>{{ formatDate(l.advanceDate) }}</td>
            <td>{{ l.referenceDocument || '—' }}</td>
            <td class="mono text-medium-emphasis">{{ l.referenceNo || '—' }}</td>
            <td class="r mono">{{ formatMoney(l.amount) }}</td>
            <td v-if="editable" class="r text-no-wrap">
              <v-btn class="press" icon="mdi-pencil-outline" size="x-small" variant="text" aria-label="Edit advance" :disabled="busyId === l.id" @click="emit('edit', l)" />
              <v-btn class="press" icon="mdi-close" size="x-small" variant="text" aria-label="Remove advance" :loading="busyId === l.id" @click="emit('remove', l)" />
            </td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="3" class="r">Total</td>
            <td class="r mono">{{ formatMoney(total) }}</td>
            <td v-if="editable"></td>
          </tr>
        </tfoot>
      </table>
    </div>
  </v-card>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.opacity-50 {
  opacity: 0.5;
}
</style>
