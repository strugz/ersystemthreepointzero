<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { SystemSettings, SystemSettingsInput } from '../types';

/** Workflow rules (approval thresholds, SLAs, filing cut-off, notifications). The parent owns the save. */
const props = withDefaults(
  defineProps<{
    initial: SystemSettings | null;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
  }>(),
  { saving: false, errors: null },
);

const emit = defineEmits<{ submit: [input: SystemSettingsInput] }>();

const blank = (): SystemSettingsInput => ({
  level2ThresholdAmount: 0,
  level2AlwaysRequired: false,
  slaDaysLevel1: 0,
  slaDaysLevel2: 0,
  escalationAfterDays: 0,
  filingCutoffDays: 0,
  receiptRequiredAboveAmount: 0,
  coolingPeriodDays: 0,
  emailNotificationsEnabled: false,
  smsNotificationsEnabled: false,
  notificationFromAddress: '',
  emailDomains: [],
});

const form = reactive<SystemSettingsInput>(blank());
let snapshot = JSON.stringify(form);

function load(value: SystemSettings | null) {
  Object.assign(form, blank());
  if (value) {
    Object.assign(form, {
      level2ThresholdAmount: value.level2ThresholdAmount,
      level2AlwaysRequired: value.level2AlwaysRequired,
      slaDaysLevel1: value.slaDaysLevel1,
      slaDaysLevel2: value.slaDaysLevel2,
      escalationAfterDays: value.escalationAfterDays,
      filingCutoffDays: value.filingCutoffDays,
      receiptRequiredAboveAmount: value.receiptRequiredAboveAmount,
      coolingPeriodDays: value.coolingPeriodDays,
      emailNotificationsEnabled: value.emailNotificationsEnabled,
      smsNotificationsEnabled: value.smsNotificationsEnabled,
      notificationFromAddress: value.notificationFromAddress ?? '',
      // An API older than the domain list sends none; show the built-in default.
      emailDomains: [...(value.emailDomains ?? ['marsmandrysdale.com'])],
    } satisfies SystemSettingsInput);
  }
  snapshot = JSON.stringify(form);
}
watch(() => props.initial, load, { immediate: true, deep: true });

const isDirty = computed(() => JSON.stringify(form) !== snapshot);

function fieldErrors(field: string): string[] | undefined {
  const errors = props.errors;
  if (!errors) return undefined;
  const pascal = field.charAt(0).toUpperCase() + field.slice(1);
  return errors[field] ?? errors[pascal];
}

const nonNegative = (v: unknown) => Number(v) >= 0;
const numericFields = [
  'level2ThresholdAmount',
  'slaDaysLevel1',
  'slaDaysLevel2',
  'escalationAfterDays',
  'filingCutoffDays',
  'receiptRequiredAboveAmount',
  'coolingPeriodDays',
] as const;
const localInvalid = computed(() => numericFields.some((k) => !Number.isFinite(Number(form[k])) || !nonNegative(form[k])));
const fromInvalid = computed(() => {
  const v = (form.notificationFromAddress ?? '').trim();
  return v !== '' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
});
// Same shape the server accepts (EmailDomainRules); the server is the authority.
const DOMAIN = /^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+$/;
const cleanDomain = (d: string) => d.trim().replace(/^@+/, '').toLowerCase();
const domainsError = computed(() => {
  const list = form.emailDomains.map(cleanDomain).filter(Boolean);
  if (list.length === 0) return 'Add at least one email domain.';
  const bad = list.find((d) => !DOMAIN.test(d));
  return bad ? `"${bad}" is not a valid email domain, e.g. marsmandrysdale.com.` : null;
});
const canSubmit = computed(() => isDirty.value && !props.saving && !localInvalid.value && !fromInvalid.value && !domainsError.value);

function submit() {
  if (!canSubmit.value) return;
  const out: SystemSettingsInput = { ...form };
  for (const k of numericFields) {
    out[k] = Number(form[k]) || 0;
  }
  out.notificationFromAddress = (form.notificationFromAddress ?? '').trim() || null;
  out.emailDomains = [...new Set(form.emailDomains.map(cleanDomain).filter(Boolean))];
  emit('submit', out);
}

function reset() {
  load(props.initial);
}

defineExpose({ isDirty });
</script>

<template>
  <v-form :disabled="saving" @submit.prevent="submit">
    <v-card class="mb-4">
      <div class="card-h"><span class="card-t">Approval routing</span></div>
      <div class="pa-4">
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="form.level2ThresholdAmount"
              type="number"
              min="0"
              step="0.01"
              prefix="₱"
              label="Level 2 threshold amount"
              hint="Reports at or above this total also require a Signatory Level 2 approval"
              persistent-hint
              :error-messages="fieldErrors('level2ThresholdAmount')"
            />
          </v-col>
          <v-col cols="12" sm="6" class="d-flex align-center">
            <v-switch v-model="form.level2AlwaysRequired" color="primary" label="Always require Level 2 approval" hide-details :error-messages="fieldErrors('level2AlwaysRequired')" />
          </v-col>
        </v-row>
      </div>
    </v-card>

    <v-card class="mb-4">
      <div class="card-h"><span class="card-t">Service levels and escalation</span></div>
      <div class="pa-4">
        <v-row dense>
          <v-col cols="12" sm="4">
            <v-text-field v-model.number="form.slaDaysLevel1" type="number" min="0" step="1" label="SLA days, Level 1" suffix="days" :error-messages="fieldErrors('slaDaysLevel1')" />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field v-model.number="form.slaDaysLevel2" type="number" min="0" step="1" label="SLA days, Level 2" suffix="days" :error-messages="fieldErrors('slaDaysLevel2')" />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="form.escalationAfterDays"
              type="number"
              min="0"
              step="1"
              label="Escalate after"
              suffix="days"
              hint="Reminder escalation once an approval is overdue"
              persistent-hint
              :error-messages="fieldErrors('escalationAfterDays')"
            />
          </v-col>
        </v-row>
      </div>
    </v-card>

    <v-card class="mb-4">
      <div class="card-h"><span class="card-t">Filing rules</span></div>
      <div class="pa-4">
        <v-row dense>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="form.filingCutoffDays"
              type="number"
              min="0"
              step="1"
              label="Filing cut-off"
              suffix="days"
              hint="Expenses older than this cannot be filed"
              persistent-hint
              :error-messages="fieldErrors('filingCutoffDays')"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="form.receiptRequiredAboveAmount"
              type="number"
              min="0"
              step="0.01"
              prefix="₱"
              label="Receipt required above"
              hint="Lines above this amount need a scanned receipt"
              persistent-hint
              :error-messages="fieldErrors('receiptRequiredAboveAmount')"
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model.number="form.coolingPeriodDays"
              type="number"
              min="0"
              step="1"
              label="New-account cooling period"
              suffix="days"
              hint="A new account cannot file until this many days after creation"
              persistent-hint
              :error-messages="fieldErrors('coolingPeriodDays')"
            />
          </v-col>
        </v-row>
      </div>
    </v-card>

    <v-card class="mb-4">
      <div class="card-h"><span class="card-t">Notifications</span></div>
      <div class="pa-4">
        <v-row dense>
          <v-col cols="12" sm="3">
            <v-switch v-model="form.emailNotificationsEnabled" color="primary" label="Email" hide-details :error-messages="fieldErrors('emailNotificationsEnabled')" />
          </v-col>
          <v-col cols="12" sm="3">
            <v-switch v-model="form.smsNotificationsEnabled" color="primary" label="SMS" hide-details :error-messages="fieldErrors('smsNotificationsEnabled')" />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.notificationFromAddress"
              type="email"
              label="Notification from address"
              :error-messages="fieldErrors('notificationFromAddress') ?? (fromInvalid ? ['Enter a valid email address.'] : undefined)"
            />
          </v-col>
        </v-row>
      </div>
    </v-card>

    <v-card class="mb-4">
      <div class="card-h"><span class="card-t">Account email domains</span></div>
      <div class="pa-4">
        <v-combobox
          v-model="form.emailDomains"
          label="Email domains"
          placeholder="Type a domain and press Enter"
          multiple
          chips
          closable-chips
          prefix="@"
          hint="Offered in the Email dropdown on the user form. The first one is the default for new accounts."
          persistent-hint
          :error-messages="fieldErrors('emailDomains') ?? (domainsError ? [domainsError] : undefined)"
        />
      </div>
    </v-card>

    <div class="d-flex justify-end ga-2">
      <v-btn class="press" variant="text" :disabled="!isDirty || saving" @click="reset">Discard changes</v-btn>
      <v-btn class="press" color="primary" variant="flat" type="submit" :loading="saving" :disabled="!canSubmit">Save rules</v-btn>
    </div>
  </v-form>
</template>
