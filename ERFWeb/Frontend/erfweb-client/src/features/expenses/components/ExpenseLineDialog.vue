<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { formatMoney, todayIso } from '@/shared/format';
import { categoryOptions, expenseTypeOptions, lineTotal, mealTypes, r47Column } from '../labels';
import type {
  EmployeeRates,
  ExpenseLine,
  ExpenseTransportItem,
  SaveExpenseLineInput,
  WorkWithOption,
} from '../types';

// Add/edit one expense line. Totals are previewed here but always recomputed by the server.
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    line?: ExpenseLine | null;
    defaultDate?: string | null;
    workWithOptions?: WorkWithOption[];
    rates?: EmployeeRates | null;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
  }>(),
  { line: null, defaultDate: null, workWithOptions: () => [], rates: null, saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: SaveExpenseLineInput];
  /** Rates depend on the expense date; the parent refetches them for the date shown. */
  'date-change': [date: string];
}>();

const blank = (): SaveExpenseLineInput => ({
  transactionDate: props.defaultDate ?? todayIso(),
  particulars: '',
  workWith: '',
  invoiceOrNo: '',
  expenseType: 'Local',
  category: 'Meals',
  amount: 0,
  vatAmount: 0,
  location: '',
  serviceNumber: '',
  instrument: '',
  serialNumber: '',
  isPerdiem: false,
  multiplier: 1,
  minusDays: 0,
  totalDays: null,
  remarks: '',
  meals: [],
  transports: [],
  rowVersion: null,
});

const form = reactive<SaveExpenseLineInput>(blank());
const selectedMeals = ref<string[]>([]);
const showDetails = ref(false);
const dateInput = ref<HTMLElement | null>(null);

/** Clears the form for the next line (date follows `defaultDate`) and puts the cursor back on Date. */
function reset() {
  Object.assign(form, blank());
  selectedMeals.value = [];
  showDetails.value = false;
  requestAnimationFrame(() => dateInput.value?.querySelector('input')?.focus());
}

defineExpose({ reset });

watch(
  () => props.modelValue,
  (open) => {
    if (!open) {
      return;
    }
    const l = props.line;
    Object.assign(form, blank());
    if (l) {
      Object.assign(form, {
        transactionDate: l.transactionDate,
        particulars: l.particulars ?? '',
        workWith: l.workWith ?? '',
        invoiceOrNo: l.invoiceOrNo ?? '',
        expenseType: l.expenseType,
        category: l.category,
        amount: l.amount,
        vatAmount: l.vatAmount,
        location: l.location ?? '',
        serviceNumber: l.serviceNumber ?? '',
        instrument: l.instrument ?? '',
        serialNumber: l.serialNumber ?? '',
        isPerdiem: l.isPerdiem,
        multiplier: l.multiplier,
        minusDays: l.minusDays ?? 0,
        totalDays: l.totalDays,
        remarks: l.remarks ?? '',
        meals: l.meals.map((m) => ({ ...m })),
        transports: l.transports.map((t) => ({ ...t })),
        rowVersion: l.rowVersion,
      } satisfies SaveExpenseLineInput);
    }
    selectedMeals.value = form.meals.map((m) => m.mealType);
    showDetails.value = !!(form.instrument || form.serialNumber || form.serviceNumber);
  },
);

// Fires on open (initial date) and on every date edit while open; the parent debounces the fetch.
watch(
  () => [props.modelValue, form.transactionDate] as const,
  ([open, date]) => {
    if (open && date) {
      emit('date-change', date);
    }
  },
);

const isEdit = computed(() => props.line !== null);
const hasInvoice = computed(() => (form.invoiceOrNo ?? '').trim().length > 0);
watch(hasInvoice, (has) => {
  if (!has) {
    form.vatAmount = 0;
  }
});

const effectiveMultiplier = computed(() => {
  if (form.isPerdiem) {
    return Math.max(0, (form.totalDays ?? 0) - (form.minusDays ?? 0));
  }
  return Math.max(1, Number(form.multiplier) || 1);
});
// Same rounding as the server: receipt-backed single lines keep centavos, everything else is whole pesos.
const previewTotal = computed(() => lineTotal(Number(form.amount) || 0, effectiveMultiplier.value, hasInvoice.value));
const perdiemText = computed(() => {
  if (!form.isPerdiem || !form.totalDays) {
    return '';
  }
  const minus = form.minusDays ?? 0;
  return minus > 0
    ? `(${form.totalDays}Days - ${minus}Days) × ${form.amount}`
    : `(${form.totalDays}Days) × ${form.amount}`;
});
const perdiemInvalid = computed(() => form.isPerdiem && effectiveMultiplier.value < 1);

// Meals: chips pick the meal types; the sum of configured rates is offered as the amount.
const rateFor: Record<string, keyof EmployeeRates> = {
  Breakfast: 'breakfastRate',
  Lunch: 'lunchRate',
  Dinner: 'dinnerRate',
  'OT Meal': 'otMealRate',
};
const suggestedMealAmount = computed(() =>
  props.rates ? selectedMeals.value.reduce((sum, m) => sum + (props.rates?.[rateFor[m]] ?? 0), 0) : 0,
);
watch(selectedMeals, (types) => {
  form.meals = types.map((mealType) => form.meals.find((m) => m.mealType === mealType) ?? { mealType, paidFor: null, paidEmployeeId: null });
  if (suggestedMealAmount.value > 0 && (!form.amount || form.amount === 0)) {
    form.amount = suggestedMealAmount.value;
  }
});

// Transportation: optional fare legs; their sum is offered as the amount.
function addLeg() {
  form.transports.push({ fareFrom: '', fareTo: '', fareAmount: 0 });
}
function removeLeg(index: number) {
  form.transports.splice(index, 1);
}
const legsTotal = computed(() => form.transports.reduce((s, t: ExpenseTransportItem) => s + (Number(t.fareAmount) || 0), 0));
function useLegsTotal() {
  form.amount = Math.round(legsTotal.value * 100) / 100;
}

const workWithItems = computed(() => props.workWithOptions.map((o) => o.fullName));

function fieldErrors(field: string): string[] | undefined {
  const all = props.errors ?? {};
  return all[field] ?? all[field.charAt(0).toUpperCase() + field.slice(1)];
}

const canSave = computed(
  () => (form.particulars ?? '').trim().length > 0 && Number(form.amount) > 0 && !perdiemInvalid.value && !props.saving,
);

function save() {
  if (!canSave.value) {
    return;
  }
  const isMeals = form.category === 'Meals';
  const isTransport = form.category === 'Transportation';
  emit('save', {
    ...form,
    amount: Number(form.amount),
    vatAmount: hasInvoice.value ? Number(form.vatAmount) || 0 : 0,
    multiplier: form.isPerdiem ? 1 : Math.max(1, Number(form.multiplier) || 1),
    totalDays: form.isPerdiem ? Number(form.totalDays) || null : null,
    minusDays: form.isPerdiem ? Number(form.minusDays) || 0 : null,
    meals: isMeals ? form.meals : [],
    transports: isTransport ? form.transports.filter((t) => t.fareFrom || t.fareTo || t.fareAmount) : [],
  });
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="860" scrollable :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="d-flex align-center">
        <span class="text-subtitle-1 font-weight-bold">{{ isEdit ? 'Edit expense line' : 'Add expense line' }}</span>
        <v-spacer />
        <v-chip size="small" variant="tonal" color="primary" label>{{ r47Column(form.category, form.expenseType) }} column</v-chip>
      </v-card-title>

      <v-card-text>
        <v-form :disabled="saving" @submit.prevent="save">
          <v-row dense>
            <v-col cols="12" sm="4">
              <v-text-field
                ref="dateInput"
                v-model="form.transactionDate"
                type="date"
                label="Date"
                :error-messages="fieldErrors('transactionDate')"
                autofocus
              />
            </v-col>
            <v-col cols="12" sm="4">
              <v-select v-model="form.category" :items="categoryOptions" label="Category" />
            </v-col>
            <v-col cols="12" sm="4">
              <v-select v-model="form.expenseType" :items="expenseTypeOptions" label="Expense type" />
            </v-col>

            <v-col cols="12" md="8">
              <v-text-field
                v-model="form.particulars"
                label="Particulars"
                maxlength="200"
                :error-messages="fieldErrors('particulars')"
                hint="e.g. SUCAT - NICHOLS (TOLL)"
                persistent-hint
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="form.location" label="Location" hint="e.g. MDMPI, CALMC" persistent-hint />
            </v-col>

            <v-col cols="12" md="6">
              <v-combobox
                v-model="form.workWith"
                :items="workWithItems"
                label="Work with"
                hint="Printed after the location exactly as typed, e.g. Work With JSO. Groups lines on the R47."
                persistent-hint
                clearable
              />
            </v-col>
            <v-col cols="12" sm="6" md="3">
              <v-text-field v-model="form.invoiceOrNo" label="Invoice / OR no." :error-messages="fieldErrors('invoiceOrNo')" />
            </v-col>
            <v-col cols="12" sm="6" md="3">
              <v-text-field
                v-model.number="form.vatAmount"
                type="number"
                min="0"
                step="0.01"
                label="VAT amount"
                :disabled="!hasInvoice"
                :hint="hasInvoice ? undefined : 'Enter an Invoice/OR no. to record VAT'"
                persistent-hint
                :error-messages="fieldErrors('vatAmount')"
              />
            </v-col>
          </v-row>

          <!-- Meals -->
          <v-expand-transition>
            <div v-if="form.category === 'Meals'" class="mb-2">
              <div class="text-overline text-medium-emphasis">Meals</div>
              <v-chip-group v-model="selectedMeals" multiple column selected-class="text-primary">
                <v-chip v-for="m in mealTypes" :key="m" :value="m" filter variant="outlined" class="press">
                  {{ m }}
                  <span v-if="rates && rates[rateFor[m]]" class="ml-1 text-medium-emphasis">· {{ formatMoney(rates[rateFor[m]]) }}</span>
                </v-chip>
              </v-chip-group>
              <div v-if="suggestedMealAmount > 0" class="text-caption text-medium-emphasis">
                Rates total {{ formatMoney(suggestedMealAmount) }}
                <v-btn class="press ml-1" size="x-small" variant="text" @click="form.amount = suggestedMealAmount">Use</v-btn>
              </div>
            </div>
          </v-expand-transition>

          <!-- Transportation legs -->
          <v-expand-transition>
            <div v-if="form.category === 'Transportation'" class="mb-2">
              <div class="d-flex align-center">
                <div class="text-overline text-medium-emphasis">Fare legs (optional)</div>
                <v-spacer />
                <v-btn class="press" size="small" variant="text" prepend-icon="mdi-plus" @click="addLeg">Add leg</v-btn>
              </div>
              <v-row v-for="(leg, i) in form.transports" :key="i" dense class="align-center">
                <v-col cols="12" sm="4"><v-text-field v-model="leg.fareFrom" label="From" density="compact" hide-details /></v-col>
                <v-col cols="12" sm="4"><v-text-field v-model="leg.fareTo" label="To" density="compact" hide-details /></v-col>
                <v-col cols="9" sm="3">
                  <v-text-field v-model.number="leg.fareAmount" type="number" min="0" step="0.01" label="Fare" density="compact" hide-details />
                </v-col>
                <v-col cols="3" sm="1" class="text-right">
                  <v-btn class="press" icon="mdi-close" size="small" variant="text" aria-label="Remove leg" @click="removeLeg(i)" />
                </v-col>
              </v-row>
              <div v-if="form.transports.length" class="text-caption text-medium-emphasis mt-1">
                Legs total {{ formatMoney(legsTotal) }}
                <v-btn class="press ml-1" size="x-small" variant="text" @click="useLegsTotal">Use</v-btn>
              </div>
            </div>
          </v-expand-transition>

          <!-- Amount & per-diem -->
          <v-row dense class="align-center">
            <v-col cols="12" sm="4">
              <v-text-field
                v-model.number="form.amount"
                type="number"
                min="0"
                step="0.01"
                :label="form.isPerdiem ? 'Daily amount' : 'Amount'"
                prefix="₱"
                :error-messages="fieldErrors('amount')"
              />
            </v-col>
            <v-col cols="12" sm="4">
              <v-switch v-model="form.isPerdiem" label="Per-diem / allowance" color="primary" hide-details inset />
            </v-col>
            <v-col v-if="!form.isPerdiem" cols="12" sm="4">
              <v-text-field v-model.number="form.multiplier" type="number" min="1" step="1" label="Multiplier" hide-details="auto" />
            </v-col>
            <template v-else>
              <v-col cols="6" sm="2">
                <v-text-field v-model.number="form.totalDays" type="number" min="1" step="1" label="# of days" :error-messages="fieldErrors('totalDays')" />
              </v-col>
              <v-col cols="6" sm="2">
                <v-text-field
                  v-model.number="form.minusDays"
                  type="number"
                  min="0"
                  step="1"
                  label="− days"
                  :error-messages="perdiemInvalid ? ['0 or less than 0 multiplier is not allowed'] : fieldErrors('minusDays')"
                />
              </v-col>
            </template>
          </v-row>

          <div class="d-flex align-center flex-wrap ga-2 mb-2">
            <v-chip size="small" label variant="tonal">× {{ effectiveMultiplier }}</v-chip>
            <v-chip size="small" label variant="tonal" color="primary" class="font-weight-bold">Total {{ formatMoney(previewTotal) }}</v-chip>
            <span v-if="perdiemText" class="text-caption text-medium-emphasis">{{ perdiemText }}</span>
          </div>

          <!-- Instrument details -->
          <v-btn class="press px-0" variant="text" size="small" :append-icon="showDetails ? 'mdi-chevron-up' : 'mdi-chevron-down'" @click="showDetails = !showDetails">
            Instrument / service details
          </v-btn>
          <v-expand-transition>
            <v-row v-if="showDetails" dense>
              <v-col cols="12" sm="4"><v-text-field v-model="form.instrument" label="Instrument" density="compact" /></v-col>
              <v-col cols="12" sm="4"><v-text-field v-model="form.serialNumber" label="Serial no." density="compact" /></v-col>
              <v-col cols="12" sm="4"><v-text-field v-model="form.serviceNumber" label="Service no." density="compact" /></v-col>
            </v-row>
          </v-expand-transition>

          <v-textarea v-model="form.remarks" label="Remarks" rows="1" auto-grow class="mt-2" />
        </v-form>
      </v-card-text>

      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">
          {{ isEdit ? 'Save line' : 'Add line' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
