<script setup lang="ts">
import { computed, ref } from 'vue';
import { useDisplay } from 'vuetify';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDateTime } from '@/shared/format';
import { grantableRoles, roleLabel } from '@/features/auth/labels';
import type { UserRole } from '@/features/auth/types';
import { grantStatusLabel, grantStatusTone } from '../labels';
import type { RoleGrant } from '../types';

/**
 * Role grants for one account. Privileged roles are granted in PendingActivation and need a
 * countersignature from a second administrator (or the Business Owner) before they take effect —
 * the server decides who may countersign; the UI only hides the obvious cases (the granter, the
 * account itself).
 */
const props = withDefaults(
  defineProps<{
    grants: RoleGrant[];
    /** The signed-in administrator; used to disable countersigning one's own grant. */
    currentUserId: number;
    /** The account being administered. */
    userId: number;
    acting?: boolean;
    disabled?: boolean;
    /** Server validation errors for the grant dialog (e.g. errors.role for stacking violations). */
    grantErrors?: Record<string, string[]> | null;
  }>(),
  { acting: false, disabled: false, grantErrors: null },
);

const emit = defineEmits<{
  grant: [role: UserRole, note: string | null];
  countersign: [grant: RoleGrant, note: string | null];
  revoke: [grant: RoleGrant, note: string | null];
  'clear-grant-errors': [];
}>();

const { xs } = useDisplay();

const isSelf = computed(() => props.userId === props.currentUserId);

// Revoked grants sit last so the live picture reads first.
const ordered = computed(() =>
  [...props.grants].sort((a, b) => {
    const rank = (g: RoleGrant) => (g.status === 'Revoked' ? 1 : 0);
    return rank(a) - rank(b) || grantableRoles.indexOf(a.role) - grantableRoles.indexOf(b.role);
  }),
);

const availableRoles = computed(() => {
  const taken = new Set(props.grants.filter((g) => g.status !== 'Revoked').map((g) => g.role));
  return grantableRoles.filter((r) => !taken.has(r)).map((value) => ({ value, title: roleLabel(value) }));
});

function canCountersign(grant: RoleGrant): boolean {
  return grant.status === 'PendingActivation' && grant.grantedByUserId !== props.currentUserId && !isSelf.value;
}

// ---- grant dialog -----------------------------------------------------------
const grantOpen = ref(false);
const grantRole = ref<UserRole | null>(null);
const grantNote = ref('');

function openGrant() {
  grantRole.value = null;
  grantNote.value = '';
  emit('clear-grant-errors');
  grantOpen.value = true;
}

function submitGrant() {
  if (!grantRole.value) return;
  emit('grant', grantRole.value, grantNote.value.trim() || null);
}

/** Parent calls this after a successful grant. */
function closeGrant() {
  grantOpen.value = false;
}

// ---- countersign / revoke ----------------------------------------------------
const pending = ref<{ kind: 'countersign' | 'revoke'; grant: RoleGrant } | null>(null);
const actionNote = ref('');

function ask(kind: 'countersign' | 'revoke', grant: RoleGrant) {
  actionNote.value = '';
  pending.value = { kind, grant };
}

function confirmAction() {
  if (!pending.value) return;
  const { kind, grant } = pending.value;
  const note = actionNote.value.trim() || null;
  if (kind === 'countersign') {
    emit('countersign', grant, note);
  } else {
    emit('revoke', grant, note);
  }
}

/** Parent calls this after the action succeeds. */
function closeAction() {
  pending.value = null;
}
defineExpose({ closeGrant, closeAction });

const actionTitle = computed(() => (pending.value?.kind === 'countersign' ? 'Countersign role' : 'Revoke role'));
const actionMessage = computed(() => {
  const p = pending.value;
  if (!p) return '';
  const role = roleLabel(p.grant.role);
  return p.kind === 'countersign'
    ? `Activate ${role} for this account? The role takes effect immediately and the countersignature is recorded in the audit log.`
    : `Revoke ${role} from this account? Any pending or active grant of this role stops immediately.`;
});
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">Roles</span>
        <span class="card-meta d-none d-sm-inline">Employee is implicit; privileged roles need a countersignature</span>
      </div>
      <v-btn
        class="press"
        size="small"
        variant="outlined"
        prepend-icon="mdi-plus"
        :disabled="disabled || isSelf || availableRoles.length === 0"
        @click="openGrant"
      >
        Grant role
      </v-btn>
    </div>

    <v-alert v-if="isSelf" type="info" variant="tonal" density="compact" class="ma-4">
      You cannot change your own roles. Ask another administrator.
    </v-alert>

    <div v-if="ordered.length === 0" class="text-body-2 text-medium-emphasis pa-5">No roles granted beyond Employee.</div>

    <!-- Phone: stacked rows -->
    <ul v-else-if="xs" class="grants">
      <li v-for="g in ordered" :key="g.role" class="grant">
        <div class="d-flex align-center ga-2 flex-wrap">
          <span class="font-weight-medium">{{ roleLabel(g.role) }}</span>
          <span class="pill" :class="grantStatusTone[g.status]">{{ grantStatusLabel[g.status] }}</span>
          <v-chip v-if="g.isBootstrap" size="x-small" label variant="tonal" color="primary">Bootstrap</v-chip>
        </div>
        <div class="text-caption text-medium-emphasis">
          Granted by {{ g.grantedByName ?? '—' }} · {{ formatDateTime(g.grantedAt) }}
          <template v-if="g.countersignedAt"><br />Countersigned by {{ g.countersignedByName ?? '—' }} · {{ formatDateTime(g.countersignedAt) }}</template>
          <template v-if="g.revokedAt"><br />Revoked by {{ g.revokedByName ?? '—' }} · {{ formatDateTime(g.revokedAt) }}</template>
        </div>
        <div v-if="g.note" class="text-caption">{{ g.note }}</div>
        <div v-if="g.status !== 'Revoked' && !isSelf" class="d-flex ga-2 mt-1">
          <v-btn v-if="g.status === 'PendingActivation'" class="press" size="small" variant="tonal" color="success" :disabled="disabled || acting || !canCountersign(g)" @click="ask('countersign', g)">Countersign</v-btn>
          <v-btn class="press" size="small" variant="text" color="error" :disabled="disabled || acting" @click="ask('revoke', g)">Revoke</v-btn>
        </div>
      </li>
    </ul>

    <!-- Tablet / desktop: ledger -->
    <div v-else class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th>Role</th>
            <th>Status</th>
            <th>Granted</th>
            <th class="d-none d-md-table-cell">Countersigned</th>
            <th class="d-none d-lg-table-cell">Note</th>
            <th class="r"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="g in ordered" :key="g.role" :class="{ 'text-medium-emphasis': g.status === 'Revoked' }">
            <td class="font-weight-medium text-no-wrap">
              {{ roleLabel(g.role) }}
              <v-chip v-if="g.isBootstrap" size="x-small" label variant="tonal" color="primary" class="ml-1">Bootstrap</v-chip>
            </td>
            <td><span class="pill" :class="grantStatusTone[g.status]">{{ grantStatusLabel[g.status] }}</span></td>
            <td class="text-no-wrap">
              <div>{{ g.grantedByName ?? '—' }}</div>
              <div class="text-caption text-medium-emphasis">{{ formatDateTime(g.grantedAt) }}</div>
            </td>
            <td class="text-no-wrap d-none d-md-table-cell">
              <template v-if="g.status === 'Revoked'">
                <div>Revoked by {{ g.revokedByName ?? '—' }}</div>
                <div class="text-caption text-medium-emphasis">{{ formatDateTime(g.revokedAt) }}</div>
              </template>
              <template v-else-if="g.countersignedAt">
                <div>{{ g.countersignedByName ?? '—' }}</div>
                <div class="text-caption text-medium-emphasis">{{ formatDateTime(g.countersignedAt) }}</div>
              </template>
              <span v-else class="text-medium-emphasis">Awaiting</span>
            </td>
            <td class="d-none d-lg-table-cell text-truncate" style="max-width: 260px" :title="g.note ?? ''">{{ g.note || '—' }}</td>
            <td class="r text-no-wrap">
              <template v-if="g.status !== 'Revoked' && !isSelf">
                <v-tooltip v-if="g.status === 'PendingActivation'" :disabled="canCountersign(g)" text="The granter cannot countersign their own grant." location="top">
                  <template #activator="{ props: tip }">
                    <span v-bind="tip">
                      <v-btn class="press" size="small" variant="tonal" color="success" :disabled="disabled || acting || !canCountersign(g)" @click="ask('countersign', g)">Countersign</v-btn>
                    </span>
                  </template>
                </v-tooltip>
                <v-btn class="press ml-1" size="small" variant="text" color="error" :disabled="disabled || acting" @click="ask('revoke', g)">Revoke</v-btn>
              </template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Grant dialog -->
    <v-dialog v-model="grantOpen" max-width="520" :persistent="acting">
      <v-card>
        <v-card-title class="text-subtitle-1 font-weight-bold">Grant role</v-card-title>
        <v-card-text>
          <v-form :disabled="acting" @submit.prevent="submitGrant">
            <v-select v-model="grantRole" :items="availableRoles" label="Role" autofocus :error-messages="grantErrors?.role ?? grantErrors?.Role" />
            <v-textarea v-model="grantNote" label="Note (optional)" rows="2" auto-grow maxlength="500" counter :error-messages="grantErrors?.note ?? grantErrors?.Note" />
            <p class="text-caption text-medium-emphasis mb-0">
              Signatory, Finance, Expense Verifier, System Administrator and Business Owner grants stay pending until another administrator countersigns.
            </p>
          </v-form>
        </v-card-text>
        <v-divider />
        <v-card-actions class="px-4 py-3">
          <v-spacer />
          <v-btn class="press" variant="text" :disabled="acting" @click="grantOpen = false">Cancel</v-btn>
          <v-btn class="press" color="primary" variant="flat" :loading="acting" :disabled="!grantRole" @click="submitGrant">Grant</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Countersign / revoke -->
    <AppConfirmDialog
      :model-value="pending !== null"
      :title="actionTitle"
      :message="actionMessage"
      :confirm-text="pending?.kind === 'countersign' ? 'Countersign' : 'Revoke'"
      :confirm-color="pending?.kind === 'countersign' ? 'success' : 'error'"
      :loading="acting"
      @update:model-value="(v: boolean) => !v && (pending = null)"
      @confirm="confirmAction"
    >
      <v-textarea v-model="actionNote" label="Note (optional)" rows="2" auto-grow maxlength="500" hide-details :disabled="acting" />
    </AppConfirmDialog>
  </v-card>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.grants {
  list-style: none;
  margin: 0;
  padding: 0;
}
.grant {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}
.grant:last-child {
  border-bottom: 0;
}
</style>
