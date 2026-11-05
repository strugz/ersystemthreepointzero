<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { R47Format, SystemSettings, SystemSettingsInput } from '../types';

/** Workflow rules (approval thresholds, SLAs, filing cut-off, R47 reviewer and format, notifications). The parent owns the save. */
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
  r47ReviewedBy: '',
  r47SignatureMode: 'ESignature',
  r47Format: blankFormat(),
});

// The form as printed today; also what an API older than the R47 format is shown.
function blankFormat(): R47Format {
  return {
    engine: 'QuestPdf',
    companyName: 'Marsman Drysdale Medical Products, Inc.',
    companyShortName: 'MDMPI',
    title: 'EXPENSE REPORT',
    documentCode: 'R47',
    effectivityDate: 'May 15, 2023',
    revisionNo: '0',
  };
}

// Same limits as the server (Domain R47FormatRules): each value must fit its box on the R47 header.
const FORMAT_FIELDS = [
  { key: 'companyName', label: 'Company name', max: 50, cols: 8 },
  { key: 'companyShortName', label: 'Company short name', max: 20, cols: 4, hint: 'Author of the PDF and the BAL DUE TO line' },
  { key: 'title', label: 'Form title', max: 30, cols: 6 },
  { key: 'documentCode', label: 'Document code', max: 12, cols: 6 },
  { key: 'effectivityDate', label: 'Effectivity date', max: 18, cols: 6, hint: 'Printed as typed, e.g. May 15, 2023' },
  { key: 'revisionNo', label: 'Revision no.', max: 10, cols: 6 },
] as const;

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
      // Likewise for an API older than the REVIEWED BY rule.
      r47ReviewedBy: value.r47ReviewedBy ?? 'FINANCE',
      // And for an API older than the R47 signature rule.
      r47SignatureMode: value.r47SignatureMode ?? 'ESignature',
      // And for an API older than the R47 format.
      r47Format: { ...(value.r47Format ?? blankFormat()) },
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
// Same limit as the server (SystemSettingKeys.R47ReviewedByMaxLength).
const REVIEWED_BY_MAX = 60;
const reviewedByError = computed(() => {
  const v = form.r47ReviewedBy.trim();
  if (!v) return 'Enter who reviews the report, e.g. FINANCE.';
  return v.length > REVIEWED_BY_MAX ? `Use at most ${REVIEWED_BY_MAX} characters.` : null;
});
function formatError(key: (typeof FORMAT_FIELDS)[number]['key'], max: number): string | null {
  const v = form.r47Format[key].trim();
  if (!v) return 'Required.';
  return v.length > max ? `Use at most ${max} characters.` : null;
}
const formatInvalid = computed(() => FORMAT_FIELDS.some((f) => formatError(f.key, f.max) !== null));
const canSubmit = computed(
  () =>
    isDirty.value &&
    !props.saving &&
    !localInvalid.value &&
    !fromInvalid.value &&
    !domainsError.value &&
    !reviewedByError.value &&
    !formatInvalid.value,
);

function submit() {
  if (!canSubmit.value) return;
  const out: SystemSettingsInput = { ...form };
  for (const k of numericFields) {
    out[k] = Number(form[k]) || 0;
  }
  out.notificationFromAddress = (form.notificationFromAddress ?? '').trim() || null;
  out.emailDomains = [...new Set(form.emailDomains.map(cleanDomain).filter(Boolean))];
  out.r47ReviewedBy = form.r47ReviewedBy.trim();
  out.r47Format = { ...form.r47Format };
  for (const f of FORMAT_FIELDS) {
    out.r47Format[f.key] = form.r47Format[f.key].trim();
  }
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
      <div class="card-h"><span class="card-t">Expense report (R47)</span></div>
      <div class="pa-4">
        <v-text-field
          v-model="form.r47ReviewedBy"
          label="Reviewed by"
          :counter="REVIEWED_BY_MAX"
          hint="Printed in REVIEWED BY on the R47, e.g. FINANCE, unless the employee (Users) or the department (Master data) has its own"
          persistent-hint
          :error-messages="fieldErrors('r47ReviewedBy') ?? (reviewedByError ? [reviewedByError] : undefined)"
        />
        <v-radio-group
          v-model="form.r47SignatureMode"
          label="Signatures on the R47"
          class="mt-4"
          hide-details="auto"
          :error-messages="fieldErrors('r47SignatureMode')"
        >
          <v-radio value="ESignature">
            <template #label>
              <span>
                <strong>E-signature</strong>
                <span class="text-medium-emphasis"> · signatures Finance approved print with an ELECTRONIC SIGNATURE watermark and the date and time</span>
              </span>
            </template>
          </v-radio>
          <v-radio value="StatusOnly">
            <template #label>
              <span>
                <strong>Status only</strong>
                <span class="text-medium-emphasis"> · no signature images; each place prints "Electronically approved" (or "filed") with the date and time</span>
              </span>
            </template>
          </v-radio>
        </v-radio-group>

        <div class="text-subtitle-2 mt-6 mb-1">Format of the R47</div>
        <v-radio-group
          v-model="form.r47Format.engine"
          label="PDF engine"
          hide-details="auto"
          :error-messages="fieldErrors('r47Format.engine')"
        >
          <v-radio value="QuestPdf">
            <template #label>
              <span>
                <strong>QuestPDF</strong>
                <span class="text-medium-emphasis"> · the original R47 renderer</span>
              </span>
            </template>
          </v-radio>
          <v-radio value="PdfSharp">
            <template #label>
              <span>
                <strong>PDFsharp</strong>
                <span class="text-medium-emphasis"> · the same form drawn with PDFsharp</span>
              </span>
            </template>
          </v-radio>
        </v-radio-group>
        <div class="text-caption text-medium-emphasis mb-3">
          Both print the same form and page breaks. The change applies to the next R47 printed or emailed; no restart.
        </div>
        <v-row dense>
          <v-col v-for="f in FORMAT_FIELDS" :key="f.key" cols="12" :sm="f.cols">
            <v-text-field
              v-model="form.r47Format[f.key]"
              :label="f.label"
              :counter="f.max"
              :hint="'hint' in f ? f.hint : undefined"
              :persistent-hint="'hint' in f"
              :error-messages="fieldErrors(`r47Format.${f.key}`) ?? (formatError(f.key, f.max) ? [formatError(f.key, f.max)!] : undefined)"
            />
          </v-col>
        </v-row>
        <div class="text-caption text-medium-emphasis">
          Printed in the R47 header on every page. Fonts, page size and rows per page stay in the server settings.
        </div>
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
