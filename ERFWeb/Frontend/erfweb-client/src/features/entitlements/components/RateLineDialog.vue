<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { addDaysIso, formatDate, todayIso } from '@/shared/format';
import { fieldErrors, numberOrNull, textOrNull } from '../forms';
import type { FieldErrors } from '../forms';
import { categoryOptions, mealOptions, rateKeyLabel, rateTypeOptions } from '../labels';
import type { AddRateLineInput, MealSubCategory, RateCategory, RateLine, RateType, UpdateRateLineInput } from '../types';

/**
 * Adds a rate line version, or corrects one in place. A new version closes the open one for the
 * same category/meal the day before it starts. A correction keeps the category and meal, needs a
 * reason, and may not overlap another version. Starting before today while replacing a rate in
 * force is a backdate: it needs the explicit override and a reason, both recorded on the line.
 *
 * A fresh Meals line may pick several meals at once: each becomes its own line with the same
 * amount and terms, saved one by one by the parent (see `dropMeals` for a partial failure).
 */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    /** Prefill from the version being superseded (category and meal become fixed). */
    from?: RateLine | null;
    /** The version being corrected in place; takes precedence over `from`. */
    editing?: RateLine | null;
    /** The profile's existing lines: a backdate needs the override only when it replaces one of them. */
    lines?: RateLine[];
    saving?: boolean;
    errors?: FieldErrors;
  }>(),
  { from: null, editing: null, lines: () => [], saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [inputs: AddRateLineInput[]];
  update: [input: UpdateRateLineInput];
}>();

interface Form {
  category: RateCategory;
  subCategory: MealSubCategory | null;
  /** Meals picked when adding fresh lines (several at once). */
  meals: MealSubCategory[];
  rateType: RateType;
  amount: number | string | null;
  currency: string;
  frequencyCap: number | string | null;
  requiresReceipt: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  changeReason: string;
  backdateOverride: boolean;
}

const today = todayIso();

function blank(): Form {
  return {
    category: 'Meals',
    subCategory: 'Breakfast',
    meals: ['Breakfast'],
    rateType: 'FixedPerDay',
    amount: null,
    currency: '',
    frequencyCap: null,
    requiresReceipt: false,
    effectiveFrom: today,
    effectiveTo: null,
    changeReason: '',
    backdateOverride: false,
  };
}

const form = reactive<Form>(blank());

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    Object.assign(form, blank());
    const l = props.editing ?? props.from;
    if (l) {
      Object.assign(form, {
        category: l.category,
        subCategory: l.subCategory,
        rateType: l.rateType,
        amount: l.amount,
        currency: l.currency ?? '',
        frequencyCap: l.frequencyCap,
        requiresReceipt: l.requiresReceipt,
      });
    }
    if (props.editing) {
      form.effectiveFrom = props.editing.effectiveFrom;
      form.effectiveTo = props.editing.effectiveTo;
    }
  },
);

const isEdit = computed(() => !!props.editing);
const keyFixed = computed(() => !!props.from || isEdit.value);

const isMeals = computed(() => form.category === 'Meals');
watch(
  () => form.category,
  (c) => {
    if (c !== 'Meals') form.subCategory = null;
    else if (!form.subCategory) form.subCategory = 'Breakfast';
  },
);

/** Fresh Meals lines: one dialog, several meals, same amount. */
const multiMeal = computed(() => isMeals.value && !keyFixed.value);
const allMeals = computed(() => form.meals.length === mealOptions.length);
function toggleAllMeals() {
  form.meals = allMeals.value ? [] : mealOptions.map((m) => m.value);
}

/** Removes meals the parent already saved, so a retry after a partial failure sends only the rest. */
function dropMeals(saved: MealSubCategory[]) {
  form.meals = form.meals.filter((m) => !saved.includes(m));
}
defineExpose({ dropMeals });

/** The meal of each line to save (null outside Meals), in menu order. */
const chosenMeals = computed<(MealSubCategory | null)[]>(() => {
  if (!isMeals.value) return [null];
  return multiMeal.value ? mealOptions.map((m) => m.value).filter((m) => form.meals.includes(m)) : [form.subCategory];
});

// Mirrors the server (RateRules.PlanVersion): a new line cuts short the version still running on
// its start date. With none, a past start only fills a gap (e.g. claims from before go-live).
const replacesEarlierRate = computed(
  () =>
    isEdit.value ||
    chosenMeals.value.some((sub) =>
      props.lines.some(
        (l) => l.category === form.category && (l.subCategory ?? null) === sub && (!l.effectiveTo || l.effectiveTo >= form.effectiveFrom),
      ),
    ),
);
const isBackdated = computed(() => !!form.effectiveFrom && form.effectiveFrom < today && replacesEarlierRate.value);
watch(isBackdated, (b) => {
  if (!b) form.backdateOverride = false;
});

const toBeforeFrom = computed(() => !!form.effectiveTo && !!form.effectiveFrom && form.effectiveTo < form.effectiveFrom);
const reasonRequired = computed(() => isBackdated.value || isEdit.value);
const reasonMissing = computed(() => reasonRequired.value && !form.changeReason.trim());
const amountValue = computed(() => numberOrNull(form.amount));

const canSave = computed(
  () =>
    !props.saving &&
    amountValue.value !== null &&
    amountValue.value >= 0 &&
    !!form.effectiveFrom &&
    !toBeforeFrom.value &&
    (!isMeals.value || (multiMeal.value ? form.meals.length > 0 : !!form.subCategory)) &&
    !reasonMissing.value &&
    (!isBackdated.value || form.backdateOverride),
);

const err = (field: string) => fieldErrors(props.errors, field);
const title = computed(() => {
  if (props.editing) return `Edit · ${rateKeyLabel(props.editing.category, props.editing.subCategory)}`;
  return props.from ? `New version · ${rateKeyLabel(props.from.category, props.from.subCategory)}` : 'Add rate line';
});

const saveLabel = computed(() => {
  if (isEdit.value) return 'Save changes';
  return multiMeal.value && form.meals.length > 1 ? `Add ${form.meals.length} lines` : 'Add line';
});

function save() {
  if (!canSave.value) return;
  const cap = numberOrNull(form.frequencyCap);
  if (props.editing) {
    emit('update', {
      rateType: form.rateType,
      amount: amountValue.value as number,
      currency: textOrNull(form.currency)?.toUpperCase() ?? null,
      frequencyCap: cap === null ? null : Math.trunc(cap),
      requiresReceipt: form.requiresReceipt,
      effectiveFrom: form.effectiveFrom,
      effectiveTo: form.effectiveTo || null,
      changeReason: form.changeReason.trim(),
      backdateOverride: isBackdated.value && form.backdateOverride,
      rowVersion: props.editing.rowVersion,
    });
    return;
  }
  const base = {
    category: form.category,
    rateType: form.rateType,
    amount: amountValue.value as number,
    currency: textOrNull(form.currency)?.toUpperCase() ?? null,
    frequencyCap: cap === null ? null : Math.trunc(cap),
    requiresReceipt: form.requiresReceipt,
    effectiveFrom: form.effectiveFrom,
    effectiveTo: form.effectiveTo || null,
    changeReason: textOrNull(form.changeReason),
    backdateOverride: isBackdated.value && form.backdateOverride,
  };
  emit(
    'save',
    chosenMeals.value.map((subCategory) => ({ ...base, subCategory })),
  );
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="600" scrollable :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="text-subtitle-1 font-weight-bold">{{ title }}</v-card-title>
      <v-card-text>
        <v-form :disabled="saving" @submit.prevent="save">
          <v-row dense>
            <v-col cols="12" :sm="isMeals ? 6 : 12">
              <v-select v-model="form.category" :items="categoryOptions" label="Category" :disabled="saving || keyFixed" :error-messages="err('category')" />
            </v-col>
            <v-col v-if="multiMeal" cols="12" sm="6">
              <v-select
                v-model="form.meals"
                :items="mealOptions"
                label="Meals *"
                multiple
                chips
                closable-chips
                :error-messages="form.meals.length ? err('subCategory') : ['Pick at least one meal.']"
              >
                <template #prepend-item>
                  <v-list-item title="All meals" @click="toggleAllMeals">
                    <template #prepend>
                      <v-checkbox-btn :model-value="allMeals" :indeterminate="form.meals.length > 0 && !allMeals" />
                    </template>
                  </v-list-item>
                  <v-divider />
                </template>
              </v-select>
            </v-col>
            <v-col v-else-if="isMeals" cols="12" sm="6">
              <v-select v-model="form.subCategory" :items="mealOptions" label="Meal *" :disabled="saving || keyFixed" :error-messages="err('subCategory')" />
            </v-col>
            <v-col cols="12" sm="6">
              <v-select v-model="form.rateType" :items="rateTypeOptions" label="Rate type" :error-messages="err('rateType')" />
            </v-col>
            <v-col cols="8" sm="4">
              <v-text-field v-model="form.amount" type="number" min="0" step="0.01" label="Amount *" autofocus :error-messages="err('amount')" />
            </v-col>
            <v-col cols="4" sm="2">
              <v-text-field
                :model-value="form.currency"
                label="Currency"
                maxlength="3"
                placeholder="PHP"
                :error-messages="err('currency')"
                @update:model-value="(v: string) => (form.currency = (v ?? '').toUpperCase())"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="form.frequencyCap"
                type="number"
                min="0"
                step="1"
                label="Frequency cap"
                hint="Times per day; blank for no cap"
                persistent-hint
                :error-messages="err('frequencyCap')"
              />
            </v-col>
            <v-col cols="12" sm="6" class="d-flex align-center">
              <v-switch v-model="form.requiresReceipt" color="primary" label="Requires receipt" hide-details />
            </v-col>
            <v-col cols="6">
              <v-text-field v-model="form.effectiveFrom" type="date" label="Effective from *" :error-messages="err('effectiveFrom')" />
            </v-col>
            <v-col cols="6">
              <v-text-field
                v-model="form.effectiveTo"
                type="date"
                :min="form.effectiveFrom || undefined"
                label="Effective to"
                clearable
                :error-messages="toBeforeFrom ? ['Ends before it starts.'] : err('effectiveTo')"
              />
            </v-col>
          </v-row>

          <p v-if="isEdit" class="text-caption text-medium-emphasis mt-1 mb-2">
            Corrects this version in place. Filed reports keep their amounts; the old values stay in the history.
          </p>
          <p v-else-if="from && form.effectiveFrom" class="text-caption text-medium-emphasis mt-1 mb-2">
            The current version ends {{ formatDate(addDaysIso(form.effectiveFrom, -1)) }}.
          </p>

          <v-expand-transition>
            <div v-if="isBackdated">
              <v-alert type="warning" variant="tonal" density="compact" class="mb-2">
                This version starts before today. Confirm the backdate and give a reason.
              </v-alert>
              <v-checkbox
                v-model="form.backdateOverride"
                color="warning"
                label="Backdate override"
                hide-details="auto"
                :error-messages="err('backdateOverride')"
              />
            </div>
          </v-expand-transition>

          <v-textarea
            v-model="form.changeReason"
            :label="reasonRequired ? 'Reason for change *' : 'Reason for change (optional)'"
            rows="2"
            auto-grow
            maxlength="500"
            counter
            class="mt-2"
            :error-messages="err('changeReason') ?? (reasonMissing && form.backdateOverride && !isEdit ? ['A reason is required to backdate.'] : undefined)"
          />
        </v-form>
      </v-card-text>
      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">{{ saveLabel }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
