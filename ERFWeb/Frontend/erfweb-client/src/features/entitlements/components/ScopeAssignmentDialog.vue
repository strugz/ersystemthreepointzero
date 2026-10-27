<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { todayIso } from '@/shared/format';
import { fieldErrors, numberOrNull, personItems, textOrNull } from '../forms';
import type { FieldErrors } from '../forms';
import { scopeLevelOptions, scopeTypeHint, scopeTypeOptions } from '../labels';
import type {
  CreateScopeAssignmentInput,
  DepartmentOption,
  PersonOption,
  ScopeAssignment,
  ScopeAssignmentType,
  ScopeLevel,
  UpdateScopeAssignmentInput,
} from '../types';

/**
 * Create or edit one scope assignment. On create the type decides which fields apply:
 * Individual = named members only, Department = the whole department, Exclusion = department
 * minus the named members. Every field can be edited; an unchanged effective-from date may stay
 * in the past. The server is the authority on who may set which level and whether the result
 * would give someone two signatories (errors.scope).
 */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    assignment?: ScopeAssignment | null;
    people: PersonOption[];
    departments: DepartmentOption[];
    /** Level preselected on create (the view's level filter). */
    defaultLevel?: ScopeLevel | null;
    saving?: boolean;
    errors?: FieldErrors;
  }>(),
  { assignment: null, defaultLevel: null, saving: false, errors: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  create: [input: CreateScopeAssignmentInput];
  update: [input: UpdateScopeAssignmentInput];
}>();

interface Form {
  holderUserId: number | null;
  level: ScopeLevel;
  type: ScopeAssignmentType;
  departmentId: number | null;
  memberUserIds: number[];
  approvalLimit: number | string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  note: string;
}

function blank(): Form {
  return {
    holderUserId: null,
    level: props.defaultLevel ?? 1,
    type: 'Department',
    departmentId: null,
    memberUserIds: [],
    approvalLimit: null,
    effectiveFrom: todayIso(),
    effectiveTo: null,
    isActive: true,
    note: '',
  };
}

const form = reactive<Form>(blank());
const isEdit = computed(() => props.assignment !== null);
const today = todayIso();

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    Object.assign(form, blank());
    const a = props.assignment;
    if (a) {
      Object.assign(form, {
        holderUserId: a.holderUserId,
        level: a.level,
        type: a.type,
        departmentId: a.departmentId,
        memberUserIds: a.members.map((m) => m.userId),
        approvalLimit: a.approvalLimit,
        effectiveFrom: a.effectiveFrom,
        effectiveTo: a.effectiveTo,
        isActive: a.isActive,
        note: a.note ?? '',
      } satisfies Form);
    }
  },
);

const usesDepartment = computed(() => form.type !== 'Individual');
const usesMembers = computed(() => form.type !== 'Department');

// Switching type drops fields that no longer apply, so the request carries only what the type needs.
watch(
  () => form.type,
  (type) => {
    if (type === 'Individual') form.departmentId = null;
    if (type === 'Department') form.memberUserIds = [];
  },
);

// The current holder and department stay selectable on edit even if they no longer appear in the lists
// (holder lost the role, department deactivated); the server checks them only when they change.
const holderItems = computed(() => {
  const items = personItems(props.people);
  const a = props.assignment;
  if (a && !items.some((i) => i.value === a.holderUserId)) items.push({ value: a.holderUserId, title: a.holderName, subtitle: undefined });
  return items;
});
const departmentItems = computed(() => {
  const items = props.departments.map((d) => ({ value: d.id, title: d.name }));
  const a = props.assignment;
  if (a?.departmentId && a.departmentName && !items.some((i) => i.value === a.departmentId)) items.push({ value: a.departmentId, title: a.departmentName });
  return items;
});
const memberItems = computed(() => {
  // Keep current members selectable even if they no longer appear in coverage (e.g. deactivated).
  const items = personItems(props.people.filter((p) => p.userId !== form.holderUserId));
  for (const m of props.assignment?.members ?? []) {
    if (!items.some((i) => i.value === m.userId)) items.push({ value: m.userId, title: m.fullName, subtitle: undefined });
  }
  return items;
});

const membersLabel = computed(() => (form.type === 'Exclusion' ? 'People excluded *' : 'People covered *'));
// An edit may keep a start date that has already passed; only a new start date must be today or later.
const originalFrom = computed(() => props.assignment?.effectiveFrom ?? null);
const fromMin = computed(() => (originalFrom.value && originalFrom.value < today ? originalFrom.value : today));
const fromInPast = computed(() => !!form.effectiveFrom && form.effectiveFrom < today && form.effectiveFrom !== originalFrom.value);
const toBeforeFrom = computed(() => !!form.effectiveTo && !!form.effectiveFrom && form.effectiveTo < form.effectiveFrom);

const canSave = computed(() => {
  if (props.saving || toBeforeFrom.value) return false;
  if (usesMembers.value && form.memberUserIds.length === 0) return false;
  if (!form.holderUserId || fromInPast.value) return false;
  if (usesDepartment.value && !form.departmentId) return false;
  return true;
});

const err = (field: string) => fieldErrors(props.errors, field);
const scopeErrors = computed(() => err('scope') ?? []);

function save() {
  if (!canSave.value) return;
  const approvalLimit = numberOrNull(form.approvalLimit);
  const effectiveTo = form.effectiveTo || null;
  const note = textOrNull(form.note);
  if (props.assignment) {
    emit('update', {
      holderUserId: form.holderUserId as number,
      level: form.level,
      type: form.type,
      departmentId: usesDepartment.value ? form.departmentId : null,
      effectiveFrom: form.effectiveFrom || null,
      memberUserIds: usesMembers.value ? form.memberUserIds : [],
      approvalLimit,
      effectiveTo,
      isActive: form.isActive,
      note,
      rowVersion: props.assignment.rowVersion,
    });
    return;
  }
  emit('create', {
    holderUserId: form.holderUserId as number,
    level: form.level,
    type: form.type,
    departmentId: usesDepartment.value ? form.departmentId : null,
    memberUserIds: usesMembers.value ? form.memberUserIds : [],
    approvalLimit,
    effectiveFrom: form.effectiveFrom || null,
    effectiveTo,
    note,
  });
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="620" scrollable :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="d-flex align-center ga-2">
        <span class="text-subtitle-1 font-weight-bold">{{ isEdit ? 'Edit scope assignment' : 'New scope assignment' }}</span>
        <v-chip v-if="assignment?.isBootstrap" size="x-small" label variant="tonal" color="primary">Bootstrap</v-chip>
      </v-card-title>

      <v-card-text>
        <v-alert v-if="scopeErrors.length" type="error" variant="tonal" density="compact" class="mb-4">
          <div v-for="e in scopeErrors" :key="e">{{ e }}</div>
        </v-alert>

        <v-form :disabled="saving" @submit.prevent="save">
          <v-alert v-if="assignment" type="info" variant="tonal" density="compact" class="mb-4">
            Changes apply to reports filed from now on. Reports already filed keep the signatories they were routed to.
          </v-alert>

          <v-autocomplete
            v-model="form.holderUserId"
            :items="holderItems"
            item-props
            label="Signatory *"
            :autofocus="!assignment"
            :error-messages="err('holderUserId')"
            hint="The person who signs. The server checks they hold the matching signatory role."
            persistent-hint
            class="mb-2"
          />

          <fieldset class="group">
            <legend class="lbl">Level</legend>
            <v-radio-group v-model="form.level" inline hide-details="auto" :error-messages="err('level')">
              <v-radio v-for="o in scopeLevelOptions" :key="o.value" :value="o.value" :label="o.title" />
            </v-radio-group>
          </fieldset>

          <fieldset class="group">
            <legend class="lbl">Covers</legend>
            <v-radio-group v-model="form.type" hide-details="auto" :error-messages="err('type')">
              <v-radio v-for="o in scopeTypeOptions" :key="o.value" :value="o.value">
                <template #label>
                  <div>
                    <div>{{ o.title }}</div>
                    <div class="text-caption text-medium-emphasis">{{ scopeTypeHint[o.value] }}</div>
                  </div>
                </template>
              </v-radio>
            </v-radio-group>
          </fieldset>

          <v-expand-transition>
            <div v-if="usesDepartment">
              <v-autocomplete
                v-model="form.departmentId"
                :items="departmentItems"
                label="Department *"
                class="mt-3"
                :error-messages="err('departmentId')"
                :no-data-text="departments.length ? 'No match' : 'No departments available'"
              />
            </div>
          </v-expand-transition>

          <v-expand-transition>
            <div v-if="usesMembers">
              <v-autocomplete
                v-model="form.memberUserIds"
                :items="memberItems"
                item-props
                :label="membersLabel"
                multiple
                chips
                closable-chips
                class="mt-1"
                :error-messages="err('memberUserIds')"
              />
            </div>
          </v-expand-transition>

          <v-row dense class="mt-1">
            <v-col cols="12" sm="4">
              <v-text-field
                v-model="form.approvalLimit"
                type="number"
                min="0"
                step="0.01"
                label="Approval limit"
                prefix="₱"
                hint="Blank for no limit"
                persistent-hint
                :error-messages="err('approvalLimit')"
              />
            </v-col>
            <v-col cols="6" sm="4">
              <v-text-field
                v-model="form.effectiveFrom"
                type="date"
                :min="fromMin"
                label="Effective from"
                :error-messages="fromInPast ? ['Cannot start in the past.'] : err('effectiveFrom')"
              />
            </v-col>
            <v-col cols="6" sm="4">
              <v-text-field
                v-model="form.effectiveTo"
                type="date"
                :min="form.effectiveFrom || today"
                label="Effective to"
                clearable
                hint="Blank for open-ended"
                persistent-hint
                :error-messages="toBeforeFrom ? ['Ends before it starts.'] : err('effectiveTo')"
              />
            </v-col>
          </v-row>

          <v-switch
            v-if="assignment"
            v-model="form.isActive"
            color="primary"
            label="Active"
            hint="Deactivating stops this signatory covering these people immediately."
            persistent-hint
            class="mt-1"
          />

          <v-textarea v-model="form.note" label="Note (optional)" rows="2" auto-grow maxlength="500" counter class="mt-2" :error-messages="err('note')" />
        </v-form>
      </v-card-text>

      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">
          {{ isEdit ? 'Save' : 'Create' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.group {
  border: 0;
  margin: 8px 0 0;
  padding: 0;
  min-width: 0;
}
</style>
