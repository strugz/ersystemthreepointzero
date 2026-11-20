<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import { addDaysIso, formatDate, formatMoney, todayIso } from '@/shared/format';
import { hasCamera } from '@/shared/images';
import AppCameraCapture from '@/shared/ui/AppCameraCapture.vue';
import AppPickerField from '@/shared/ui/AppPickerField.vue';
import {
  VEHICLE_SEPARATOR,
  categoryDefaults,
  categoryLabel,
  categoryOptions,
  expenseTypeOptions,
  fareModeIcon,
  isVehicleCategory,
  lineTotal,
  mealTypes,
  r47Column,
  vehicleParts,
  vehicleReasons,
} from '../labels';
import CrmActivityPicker from './CrmActivityPicker.vue';
import type {
  AttachmentDocumentType,
  CrmActivityOption,
  CrmActivityPickList,
  EmployeeRates,
  ExpenseCategory,
  ExpenseLine,
  ExpenseTransportItem,
  FareModeOption,
  MyVehicle,
  RequiredDocument,
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
    /** The report period, which bounds the days the CRM picker can step to. */
    periodFrom?: string | null;
    periodTo?: string | null;
    /** Categories switched off on the Rules page (Finance rules); a saved line keeps its own. */
    unavailableCategories?: ExpenseCategory[];
    /** Categories whose lines need a document attached to the report first (Sponsorship, Demo). */
    requiredDocuments?: RequiredDocument[];
    /** Document types already attached to the report. */
    attachedDocumentTypes?: AttachmentDocumentType[];
    /** A document picked here is being uploaded by the parent. */
    uploadingDocument?: boolean;
    /** The employee's saved vehicle; fills Vehicle model and Plate no. on a new Gas or Vehicle emergency purchase line. */
    savedVehicle?: MyVehicle | null;
    /** Finance rules: what the people tagged as Work With can no longer claim; null when the rule is off. */
    workWithNotice?: string | null;
    /** Finance rules: the employee has a fleet card, so a Gas line's Reason is one of these; null when it does not apply. */
    fleetCardGasReasons?: string[] | null;
    fleetCardOtherReasonAllowed?: boolean;
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
    unavailableCategories: () => [],
    requiredDocuments: () => [],
    attachedDocumentTypes: () => [],
    uploadingDocument: false,
    savedVehicle: null,
    workWithNotice: null,
    fleetCardGasReasons: null,
    fleetCardOtherReasonAllowed: false,
    crmError: null,
    periodFrom: null,
    periodTo: null,
  },
);

const { compact } = useLayoutMode();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: SaveExpenseLineInput];
  /** Rates depend on the expense date; the parent refetches them for the date shown. */
  'date-change': [date: string];
  /** The employee opened the CRM picker or stepped to another day; the parent loads the activities of that day. */
  'crm-load': [date: string];
  /** Upload the document the chosen category needs; the parent attaches it to the report under Receipts. */
  'upload-document': [file: File, documentType: AttachmentDocumentType];
  /** Remove the line being edited (phones and tablets, where the list has no ✕); the parent confirms first. */
  remove: [line: ExpenseLine];
}>();

/** The date moved into the report period (ISO dates compare as text). */
function inPeriod(date: string): string {
  if (props.periodFrom && date < props.periodFrom) return props.periodFrom;
  if (props.periodTo && date > props.periodTo) return props.periodTo;
  return date;
}

const blank = (): SaveExpenseLineInput => ({
  transactionDate: inPeriod(props.defaultDate ?? todayIso()),
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
  crmActivityIds: [],
});

const form = reactive<SaveExpenseLineInput>(blank());
const selectedMeals = ref<string[]>([]);
/** Set by a save attempt, so a leg without a mode is only flagged once the employee tries to save. */
const triedSave = ref(false);
const dateInput = ref<HTMLElement | null>(null);

// ---- category defaults (new lines only) ----------------------------------------------
// `auto` remembers what the form filled in by itself. A field is replaced on a category change only
// while it is still empty or still holds that auto value, so anything the employee typed is kept.
const auto = reactive({ particulars: '', amount: 0 });

// Gas and Vehicle emergency purchase: Particulars typed as Reason, Vehicle model and Plate no. A new line takes the
// model and plate from the employee's saved vehicle.
// Reason is set by a picker; an older value may be null.
const vehicle = reactive<{ reason: string | null; model: string; plate: string }>({ reason: '', model: '', plate: '' });
const isVehicleLine = computed(() => isVehicleCategory(form.category));
const fleetCardGas = computed(() => form.category === 'Gas' && !!props.fleetCardGasReasons);
const reasonItems = computed(() =>
  fleetCardGas.value ? (props.fleetCardGasReasons ?? []) : vehicleReasons[form.category === 'Gas' ? 'Gas' : 'VehicleEmergency'],
);

function loadVehicleFields() {
  const parts = vehicleParts(form.particulars ?? '');
  if (props.line === null && !parts.model && !parts.plate && props.savedVehicle?.model && props.savedVehicle.plateNo) {
    parts.model = props.savedVehicle.model;
    parts.plate = props.savedVehicle.plateNo;
  }
  Object.assign(vehicle, parts);
}

watch(vehicle, () => {
  if (!isVehicleLine.value) return;
  const parts = [vehicle.reason, vehicle.model, vehicle.plate].map((p) => (p ?? '').trim());
  form.particulars = parts.every((p) => p.length > 0) ? parts.join(VEHICLE_SEPARATOR) : '';
  auto.particulars = form.particulars;
});

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
  syncRateAmount();
}

// After Add line, the dialog asks whether the next line is on the next date (none after the period's last day).
const nextDateOffer = ref<{ added: string; next: string } | null>(null);
function answerNextDate(goNext: boolean) {
  const offer = nextDateOffer.value;
  nextDateOffer.value = null;
  if (goNext && offer) form.transactionDate = offer.next;
}
watch(
  () => form.transactionDate,
  (date) => {
    if (nextDateOffer.value && date !== nextDateOffer.value.added) nextDateOffer.value = null;
  },
);

/**
 * Clears the form for the next line (date follows `defaultDate`) and puts the cursor back on Date. With the date of
 * the line just added, it asks whether the next line goes on the next date.
 */
function reset(addedDate?: string) {
  Object.assign(form, blank());
  Object.assign(auto, { particulars: '', amount: 0 });
  Object.assign(vehicle, { reason: '', model: '', plate: '' });
  selectedMeals.value = [];
  triedSave.value = false;
  pickedActivities.value = [];
  applyCategoryDefaults();
  const next = addedDate ? addDaysIso(addedDate, 1) : null;
  nextDateOffer.value = addedDate && next && inPeriod(next) === next ? { added: form.transactionDate, next } : null;
  requestAnimationFrame(() => dateInput.value?.querySelector('input')?.focus());
}

defineExpose({ reset });

watch(
  () => props.modelValue,
  (open) => {
    if (!open) {
      return;
    }
    nextDateOffer.value = null;
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
        crmActivityIds: [...(l.crmActivityIds ?? [])],
      } satisfies SaveExpenseLineInput);
    }
    selectedMeals.value = form.meals.map((m) => m.mealType);
    triedSave.value = false;
    pickedActivities.value = [];
    Object.assign(auto, { particulars: '', amount: 0 });
    applyCategoryDefaults();
    if (isVehicleLine.value) loadVehicleFields();
  },
);

watch(
  () => form.category,
  () => {
    applyCategoryDefaults();
    if (isVehicleLine.value) loadVehicleFields();
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

// The date picker offers only the report period; a date typed outside it is caught here, before the server refuses it.
const periodText = computed(() =>
  props.periodFrom && props.periodTo ? `${formatDate(props.periodFrom)} to ${formatDate(props.periodTo)}` : null,
);
const dateOutsidePeriod = computed(() => !!form.transactionDate && form.transactionDate !== inPeriod(form.transactionDate));

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
/** Lodging: the per-night rate from the rate profile or the Finance rules' flat amount (0 when none is set). */
const lodgingRate = computed(() => props.rates?.lodgingRate ?? 0);
const suggestedAmount = computed(() => {
  if (form.category === 'Meals') return suggestedMealAmount.value;
  if (form.category === 'Lodging') return lodgingRate.value;
  return 0;
});
/**
 * Offers the meal rates (or the lodging rate) as the amount: when the amount is empty, or (on a new line) still
 * the rate filled in earlier. Re-runs when the chips change and when the rates arrive for the expense date.
 */
function syncRateAmount() {
  const suggested = suggestedAmount.value;
  if (suggested <= 0) {
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
  syncRateAmount();
});
watch(suggestedAmount, syncRateAmount);

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
// ---- CRM activities ----------------------------------------------------------------
// Picking activities fills what the CRM knows (date, client, service details, work-with, the trip); the
// employee still chooses the category and enters the amount, Invoice/OR and VAT. A visit that served several
// instruments is several activities: their service no., instrument and serial are joined with " / ".
const crmOpen = ref(false);
const pickedActivities = ref<CrmActivityOption[]>([]);
// The day the picker shows; starts at the line's date and steps on its own until an activity is picked.
const crmDay = ref(todayIso());

function openCrm() {
  crmOpen.value = true;
  showCrmDay(form.transactionDate);
}

function showCrmDay(date: string) {
  crmDay.value = date;
  emit('crm-load', date);
}

/** The non-empty values joined by `separator` (repeats dropped when `distinct`), or null when there are none. */
function joinValues(values: (string | null | undefined)[], separator: string, distinct = false): string | null {
  const present = values.map((v) => v?.trim()).filter((v): v is string => !!v);
  const kept = distinct ? [...new Set(present)] : present;
  return kept.length ? kept.join(separator) : null;
}

function applyActivities(activities: CrmActivityOption[]) {
  const first = activities[0];
  if (!first) return;
  pickedActivities.value = activities;
  form.crmActivityIds = activities.map((a) => a.activityId);
  form.transactionDate = first.activityDate;
  form.location = joinValues(activities.map((a) => a.customerName), ' / ', true) ?? form.location;
  form.workWith = joinValues(activities.flatMap((a) => a.workWith?.split(',') ?? []), ', ', true) ?? form.workWith;
  // One entry per activity, repeats kept, so the nth service no., instrument and serial belong together
  // (two units of the same model are two entries).
  form.serviceNumber = joinValues(activities.map((a) => a.serviceNumber), ' / ') ?? form.serviceNumber;
  form.instrument = joinValues(activities.map((a) => a.instrumentName), ' / ') ?? form.instrument;
  form.serialNumber = joinValues(activities.map((a) => a.serialNumber), ' / ') ?? form.serialNumber;
  fillLegFromActivity();
}

/** On a Transportation line, the first picked activity with a trip fills the fare leg when no leg has a route yet. */
function fillLegFromActivity() {
  const a = pickedActivities.value.find((p) => p.travelOrigin || p.travelDestination);
  if (!a || form.category !== 'Transportation') return;
  if (form.transports.some((t) => t.fareFrom || t.fareTo)) return;
  if (!form.transports.length) form.transports.push(newLeg());
  const leg = form.transports[0];
  if (!leg) return;
  leg.fareFrom = a.travelOrigin ?? '';
  leg.fareTo = a.travelDestination ?? '';
}

function unlinkActivity(activityId: string) {
  form.crmActivityIds = form.crmActivityIds.filter((id) => id !== activityId);
  pickedActivities.value = pickedActivities.value.filter((a) => a.activityId !== activityId);
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
// Work With is stored as one comma-separated text (the R47 splits it into initials); the picker edits it as a list.
const workWithList = computed<string[]>({
  get: () => (form.workWith ?? '').split(',').map((n) => n.trim()).filter(Boolean),
  set: (names) => {
    form.workWith = [...new Set(names.map((n) => n.trim()).filter(Boolean))].join(', ');
  },
});

// Tile text kept to two lines so every tile row is the same height; the full name stays in the tooltip and elsewhere.
const tileLabel: Partial<Record<ExpenseCategory, string>> = { VehicleEmergency: 'Vehicle emergency' };

const categoryIcon: Record<ExpenseCategory, string> = {
  Meals: 'mdi-silverware-fork-knife',
  Transportation: 'mdi-car-outline',
  Toll: 'mdi-boom-gate-outline',
  Parking: 'mdi-parking',
  Others: 'mdi-dots-horizontal',
  Representation: 'mdi-account-tie-outline',
  Sponsorship: 'mdi-handshake-outline',
  Demo: 'mdi-presentation',
  Lodging: 'mdi-bed-outline',
  Gas: 'mdi-gas-station-outline',
  VehicleEmergency: 'mdi-car-wrench',
};

const amountLabel = computed(() => {
  if (form.isPerdiem) return 'Daily amount';
  return form.category === 'Lodging' ? 'Per night' : 'Amount';
});

// Tiles grouped by kind: meals and lodging, travel and vehicle, client costs, then Others last. Only this dialog; rate
// pages keep the category order of labels.ts. A category missing here goes before Others.
const TILE_ORDER: ExpenseCategory[] = [
  'Meals',
  'Lodging',
  'Transportation',
  'Gas',
  'Toll',
  'Parking',
  'VehicleEmergency',
  'Representation',
  'Sponsorship',
  'Demo',
  'Others',
];
const tileRank = (c: ExpenseCategory) => {
  const i = TILE_ORDER.indexOf(c);
  return i < 0 ? TILE_ORDER.length - 1.5 : i;
};
const shownCategories = computed(() =>
  categoryOptions
    .filter((c) => !props.unavailableCategories.includes(c.value) || c.value === form.category)
    .sort((x, y) => tileRank(x.value) - tileRank(y.value)),
);

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

// Finance rules: a line in some categories can be added only once its documents are attached to the report (a vehicle
// emergency purchase needs two: the receipt and the Job Order). With several missing, the employee picks which to upload.
const neededDocuments = computed(() => props.requiredDocuments.filter((d) => d.category === form.category));
const missingDocuments = computed(() => neededDocuments.value.filter((d) => !props.attachedDocumentTypes.includes(d.documentType)));
const documentMissing = computed(() => missingDocuments.value.length > 0);
const uploadChoice = ref<AttachmentDocumentType | null>(null);
const uploadDocument = computed(
  () => missingDocuments.value.find((d) => d.documentType === uploadChoice.value) ?? missingDocuments.value[0] ?? null,
);
const uploadType = computed(() => uploadDocument.value?.documentType);
const capitalise = (text: string) => text.charAt(0).toUpperCase() + text.slice(1);
const documentPicker = ref<HTMLInputElement | null>(null);
const showCamera = hasCamera();
const documentCameraOpen = ref(false);
function onDocumentCaptured(file: File) {
  if (uploadType.value) emit('upload-document', file, uploadType.value);
}
function onDocumentPicked(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (file && uploadType.value) emit('upload-document', file, uploadType.value);
  input.value = '';
}

const canSave = computed(
  () =>
    (form.particulars ?? '').trim().length > 0 &&
    Number(form.amount) > 0 &&
    !perdiemInvalid.value &&
    !dateOutsidePeriod.value &&
    !documentMissing.value &&
    !props.saving,
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
  <v-dialog :model-value="modelValue" max-width="760" :fullscreen="compact" scrollable :persistent="saving" @update:model-value="close">
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
            <v-alert v-if="nextDateOffer" type="success" variant="tonal" density="compact" class="mb-3" role="status">
              <div class="d-flex flex-wrap align-center ga-2">
                <span class="flex-grow-1">Line added. Is the next line on the next date?</span>
                <v-btn size="small" variant="text" class="press" @click="answerNextDate(false)">
                  Stay on {{ formatDate(nextDateOffer.added) }}
                </v-btn>
                <v-btn size="small" color="primary" variant="flat" class="press" @click="answerNextDate(true)">
                  Go to {{ formatDate(nextDateOffer.next) }}
                </v-btn>
              </div>
            </v-alert>
            <div class="eline__row eline__row--when">
              <v-text-field
                ref="dateInput"
                v-model="form.transactionDate"
                type="date"
                label="Date"
                :min="periodFrom ?? undefined"
                :max="periodTo ?? undefined"
                :hint="periodText ? `Report period: ${periodText}` : undefined"
                :persistent-hint="!!periodText"
                hide-details="auto"
                :error-messages="dateOutsidePeriod ? [`Pick a date in the report period, ${periodText}.`] : fieldErrors('transactionDate')"
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
                v-for="c in shownCategories"
                :key="c.value"
                type="button"
                class="cat press"
                :class="{ 'cat--on': form.category === c.value }"
                :aria-pressed="form.category === c.value"
                :title="c.title"
                @click="form.category = c.value"
              >
                <v-icon :icon="categoryIcon[c.value]" size="20" />
                <span class="cat__label">{{ tileLabel[c.value] ?? c.title }}</span>
              </button>
            </div>

            <v-alert
              v-if="neededDocuments.length"
              :type="documentMissing ? 'warning' : 'success'"
              variant="tonal"
              density="compact"
              class="mt-3"
            >
              <template v-if="documentMissing">
                A {{ categoryLabel[form.category] }} line needs
                <template v-for="(doc, i) in missingDocuments" :key="doc.documentType"
                  >{{ i > 0 ? ' and ' : '' }}the <strong>{{ doc.label }}</strong></template
                >
                attached first.
                <v-chip-group
                  v-if="missingDocuments.length > 1"
                  :model-value="uploadDocument?.documentType"
                  mandatory
                  class="mt-1"
                  @update:model-value="(v: AttachmentDocumentType) => (uploadChoice = v)"
                >
                  <v-chip v-for="doc in missingDocuments" :key="doc.documentType" :value="doc.documentType" size="small" filter>
                    Upload: {{ capitalise(doc.label) }}
                  </v-chip>
                </v-chip-group>
              </template>
              <template v-else>{{ neededDocuments.map((d) => capitalise(d.label)).join(' and ') }} attached.</template>
              <template v-if="documentMissing" #append>
                <div class="d-flex ga-2">
                  <v-btn
                    v-if="showCamera"
                    class="press"
                    size="small"
                    variant="flat"
                    color="warning"
                    prepend-icon="mdi-camera-outline"
                    :loading="uploadingDocument"
                    @click="documentCameraOpen = true"
                  >
                    Photo
                  </v-btn>
                  <v-btn
                    class="press"
                    size="small"
                    :variant="showCamera ? 'outlined' : 'flat'"
                    color="warning"
                    :loading="!showCamera && uploadingDocument"
                    :disabled="showCamera && uploadingDocument"
                    @click="documentPicker?.click()"
                  >
                    {{ showCamera ? 'File' : 'Upload' }}
                  </v-btn>
                </div>
              </template>
            </v-alert>
            <input ref="documentPicker" type="file" accept="image/jpeg,image/png,application/pdf" hidden @change="onDocumentPicked" />
            <AppCameraCapture
              v-if="showCamera && uploadDocument"
              v-model="documentCameraOpen"
              :title="capitalise(uploadDocument.label)"
              @capture="onDocumentCaptured"
            />
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
                {{ form.crmActivityIds.length ? 'Change CRM activities' : 'From CRM' }}
              </v-btn>
            </div>
            <div v-if="form.crmActivityIds.length" class="crmlink">
              <v-chip
                v-for="id in form.crmActivityIds"
                :key="id"
                size="small"
                label
                variant="tonal"
                color="primary"
                closable
                prepend-icon="mdi-link-variant"
                @click:close="unlinkActivity(id)"
              >
                <span class="mono">{{ id }}</span>
              </v-chip>
              <span class="crmlink__hint">
                Linked CRM {{ form.crmActivityIds.length === 1 ? 'activity' : 'activities' }}. Remove the links to type this line by hand.
              </span>
            </div>
            <p v-if="fieldErrors('crmActivityIds')" class="crmlink__error" role="alert">{{ fieldErrors('crmActivityIds')?.join(' ') }}</p>
            <template v-if="isVehicleLine">
              <v-alert v-if="fleetCardGas" type="info" variant="tonal" density="compact">
                You have a fleet card, so Gas is reimbursed only when the card could not be used. Pick the reason.
              </v-alert>
              <AppPickerField
                v-model="vehicle.reason"
                :items="reasonItems"
                :allow-custom="!fleetCardGas || fleetCardOtherReasonAllowed"
                label="Reason"
                :placeholder="!fleetCardGas || fleetCardOtherReasonAllowed ? 'Tap to pick or type your own' : 'Tap to pick'"
              />
              <div class="eline__row">
                <v-text-field v-model="vehicle.model" label="Vehicle model" placeholder="e.g. Toyota Vios" hide-details="auto" />
                <v-text-field v-model="vehicle.plate" label="Plate no." placeholder="e.g. ABC 1234" hide-details="auto" />
              </div>
              <p class="text-caption text-medium-emphasis mb-0">
                Particulars: {{ form.particulars || 'reason - vehicle model - plate no.' }}
              </p>
              <p v-if="fieldErrors('particulars')" class="text-caption text-error mb-0" role="alert">{{ fieldErrors('particulars')?.join(' ') }}</p>
            </template>
            <v-text-field
              v-else
              v-model="form.particulars"
              label="Particulars"
              placeholder="e.g. SUCAT - NICHOLS"
              maxlength="200"
              hide-details="auto"
              :error-messages="fieldErrors('particulars')"
            />
            <div class="eline__row">
              <v-text-field v-model="form.location" label="Location" placeholder="e.g. MDMPI, CALMC" hide-details="auto" />
              <AppPickerField
                v-model="workWithList"
                :items="workWithItems"
                label="Work with"
                title="Work with (pick one or more)"
                hint="Pick or type one or more people. Printed as initials after the location; groups lines on the R47."
                multiple
              />
            </div>
            <v-alert v-if="workWithNotice && workWithList.length > 0" type="info" variant="tonal" density="compact" class="mt-2">
              {{ workWithNotice }}
            </v-alert>
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
                <p v-if="fieldErrors('meals')" class="text-caption text-error mb-0" role="alert">{{ fieldErrors('meals')?.join(' ') }}</p>
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
                :label="amountLabel"
                prefix="₱"
                :hint="form.category === 'Lodging' && lodgingRate > 0 ? `Lodging rate ${formatMoney(lodgingRate)} a night` : undefined"
                :persistent-hint="form.category === 'Lodging' && lodgingRate > 0"
                hide-details="auto"
                :error-messages="fieldErrors('amount')"
              />
              <v-text-field v-if="!form.isPerdiem" v-model.number="form.multiplier" type="number" min="1" step="1" :label="form.category === 'Lodging' ? 'Nights' : 'Multiplier'" hide-details="auto" />
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

          <section class="eline__section">
            <div class="eyebrow">Service details</div>
            <div class="eline__row eline__row--three">
              <v-text-field v-model="form.instrument" label="Instrument" hide-details />
              <v-text-field v-model="form.serialNumber" label="Serial no." hide-details />
              <v-text-field v-model="form.serviceNumber" label="Service no." hide-details />
            </div>
            <v-textarea v-model="form.remarks" label="Remarks" rows="1" auto-grow hide-details="auto" />
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
          <v-btn v-if="line && compact" class="press eline__remove" variant="text" color="error" :disabled="saving" @click="emit('remove', line)">Remove</v-btn>
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
    :date="crmDay"
    :min-date="periodFrom"
    :max-date="periodTo"
    :list="crmList"
    :loading="crmLoading"
    :error="crmError"
    :selected="form.crmActivityIds"
    @update:date="showCrmDay"
    @pick="applyActivities"
    @retry="emit('crm-load', crmDay)"
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

/* Segmented Local / Foreign: as tall as the Date field beside it (56px), so the row reads as one line. */
.seg {
  display: inline-flex;
  align-items: stretch;
  height: 56px;
  box-sizing: border-box;
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

/* Category tiles: every row the same height (grid-auto-rows), labels at most two lines. */
.cats {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  grid-auto-rows: 1fr;
  gap: 8px;
}
.cat {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  min-height: 76px;
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
.cat__label {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.25;
  text-align: center;
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
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
  /* Phone and small tablet: mode and remove on top, From / To, then fare (and the typed mode). */
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
    height: 48px;
  }
  .more__hint {
    display: none;
  }
  /* Full screen on phones: total on its own line, buttons below, clear of the home indicator. */
  .eline__foot {
    flex-wrap: wrap;
    row-gap: 8px;
    padding: 12px 16px calc(12px + env(safe-area-inset-bottom));
  }
  .eline__actions {
    width: 100%;
    justify-content: flex-end;
  }
  .eline__remove {
    margin-right: auto;
  }
}
</style>
