<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { addDaysIso, formatDate } from '@/shared/format';
import type { CrmActivityOption, CrmActivityPickList } from '../types';

// Lists every CRM activity of the employee on one day (first the line's date; the arrows step a day at a time
// inside the report period). The employee ticks one or more (a visit that served several instruments) and the
// ticked ones pre-fill the line, date included. Open and Cancelled activities, and one already on another
// report (one activity, one report), show but cannot be ticked.
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    /** The day shown. */
    date: string;
    /** The report period; the API refuses days outside it. */
    minDate?: string | null;
    maxDate?: string | null;
    list?: CrmActivityPickList | null;
    loading?: boolean;
    error?: string | null;
    /** The activities the line already links; ticked when the picker opens. */
    selected?: string[];
  }>(),
  { minDate: null, maxDate: null, list: null, loading: false, error: null, selected: () => [] },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  'update:date': [date: string];
  /** The ticked activities, in the order ticked. */
  pick: [activities: CrmActivityOption[]];
  retry: [];
}>();

const search = ref('');
const checked = ref<string[]>([]);
watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    search.value = '';
    checked.value = [...props.selected];
  },
);

const canGoBack = computed(() => !props.loading && (!props.minDate || props.date > props.minDate));
const canGoForward = computed(() => !props.loading && (!props.maxDate || props.date < props.maxDate));

// A line's date must fall on every linked activity, so ticks are for one day only.
function stepDay(days: number) {
  search.value = '';
  checked.value = [];
  emit('update:date', addDaysIso(props.date, days));
}

const items = computed(() => {
  const all = props.list?.items ?? [];
  const words = search.value.trim().toLowerCase().split(/\s+/).filter(Boolean);
  if (!words.length) return all;
  return all.filter((a) => {
    const text = [a.activityId, a.activityType, a.subject, a.customerName, a.instrumentName, a.serialNumber, a.serviceNumber, a.workWith]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();
    return words.every((w) => text.includes(w));
  });
});

/** Why an activity cannot be picked, or null when it can. */
function blockedReason(a: CrmActivityOption): string | null {
  if (!a.claimable) {
    return a.status === 'Cancelled' ? 'Cancelled in the CRM' : 'Not completed in the CRM yet. Complete it there to claim it.';
  }
  if (a.claimedElsewhere) return `Already claimed on ${a.claimedInReferenceNo ?? 'another report not filed yet'}`;
  return null;
}

function toggle(a: CrmActivityOption) {
  if (blockedReason(a)) return;
  checked.value = checked.value.includes(a.activityId)
    ? checked.value.filter((id) => id !== a.activityId)
    : [...checked.value, a.activityId];
}

/** The ticked activities that are on the day shown, in the order ticked. */
const picked = computed(() => {
  const byId = new Map((props.list?.items ?? []).map((a) => [a.activityId, a]));
  return checked.value.map((id) => byId.get(id)).filter((a): a is CrmActivityOption => !!a && !blockedReason(a));
});

function use() {
  if (!picked.value.length) return;
  emit('pick', picked.value);
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="680" scrollable @update:model-value="(v: boolean) => emit('update:modelValue', v)">
    <v-card class="crm">
      <header class="crm__head">
        <div>
          <h2 class="crm__title">Pick CRM activities</h2>
          <p class="crm__sub">
            Your CRM activities, one day at a time. Tick each instrument the expense covers; their service no., instrument and serial
            are joined with " / ". You still enter the amount and receipt.
          </p>
        </div>
        <v-btn class="press" icon="mdi-close" variant="text" size="small" aria-label="Close" @click="emit('update:modelValue', false)" />
      </header>

      <v-card-text class="crm__body">
        <div v-if="!list || list.linked" class="crm__day" role="group" aria-label="Day">
          <v-btn
            class="press"
            icon="mdi-chevron-left"
            variant="text"
            size="small"
            aria-label="Previous day"
            :disabled="!canGoBack"
            @click="stepDay(-1)"
          />
          <span class="crm__date" aria-live="polite">{{ formatDate(date) }}</span>
          <v-btn
            class="press"
            icon="mdi-chevron-right"
            variant="text"
            size="small"
            aria-label="Next day"
            :disabled="!canGoForward"
            @click="stepDay(1)"
          />
        </div>

        <div v-if="loading" class="crm__state">
          <v-progress-circular indeterminate size="28" color="primary" />
          <span>Asking the CRM…</span>
        </div>

        <v-alert v-else-if="error" type="warning" variant="tonal" density="compact">
          {{ error }}
          <template #append>
            <v-btn class="press" size="small" variant="text" @click="emit('retry')">Try again</v-btn>
          </template>
        </v-alert>

        <v-alert v-else-if="list && !list.linked" type="info" variant="tonal" density="compact">
          Your account is not linked to the CRM yet. Ask the system administrator to link it, or type the line instead.
        </v-alert>

        <template v-else-if="list">
          <v-text-field
            v-model="search"
            prepend-inner-icon="mdi-magnify"
            placeholder="Filter by client, instrument, service no."
            density="compact"
            hide-details
            clearable
            class="mb-3"
          />
          <p v-if="!items.length" class="crm__empty">
            {{ list.items.length ? 'No activity matches the filter.' : 'No CRM activities on this date. Try the previous or next day.' }}
          </p>
          <ul v-else class="crm__list">
            <li v-for="a in items" :key="a.activityId">
              <button
                type="button"
                role="checkbox"
                class="act press"
                :class="{ 'act--off': !!blockedReason(a), 'act--on': checked.includes(a.activityId) }"
                :disabled="!!blockedReason(a)"
                :aria-checked="checked.includes(a.activityId)"
                :aria-label="`${a.activityId}${a.customerName ? `, ${a.customerName}` : ''}`"
                @click="toggle(a)"
              >
                <div class="act__top">
                  <v-icon
                    class="act__check"
                    size="18"
                    :icon="checked.includes(a.activityId) ? 'mdi-checkbox-marked' : 'mdi-checkbox-blank-outline'"
                  />
                  <span class="act__date">{{ formatDate(a.activityDate) }}</span>
                  <span class="mono act__id">{{ a.activityId }}</span>
                  <v-chip v-if="a.activityType" size="x-small" label variant="tonal">{{ a.activityType }}</v-chip>
                  <v-chip v-if="!a.claimable && a.status" size="x-small" label variant="outlined">{{ a.status }}</v-chip>
                  <v-chip v-if="a.role === 'Companion'" size="x-small" label variant="outlined">Work-with</v-chip>
                  <v-chip v-if="a.isOvernight" size="x-small" label variant="tonal" color="warning">Multi-day</v-chip>
                </div>
                <div class="act__main">{{ a.customerName ?? 'No client on record' }}</div>
                <div v-if="a.subject" class="act__sub">{{ a.subject }}</div>
                <div v-if="a.instrumentName || a.serialNumber" class="act__sub">
                  {{ [a.instrumentName, a.serialNumber].filter(Boolean).join(' · ') }}
                </div>
                <div v-if="blockedReason(a)" class="act__note act__note--off">{{ blockedReason(a) }}</div>
                <div v-else-if="a.linesInThisReport > 0" class="act__note">
                  Used on {{ a.linesInThisReport }} line{{ a.linesInThisReport === 1 ? '' : 's' }} of this report
                </div>
              </button>
            </li>
          </ul>
        </template>
      </v-card-text>

      <template v-if="list?.linked">
        <v-divider />
        <v-card-actions class="px-4 py-3">
          <span class="crm__count">{{ picked.length ? `${picked.length} ticked` : 'Tick one or more activities' }}</span>
          <v-spacer />
          <v-btn class="press" variant="text" @click="emit('update:modelValue', false)">Cancel</v-btn>
          <v-btn class="press" color="primary" variant="flat" :disabled="!picked.length" @click="use">
            {{ picked.length > 1 ? `Use ${picked.length} activities` : 'Use activity' }}
          </v-btn>
        </v-card-actions>
      </template>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.crm {
  background: var(--card);
}
.crm__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 20px 20px 4px 24px;
}
.crm__title {
  font-size: 18px;
  font-weight: 600;
  color: var(--ink);
}
.crm__sub {
  margin-top: 2px;
  font-size: 13px;
  color: var(--ink-3);
}
.crm__body {
  padding: 12px 24px 20px !important;
}
.crm__day {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  margin-bottom: 12px;
}
.crm__date {
  min-width: 120px;
  font-weight: 600;
  text-align: center;
  color: var(--ink);
}
.crm__state {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 24px 0;
  color: var(--ink-3);
}
.crm__count {
  font-size: 13px;
  color: var(--ink-3);
}
.crm__empty {
  padding: 12px 0;
  font-size: 13px;
  color: var(--ink-3);
}
.crm__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0;
  list-style: none;
}
.act {
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 100%;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-radius: var(--radius);
  background: var(--card);
  font: inherit;
  text-align: left;
  color: var(--ink);
  cursor: pointer;
  transition:
    border-color var(--dur-fast) ease,
    background-color var(--dur-fast) ease,
    transform var(--dur-press) var(--ease-out);
}
@media (hover: hover) and (pointer: fine) {
  .act:hover:not(.act--off) {
    border-color: var(--navy);
    background: var(--navy-soft);
  }
}
.act:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.act--on {
  border-color: var(--navy);
  background: var(--navy-soft);
}
.act--off {
  cursor: not-allowed;
  opacity: 0.6;
}
.act__check {
  color: var(--ink-3);
}
.act--on .act__check {
  color: var(--navy);
}
.act__top {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--ink-3);
}
.act__date {
  font-weight: 600;
  color: var(--ink-2);
}
.act__id {
  color: var(--navy);
}
.act__main {
  font-weight: 600;
}
.act__sub {
  font-size: 13px;
  color: var(--ink-2);
}
.act__note {
  margin-top: 2px;
  font-size: 12px;
  color: var(--navy);
}
.act__note--off {
  color: var(--ink-3);
}
</style>
