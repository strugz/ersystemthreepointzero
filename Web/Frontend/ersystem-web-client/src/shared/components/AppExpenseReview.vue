<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import AppDate from '@/shared/components/AppDate.vue'
import AppExpenseCard from '@/shared/components/AppExpenseCard.vue'
import AppExpenseDetails from '@/shared/components/AppExpenseDetails.vue'
import AppMoney from '@/shared/components/AppMoney.vue'
import type { ExpenseLine } from '@/shared/types/reportReview'
import { resolveExpenseAmount } from '@/shared/utils/reportAmounts'
import {
  createExpenseTableHeaders,
  expenseLineCountLabel,
  expensePresentationKey,
  hasDisplayMoney,
  hasDisplayText
} from '@/shared/utils/reportReview'

type ExpenseTableRow = ExpenseLine & { presentationKey: string }

const props = defineProps<{
  expenses: ExpenseLine[]
  desktop: boolean
}>()

const expanded = ref<string[]>([])
const headers = computed(() => createExpenseTableHeaders(props.expenses))
const rows = computed<ExpenseTableRow[]>(() => props.expenses.map((expense, index) => ({
  ...expense,
  presentationKey: expensePresentationKey(expense, index)
})))

watch(() => props.expenses, expenses => {
  const firstExpense = expenses[0]
  expanded.value = firstExpense ? [expensePresentationKey(firstExpense, 0)] : []
}, { immediate: true })
</script>

<template>
  <v-card
    v-if="expenses.length"
    class="mb-4 border"
    variant="flat"
  >
    <v-card-title class="report-expenses-heading">
      <span>Expenses</span>
      <v-chip
        color="primary"
        size="small"
        variant="tonal"
      >
        {{ expenseLineCountLabel(expenses.length) }}
      </v-chip>
    </v-card-title>
    <v-data-table
      v-if="desktop"
      v-model:expanded="expanded"
      class="report-expense-table"
      :headers="headers"
      :items="rows"
      density="comfortable"
      item-value="presentationKey"
      show-expand
    >
      <template #item.transactionDate="{ item }">
        <AppDate
          v-if="hasDisplayText(item.transactionDate)"
          :value="item.transactionDate"
        />
      </template>
      <template #item.amount="{ item }">
        <strong class="report-expense-table__amount text-primary"><AppMoney :value="resolveExpenseAmount(item)" /></strong>
      </template>
      <template #item.vatAmount="{ item }">
        <AppMoney
          v-if="hasDisplayMoney(item.vatAmount)"
          :value="item.vatAmount"
        />
      </template>
      <template #expanded-row="{ columns, item }">
        <tr class="report-expense-table__expanded-row">
          <td :colspan="columns.length">
            <AppExpenseDetails :expense="item" />
          </td>
        </tr>
      </template>
    </v-data-table>
    <v-expansion-panels
      v-else
      v-model="expanded"
      class="expense-mobile-list"
      multiple
    >
      <AppExpenseCard
        v-for="(expense, index) in expenses"
        :key="expense.id ?? `${expense.transactionDate}-${expense.particulars}`"
        :expense="expense"
        :index="index"
        :value="expensePresentationKey(expense, index)"
      />
    </v-expansion-panels>
  </v-card>
</template>
