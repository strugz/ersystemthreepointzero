<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { formatMoney, todayIso } from '@/shared/format';
import { categoryDefaults, categoryOptions, expenseTypeOptions, fareModeIcon, lineTotal, mealTypes, r47Column } from '../labels';
import CrmActivityPicker from './CrmActivityPicker.vue';
import type {
  CrmActivityOption,
  CrmActivityPickList,
  EmployeeRates,
  ExpenseCategory,
  ExpenseLine,
  ExpenseTransportItem,
  FareModeOption,
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
    /** Active fare modes, in display order. */
    fareModes?: FareModeOption[];
    saving?: boolean;
    errors?: Record<string, string[]> | null;
    /** The employee is linked to the CRM: offer "From CRM". */
    crmAvailable?: boolean;
    /** The employee's CRM activities on the line's date; loaded by the parent on `crm-load`. */
    crmList?: CrmActivityPickList | null;
    crmLoading?: boolean;
    crmError?: string | null;
  }>(),
  {
    line: null,
    defaultDate: null,
    workWithOptions: () => [],
    rates: null,
    fareModes: () => [],
    saving: false,
    errors: null,
    crmAvailable: false,
    crmList: null,
    crmLoading: false,
    crmError: null,
  },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: SaveExpenseLineInput];
  /** Rates depend on the expense date; the parent refetches them for the date shown. */
  'date-change': [date: string];
  /** The employee opened the CRM picker; the parent loads the activities of that day. */
  'crm-load': [date: string];
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
  crmActivityId: null,
});

const form = reactive<SaveExpenseLineInput>(blank());
const selectedMeals = ref<string[]>([]);
const showDetails = ref(false);
/** Set by a save attempt, so a leg without a mode is only flagged once the employee tries to save. */
const triedSave = ref(false);
const dateInput = ref<HTMLElement | null>(null);

// ---- category defaults (new lines only) ----------------------------------------------
// `auto` remembers what the form filled in by itself. A field is replaced on a category change only
// while it is still empty or still holds that auto value, so anything the employee typed is kept.
const auto = reactive({ particulars: '', amount: 0 });

function applyCategoryDefaults() {
  if (props.line !== null) {
    return;
  }
  const defaults = categoryDefaults[form.category];
  const particulars = (form.particulars ?? '').trim();
  if (!particulars || particulars === auto.particulars) {
    form.particulars = defaults.particulars;
    auto.particulars = defaults.particulars;
  }
  const amount = Number(form.amount) || 0;
  if (amount === auto.amount) {
    form.amount = 0;
    auto.amount = 0;
  }
  if (form.category === 'Meals' && selectedMeals.value.length === 0) {
    selectedMeals.value = [...defaults.meals];
  }
  if (defaults.fareLeg && form.transports.length === 0) {
    form.transports.push(newLeg());
  }
  syncMealAmount();
}

/** Clears the form for the next line (date follows `defaultDate`) and puts the cursor back on Date. */
function reset() {
  Object.assign(form, blank());
  Object.assign(auto, { particulars: '', amount: 0 });
  selectedMeals.value = [];
  showDetails.value = false;
  triedSave.value = false;
  pickedActivity.value = null;
  applyCategoryDefaults();
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
        crmActivityId: l.crmActivityId ?? null,
      } satisfies SaveExpenseLineInput);
    }
    selectedMeals.value = form.meals.map((m) => m.mealType);
    showDetails.value = !!(form.remarks || form.instrument || form.serialNumber || form.serviceNumber);
    triedSave.value = false;
    pickedActivity.value = null;
    Object.assign(auto, { particulars: '', amount: 0 });
    applyCategoryDefaults();
  },
);

watch(
  () => form.category,
  () => {
    applyCategoryDefaults();
    fillLegFromActivity();
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
/**
 * Offers the meal rates as the amount: when the amount is empty, or (on a new line) still the rate total
 * filled in earlier. Re-runs when the chips change and when the rates arrive for the expense date.
 */
function syncMealAmount() {
  const suggested = suggestedMealAmount.value;
  if (form.category !== 'Meals' || suggested <= 0) {
    return;
  }
  const amount = Number(form.amount) || 0;
  if (amount === 0 || (props.line === null && amount === auto.amount)) {
    form.amount = suggested;
    auto.amount = suggested;
  }
}
watch(selectedMeals, (types) => {
  form.meals = types.map((mealType) => form.meals.find((m) => m.mealType === mealType) ?? { mealType, paidFor: null, paidEmployeeId: null });
  syncMealAmount();
});
watch(suggestedMealAmount, syncMealAmount);

// Transportation: optional fare legs; their sum is offered as the amount. Each leg names how it was
// travelled. A new leg starts where the previous one ended, by the same mode, since most trips repeat it.
function newLeg(previous?: ExpenseTransportItem): ExpenseTransportItem {
  return {
    fareFrom: previous?.fareTo ?? '',
    fareTo: '',
    fareAmount: 0,
    fareModeId: previous?.fareModeId ?? null,
    fareModeDetail: null,
  };
}
function addLeg() {
  form.transports.push(newLeg(form.transports[form.transports.length - 1]));
}
function removeLeg(index: number) {
  form.transports.splice(index, 1);
}
// ---- CRM activity ------------------------------------------------------------------
// Picking an activity fills what the CRM knows (date, client, service details, work-with, the trip);
// the employee still chooses the category and enters the amount, Invoice/OR and VAT.
const crmOpen = ref(false);
const pickedActivity = ref<CrmActivityOption | null>(null);

function openCrm() {
  crmOpen.value = true;
  emit('crm-load', form.transactionDate);
}

function applyActivity(a: CrmActivityOption) {
  pickedActivity.value = a;
  form.crmActivityId = a.activityId;
  form.transactionDate = a.activityDate;
  const particulars = (a.subject ?? a.activityType ?? '').slice(0, 200);
  if (particulars) form.particulars = particulars;
  form.location = a.customerName ?? form.location;
  form.workWith = a.workWith ?? form.workWith;
  form.serviceNumber = a.serviceNumber ?? form.serviceNumber;
  form.instrument = a.instrumentName ?? form.instrument;
  form.serialNumber = a.serialNumber ?? form.serialNumber;
  if (form.serviceNumber || form.instrument || form.serialNumber) showDetails.value = true;
  fillLegFromActivity();
}

/** On a Transportation line, the picked activity's trip fills the fare leg when no leg has a route yet. */
function fillLegFromActivity() {
  const a = pickedActivity.value;
  if (!a || form.category !== 'Transportation' || !(a.travelOrigin || a.travelDestination)) return;
  if (form.transports.some((t) => t.fareFrom || t.fareTo)) return;
  if (!form.transports.length) form.transports.push(newLeg());
  const leg = form.transports[0];
  if (!leg) return;
  leg.fareFrom = a.travelOrigin ?? '';
  leg.fareTo = a.travelDestination ?? '';
}

function unlinkActivity() {
  form.crmActivityId = null;
  pickedActivity.value = null;
}

const legsTotal = computed(() => form.transports.reduce((s, t: ExpenseTransportItem) => s + (Number(t.fareAmount) || 0), 0));

const modeById = computed(() => new Map(props.fareModes.map((m) => [m.id, m])));
/**
 * The picker lists the active modes, plus the leg's own mode when it has since been deactivated, so an
 * old line still shows (and keeps) what was saved.
 */
function modeItems(leg: ExpenseTransportItem): FareModeOption[] {
  if (leg.fareModeId === null || modeById.value.has(leg.fareModeId)) {
    return props.fareModes;
  }
  const retired: FareModeOption = {
    id: leg.fareModeId,
    code: leg.fareModeCode ?? '',
    name: leg.fareModeName ?? 'Unavailable mode',
    requiresDetail: !!leg.fareModeDetail,
    receiptExpected: false,
  };
  return [...props.fareModes, retired];
}
function modeOf(leg: ExpenseTransportItem): FareModeOption | undefined {
  return leg.fareModeId === null ? undefined : modeItems(leg).find((m) => m.id === leg.fareModeId);
}
const needsDetail = (leg: ExpenseTransportItem) => !!modeOf(leg)?.requiresDetail;
/** A leg is kept on save when anything on it was filled in. */
const legHasContent = (t: ExpenseTransportItem) => !!(t.fareFrom || t.fareTo || t.fareAmount || t.fareModeId);
function legModeErrors(leg: ExpenseTransportItem, i: number): string[] | undefined {
  const server = fieldErrors(`transports[${i}].fareModeId`);
  if (server) {
    return server;
  }
  return triedSave.value && legHasContent(leg) && leg.fareModeId === null ? ['Choose a mode'] : undefined;
}
function legDetailErrors(leg: ExpenseTransportItem, i: number): string[] | undefined {
  const server = fieldErrors(`transports[${i}].fareModeDetail`);
  if (server) {
    return server;
  }
  return triedSave.value && !(leg.fareModeDetail ?? '').trim() ? ['Type the mode'] : undefined;
}
const legsIncomplete = computed(
  () =>
    form.category === 'Transportation' &&
    form.transports.some((t) => legHasContent(t) && (t.fareModeId === null || (needsDetail(t) && !(t.fareModeDetail ?? '').trim()))),
);
function useLegsTotal() {
  form.amount = Math.round(legsTotal.value * 100) / 100;
}

const workWithItems = computed(() => props.workWithOptions.map((o) => o.fullName));

const categoryIcon: Record<ExpenseCategory, string> = {
  Meals: 'mdi-silverware-fork-knife',
  Transportation: 'mdi-car-outline',
  Toll: 'mdi-boom-gate-outline',
  Parking: 'mdi-parking',
  Others: 'mdi-dots-horizontal',
  Representation: 'mdi-account-tie-outline',
};

/** How the footer total is made up, e.g. "150.00 × 2" or the per-diem days; empty when it is just the amount. */
const amountFormula = computed(() => {
  if (form.isPerdiem) {
    return perdiemText.value;
  }
  return effectiveMultiplier.value > 1 ? `${formatMoney(Number(form.amount) || 0)} × ${effectiveMultiplier.value}` : '';
});

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
  if (legsIncomplete.value) {
    triedSave.value = true;
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
    transports: isTransport
      ? form.transports.filter(legHasContent).map((t) => ({
          fareFrom: t.fareFrom,
          fareTo: t.fareTo,
          fareAmount: Number(t.fareAmount) || 0,
          fareModeId: t.fareModeId,
          fareModeDetail: needsDetail(t) ? (t.fareModeDetail ?? '').trim() || null : null,
        }))
      : [],
  });
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="760" scrollable :persistent="saving" @update:model-value="close">
    <v-card class="eline">
      <header class="eline__head">
        <div>
          <h2 class="eline__title">{{ isEdit ? 'Edit expense line' : 'Add expense line' }}</h2>
          <p class="eline__sub">Goes in the <span class="mono">{{ r47Column(form.category, form.expenseType) }}</span> column of the R47</p>
        </div>
        <v-btn class="press" icon="mdi-close" variant="text" size="small" aria-label="Close" :disabled="saving" @click="close" />
      </header>

      <v-card-text class="eline__body">
        <v-form :disabled="saving" @submit.prevent="save">
          <!-- When and what -->
          <section class="eline__section">
            <div class="eline__row eline__row--when">
              <v-text-field
                ref="dateInput"
                v-model="form.transactionDate"
                type="date"
                label="Date"
                hide-details="auto"
                :error-messages="fieldErrors('transactionDate')"
                autofocus
              />
              <div class="seg" role="group" aria-label="Expense type">
                <button
                  v-for="t in expenseTypeOptions"
                  :key="t.value"
                  type="button"
                  class="seg__btn press"
                  :class="{ 'seg__btn--on': form.expenseType === t.value }"
                  :aria-pressed="form.expenseType === t.value"
                  @click="form.expenseType = t.value"
                >
                  {{ t.value === 'Local' ? 'Local' : 'Foreign' }}
                </button>
              </div>
            </div>

            <div class="cats" role="group" aria-label="Category">
              <button
                v-for="c in categoryOptions"
                :key="c.value"
                type="button"
                class="cat press"
                :class="{ 'cat--on': form.category === c.value }"
                :aria-pressed="form.category === c.value"
                @click="form.category = c.value"
              >
                <v-icon :icon="categoryIcon[c.value]" size="20" />
                <span>{{ c.title }}</span>
              </button>
            </div>
          </section>

          <!-- Details -->
          <section class="eline__section">
            <div class="eline__details-head">
              <div class="eyebrow">Details</div>
              <v-btn
                v-if="crmAvailable"
                class="press"
                size="small"
                variant="tonal"
                color="primary"
                prepend-icon="mdi-briefcase-search-outline"
                @click="openCrm"
              >
                {{ form.crmActivityId ? 'Change CRM activity' : 'From CRM' }}
              </v-btn>
            </div>
            <div v-if="form.crmActivityId" class="crmlink">
              <v-chip size="small" label variant="tonal" color="primary" closable prepend-icon="mdi-link-variant" @click:close="unlinkActivity">
                <span class="mono">{{ form.crmActivityId }}</span>
              </v-chip>
              <span class="crmlink__hint">Linked CRM activity. Remove the link to type this line by hand.</span>
            </div>
            <p v-if="fieldErrors('crmActivityId')" class="crmlink__error" role="alert">{{ fieldErrors('crmActivityId')?.join(' ') }}</p>
            <v-text-field
              v-model="form.particulars"
              label="Particulars"
              placeholder="e.g. SUCAT - NICHOLS"
              maxlength="200"
              hide-details="auto"
              :error-messages="fieldErrors('particulars')"
            />
            <div class="eline__row">
              <v-text-field v-model="form.location" label="Location" placeholder="e.g. MDMPI, CALMC" hide-details="auto" />
              <!-- An empty string counts as a value to v-combobox and lifts the label, so pass null instead. -->
              <v-combobox
                :model-value="form.workWith || null"
                :items="workWithItems"
                @update:model-value="(v: string | null) => (form.workWith = v ?? '')"
                label="Work with"
                hint="Printed after the location as typed; groups lines on the R47."
                hide-details="auto"
                clearable
              />
            </div>
          </section>

          <!-- Meals -->
          <v-expand-transition>
            <section v-if="form.category === 'Meals'" class="eline__section">
              <div class="panel">
                <div class="panel__head">
                  <span class="eyebrow">Meals</span>
                  <span v-if="suggestedMealAmount > 0" class="panel__meta">Rates total {{ formatMoney(suggestedMealAmount) }}</span>
                </div>
                <v-chip-group v-model="selectedMeals" multiple column selected-class="meal--on">
                  <v-chip v-for="m in mealTypes" :key="m" :value="m" variant="outlined" class="meal press">
                    {{ m }}
                    <span v-if="rates && rates[rateFor[m]]" class="meal__rate">{{ formatMoney(rates[rateFor[m]]) }}</span>
                  </v-chip>
                </v-chip-group>
              </div>
            </section>
          </v-expand-transition>

          <!-- Transportation legs -->
          <v-expand-transition>
            <section v-if="form.category === 'Transportation'" class="eline__section">
              <div class="panel">
                <div class="panel__head">
                  <span class="eyebrow">Fare legs <span class="eyebrow__opt">optional · mode required per leg</span></span>
                  <v-btn class="press" size="small" variant="text" prepend-icon="mdi-plus" @click="addLeg">Add leg</v-btn>
                </div>
                <div v-for="(leg, i) in form.transports" :key="i" class="leg">
                  <v-select
                    v-model="leg.fareModeId"
                    :items="modeItems(leg)"
                    item-value="id"
                    item-title="name"
                    label="Mode"
                    :placeholder="leg.fareModeId === null && legHasContent(leg) ? 'Not specified' : undefined"
                    density="compact"
                    hide-details="auto"
                    :error-messages="legModeErrors(leg, i)"
                    class="leg__mode"
                    :aria-label="`Leg ${i + 1} mode`"
                  >
                    <template #selection="{ item }">
                      <v-icon :icon="fareModeIcon(item.raw.code)" size="16" class="leg__mode-icon" />
                      <span class="leg__mode-name">{{ item.title }}</span>
                    </template>
                    <template #item="{ props: itemProps, item }">
                      <v-list-item v-bind="itemProps" :prepend-icon="fareModeIcon(item.raw.code)" density="compact" />
                    </template>
                  </v-select>
                  <v-text-field v-model="leg.fareFrom" label="From" density="compact" hide-details class="leg__from" />
                  <v-icon icon="mdi-arrow-right" size="16" class="leg__arrow" />
                  <v-text-field v-model="leg.fareTo" label="To" density="compact" hide-details class="leg__to" />
                  <v-text-field v-model.number="leg.fareAmount" type="number" min="0" step="0.01" label="Fare" prefix="₱" density="compact" hide-details class="leg__fare" />
                  <v-btn class="press leg__remove" icon="mdi-close" size="x-small" variant="text" :aria-label="`Remove leg ${i + 1}`" @click="removeLeg(i)" />
                  <v-text-field
                    v-if="needsDetail(leg)"
                    v-model="leg.fareModeDetail"
                    label="Specify the mode"
                    placeholder="e.g. Kalesa, company shuttle"
                    maxlength="60"
                    density="compact"
                    hide-details="auto"
                    :error-messages="legDetailErrors(leg, i)"
                    class="leg__detail"
                  />
                </div>
                <div v-if="form.transports.length" class="panel__foot">
                  <span>Legs total <span class="mono">{{ formatMoney(legsTotal) }}</span></span>
                  <v-btn class="press" size="x-small" variant="tonal" color="primary" :disabled="legsTotal <= 0" @click="useLegsTotal">Use as amount</v-btn>
                </div>
              </div>
            </section>
          </v-expand-transition>

          <!-- Amount -->
          <section class="eline__section">
            <div class="eyebrow">Amount</div>
            <div class="eline__row eline__row--amount">
              <v-text-field
                v-model.number="form.amount"
                type="number"
                min="0"
                step="0.01"
                :label="form.isPerdiem ? 'Daily amount' : 'Amount'"
                prefix="₱"
                hide-details="auto"
                :error-messages="fieldErrors('amount')"
              />
              <v-text-field v-if="!form.isPerdiem" v-model.number="form.multiplier" type="number" min="1" step="1" label="Multiplier" hide-details="auto" />
              <template v-else>
                <v-text-field v-model.number="form.totalDays" type="number" min="1" step="1" label="# of days" hide-details="auto" :error-messages="fieldErrors('totalDays')" />
                <v-text-field
                  v-model.number="form.minusDays"
                  type="number"
                  min="0"
                  step="1"
                  label="− days"
                  hide-details="auto"
                  :error-messages="perdiemInvalid ? ['Days must be more than the days taken off.'] : fieldErrors('minusDays')"
                />
              </template>
            </div>
            <v-checkbox v-model="form.isPerdiem" label="Per-diem / allowance (amount × days)" density="compact" hide-details color="primary" class="eline__check" />
            <div class="eline__row">
              <v-text-field v-model="form.invoiceOrNo" label="Invoice / OR no." hide-details="auto" :error-messages="fieldErrors('invoiceOrNo')" />
              <v-text-field
                v-model.number="form.vatAmount"
                type="number"
                min="0"
                step="0.01"
                label="VAT amount"
                prefix="₱"
                :disabled="!hasInvoice"
                :hint="hasInvoice ? undefined : 'Enter an Invoice/OR no. first'"
                :persistent-hint="!hasInvoice"
                hide-details="auto"
                :error-messages="fieldErrors('vatAmount')"
              />
            </div>
          </section>

          <!-- More -->
          <section class="eline__section">
            <button type="button" class="more press" :aria-expanded="showDetails" @click="showDetails = !showDetails">
              <v-icon :icon="showDetails ? 'mdi-chevron-up' : 'mdi-chevron-down'" size="18" />
              More details
              <span class="more__hint">remarks, instrument, serial and service no.</span>
            </button>
            <v-expand-transition>
              <div v-if="showDetails" class="more__body">
                <v-textarea v-model="form.remarks" label="Remarks" rows="1" auto-grow hide-details="auto" />
                <div class="eline__row eline__row--three">
                  <v-text-field v-model="form.instrument" label="Instrument" density="compact" hide-details />
                  <v-text-field v-model="form.serialNumber" label="Serial no." density="compact" hide-details />
                  <v-text-field v-model="form.serviceNumber" label="Service no." density="compact" hide-details />
                </div>
              </div>
            </v-expand-transition>
          </section>
        </v-form>
      </v-card-text>

      <footer class="eline__foot">
        <div class="total" aria-live="polite">
          <span class="total__label">Line total</span>
          <span class="total__value mono">{{ formatMoney(previewTotal) }}</span>
          <span v-if="amountFormula" class="total__formula mono">{{ amountFormula }}</span>
        </div>
        <div class="eline__actions">
          <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
          <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">
            {{ isEdit ? 'Save line' : 'Add line' }}
          </v-btn>
        </div>
      </footer>
    </v-card>
  </v-dialog>

  <CrmActivityPicker
    v-model="crmOpen"
    :list="crmList"
    :loading="crmLoading"
    :error="crmError"
    @pick="applyActivity"
    @retry="emit('crm-load', form.transactionDate)"
  />
</template>

<style scoped>
.eline {
  background: var(--card);
}
.eline__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 20px 20px 4px 24px;
}
.eline__title {
  font-size: 18px;
  font-weight: 600;
  line-height: 1.3;
  color: var(--ink);
}
.eline__sub {
  margin-top: 2px;
  font-size: 13px;
  color: var(--ink-3);
}
.eline__sub .mono {
  color: var(--navy);
  font-weight: 500;
}
.eline__body {
  padding: 12px 24px 8px !important;
}
.eline__section {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 20px;
}
.eline__row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: start;
  gap: 12px;
}
.eline__row--when {
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
}
.eline__row--amount {
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
}
.eline__row--three {
  grid-template-columns: repeat(3, 1fr);
}
.eline__check {
  margin-top: -6px;
}
.eline__details-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 28px;
}
.crmlink {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
.crmlink__hint {
  font-size: 12px;
  color: var(--ink-3);
}
.crmlink__error {
  font-size: 12px;
  color: rgb(var(--v-theme-error));
}
.eyebrow {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--ink-3);
}
.eyebrow__opt {
  margin-left: 4px;
  font-weight: 500;
  text-transform: none;
  letter-spacing: 0;
}

/* Segmented Local / Foreign */
.seg {
  display: inline-flex;
  padding: 3px;
  border: 1px solid var(--line-2);
  border-radius: 10px;
  background: var(--paper);
}
.seg__btn {
  min-width: 76px;
  padding: 8px 14px;
  border: 0;
  border-radius: 7px;
  background: transparent;
  font: inherit;
  font-size: 14px;
  color: var(--ink-2);
  cursor: pointer;
  transition:
    background-color var(--dur-fast) ease,
    color var(--dur-fast) ease,
    transform var(--dur-press) var(--ease-out);
}
.seg__btn--on {
  background: var(--card);
  color: var(--ink);
  font-weight: 500;
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.08);
}

/* Category tiles */
.cats {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 8px;
}
.cat {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  min-height: 72px;
  padding: 10px 6px;
  border: 1px solid var(--line-2);
  border-radius: var(--radius);
  background: var(--card);
  font: inherit;
  font-size: 12.5px;
  color: var(--ink-2);
  cursor: pointer;
  transition:
    border-color var(--dur-fast) ease,
    background-color var(--dur-fast) ease,
    color var(--dur-fast) ease,
    transform var(--dur-press) var(--ease-out);
}
@media (hover: hover) and (pointer: fine) {
  .cat:hover:not(.cat--on) {
    border-color: var(--ink-3);
    color: var(--ink);
  }
}
.cat--on {
  border-color: var(--navy);
  background: var(--navy-soft);
  color: var(--navy);
  font-weight: 600;
}
.cat:focus-visible,
.seg__btn:focus-visible,
.more:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}

/* Category panels */
.panel {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px 16px;
  border-radius: var(--radius);
  background: var(--paper);
}
.panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 28px;
}
.panel__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.panel__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 13px;
  color: var(--ink-2);
}
.meal {
  border-color: var(--line-2) !important;
  background: var(--card);
}
.meal--on {
  border-color: var(--navy) !important;
  background: var(--navy-soft) !important;
  color: var(--navy) !important;
}
.meal__rate {
  margin-left: 6px;
  font-family: var(--font-mono);
  font-size: 12px;
  opacity: 0.7;
}
.leg {
  display: grid;
  grid-template-columns: 168px minmax(0, 1fr) auto minmax(0, 1fr) 120px auto;
  align-items: start;
  gap: 8px;
}
.leg > .leg__arrow,
.leg > .leg__remove {
  margin-top: 10px;
}
.leg__mode-icon {
  margin-right: 6px;
  color: var(--ink-2);
}
.leg__mode-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.leg__detail {
  grid-column: 1 / 3;
}
.leg :deep(.v-field) {
  background: var(--card);
}
.leg__arrow {
  color: var(--ink-3);
}

/* More details */
.more {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  align-self: flex-start;
  padding: 4px 0;
  border: 0;
  background: none;
  font: inherit;
  font-size: 14px;
  font-weight: 500;
  color: var(--ink);
  cursor: pointer;
}
.more__hint {
  font-weight: 400;
  font-size: 12.5px;
  color: var(--ink-3);
}
.more__body {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-top: 4px;
}

/* Footer with the running total */
.eline__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px 14px 24px;
  border-top: 1px solid var(--line);
  background: var(--card);
}
.total {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  column-gap: 10px;
  min-width: 0;
}
.total__label {
  font-size: 12px;
  color: var(--ink-3);
}
.total__value {
  font-size: 20px;
  font-weight: 600;
  color: var(--ink);
  font-variant-numeric: tabular-nums;
}
.total__formula {
  font-size: 12px;
  color: var(--ink-3);
}
.eline__actions {
  display: flex;
  gap: 8px;
}

@media (max-width: 699.98px) {
  .cats {
    grid-template-columns: repeat(3, 1fr);
  }
}
@media (max-width: 559.98px) {
  .eline__head {
    padding: 16px 12px 0 16px;
  }
  .eline__body {
    padding: 12px 16px 4px !important;
  }
  .eline__row,
  .eline__row--three {
    grid-template-columns: 1fr;
  }
  .eline__row--when {
    grid-template-columns: 1fr;
  }
  .seg {
    justify-self: start;
  }
  /* Phone: mode and remove on top, From / To, then fare (and the typed mode). */
  .leg {
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
    padding-bottom: 10px;
    border-bottom: 1px solid var(--line);
  }
  .leg:last-of-type {
    border-bottom: 0;
  }
  .leg__mode {
    grid-column: 1 / 3;
    grid-row: 1;
  }
  .leg > .leg__remove {
    grid-column: 3;
    grid-row: 1;
  }
  .leg__arrow {
    display: none;
  }
  .leg__from {
    grid-column: 1;
  }
  .leg__to {
    grid-column: 2 / 4;
  }
  .leg__fare,
  .leg__detail {
    grid-column: 1 / 4;
  }
  .more__hint {
    display: none;
  }
  .eline__foot {
    padding: 12px 16px;
  }
}
</style>
