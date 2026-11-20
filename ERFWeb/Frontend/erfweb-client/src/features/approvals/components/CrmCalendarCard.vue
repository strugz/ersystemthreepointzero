<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { describeError } from '@/shared/composables/useSnackbar';
import { approvalsApi } from '../api';
import type { CrmCalendar, CrmCalendarEntry } from '../types';

const props = defineProps<{ reportId: string; startOpen: boolean }>();

const open = ref(props.startOpen);
const calendar = ref<CrmCalendar | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    calendar.value = await approvalsApi.crmCalendar(props.reportId);
  } catch (err) {
    error.value = describeError(err, 'The CRM calendar could not be loaded.');
  } finally {
    loading.value = false;
  }
}
onMounted(load);

// CRM times carry +08:00; show them in Philippine time whatever the viewer's clock says.
const time = new Intl.DateTimeFormat('en-PH', { hour: 'numeric', minute: '2-digit', timeZone: 'Asia/Manila' });
const day = new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

function dayLabel(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number);
  return day.format(new Date(y, m - 1, d));
}

function timeRange(e: CrmCalendarEntry): string | null {
  if (!e.startAt && !e.endAt) return null;
  const from = e.startAt ? time.format(new Date(e.startAt)) : '?';
  const to = e.endAt ? time.format(new Date(e.endAt)) : '?';
  return `${from} – ${to}`;
}

function details(e: CrmCalendarEntry): string {
  const instrument = [e.instrumentName, e.serialNumber].filter(Boolean).join(' · ');
  return [e.serviceNumber, instrument].filter(Boolean).join(' · ');
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">CRM calendar</span>
        <span v-if="calendar" class="card-meta">{{ calendar.employeeName }}'s activities in the CRM, day by day</span>
      </div>
      <v-btn
        class="press"
        variant="text"
        size="small"
        :append-icon="open ? 'mdi-chevron-up' : 'mdi-chevron-down'"
        :aria-expanded="open"
        @click="open = !open"
      >
        {{ open ? 'Hide' : 'Show' }}
      </v-btn>
    </div>

    <template v-if="open">
      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />
      <v-alert v-if="error" type="warning" variant="tonal" density="compact" class="ma-4">
        {{ error }}
        <template #append><v-btn variant="text" size="small" @click="load">Try again</v-btn></template>
      </v-alert>
      <v-alert v-else-if="calendar && !calendar.linked" type="info" variant="tonal" density="compact" class="ma-4">
        {{ calendar.employeeName }}'s account is not linked to the CRM, so there is no CRM calendar to show.
      </v-alert>
      <div v-else-if="calendar && calendar.days.length === 0" class="pa-4 text-body-2 text-medium-emphasis">
        The report has no period yet.
      </div>

      <ol v-else-if="calendar" class="days">
        <li v-for="d in calendar.days" :key="d.date" class="day">
          <div class="day__head">
            <span class="day__date">{{ dayLabel(d.date) }}</span>
            <span v-if="d.expenseLineCount" class="card-meta">{{ d.expenseLineCount }} expense line{{ d.expenseLineCount === 1 ? '' : 's' }}</span>
          </div>

          <div v-if="d.entries.length === 0" class="day__empty" :class="{ 'day__empty--warn': d.expenseLineCount > 0 }">
            <v-icon v-if="d.expenseLineCount > 0" icon="mdi-alert-outline" size="16" />
            {{ d.expenseLineCount > 0 ? 'No CRM activity on a day with expense lines.' : 'No CRM activity.' }}
          </div>

          <ul v-else class="entries">
            <li v-for="e in d.entries" :key="e.activityId" class="entry" :class="{ 'entry--muted': !e.completed }">
              <div class="entry__time mono">{{ timeRange(e) ?? 'No time' }}</div>
              <div class="entry__body">
                <div class="entry__title">
                  <span class="font-weight-medium">{{ e.customerName ?? e.subject ?? e.activityId }}</span>
                  <span v-if="e.activityType" class="text-medium-emphasis"> · {{ e.activityType }}</span>
                  <span v-if="!e.completed" class="pill entry__pill">{{ e.status ?? 'Not completed' }}</span>
                  <span v-if="e.claimedOnThisReport" class="pill entry__pill entry__pill--claimed">On this report</span>
                </div>
                <div v-if="details(e)" class="text-caption text-medium-emphasis">{{ details(e) }}</div>
                <div class="text-caption text-medium-emphasis">
                  {{ e.role }}<span v-if="e.workWith"> · with {{ e.workWith }}</span>
                </div>
              </div>
            </li>
          </ul>
        </li>
      </ol>
    </template>
  </v-card>
</template>

<style scoped>
.days {
  list-style: none;
  margin: 0;
  padding: 0;
}
.day {
  padding: 12px 20px;
  border-bottom: 1px solid var(--line);
}
.day:last-child {
  border-bottom: 0;
}
.day__head {
  display: flex;
  align-items: baseline;
  gap: 12px;
  margin-bottom: 6px;
}
.day__date {
  font-size: 13px;
  font-weight: 600;
}
.day__empty {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: var(--ink-3);
}
.day__empty--warn {
  color: rgb(var(--v-theme-warning));
}
.entries {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 8px;
}
.entry {
  display: grid;
  grid-template-columns: 150px 1fr;
  gap: 12px;
  font-size: 13px;
}
.entry--muted {
  opacity: 0.6;
}
.entry__time {
  color: var(--ink-2);
}
.entry__title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
}
.entry__pill {
  height: 20px;
  font-size: 11px;
}
.entry__pill--claimed {
  color: rgb(var(--v-theme-success));
}
@media (max-width: 599px) {
  .day {
    padding: 12px 16px;
  }
  .entry {
    grid-template-columns: 1fr;
    gap: 2px;
  }
}
</style>
