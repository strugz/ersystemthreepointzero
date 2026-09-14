<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { formatDate, formatMoney } from '@/shared/format';
import { expensesApi } from '@/features/expenses/api';
import ReportStatusChip from '@/features/expenses/components/ReportStatusChip.vue';
import type { ExpenseReportListItem } from '@/features/expenses/types';

const session = useSessionStore();
const router = useRouter();
const { xs } = useDisplay();

const recent = ref<ExpenseReportListItem[]>([]);
const counts = ref<{ all: number; drafts: number; forApproval: number; returned: number } | null>(null);
const loading = ref(true);

onMounted(async () => {
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

const tiles = [
  { key: 'drafts', label: 'Drafts', status: 'New', tone: 'tile--neutral' },
  { key: 'forApproval', label: 'Awaiting approval', status: 'ForApproval', tone: 'tile--warning' },
  { key: 'returned', label: 'Returned to me', status: 'Returned', tone: 'tile--error' },
] as const;
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Dashboard' }]">
    <template #actions>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-plus" :to="{ name: 'report-new' }">New report</v-btn>
    </template>

    <div class="mb-5">
      <h1 class="page-title">{{ xs ? 'Hello' : 'Good day' }}{{ session.user ? `, ${session.user.fullName.split(' ')[0]}` : '' }}</h1>
      <p class="text-body-2 text-medium-emphasis mb-0">Prepare and file your expense reports, then print the R47.</p>
    </div>

    <div class="tiles">
      <router-link
        v-for="(t, i) in tiles"
        :key="t.key"
        class="tile press stagger-in"
        :class="t.tone"
        :style="{ '--i': i }"
        :to="{ name: 'reports', query: { status: t.status } }"
      >
        <span class="lbl">{{ t.label }}</span>
        <span class="mono tile__num">{{ counts ? counts[t.key] : '—' }}</span>
      </router-link>
    </div>

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
              <span class="mono">{{ item.referenceNo || 'Draft' }}</span> · {{ formatDate(item.dateFrom) }} – {{ formatDate(item.dateTo) }} · {{ item.lineCount }} line{{ item.lineCount === 1 ? '' : 's' }}
            </div>
          </div>
          <span class="mono recent__amt">{{ formatMoney(item.totalAmount) }}</span>
          <ReportStatusChip :status="item.status" />
        </button>
      </div>
    </v-card>
  </AuthenticatedLayout>
</template>

<style scoped>
.tiles {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
@media (max-width: 599.98px) {
  .tiles {
    grid-template-columns: 1fr;
    gap: 10px;
  }
}
.tile {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 18px 20px;
  border-radius: var(--radius);
  border: 1px solid var(--line);
  background: var(--card);
  text-decoration: none;
  color: var(--ink);
  transition: border-color 120ms ease, transform var(--dur-press) var(--ease-out);
}
@media (hover: hover) and (pointer: fine) {
  .tile:hover {
    border-color: var(--line-2);
  }
}
.tile:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.tile__num {
  font-size: 30px;
  font-weight: 500;
  line-height: 1.1;
}
.tile--warning .tile__num {
  color: rgb(var(--v-theme-warning));
}
.tile--error .tile__num {
  color: rgb(var(--v-theme-error));
}
@media (max-width: 599.98px) {
  .tile {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    padding: 14px 16px;
  }
  .tile__num {
    font-size: 22px;
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
@media (max-width: 599.98px) {
  .recent__row {
    padding: 12px 16px;
    gap: 10px;
  }
}
</style>
