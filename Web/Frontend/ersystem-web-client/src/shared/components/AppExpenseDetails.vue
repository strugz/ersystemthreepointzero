<script setup lang="ts">
import AppDate from '@/shared/components/AppDate.vue'
import AppMoney from '@/shared/components/AppMoney.vue'
import type { ExpenseLine } from '@/shared/types/reportReview'
import { resolveExpenseAmount } from '@/shared/utils/reportAmounts'
import {
  hasDisplayMoney,
  hasDisplayNumber,
  hasDisplayText,
  hasExpenseDetailText
} from '@/shared/utils/reportReview'

const props = defineProps<{ expense: ExpenseLine }>()
</script>

<template>
  <div class="report-expense-details">
    <div v-if="hasDisplayText(props.expense.transactionDate)">
      <span class="field-label">Transaction date</span>
      <AppDate :value="props.expense.transactionDate" />
    </div>
    <div v-if="hasExpenseDetailText(props.expense.invoiceNumber)">
      <span class="field-label">Invoice/OR number</span>
      {{ props.expense.invoiceNumber }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.expenseType)">
      <span class="field-label">Expense type</span>
      {{ props.expense.expenseType }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.category)">
      <span class="field-label">Category</span>
      {{ props.expense.category }}
    </div>
    <div v-if="hasDisplayMoney(props.expense.amount)">
      <span class="field-label">Base amount</span>
      <AppMoney :value="props.expense.amount" />
    </div>
    <div v-if="hasDisplayMoney(props.expense.vatAmount)">
      <span class="field-label">VAT amount</span>
      <AppMoney :value="props.expense.vatAmount" />
    </div>
    <div>
      <span class="field-label">Filed amount</span>
      <strong class="text-primary"><AppMoney :value="resolveExpenseAmount(props.expense)" /></strong>
    </div>
    <div v-if="hasDisplayNumber(props.expense.multiplier)">
      <span class="field-label">Multiplier</span>
      {{ props.expense.multiplier }}
    </div>
    <div v-if="props.expense.isPerDiem">
      <span class="field-label">Per diem</span>
      <v-chip
        color="primary"
        label
        size="small"
        variant="tonal"
      >
        Included
      </v-chip>
    </div>
    <div v-if="hasExpenseDetailText(props.expense.location)">
      <span class="field-label">Location</span>
      {{ props.expense.location }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.workWith)">
      <span class="field-label">Work with</span>
      {{ props.expense.workWith }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.instrument)">
      <span class="field-label">Instrument</span>
      {{ props.expense.instrument }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.serialNumber)">
      <span class="field-label">Serial number</span>
      {{ props.expense.serialNumber }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.serviceNumber)">
      <span class="field-label">Service number</span>
      {{ props.expense.serviceNumber }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.totalDays)">
      <span class="field-label">Total days</span>
      {{ props.expense.totalDays }}
    </div>
    <div v-if="hasExpenseDetailText(props.expense.minusDays)">
      <span class="field-label">Less days</span>
      {{ props.expense.minusDays }}
    </div>
    <div
      v-if="hasExpenseDetailText(props.expense.computation)"
      class="report-expense-details__wide report-expense-details__note"
    >
      <span class="field-label">Computation</span>
      {{ props.expense.computation }}
    </div>
    <div
      v-if="hasExpenseDetailText(props.expense.remarks)"
      class="report-expense-details__wide report-expense-details__note"
    >
      <span class="field-label">Employee remarks</span>
      {{ props.expense.remarks }}
    </div>
  </div>
</template>
