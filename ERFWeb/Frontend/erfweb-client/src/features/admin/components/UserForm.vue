<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import type { CreateUserInput, CrmEmployee, Department, UserDetail, UserProfileInput } from '../types';
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
    /** Company email domains from the settings; the first is the default. */
    emailDomains?: string[];
    saving?: boolean;
    disabled?: boolean;
    /** Server validation errors keyed by field (camelCase or PascalCase, both are mapped). */
    errors?: Record<string, string[]> | null;
  }>(),
  { initial: null, emailDomains: () => ['marsmandrysdale.com'], saving: false, disabled: false, errors: null },
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
  r47ReviewedBy: '',
});

const form = reactive<FormState>(blank());
let snapshot = JSON.stringify(form);

// Email is typed as a name plus a company domain from the dropdown. Typing a full address (with '@')
// keeps it as typed, for an address at a domain that is not in the list.
const emailName = ref('');
const emailDomain = ref('');
const defaultDomain = computed(() => props.emailDomains[0] ?? '');
const domainItems = computed(() =>
  emailDomain.value && !props.emailDomains.includes(emailDomain.value) ? [...props.emailDomains, emailDomain.value] : props.emailDomains,
);
const typedFullAddress = computed(() => emailName.value.includes('@'));

function splitEmail(email: string) {
  const at = email.lastIndexOf('@');
  const domain = at > 0 ? props.emailDomains.find((d) => d === email.slice(at + 1).toLowerCase()) : undefined;
  emailName.value = domain ? email.slice(0, at) : email;
  emailDomain.value = domain ?? defaultDomain.value;
}

function composeEmail(): string {
  const name = emailName.value.trim();
  if (!name || name.includes('@')) return name;
  return emailDomain.value ? `${name}@${emailDomain.value}` : name;
}

// Synchronous, so the form is complete before load() takes its "unchanged" snapshot.
watch([emailName, emailDomain], () => (form.email = composeEmail()), { flush: 'sync' });
// The domain list arrives from the settings after the form may have loaded: split the address again.
watch(() => props.emailDomains, () => splitEmail(form.email ?? ''));

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
      r47ReviewedBy: value.r47ReviewedBy ?? '',
    });
  }
  splitEmail(form.email ?? '');
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
// What prints in REVIEWED BY while this account has none of its own.
const reviewedByFallback = computed(() => {
  const own = props.departments.find((d) => d.id === form.departmentId)?.reviewedBy;
  return own ? `the department's reviewer (${own})` : 'the default on the Rules page';
});

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
    // Not edited here any more (a report counts against its department's cost center); sent back unchanged.
    costCenterId: form.costCenterId,
    workWithEnabled: form.workWithEnabled,
    crmEmployeeId: form.crmEmployeeId,
    r47ReviewedBy: form.r47ReviewedBy.trim(),
  });
}

// Picking a CRM person fills an empty name and mobile number from the CRM; it never overwrites what is already typed.
function onCrmPicked(employee: CrmEmployee) {
  if (!form.fullName.trim() && employee.fullName) form.fullName = employee.fullName.trim();
  if (!(form.mobileNo ?? '').trim() && employee.mobileNo) form.mobileNo = employee.mobileNo.trim();
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
        <div class="d-flex ga-2">
          <v-text-field
            v-model="emailName"
            label="Email"
            autocomplete="off"
            maxlength="200"
            class="flex-grow-1"
            :error-messages="fieldErrors('email') ?? (emailInvalid ? ['Enter a valid email address.'] : undefined)"
          />
          <v-select
            v-model="emailDomain"
            :items="domainItems"
            label="Domain"
            prefix="@"
            :disabled="typedFullAddress"
            class="email-domain"
          />
        </div>
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
        <v-text-field
          v-model="form.r47ReviewedBy"
          label="Reviewed by (R47)"
          maxlength="60"
          :hint="`Printed in REVIEWED BY on this employee's R47; leave blank to use ${reviewedByFallback}`"
          persistent-hint
          :error-messages="fieldErrors('r47ReviewedBy')"
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

<style scoped>
.email-domain {
  flex: 0 1 55%;
  min-width: 0;
}
</style>
