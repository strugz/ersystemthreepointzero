<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { formatDate } from '@/shared/format';
import type { CrmActivityOption, CrmActivityPickList } from '../types';

// Lists the employee's completed CRM activities in the report period; picking one pre-fills the line.
// An activity already on another report is shown but cannot be picked (one activity, one report).
const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    list?: CrmActivityPickList | null;
    loading?: boolean;
    error?: string | null;
  }>(),
  { list: null, loading: false, error: null },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  pick: [activity: CrmActivityOption];
  retry: [];
}>();

const search = ref('');
watch(
  () => props.modelValue,
  (open) => {
    if (open) search.value = '';
  },
);

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

const more = computed(() => (props.list ? props.list.total - props.list.items.length : 0));

function pick(a: CrmActivityOption) {
  if (a.claimedElsewhere) return;
  emit('pick', a);
  emit('update:modelValue', false);
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="680" scrollable @update:model-value="(v: boolean) => emit('update:modelValue', v)">
    <v-card class="crm">
      <header class="crm__head">
        <div>
          <h2 class="crm__title">Pick a CRM activity</h2>
          <p v-if="list?.linked && list.dateFrom" class="crm__sub">
            Completed work from {{ formatDate(list.dateFrom) }} to {{ formatDate(list.dateTo) }}. You still enter the amount and receipt.
          </p>
        </div>
        <v-btn class="press" icon="mdi-close" variant="text" size="small" aria-label="Close" @click="emit('update:modelValue', false)" />
      </header>

      <v-card-text class="crm__body">
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
            {{ list.items.length ? 'No activity matches the filter.' : 'No completed CRM activities in this period.' }}
          </p>
          <ul v-else class="crm__list">
            <li v-for="a in items" :key="a.activityId">
              <button
                type="button"
                class="act press"
                :class="{ 'act--off': a.claimedElsewhere }"
                :disabled="a.claimedElsewhere"
                :aria-label="`Use ${a.activityId}${a.customerName ? `, ${a.customerName}` : ''}`"
                @click="pick(a)"
              >
                <div class="act__top">
                  <span class="act__date">{{ formatDate(a.activityDate) }}</span>
                  <span class="mono act__id">{{ a.activityId }}</span>
                  <v-chip v-if="a.activityType" size="x-small" label variant="tonal">{{ a.activityType }}</v-chip>
                  <v-chip v-if="a.role === 'Companion'" size="x-small" label variant="outlined">Work-with</v-chip>
                  <v-chip v-if="a.isOvernight" size="x-small" label variant="tonal" color="warning">Multi-day</v-chip>
                </div>
                <div class="act__main">{{ a.customerName ?? 'No client on record' }}</div>
                <div v-if="a.subject" class="act__sub">{{ a.subject }}</div>
                <div v-if="a.instrumentName || a.serialNumber" class="act__sub">
                  {{ [a.instrumentName, a.serialNumber].filter(Boolean).join(' · ') }}
                </div>
                <div v-if="a.claimedElsewhere" class="act__note act__note--off">
                  Already claimed on {{ a.claimedInReferenceNo ?? 'another report not filed yet' }}
                </div>
                <div v-else-if="a.linesInThisReport > 0" class="act__note">
                  Used on {{ a.linesInThisReport }} line{{ a.linesInThisReport === 1 ? '' : 's' }} of this report
                </div>
              </button>
            </li>
          </ul>
          <p v-if="more > 0" class="crm__empty">{{ more }} more not shown. Shorten the report period to see them.</p>
        </template>
      </v-card-text>
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
.crm__state {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 24px 0;
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
.act--off {
  cursor: not-allowed;
  opacity: 0.6;
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
