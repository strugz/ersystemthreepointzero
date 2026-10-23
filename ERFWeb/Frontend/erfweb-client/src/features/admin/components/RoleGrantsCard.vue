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
 * Role grants for one account. A grant takes effect as soon as it is saved; the server checks the
 * stacking rules and refuses changes to the administrator's own account.
 */
const props = withDefaults(
  defineProps<{
    grants: RoleGrant[];
    /** The signed-in administrator; used to block changes to one's own roles. */
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

// ---- revoke -----------------------------------------------------------------
const pending = ref<RoleGrant | null>(null);
const actionNote = ref('');

function askRevoke(grant: RoleGrant) {
  actionNote.value = '';
  pending.value = grant;
}

function confirmAction() {
  if (!pending.value) return;
  emit('revoke', pending.value, actionNote.value.trim() || null);
}

/** Parent calls this after the action succeeds. */
function closeAction() {
  pending.value = null;
}
defineExpose({ closeGrant, closeAction });

const actionMessage = computed(() =>
  pending.value ? `Revoke ${roleLabel(pending.value.role)} from this account? The role stops immediately.` : '',
);
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">Roles</span>
        <span class="card-meta d-none d-sm-inline">Employee is implicit; a granted role applies at once</span>
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
        </div>
        <div class="text-caption text-medium-emphasis">
          Granted by {{ g.grantedByName ?? '—' }} · {{ formatDateTime(g.grantedAt) }}
          <template v-if="g.revokedAt"><br />Revoked by {{ g.revokedByName ?? '—' }} · {{ formatDateTime(g.revokedAt) }}</template>
        </div>
        <div v-if="g.note" class="text-caption">{{ g.note }}</div>
        <div v-if="g.status !== 'Revoked' && !isSelf" class="d-flex ga-2 mt-1">
          <v-btn class="press" size="small" variant="text" color="error" :disabled="disabled || acting" @click="askRevoke(g)">Revoke</v-btn>
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
            <th class="d-none d-md-table-cell">Revoked</th>
            <th class="d-none d-lg-table-cell">Note</th>
            <th class="r"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="g in ordered" :key="g.role" :class="{ 'text-medium-emphasis': g.status === 'Revoked' }">
            <td class="font-weight-medium text-no-wrap">{{ roleLabel(g.role) }}</td>
            <td><span class="pill" :class="grantStatusTone[g.status]">{{ grantStatusLabel[g.status] }}</span></td>
            <td class="text-no-wrap">
              <div>{{ g.grantedByName ?? '—' }}</div>
              <div class="text-caption text-medium-emphasis">{{ formatDateTime(g.grantedAt) }}</div>
            </td>
            <td class="text-no-wrap d-none d-md-table-cell">
              <template v-if="g.status === 'Revoked'">
                <div>{{ g.revokedByName ?? '—' }}</div>
                <div class="text-caption text-medium-emphasis">{{ formatDateTime(g.revokedAt) }}</div>
              </template>
              <span v-else class="text-medium-emphasis">—</span>
            </td>
            <td class="d-none d-lg-table-cell text-truncate" style="max-width: 260px" :title="g.note ?? ''">{{ g.note || '—' }}</td>
            <td class="r text-no-wrap">
              <v-btn
                v-if="g.status !== 'Revoked' && !isSelf"
                class="press"
                size="small"
                variant="text"
                color="error"
                :disabled="disabled || acting"
                @click="askRevoke(g)"
              >
                Revoke
              </v-btn>
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
              The role applies as soon as you grant it. A signatory also needs a scope (Scope page) before reports route to them.
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

    <!-- Revoke -->
    <AppConfirmDialog
      :model-value="pending !== null"
      title="Revoke role"
      :message="actionMessage"
      confirm-text="Revoke"
      confirm-color="error"
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
