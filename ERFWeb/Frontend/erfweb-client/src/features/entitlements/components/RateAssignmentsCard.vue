<script setup lang="ts">
import { computed, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import { addDaysIso, formatDate, formatDateTime, todayIso } from '@/shared/format';
import { fieldErrors } from '../forms';
import type { FieldErrors } from '../forms';
import { rateTargetText, targetTypeLabel } from '../labels';
import type { RateAssignment } from '../types';

/**
 * Who a profile applies to. Edit corrects an assignment, ended ones too, in place. Ending an assignment asks for the last day it applies (yesterday at the
 * earliest; the server enforces it) and an optional reason.
 */
const props = withDefaults(
  defineProps<{
    assignments: RateAssignment[];
    canWrite?: boolean;
    disabled?: boolean;
    acting?: boolean;
    endErrors?: FieldErrors;
  }>(),
  { canWrite: false, disabled: false, acting: false, endErrors: null },
);

const emit = defineEmits<{
  create: [];
  edit: [assignment: RateAssignment];
  end: [assignment: RateAssignment, effectiveTo: string, changeReason: string | null];
  'clear-end-errors': [];
}>();

const { xs } = useDisplay();
const today = todayIso();
const yesterday = addDaysIso(today, -1);

const isEnded = (a: RateAssignment) => !!a.effectiveTo && a.effectiveTo < today;
const showEnded = ref(false);
const endedCount = computed(() => props.assignments.filter(isEnded).length);
const visible = computed(() =>
  [...props.assignments]
    .filter((a) => showEnded.value || !isEnded(a))
    .sort((a, b) => Number(isEnded(a)) - Number(isEnded(b)) || b.effectiveFrom.localeCompare(a.effectiveFrom)),
);

function periodText(a: RateAssignment): string {
  return `${formatDate(a.effectiveFrom)} – ${a.effectiveTo ? formatDate(a.effectiveTo) : 'open'}`;
}

// ---- end dialog ---------------------------------------------------------------
const ending = ref<RateAssignment | null>(null);
const endDate = ref(today);
const endReason = ref('');

function askEnd(a: RateAssignment) {
  ending.value = a;
  endDate.value = today;
  endReason.value = '';
  emit('clear-end-errors');
}

function confirmEnd() {
  if (!ending.value || !endDate.value || endDate.value < yesterday) return;
  emit('end', ending.value, endDate.value, endReason.value.trim() || null);
}

/** Parent calls this after the end succeeds. */
function closeEnd() {
  ending.value = null;
}
defineExpose({ closeEnd });

const endMessage = computed(() =>
  ending.value
    ? `${targetTypeLabel[ending.value.targetType]}: ${rateTargetText(ending.value)}. The profile stops applying after the date below.`
    : '',
);
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">Assignments</span>
        <span class="card-meta">{{ visible.length }} of {{ assignments.length }}</span>
      </div>
      <div class="d-flex align-center ga-2">
        <v-switch
          v-if="endedCount > 0"
          v-model="showEnded"
          density="compact"
          hide-details
          color="primary"
          :label="xs ? 'Ended' : `Show ended (${endedCount})`"
          class="mr-2"
        />
        <v-btn v-if="canWrite" class="press" size="small" variant="outlined" prepend-icon="mdi-plus" :disabled="disabled" @click="emit('create')">
          {{ xs ? 'Assign' : 'Assign profile' }}
        </v-btn>
      </div>
    </div>

    <AppEmptyState
      v-if="visible.length === 0"
      icon="mdi-account-arrow-right-outline"
      :title="assignments.length === 0 ? 'Not assigned to anyone' : 'No current assignments'"
      :hint="
        assignments.length === 0
          ? canWrite
            ? 'Assign the profile to a person, job grade, department or the whole company.'
            : 'Nobody has assigned this profile yet.'
          : 'Turn on “Show ended” to see past assignments.'
      "
    >
      <v-btn v-if="canWrite && assignments.length === 0" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :disabled="disabled" @click="emit('create')">
        Assign profile
      </v-btn>
    </AppEmptyState>

    <ul v-else-if="xs" class="rows">
      <li v-for="a in visible" :key="a.id" class="row" :class="{ 'text-medium-emphasis': isEnded(a) }">
        <div class="d-flex align-center ga-2 flex-wrap">
          <span class="font-weight-medium">{{ rateTargetText(a) }}</span>
          <span class="pill pill--neutral">{{ targetTypeLabel[a.targetType] }}</span>
          <span v-if="isEnded(a)" class="pill pill--neutral">Ended</span>
          <v-chip v-if="a.isBootstrap" size="x-small" label variant="tonal" color="primary">Bootstrap</v-chip>
        </div>
        <div class="text-caption">{{ periodText(a) }}</div>
        <div class="text-caption text-medium-emphasis">
          {{ a.assignedByName ?? '—' }} · {{ formatDateTime(a.assignedAt) }}<template v-if="a.changeReason"> · “{{ a.changeReason }}”</template>
        </div>
        <div v-if="canWrite" class="mt-1 d-flex ga-1">
          <v-btn class="press" size="small" variant="text" prepend-icon="mdi-pencil-outline" :disabled="disabled || acting" @click="emit('edit', a)">Edit</v-btn>
          <v-btn v-if="!isEnded(a)" class="press" size="small" variant="text" color="error" :disabled="disabled || acting" @click="askEnd(a)">End</v-btn>
        </div>
      </li>
    </ul>

    <div v-else class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th>Applies to</th>
            <th>Type</th>
            <th>Period</th>
            <th class="d-none d-md-table-cell">Assigned by</th>
            <th v-if="canWrite" class="r"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in visible" :key="a.id" :class="{ 'text-medium-emphasis': isEnded(a) }">
            <td class="font-weight-medium">
              {{ rateTargetText(a) }}
              <v-chip v-if="a.isBootstrap" size="x-small" label variant="tonal" color="primary" class="ml-1">Bootstrap</v-chip>
            </td>
            <td class="text-no-wrap">{{ targetTypeLabel[a.targetType] }}</td>
            <td class="text-no-wrap">
              {{ periodText(a) }}
              <span v-if="isEnded(a)" class="pill pill--neutral ml-1">Ended</span>
            </td>
            <td class="d-none d-md-table-cell">
              <div class="text-no-wrap">{{ a.assignedByName ?? '—' }}</div>
              <div class="text-caption text-medium-emphasis text-no-wrap">{{ formatDateTime(a.assignedAt) }}</div>
              <div v-if="a.changeReason" class="text-caption text-medium-emphasis text-truncate" style="max-width: 240px" :title="a.changeReason">{{ a.changeReason }}</div>
            </td>
            <td v-if="canWrite" class="r text-no-wrap">
              <v-btn
                class="press"
                size="small"
                variant="text"
                prepend-icon="mdi-pencil-outline"
                :disabled="disabled || acting"
                :aria-label="`Edit assignment for ${rateTargetText(a)}`"
                @click="emit('edit', a)"
              >
                Edit
              </v-btn>
              <v-btn
                v-if="!isEnded(a)"
                class="press"
                size="small"
                variant="text"
                color="error"
                :disabled="disabled || acting"
                :aria-label="`End assignment for ${rateTargetText(a)}`"
                @click="askEnd(a)"
              >
                End
              </v-btn>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppConfirmDialog
      :model-value="ending !== null"
      title="End assignment"
      :message="endMessage"
      confirm-text="End assignment"
      confirm-color="error"
      :loading="acting"
      @update:model-value="(v: boolean) => !v && (ending = null)"
      @confirm="confirmEnd"
    >
      <v-text-field
        v-model="endDate"
        type="date"
        :min="yesterday"
        label="Last day it applies"
        :disabled="acting"
        :error-messages="fieldErrors(endErrors, 'effectiveTo') ?? (endDate && endDate < yesterday ? ['Yesterday at the earliest.'] : undefined)"
      />
      <v-textarea
        v-model="endReason"
        label="Reason (optional)"
        rows="2"
        auto-grow
        maxlength="500"
        :disabled="acting"
        :error-messages="fieldErrors(endErrors, 'changeReason')"
      />
    </AppConfirmDialog>
  </v-card>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.rows {
  list-style: none;
  margin: 0;
  padding: 0;
}
.row {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}
.row:last-child {
  border-bottom: 0;
}
</style>
