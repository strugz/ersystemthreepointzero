<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { addDaysIso, formatDate, todayIso } from '@/shared/format';
import { fieldErrors, numberOrNull, textOrNull } from '../forms';
import type { FieldErrors } from '../forms';
import { categoryOptions, mealOptions, rateKeyLabel, rateTypeOptions } from '../labels';
import type { AddRateLineInput, MealSubCategory, RateCategory, RateLine, RateType } from '../types';

/**
 * Adds a rate line version. A new version closes the open one for the same category/meal the day
 * before it starts. Starting before today is a backdate: it needs the explicit override and a
 * reason, both recorded on the line.
 */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    /** Prefill from the version being superseded (category and meal become fixed). */
    from?: RateLine | null;
    saving?: boolean;
    errors?: FieldErrors;
  }>(),
  { from: null, saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: AddRateLineInput];
}>();

interface Form {
  category: RateCategory;
  subCategory: MealSubCategory | null;
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
    const l = props.from;
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
  },
);

const isMeals = computed(() => form.category === 'Meals');
watch(
  () => form.category,
  (c) => {
    if (c !== 'Meals') form.subCategory = null;
    else if (!form.subCategory) form.subCategory = 'Breakfast';
  },
);

const isBackdated = computed(() => !!form.effectiveFrom && form.effectiveFrom < today);
watch(isBackdated, (b) => {
  if (!b) form.backdateOverride = false;
});

const toBeforeFrom = computed(() => !!form.effectiveTo && !!form.effectiveFrom && form.effectiveTo < form.effectiveFrom);
const reasonRequired = computed(() => isBackdated.value);
const reasonMissing = computed(() => reasonRequired.value && !form.changeReason.trim());
const amountValue = computed(() => numberOrNull(form.amount));

const canSave = computed(
  () =>
    !props.saving &&
    amountValue.value !== null &&
    amountValue.value >= 0 &&
    !!form.effectiveFrom &&
    !toBeforeFrom.value &&
    (!isMeals.value || !!form.subCategory) &&
    (!isBackdated.value || (form.backdateOverride && !reasonMissing.value)),
);

const err = (field: string) => fieldErrors(props.errors, field);
const title = computed(() => (props.from ? `New version · ${rateKeyLabel(props.from.category, props.from.subCategory)}` : 'Add rate line'));

function save() {
  if (!canSave.value) return;
  const cap = numberOrNull(form.frequencyCap);
  emit('save', {
    category: form.category,
    subCategory: isMeals.value ? form.subCategory : null,
    rateType: form.rateType,
    amount: amountValue.value as number,
    currency: textOrNull(form.currency)?.toUpperCase() ?? null,
    frequencyCap: cap === null ? null : Math.trunc(cap),
    requiresReceipt: form.requiresReceipt,
    effectiveFrom: form.effectiveFrom,
    effectiveTo: form.effectiveTo || null,
    changeReason: textOrNull(form.changeReason),
    backdateOverride: isBackdated.value && form.backdateOverride,
  });
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
              <v-select v-model="form.category" :items="categoryOptions" label="Category" :disabled="saving || !!from" :error-messages="err('category')" />
            </v-col>
            <v-col v-if="isMeals" cols="12" sm="6">
              <v-select v-model="form.subCategory" :items="mealOptions" label="Meal *" :disabled="saving || !!from" :error-messages="err('subCategory')" />
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

          <p v-if="from && form.effectiveFrom" class="text-caption text-medium-emphasis mt-1 mb-2">
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
            :error-messages="err('changeReason') ?? (reasonMissing && form.backdateOverride ? ['A reason is required to backdate.'] : undefined)"
          />
        </v-form>
      </v-card-text>
      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">Add line</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
