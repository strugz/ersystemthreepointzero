<script setup lang="ts">
import AppExpenseDetails from '@/shared/components/AppExpenseDetails.vue'
import AppMoney from '@/shared/components/AppMoney.vue'
import type { ExpenseLine } from '@/shared/types/reportReview'
import { resolveExpenseAmount } from '@/shared/utils/reportAmounts'
import { hasDisplayMoney, hasDisplayText } from '@/shared/utils/reportReview'

const props = defineProps<{
  expense: ExpenseLine
  index: number
  value: string
}>()
</script>

<template>
  <v-expansion-panel
    class="report-expense-card border"
    :value="value"
  >
    <v-expansion-panel-title class="report-expense-card__title">
      <div class="report-expense-card__header">
        <div class="report-expense-card__identity">
          <span class="report-expense-card__sequence">Expense {{ index + 1 }}</span>
          <div
            v-if="hasDisplayText(props.expense.particulars)"
            class="report-expense-card__particulars"
          >
            {{ props.expense.particulars }}
          </div>
          <v-chip
            v-if="hasDisplayText(props.expense.category)"
            class="report-expense-card__category"
            color="primary"
            label
            size="small"
            variant="tonal"
          >
            {{ props.expense.category }}
          </v-chip>
        </div>
        <div class="report-expense-card__amount">
          <span class="report-expense-card__amount-label">Filed amount</span>
          <strong><AppMoney :value="resolveExpenseAmount(props.expense)" /></strong>
          <span
            v-if="hasDisplayMoney(props.expense.vatAmount)"
            class="report-expense-card__vat"
          >
            VAT <AppMoney :value="props.expense.vatAmount" />
          </span>
        </div>
      </div>
    </v-expansion-panel-title>
    <v-expansion-panel-text>
      <AppExpenseDetails :expense="props.expense" />
    </v-expansion-panel-text>
  </v-expansion-panel>
</template>
