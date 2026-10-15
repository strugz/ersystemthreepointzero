<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { addDaysIso } from '@/shared/format';
import type { CreateDelegationInput, DelegationOptions } from '../types';

/**
 * Names a delegate for a date range. A Level 2 may also assign one for a Level 1 they are over
 * (when that Level 1 left none). The delegate list is narrowed to the required level here for
 * convenience; the server is the authority on level, overlap and the date rules.
 */
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    options: DelegationOptions;
    saving?: boolean;
    /** The server's refusal, shown above the form. */
    error?: string | null;
  }>(),
  { saving: false, error: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  create: [input: CreateDelegationInput];
}>();

type Mode = 'self' | 'level1';

interface Form {
  mode: Mode;
  signatoryUserId: number | null;
  delegateUserId: number | null;
  startsOn: string;
  endsOn: string;
  reason: string;
}

function blank(): Form {
  return { mode: 'self', signatoryUserId: null, delegateUserId: null, startsOn: props.options.today, endsOn: addDaysIso(props.options.today, 4), reason: '' };
}

const form = reactive<Form>(blank());

watch(
  () => props.modelValue,
  (open) => {
    if (open) Object.assign(form, blank());
  },
);

const canAssignForLevel1 = computed(() => props.options.myLevel === 2 && props.options.coveredLevel1s.length > 0);
const requiredLevel = computed(() => (form.mode === 'level1' ? 1 : (props.options.myLevel ?? 1)));

const signatoryItems = computed(() =>
  props.options.coveredLevel1s.map((s) => ({
    value: s.userId,
    title: s.fullName,
    subtitle: [s.departmentName, s.pendingReports ? `${s.pendingReports} report${s.pendingReports === 1 ? '' : 's'} waiting` : null].filter(Boolean).join(' · ') || undefined,
  })),
);

const delegateItems = computed(() =>
  props.options.candidates
    .filter((c) => c.level >= requiredLevel.value && c.userId !== form.signatoryUserId)
    .map((c) => ({ value: c.userId, title: c.fullName, subtitle: [`Level ${c.level}`, c.departmentName].filter(Boolean).join(' · ') })),
);

// Switching mode or signatory may leave a delegate who no longer qualifies; clear it.
watch([() => form.mode, () => form.signatoryUserId], () => {
  if (form.mode === 'self') form.signatoryUserId = null;
  if (form.delegateUserId && !delegateItems.value.some((i) => i.value === form.delegateUserId)) form.delegateUserId = null;
});

function days(from: string, to: string): number {
  const [a, b] = [from, to].map((v) => {
    const [y, m, d] = v.split('-').map(Number);
    return Date.UTC(y, m - 1, d);
  });
  return Math.round((b - a) / 86_400_000) + 1;
}

const dateError = computed(() => {
  if (!form.startsOn || !form.endsOn) return 'Choose both dates.';
  if (form.endsOn < form.startsOn) return 'The end date cannot be before the start date.';
  if (form.endsOn < props.options.today) return 'The date range has already ended.';
  if (days(form.startsOn, form.endsOn) > props.options.maxDays) return `A delegation can cover at most ${props.options.maxDays} days.`;
  return null;
});

const canSave = computed(
  () => !props.saving && !!form.delegateUserId && !dateError.value && (form.mode === 'self' || !!form.signatoryUserId),
);

function save() {
  if (!canSave.value) return;
  emit('create', {
    signatoryUserId: form.mode === 'level1' ? form.signatoryUserId : null,
    delegateUserId: form.delegateUserId as number,
    startsOn: form.startsOn,
    endsOn: form.endsOn,
    reason: form.reason.trim() || null,
  });
}

function close() {
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="560" scrollable :persistent="saving" @update:model-value="close">
    <v-card>
      <v-card-title class="text-subtitle-1 font-weight-bold">New delegation</v-card-title>

      <v-card-text>
        <v-alert v-if="error" type="error" variant="tonal" density="compact" class="mb-4">{{ error }}</v-alert>

        <v-form :disabled="saving" @submit.prevent="save">
          <fieldset v-if="canAssignForLevel1" class="group mb-3">
            <legend class="lbl">Whose approvals</legend>
            <v-radio-group v-model="form.mode" hide-details>
              <v-radio value="self" label="Mine" />
              <v-radio value="level1">
                <template #label>
                  <div>
                    <div>A Level 1 I am over</div>
                    <div class="text-caption text-medium-emphasis">When they are away and left no delegate.</div>
                  </div>
                </template>
              </v-radio>
            </v-radio-group>
          </fieldset>

          <v-expand-transition>
            <div v-if="form.mode === 'level1'">
              <v-autocomplete v-model="form.signatoryUserId" :items="signatoryItems" item-props label="Level 1 signatory *" class="mb-2" />
            </div>
          </v-expand-transition>

          <v-autocomplete
            v-model="form.delegateUserId"
            :items="delegateItems"
            item-props
            label="Delegate *"
            :hint="`Must be a Level ${requiredLevel} signatory or higher.`"
            persistent-hint
            no-data-text="No signatory of that level is available"
            class="mb-4"
          />

          <div class="dates">
            <v-text-field v-model="form.startsOn" type="date" label="From *" :min="options.today" hide-details="auto" />
            <v-text-field v-model="form.endsOn" type="date" label="To *" :min="form.startsOn || options.today" hide-details="auto" />
          </div>
          <div v-if="dateError" class="text-caption text-error mt-1">{{ dateError }}</div>

          <v-textarea v-model="form.reason" label="Reason" placeholder="e.g. On leave" rows="2" auto-grow counter="1000" maxlength="1000" class="mt-4" />

          <p class="text-caption text-medium-emphasis mb-0">
            The delegate approves, returns or rejects in the signatory's place during these dates. Each action is stamped "on behalf of" the signatory.
            Rates and scope are never delegated.
          </p>
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn class="press" variant="text" :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.group {
  border: 0;
  margin: 0;
  padding: 0;
}
.lbl {
  font-size: 12px;
  color: var(--ink-3);
}
.dates {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
@media (max-width: 599.98px) {
  .dates {
    grid-template-columns: 1fr;
  }
}
</style>
