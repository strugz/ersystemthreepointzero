<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { todayIso } from '@/shared/format';
import { fieldErrors, personItems, textOrNull } from '../forms';
import type { FieldErrors } from '../forms';
import { targetTypeOptions } from '../labels';
import type { CreateRateAssignmentInput, DepartmentOption, PersonOption, RateAssignment, RateTargetType } from '../types';

/**
 * Assigns a profile to one target. The target type decides the single field sent (person, job
 * grade, department, or nothing for company-wide). An open assignment for the same target is
 * closed by the server. Starting before today while ending another assignment for the same target
 * needs the backdate override and a reason; a first assignment for a target only fills a gap.
 */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    profileId: number;
    profileName: string;
    people: PersonOption[];
    departments: DepartmentOption[];
    /** Every assignment, ended ones included; null while unknown (then any backdate asks for the override). */
    existing?: RateAssignment[] | null;
    saving?: boolean;
    errors?: FieldErrors;
  }>(),
  { existing: null, saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [input: CreateRateAssignmentInput];
}>();

interface Form {
  targetType: RateTargetType;
  userId: number | null;
  jobGrade: string;
  departmentId: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  changeReason: string;
  backdateOverride: boolean;
}

const today = todayIso();
const blank = (): Form => ({
  targetType: 'Individual',
  userId: null,
  jobGrade: '',
  departmentId: null,
  effectiveFrom: today,
  effectiveTo: null,
  changeReason: '',
  backdateOverride: false,
});

const form = reactive<Form>(blank());

watch(
  () => props.modelValue,
  (open) => {
    if (open) Object.assign(form, blank());
  },
);

const personOptions = computed(() => personItems(props.people));
const departmentItems = computed(() => props.departments.map((d) => ({ value: d.id, title: d.name })));

const sameTarget = (a: RateAssignment) => {
  if (a.targetType !== form.targetType) return false;
  switch (form.targetType) {
    case 'Individual':
      return a.userId === form.userId;
    case 'Grade':
      return a.jobGrade === form.jobGrade.trim();
    case 'Department':
      return a.departmentId === form.departmentId;
    default:
      return true;
  }
};

// Mirrors the server: the new assignment ends any for the same target that overlaps its period.
// With none, a past start only fills a gap (e.g. claims from before go-live).
const replacesEarlierAssignment = computed(() => {
  if (!props.existing) return true;
  const from = form.effectiveFrom;
  const to = form.effectiveTo || null;
  return props.existing.some((a) => sameTarget(a) && (!to || a.effectiveFrom <= to) && (!a.effectiveTo || a.effectiveTo >= from));
});
const isBackdated = computed(() => !!form.effectiveFrom && form.effectiveFrom < today && replacesEarlierAssignment.value);
watch(isBackdated, (b) => {
  if (!b) form.backdateOverride = false;
});
const reasonMissing = computed(() => isBackdated.value && !form.changeReason.trim());
const toBeforeFrom = computed(() => !!form.effectiveTo && !!form.effectiveFrom && form.effectiveTo < form.effectiveFrom);

const targetChosen = computed(() => {
  switch (form.targetType) {
    case 'Individual':
      return form.userId !== null;
    case 'Grade':
      return form.jobGrade.trim() !== '';
    case 'Department':
      return form.departmentId !== null;
    default:
      return true;
  }
});

const canSave = computed(
  () => !props.saving && targetChosen.value && !toBeforeFrom.value && (!isBackdated.value || (form.backdateOverride && !reasonMissing.value)),
);

const err = (field: string) => fieldErrors(props.errors, field);

function save() {
  if (!canSave.value) return;
  const t = form.targetType;
  emit('save', {
    profileId: props.profileId,
    targetType: t,
    userId: t === 'Individual' ? form.userId : null,
    jobGrade: t === 'Grade' ? form.jobGrade.trim() : null,
    departmentId: t === 'Department' ? form.departmentId : null,
    effectiveFrom: form.effectiveFrom || null,
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
  <v-dialog :model-value="modelValue" max-width="560" scrollable :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="text-subtitle-1 font-weight-bold">Assign {{ profileName }}</v-card-title>
      <v-card-text>
        <v-form :disabled="saving" @submit.prevent="save">
          <fieldset class="group">
            <legend class="lbl">Applies to</legend>
            <v-radio-group v-model="form.targetType" inline hide-details="auto" :error-messages="err('targetType')">
              <v-radio v-for="o in targetTypeOptions" :key="o.value" :value="o.value" :label="o.title" />
            </v-radio-group>
          </fieldset>

          <div class="mt-3">
            <v-autocomplete
              v-if="form.targetType === 'Individual'"
              v-model="form.userId"
              :items="personOptions"
              item-props
              label="Person *"
              :error-messages="err('userId')"
            />
            <v-text-field
              v-else-if="form.targetType === 'Grade'"
              v-model="form.jobGrade"
              label="Job grade *"
              maxlength="20"
              hint="As recorded on the employee's profile"
              persistent-hint
              :error-messages="err('jobGrade')"
            />
            <v-autocomplete
              v-else-if="form.targetType === 'Department'"
              v-model="form.departmentId"
              :items="departmentItems"
              label="Department *"
              :error-messages="err('departmentId')"
              :no-data-text="departments.length ? 'No match' : 'No departments available'"
            />
            <v-alert v-else type="info" variant="tonal" density="compact">
              Company-wide applies to everyone without a more specific assignment.
            </v-alert>
          </div>

          <v-row dense class="mt-1">
            <v-col cols="6">
              <v-text-field v-model="form.effectiveFrom" type="date" label="Effective from" :error-messages="err('effectiveFrom')" />
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

          <p class="text-caption text-medium-emphasis mb-2">An open assignment for the same target is ended automatically.</p>

          <v-expand-transition>
            <div v-if="isBackdated">
              <v-alert type="warning" variant="tonal" density="compact" class="mb-2">
                This assignment starts before today. Confirm the backdate and give a reason.
              </v-alert>
              <v-checkbox v-model="form.backdateOverride" color="warning" label="Backdate override" hide-details="auto" :error-messages="err('backdateOverride')" />
            </div>
          </v-expand-transition>

          <v-textarea
            v-model="form.changeReason"
            :label="isBackdated ? 'Reason for change *' : 'Reason for change (optional)'"
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
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">Assign</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.group {
  border: 0;
  margin: 0;
  padding: 0;
  min-width: 0;
}
</style>
