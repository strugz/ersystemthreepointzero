<script setup lang="ts">
import AppMoney from '@/shared/components/AppMoney.vue'
import { resolveExpenseAmount } from './amounts'
import { hasDisplayMoney, hasDisplayText } from './detailPresentation'
import ManagerExpenseDetails from './ManagerExpenseDetails.vue'
import type { ExpenseLine } from './types'

const props = defineProps<{
  expense: ExpenseLine
  index: number
  value: string
}>()
</script>

<template>
  <v-expansion-panel
    class="manager-expense-card border"
    :value="value"
  >
    <v-expansion-panel-title class="manager-expense-card__title">
      <div class="manager-expense-card__header">
        <div class="manager-expense-card__identity">
          <span class="manager-expense-card__sequence">Expense {{ index + 1 }}</span>
          <div
            v-if="hasDisplayText(props.expense.particulars)"
            class="manager-expense-card__particulars"
          >
            {{ props.expense.particulars }}
          </div>
          <v-chip
            v-if="hasDisplayText(props.expense.category)"
            class="manager-expense-card__category"
            color="primary"
            label
            size="small"
            variant="tonal"
          >
            {{ props.expense.category }}
          </v-chip>
        </div>
        <div class="manager-expense-card__amount">
          <span class="manager-expense-card__amount-label">Filed amount</span>
          <strong><AppMoney :value="resolveExpenseAmount(props.expense)" /></strong>
          <span
            v-if="hasDisplayMoney(props.expense.vatAmount)"
            class="manager-expense-card__vat"
          >
            VAT <AppMoney :value="props.expense.vatAmount" />
          </span>
        </div>
      </div>
    </v-expansion-panel-title>
    <v-expansion-panel-text>
      <ManagerExpenseDetails :expense="props.expense" />
    </v-expansion-panel-text>
  </v-expansion-panel>
</template>
