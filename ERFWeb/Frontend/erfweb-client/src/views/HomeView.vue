<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { formatDate, formatMoney, greetingName } from '@/shared/format';
import { expensesApi } from '@/features/expenses/api';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import type { ExpenseReportListItem } from '@/features/expenses/types';
import { approvalsApi } from '@/features/approvals/api';
import type { ApprovalInboxItem } from '@/features/approvals/types';

const session = useSessionStore();
const router = useRouter();
const { compact } = useLayoutMode();

const recent = ref<ExpenseReportListItem[]>([]);
const counts = ref<{ all: number; drafts: number; forApproval: number; returned: number } | null>(null);
const loading = ref(true);

// Signatories see the reports waiting for their approval first; the card shows only when there are some.
const isSignatory = computed(() => session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2'));
const pending = ref<ApprovalInboxItem[]>([]);
const pendingTotal = ref(0);

async function loadPending() {
  if (!isSignatory.value) return;
  try {
    const page = await approvalsApi.inbox({ view: 'Pending', page: 1, pageSize: 5 });
    pending.value = page.items;
    pendingTotal.value = page.total;
  } catch {
    // Best-effort like the rest of the dashboard; the Approvals page shows precise errors.
  }
}

onMounted(async () => {
  void loadPending();
  try {
    const [all, drafts, forApproval, returned] = await Promise.all([
      expensesApi.list({ page: 1, pageSize: 5 }),
      expensesApi.list({ page: 1, pageSize: 1, status: 'New' }),
      expensesApi.list({ page: 1, pageSize: 1, status: 'ForApproval' }),
      expensesApi.list({ page: 1, pageSize: 1, status: 'Returned' }),
    ]);
    recent.value = all.items;
    counts.value = { all: all.total, drafts: drafts.total, forApproval: forApproval.total, returned: returned.total };
  } catch {
    // The dashboard is best-effort; the reports page shows precise errors.
  } finally {
    loading.value = false;
  }
});

function open(item: ExpenseReportListItem) {
  router.push({ name: 'report-edit', params: { id: item.id } });
}

function review(item: ApprovalInboxItem) {
  router.push({ name: 'report-review', params: { id: item.reportId } });
}

function waitingFor(item: ApprovalInboxItem) {
  if (item.isMyTurn) return item.onBehalfOfName ? `Your turn, for ${item.onBehalfOfName}` : 'Your turn';
  return item.currentApproverName ? `Waiting for ${item.currentApproverName}` : 'Waiting';
}

const tiles = [
  { key: 'drafts', label: 'Drafts', short: 'Drafts', status: 'New', tone: 'tile--neutral', icon: 'mdi-file-edit-outline' },
  { key: 'forApproval', label: 'Awaiting approval', short: 'Awaiting', status: 'ForApproval', tone: 'tile--warning', icon: 'mdi-timer-sand' },
  { key: 'returned', label: 'Returned to me', short: 'Returned', status: 'Returned', tone: 'tile--error', icon: 'mdi-undo-variant' },
] as const;

const today = new Intl.DateTimeFormat('en-PH', { weekday: 'long', month: 'long', day: 'numeric' }).format(new Date());
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Dashboard' }]">
    <template #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :to="{ name: 'report-new' }">New report</v-btn>
    </template>

    <section class="hero mb-5">
      <div class="hero__body">
        <span class="hero__date">{{ today }}</span>
        <h1 class="hero__title">{{ compact ? 'Hello' : 'Good day' }}{{ session.user ? `, ${greetingName(session.user.fullName)}` : '' }}</h1>
        <p class="hero__sub">Prepare and file your expense reports, then print the R47.</p>
      </div>
      <div class="hero__stat" aria-live="polite">
        <span class="hero__stat-lbl">Total reports</span>
        <span class="mono hero__stat-num">{{ counts ? counts.all : '—' }}</span>
      </div>
    </section>

    <div class="tiles">
      <router-link
        v-for="(t, i) in tiles"
        :key="t.key"
        class="tile press stagger-in"
        :class="t.tone"
        :style="{ '--i': i }"
        :to="{ name: 'reports', query: { status: t.status } }"
      >
        <span class="tile__icon"><v-icon :icon="t.icon" size="20" /></span>
        <span class="tile__text">
          <span class="lbl">{{ compact ? t.short : t.label }}</span>
          <span class="mono tile__num">{{ counts ? counts[t.key] : '—' }}</span>
        </span>
        <v-icon icon="mdi-arrow-top-right" size="16" class="tile__go" />
      </router-link>
    </div>

    <v-card v-if="pending.length > 0" class="mt-5 approvals">
      <div class="card-h">
        <span class="card-t">For your approval <span class="approvals__count mono">{{ pendingTotal }}</span></span>
        <v-btn class="press" size="small" variant="text" color="primary" :to="{ name: 'approvals' }" append-icon="mdi-arrow-right">All approvals</v-btn>
      </div>
      <div class="recent">
        <button
          v-for="(item, i) in pending"
          :key="item.reportId"
          type="button"
          class="recent__row press stagger-in"
          :style="{ '--i': Math.min(i, 8) }"
          @click="review(item)"
        >
          <div class="recent__main">
            <div class="recent__title">{{ item.employeeName }}</div>
            <div class="recent__meta">
              <span class="mono">{{ item.referenceNo }}</span> ·
              <span :class="{ 'approvals__turn': item.isMyTurn }">{{ waitingFor(item) }}</span>
              <span v-if="item.daysWaiting > 0" class="recent__lines">· {{ item.daysWaiting }} day{{ item.daysWaiting === 1 ? '' : 's' }} waiting</span>
            </div>
          </div>
          <span class="mono recent__amt">{{ formatMoney(item.totalAmount) }}</span>
          <ReportStatusChip :status="item.status" class="recent__chip" />
        </button>
      </div>
    </v-card>

    <v-card class="mt-5">
      <div class="card-h">
        <span class="card-t">Recent reports</span>
        <v-btn class="press" size="small" variant="text" color="primary" :to="{ name: 'reports' }" append-icon="mdi-arrow-right">All reports</v-btn>
      </div>
      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />
      <div v-else-if="recent.length === 0" class="pa-5 text-body-2 text-medium-emphasis">
        No reports yet. Start your first one with <strong>New report</strong>.
      </div>
      <div v-else class="recent">
        <button
          v-for="(item, i) in recent"
          :key="item.id"
          type="button"
          class="recent__row press stagger-in"
          :style="{ '--i': Math.min(i, 8) }"
          @click="open(item)"
        >
          <div class="recent__main">
            <div class="recent__title">{{ item.purpose || item.referenceNo || 'Untitled report' }}</div>
            <div class="recent__meta">
              <span class="mono">{{ item.referenceNo || 'Draft' }}</span> · {{ formatDate(item.dateFrom) }} – {{ formatDate(item.dateTo) }}
              <span class="recent__lines">· {{ item.lineCount }} line{{ item.lineCount === 1 ? '' : 's' }}</span>
            </div>
          </div>
          <span class="mono recent__amt">{{ formatMoney(item.totalAmount) }}</span>
          <ReportStatusChip :status="item.status" class="recent__chip" />
        </button>
      </div>
    </v-card>
  </AuthenticatedLayout>
</template>

<style scoped>
.approvals {
  border-left: 3px solid rgb(var(--v-theme-warning));
}
.approvals__count {
  display: inline-block;
  min-width: 22px;
  margin-left: 6px;
  padding: 0 7px;
  border-radius: 11px;
  background: rgba(var(--v-theme-warning), 0.16);
  color: rgb(var(--v-theme-warning));
  font-size: 12px;
  line-height: 20px;
  text-align: center;
}
.approvals__turn {
  color: rgb(var(--v-theme-warning));
  font-weight: 500;
}
.tiles {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
@media (max-width: 959.98px) {
  .tiles {
    gap: 8px;
  }
}
.hero {
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  padding: 28px 32px;
  border-radius: 16px;
  color: var(--on-brand);
  background:
    radial-gradient(520px 220px at 100% 0%, rgba(226, 183, 101, 0.22), transparent 70%),
    linear-gradient(135deg, var(--brand-2), var(--brand-1));
  box-shadow: var(--shadow-2);
}
/* Faint ledger rules across the banner: brand texture without an image request. */
.hero::before {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(0deg, transparent 0 27px, rgba(255, 255, 255, 0.04) 27px 28px);
  pointer-events: none;
}
.hero__body,
.hero__stat {
  position: relative;
}
.hero__date {
  font-size: 12px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--brand-accent);
}
.hero__title {
  margin: 6px 0 4px;
  font-size: 28px;
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.15;
}
.hero__sub {
  margin: 0;
  font-size: 14px;
  color: var(--on-brand-2);
}
.hero__stat {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
  padding-left: 24px;
  border-left: 1px solid rgba(255, 255, 255, 0.14);
}
.hero__stat-lbl {
  font-size: 11px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--on-brand-2);
}
.hero__stat-num {
  font-size: 34px;
  font-weight: 500;
  line-height: 1.1;
}
@media (max-width: 959.98px) {
  .hero {
    padding: 20px;
    border-radius: 14px;
  }
  .hero__title {
    font-size: 22px;
  }
  .hero__stat {
    display: none;
  }
}

.tile {
  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 18px 20px;
  border-radius: var(--radius);
  border: 1px solid var(--line);
  background: var(--card);
  box-shadow: var(--shadow-1);
  text-decoration: none;
  color: var(--ink);
  transition: border-color 120ms ease, box-shadow 160ms ease, transform var(--dur-press) var(--ease-out);
}
@media (hover: hover) and (pointer: fine) {
  .tile:hover {
    border-color: var(--line-2);
    box-shadow: var(--shadow-2);
  }
  .tile:hover .tile__go {
    opacity: 1;
    transform: translate(0, 0);
  }
}
.tile:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.tile__icon {
  flex: none;
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--navy-soft);
  color: var(--navy);
}
.tile--warning .tile__icon {
  background: rgba(var(--v-theme-warning), 0.14);
  color: rgb(var(--v-theme-warning));
}
.tile--error .tile__icon {
  background: rgba(var(--v-theme-error), 0.12);
  color: rgb(var(--v-theme-error));
}
.tile__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.tile__go {
  position: absolute;
  top: 14px;
  right: 14px;
  color: var(--ink-3);
  opacity: 0;
  transform: translate(-3px, 3px);
  transition: opacity 160ms ease, transform 200ms var(--ease-out);
}
.tile__num {
  font-size: 28px;
  font-weight: 500;
  line-height: 1.1;
}
.tile--warning .tile__num {
  color: rgb(var(--v-theme-warning));
}
.tile--error .tile__num {
  color: rgb(var(--v-theme-error));
}
@media (max-width: 959.98px) {
  /* Phone: three compact tiles in one row (icon, number, label) instead of three full-width bars. */
  .tile {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    padding: 12px;
  }
  .tile__icon {
    width: 30px;
    height: 30px;
    border-radius: 9px;
  }
  .tile__text {
    flex-direction: column-reverse;
  }
  .tile__num {
    font-size: 22px;
  }
  .tile__go {
    display: none;
  }
}

.recent {
  display: flex;
  flex-direction: column;
}
.recent__row {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
  padding: 12px 20px;
  min-height: 56px;
  border: 0;
  border-bottom: 1px solid var(--line);
  background: none;
  font: inherit;
  color: inherit;
  text-align: left;
  cursor: pointer;
}
@media (hover: hover) and (pointer: fine) {
  .recent__row {
    transition: background-color 120ms ease, transform var(--dur-press) var(--ease-out);
  }
  .recent__row:hover {
    background: var(--navy-soft);
  }
}
.recent__row:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
.recent__row:last-child {
  border-bottom: 0;
}
.recent__main {
  flex: 1;
  min-width: 0;
}
.recent__title {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.recent__meta {
  font-size: 12px;
  color: var(--ink-3);
}
.recent__amt {
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
}
@media (max-width: 959.98px) {
  /* Phone: title and amount on the first line, reference, period and status on the second. */
  .recent__row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    grid-template-areas:
      'title amt'
      'meta chip';
    align-items: center;
    column-gap: 12px;
    row-gap: 4px;
    padding: 12px 16px;
  }
  .recent__main {
    display: contents;
  }
  .recent__title {
    grid-area: title;
    font-weight: 500;
  }
  .recent__meta {
    grid-area: meta;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .recent__lines {
    display: none;
  }
  .recent__amt {
    grid-area: amt;
    text-align: right;
  }
  .recent__chip {
    grid-area: chip;
    justify-self: end;
  }
}
</style>
