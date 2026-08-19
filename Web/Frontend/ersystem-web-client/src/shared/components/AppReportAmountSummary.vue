<script setup lang="ts">
import { computed } from 'vue'
import AppMoney from '@/shared/components/AppMoney.vue'
import type { CashAdvance, ExpenseLine } from '@/shared/types/reportReview'
import { calculateReportAmounts } from '@/shared/utils/reportAmounts'
import { hasAmountSummary } from '@/shared/utils/reportReview'

const props = defineProps<{
  expenses: ExpenseLine[]
  cashAdvance: CashAdvance | null
}>()

const amounts = computed(() => calculateReportAmounts(props.expenses, props.cashAdvance))
</script>

<template>
  <v-card
    v-if="hasAmountSummary(expenses, cashAdvance)"
    class="amount-summary border mb-4"
    variant="flat"
  >
    <v-card-text class="pa-0">
      <div class="amount-summary__primary">
        <div>
          <span class="field-label">Balance due</span>
          <span class="amount-summary__payee">Due to <strong>{{ amounts.balanceDueTo }}</strong></span>
        </div>
        <span class="amount-summary__total"><AppMoney :value="amounts.balanceDueAmount" /></span>
      </div>
      <v-divider />
      <div class="amount-summary__breakdown">
        <div class="amount-summary__metric">
          <span class="field-label">Total filed expenses</span>
          <strong><AppMoney :value="amounts.filedExpenses" /></strong>
        </div>
        <div class="amount-summary__metric">
          <span class="field-label">Cash advance</span>
          <strong><AppMoney :value="amounts.cashAdvanceAmount" /></strong>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>
