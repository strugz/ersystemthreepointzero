<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { CostCenter, CreateUserInput, CrmEmployee, Department, UserDetail, UserProfileInput } from '../types';
import CrmEmployeePicker from './CrmEmployeePicker.vue';

/**
 * Create / edit account profile. Emits the typed input; the parent owns the API call, the row version
 * and 409 handling. Username and password are only collected on create — a password reset has its own
 * dialog so the admin never retypes profile fields just to change a credential.
 */
const props = withDefaults(
  defineProps<{
    mode: 'create' | 'edit';
    initial?: UserDetail | null;
    departments: Department[];
    costCenters: CostCenter[];
    saving?: boolean;
    disabled?: boolean;
    /** Server validation errors keyed by field (camelCase or PascalCase, both are mapped). */
    errors?: Record<string, string[]> | null;
  }>(),
  { initial: null, saving: false, disabled: false, errors: null },
);

const emit = defineEmits<{
  submit: [input: CreateUserInput];
  cancel: [];
}>();

interface FormState extends UserProfileInput {
  username: string;
  password: string;
  confirmPassword: string;
}

const blank = (): FormState => ({
  username: '',
  password: '',
  confirmPassword: '',
  fullName: '',
  position: '',
  jobGrade: '',
  email: '',
  mobileNo: '',
  departmentId: null,
  costCenterId: null,
  workWithEnabled: true,
  crmEmployeeId: null,
});

const form = reactive<FormState>(blank());
let snapshot = JSON.stringify(form);

function load(value: UserDetail | null) {
  Object.assign(form, blank());
  if (value) {
    Object.assign(form, {
      fullName: value.fullName,
      position: value.position ?? '',
      jobGrade: value.jobGrade ?? '',
      email: value.email ?? '',
      mobileNo: value.mobileNo ?? '',
      departmentId: value.departmentId,
      costCenterId: value.costCenterId,
      workWithEnabled: value.workWithEnabled,
      crmEmployeeId: value.crmEmployeeId,
    });
  }
  snapshot = JSON.stringify(form);
}
watch(() => props.initial, load, { immediate: true, deep: true });

const isCreate = computed(() => props.mode === 'create');
const isDirty = computed(() => JSON.stringify(form) !== snapshot);

const departmentItems = computed(() =>
  props.departments
    .filter((d) => d.isActive || d.id === form.departmentId)
    .map((d) => ({ value: d.id, title: d.code ? `${d.code} · ${d.name}` : d.name })),
);
// Cost centers follow the chosen department when they are linked to one; unlinked ones always show.
const costCenterItems = computed(() =>
  props.costCenters
    .filter((c) => (c.isActive || c.id === form.costCenterId) && (c.departmentId === null || form.departmentId === null || c.departmentId === form.departmentId))
    .map((c) => ({ value: c.id, title: `${c.code} · ${c.name}` })),
);

// Server errors arrive keyed either "fullName" or "FullName"; look up both.
function fieldErrors(field: string): string[] | undefined {
  const errors = props.errors;
  if (!errors) return undefined;
  const pascal = field.charAt(0).toUpperCase() + field.slice(1);
  return errors[field] ?? errors[pascal];
}

const passwordMismatch = computed(() => isCreate.value && form.confirmPassword !== '' && form.password !== form.confirmPassword);
const emailInvalid = computed(() => form.email !== '' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email ?? ''));

const canSubmit = computed(() => {
  if (props.saving || props.disabled) return false;
  if (!form.fullName.trim()) return false;
  if (emailInvalid.value) return false;
  if (isCreate.value) {
    return form.username.trim() !== '' && form.password.length >= 8 && form.password === form.confirmPassword;
  }
  return isDirty.value;
});

const text = (value: string | null): string | null => {
  const t = (value ?? '').trim();
  return t === '' ? null : t;
};

function submit() {
  if (!canSubmit.value) return;
  emit('submit', {
    username: form.username.trim(),
    password: form.password,
    fullName: form.fullName.trim(),
    position: text(form.position),
    jobGrade: text(form.jobGrade),
    email: text(form.email),
    mobileNo: text(form.mobileNo),
    departmentId: form.departmentId,
    costCenterId: form.costCenterId,
    workWithEnabled: form.workWithEnabled,
    crmEmployeeId: form.crmEmployeeId,
  });
}

// Picking a CRM person on a new account fills an empty name; it never overwrites one already typed.
function onCrmPicked(employee: CrmEmployee) {
  if (!form.fullName.trim() && employee.fullName) form.fullName = employee.fullName.trim();
}

defineExpose({ isDirty });
</script>

<template>
  <v-form :disabled="saving || disabled" @submit.prevent="submit">
    <template v-if="isCreate">
      <div class="lbl mb-2">Sign-in</div>
      <v-row dense>
        <v-col cols="12" sm="4">
          <v-text-field v-model="form.username" label="Username" autocomplete="off" maxlength="50" autofocus :error-messages="fieldErrors('username')" />
        </v-col>
        <v-col cols="12" sm="4">
          <v-text-field
            v-model="form.password"
            label="Temporary password"
            type="password"
            autocomplete="new-password"
            hint="At least 8 characters"
            persistent-hint
            :error-messages="fieldErrors('password')"
          />
        </v-col>
        <v-col cols="12" sm="4">
          <v-text-field
            v-model="form.confirmPassword"
            label="Confirm password"
            type="password"
            autocomplete="new-password"
            :error-messages="passwordMismatch ? ['Passwords do not match.'] : undefined"
          />
        </v-col>
      </v-row>
      <v-divider class="my-4" />
    </template>

    <div class="lbl mb-2">CRM link</div>
    <v-row dense>
      <v-col cols="12">
        <CrmEmployeePicker
          v-model="form.crmEmployeeId"
          :disabled="saving || disabled"
          :error-messages="fieldErrors('crmEmployeeId')"
          @picked="onCrmPicked"
        />
      </v-col>
    </v-row>
    <v-divider class="my-4" />

    <div class="lbl mb-2">Profile</div>
    <v-row dense>
      <v-col cols="12" sm="6">
        <v-text-field v-model="form.fullName" label="Full name" maxlength="100" :autofocus="!isCreate" :error-messages="fieldErrors('fullName')" />
      </v-col>
      <v-col cols="12" sm="6">
        <v-text-field v-model="form.position" label="Position" maxlength="100" :error-messages="fieldErrors('position')" />
      </v-col>
      <v-col cols="12" sm="6">
        <v-text-field v-model="form.jobGrade" label="Job grade" maxlength="20" :error-messages="fieldErrors('jobGrade')" />
      </v-col>
      <v-col cols="12" sm="6">
        <v-text-field
          v-model="form.email"
          label="Email"
          type="email"
          maxlength="200"
          :error-messages="fieldErrors('email') ?? (emailInvalid ? ['Enter a valid email address.'] : undefined)"
        />
      </v-col>
      <v-col cols="12" sm="6">
        <v-text-field v-model="form.mobileNo" label="Mobile no." type="tel" maxlength="30" hint="For SMS reminders" persistent-hint :error-messages="fieldErrors('mobileNo')" />
      </v-col>
      <v-col cols="12" sm="6">
        <v-select
          v-model="form.departmentId"
          :items="departmentItems"
          label="Department"
          clearable
          :error-messages="fieldErrors('departmentId')"
        />
      </v-col>
      <v-col cols="12" sm="6">
        <v-select
          v-model="form.costCenterId"
          :items="costCenterItems"
          label="Cost center"
          clearable
          :error-messages="fieldErrors('costCenterId')"
        />
      </v-col>
      <v-col cols="12" sm="6" class="d-flex align-center">
        <v-switch
          v-model="form.workWithEnabled"
          color="primary"
          label="Available as a “Work With” person on expense lines"
          hide-details
          :error-messages="fieldErrors('workWithEnabled')"
        />
      </v-col>
    </v-row>

    <div class="d-flex justify-end ga-2 mt-4">
      <v-btn class="press" variant="text" :disabled="saving" @click="emit('cancel')">Cancel</v-btn>
      <v-btn class="press" color="primary" variant="flat" type="submit" :loading="saving" :disabled="!canSubmit">
        {{ isCreate ? 'Create account' : 'Save changes' }}
      </v-btn>
    </div>
  </v-form>
</template>
