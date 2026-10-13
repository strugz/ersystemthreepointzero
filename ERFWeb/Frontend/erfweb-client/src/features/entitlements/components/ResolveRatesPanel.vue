<script setup lang="ts">
import { computed, ref } from 'vue';
import { formatDate, formatMoney, todayIso } from '@/shared/format';
import { personItems } from '../forms';
import { rateKeyLabel, rateTypeLabel, targetTypeLabel } from '../labels';
import type { PersonOption, ResolvedRates } from '../types';

/**
 * "What would this person get on this date?" — the parent runs the lookup and passes the result,
 * so this panel stays presentational.
 */
const props = withDefaults(
  defineProps<{
    people: PersonOption[];
    result?: ResolvedRates | null;
    loading?: boolean;
    error?: string | null;
    /** Highlights when the resolved profile is the one on screen. */
    currentProfileId?: number | null;
  }>(),
  { result: null, loading: false, error: null, currentProfileId: null },
);

const emit = defineEmits<{
  resolve: [userId: number, date: string];
}>();

const userId = ref<number | null>(null);
const date = ref(todayIso());

const items = computed(() => personItems(props.people));
const personName = computed(() => props.people.find((p) => p.userId === props.result?.userId)?.fullName ?? 'This person');

function run() {
  if (userId.value === null || !date.value) return;
  emit('resolve', userId.value, date.value);
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <span class="card-t">Check who gets what</span>
    </div>
    <div class="pa-4">
      <v-form @submit.prevent="run">
        <v-autocomplete v-model="userId" :items="items" item-props label="Person" density="compact" class="mb-2" />
        <div class="d-flex ga-2 align-start">
          <v-text-field v-model="date" type="date" label="On" density="compact" class="flex-grow-1" />
          <v-btn class="press" color="primary" variant="tonal" type="submit" :loading="loading" :disabled="userId === null || !date" height="40">Check</v-btn>
        </div>
      </v-form>

      <v-alert v-if="error && !loading" type="error" variant="tonal" density="compact" class="mt-2">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="run">Retry</v-btn></template>
      </v-alert>

      <div v-else-if="result && !loading" class="mt-2" aria-live="polite">
        <div v-if="result.profileId === null" class="text-body-2 text-medium-emphasis">
          {{ personName }} has no rate profile on {{ formatDate(result.date) }}.
        </div>
        <template v-else>
          <div class="text-body-2">
            {{ personName }} gets
            <strong>{{ result.profileName }}</strong>
            on {{ formatDate(result.date) }}<template v-if="result.via"> via {{ targetTypeLabel[result.via].toLowerCase() }}</template>.
          </div>
          <v-chip v-if="currentProfileId !== null && result.profileId !== currentProfileId" size="x-small" label variant="tonal" color="warning" class="mt-1">
            A different profile
          </v-chip>
          <ul v-if="result.lines.length" class="lines mt-2">
            <li v-for="l in result.lines" :key="l.id" class="lines__row">
              <span>{{ rateKeyLabel(l.category, l.subCategory) }}</span>
              <span class="mono text-no-wrap">{{ l.currency }} {{ formatMoney(l.amount) }}</span>
              <span class="text-caption text-medium-emphasis lines__rule">
                {{ rateTypeLabel[l.rateType] }}<template v-if="l.frequencyCap !== null"> · max {{ l.frequencyCap }}×</template><template v-if="l.requiresReceipt"> · receipt</template>
              </span>
            </li>
          </ul>
          <div v-else class="text-caption text-medium-emphasis mt-1">The profile has no lines in force on that date.</div>
        </template>
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.lines {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}
.lines__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0 12px;
  padding: 6px 0;
  border-bottom: 1px solid var(--line);
  font-size: 13px;
}
.lines__row:last-child {
  border-bottom: 0;
}
.lines__rule {
  grid-column: 1 / -1;
}
</style>
