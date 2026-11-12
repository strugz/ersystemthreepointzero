<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import type { FinanceRules, R47Format, RulesSection, SystemSettings, SystemSettingsInput } from '../types';

/** Workflow rules (approval thresholds, SLAs, filing cut-off, Finance rules, R47 reviewer and format, notifications, sign-in lockout). The parent owns the save. */
const props = withDefaults(
  defineProps<{
    initial: SystemSettings | null;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
    /** The Rules tab to show; null shows every card. One form either way, so a save keeps every tab's edits. */
    section?: RulesSection | null;
  }>(),
  { saving: false, errors: null, section: null },
);

const shows = (s: RulesSection) => props.section === null || props.section === s;

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
  loginMaxFailedAttempts: LOCKOUT.attempts.default,
  loginLockoutMinutes: LOCKOUT.minutes.default,
  financeRules: blankFinanceRules(),
});

// Same defaults as the server (Domain FinanceRuleSet.Default); also what an API older than the Finance rules is shown.
function blankFinanceRules(): FinanceRules {
  return {
    cutoffEnabled: true,
    cutoffWeekday: 'Wednesday',
    cutoffTime: '12:00',
    lateHandling: 'NoticeOnly',
    sponsorshipEnabled: true,
    sponsorshipRequiresDocument: true,
    demoEnabled: true,
    demoRequiresDocument: true,
    representationRequiresDocument: true,
  };
}

const WEEKDAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] as const;

// Same limits and defaults as the server (Domain LoginLockoutRules).
const LOCKOUT = {
  attempts: { min: 3, max: 20, default: 5 },
  minutes: { min: 1, max: 1440, default: 15 },
} as const;

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
      // And for an API older than the sign-in lockout rule.
      loginMaxFailedAttempts: value.loginMaxFailedAttempts ?? LOCKOUT.attempts.default,
      loginLockoutMinutes: value.loginLockoutMinutes ?? LOCKOUT.minutes.default,
      // And for an API older than the Finance rules.
      financeRules: { ...blankFinanceRules(), ...value.financeRules },
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
  'loginMaxFailedAttempts',
  'loginLockoutMinutes',
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
function rangeError(value: unknown, range: { min: number; max: number }, unit: string): string | null {
  const n = Number(value);
  return Number.isInteger(n) && n >= range.min && n <= range.max ? null : `Enter ${range.min} to ${range.max} ${unit}.`;
}
const attemptsError = computed(() => rangeError(form.loginMaxFailedAttempts, LOCKOUT.attempts, 'attempts'));
const minutesError = computed(() => rangeError(form.loginLockoutMinutes, LOCKOUT.minutes, 'minutes'));
const formatInvalid = computed(() => FORMAT_FIELDS.some((f) => formatError(f.key, f.max) !== null));
// Same shape the server accepts (FinanceRuleSetRules): 24-hour HH:mm.
const cutoffTimeError = computed(() => (/^([01]\d|2[0-3]):[0-5]\d$/.test(form.financeRules.cutoffTime.trim()) ? null : 'Enter a time, e.g. 12:00.'));
const canSubmit = computed(
  () =>
    isDirty.value &&
    !props.saving &&
    !localInvalid.value &&
    !fromInvalid.value &&
    !domainsError.value &&
    !reviewedByError.value &&
    !formatInvalid.value &&
    !attemptsError.value &&
    !minutesError.value &&
    !cutoffTimeError.value,
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
  out.financeRules = { ...form.financeRules, cutoffTime: form.financeRules.cutoffTime.trim() };
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
    <v-card v-show="shows('approvals')" class="mb-4">
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

    <v-card v-show="shows('approvals')" class="mb-4">
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

    <v-card v-show="shows('filing')" class="mb-4">
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

    <v-card v-show="shows('filing')" class="mb-4">
      <div class="card-h"><span class="card-t">Finance rules</span></div>
      <div class="pa-4">
        <div class="text-subtitle-2 mb-1">Weekly processing cutoff</div>
        <div class="text-caption text-medium-emphasis mb-2">
          Finance processes reports every Friday. A report filed after the cutoff and before Friday ends goes into the next Friday. The filer sees
          which Friday before filing; filing is never blocked.
        </div>
        <v-switch v-model="form.financeRules.cutoffEnabled" color="primary" label="Show the cutoff notice when filing" hide-details />
        <v-row dense class="mt-2">
          <v-col cols="12" sm="6">
            <v-select
              v-model="form.financeRules.cutoffWeekday"
              :items="WEEKDAYS"
              label="Cutoff day"
              :disabled="!form.financeRules.cutoffEnabled"
              hint="Finance question 1: the cutoff day"
              persistent-hint
              :error-messages="fieldErrors('financeRules.cutoffWeekday')"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.financeRules.cutoffTime"
              type="time"
              label="Cutoff time"
              :disabled="!form.financeRules.cutoffEnabled"
              hint="Finance question 1: the exact time, business local time"
              persistent-hint
              :error-messages="fieldErrors('financeRules.cutoffTime') ?? (cutoffTimeError ? [cutoffTimeError] : undefined)"
            />
          </v-col>
        </v-row>
        <v-radio-group
          v-model="form.financeRules.lateHandling"
          label="Reports filed after the cutoff (Finance question 2)"
          class="mt-4"
          hide-details="auto"
          :disabled="!form.financeRules.cutoffEnabled"
          :error-messages="fieldErrors('financeRules.lateHandling')"
        >
          <v-radio value="NoticeOnly">
            <template #label>
              <span>
                <strong>Notice only</strong>
                <span class="text-medium-emphasis"> · the filer is told the report goes into next Friday's processing</span>
              </span>
            </template>
          </v-radio>
          <v-radio value="FlagForFinance">
            <template #label>
              <span>
                <strong>Flag for Finance</strong>
                <span class="text-medium-emphasis"> · the notice, and the report shows Late in the Finance queue and its export</span>
              </span>
            </template>
          </v-radio>
        </v-radio-group>

        <v-divider class="my-5" />
        <div class="text-subtitle-2 mb-1">Sponsorship, Demo and Representation</div>
        <div class="text-caption text-medium-emphasis mb-2">
          Expense categories that need a document attached under Receipts before the line can be added; filing checks again. Sponsorship and Demo
          print under OTHERS on the R47. A category switched off is hidden from employees, and a report that still has such a line cannot be filed.
        </div>
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-switch v-model="form.financeRules.sponsorshipEnabled" color="primary" label="Employees may claim Sponsorship" hide-details />
            <v-checkbox
              v-model="form.financeRules.sponsorshipRequiresDocument"
              :disabled="!form.financeRules.sponsorshipEnabled"
              color="primary"
              density="compact"
              label="Needs an approved Activity Request Form"
              hide-details
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-switch v-model="form.financeRules.demoEnabled" color="primary" label="Employees may claim Demo" hide-details />
            <v-checkbox
              v-model="form.financeRules.demoRequiresDocument"
              :disabled="!form.financeRules.demoEnabled"
              color="primary"
              density="compact"
              label="Needs an ISO-registered attendance sheet"
              hide-details
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-checkbox
              v-model="form.financeRules.representationRequiresDocument"
              color="primary"
              density="compact"
              label="Representation needs the food or meal receipt"
              hide-details
            />
          </v-col>
        </v-row>
      </div>
    </v-card>

    <v-card v-show="shows('r47')" class="mb-4">
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
        <div class="d-flex align-center flex-wrap ga-2 mb-4">
          <v-btn class="press" variant="tonal" size="small" prepend-icon="mdi-ruler-square" :to="{ name: 'admin-r47-designer' }">Open the R47 designer</v-btn>
          <span class="text-caption text-medium-emphasis">Sizes, column widths, type, colours and fixed texts of the form, with a preview on a real report.</span>
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

    <v-card v-show="shows('access')" class="mb-4">
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

    <v-card v-show="shows('access')" class="mb-4">
      <div class="card-h"><span class="card-t">Sign-in security</span></div>
      <div class="pa-4">
        <v-row dense>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="form.loginMaxFailedAttempts"
              type="number"
              :min="LOCKOUT.attempts.min"
              :max="LOCKOUT.attempts.max"
              step="1"
              label="Failed attempts before lock"
              suffix="attempts"
              hint="Failed sign-ins in a row that lock the username"
              persistent-hint
              :error-messages="fieldErrors('loginMaxFailedAttempts') ?? (attemptsError ? [attemptsError] : undefined)"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model.number="form.loginLockoutMinutes"
              type="number"
              :min="LOCKOUT.minutes.min"
              :max="LOCKOUT.minutes.max"
              step="1"
              label="Lock for"
              suffix="minutes"
              hint="Failures older than this no longer count either"
              persistent-hint
              :error-messages="fieldErrors('loginLockoutMinutes') ?? (minutesError ? [minutesError] : undefined)"
            />
          </v-col>
        </v-row>
        <div class="text-caption text-medium-emphasis mt-2">
          Applies to the next lock; a username already locked keeps its end time. Resetting a user's password unlocks them at once.
        </div>
      </div>
    </v-card>

    <v-card v-show="shows('access')" class="mb-4">
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

    <!-- Stays in view while scrolling, so a change is never far from its save. -->
    <div class="savebar d-flex align-center ga-2">
      <span v-if="isDirty" class="text-caption text-medium-emphasis">Unsaved changes</span>
      <v-spacer />
      <v-btn class="press" variant="text" :disabled="!isDirty || saving" @click="reset">Discard changes</v-btn>
      <v-btn class="press" color="primary" variant="flat" type="submit" :loading="saving" :disabled="!canSubmit">Save rules</v-btn>
    </div>
  </v-form>
</template>

<style scoped>
.savebar {
  position: sticky;
  bottom: 0;
  z-index: 2;
  padding: 10px 0;
  background: var(--paper);
  border-top: 1px solid var(--line);
}
</style>
