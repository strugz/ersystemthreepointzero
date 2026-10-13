<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import type { Crumb } from '@/layouts/AuthenticatedLayout.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDateTime } from '@/shared/format';
import { useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { adminApi } from '@/features/admin/api';
import UserForm from '@/features/admin/components/UserForm.vue';
import RoleGrantsCard from '@/features/admin/components/RoleGrantsCard.vue';
import SignatureUpload from '@/features/admin/components/SignatureUpload.vue';
import ResetPasswordDialog from '@/features/admin/components/ResetPasswordDialog.vue';
import { roleLabel } from '@/features/auth/labels';
import type { UserRole } from '@/features/auth/types';
import type { CostCenter, CreateUserInput, Department, RoleGrant, UserDetail } from '@/features/admin/types';

const route = useRoute();
const router = useRouter();
const session = useSessionStore();
const snack = useSnackbar();
const { xs, lgAndUp } = useDisplay();
const userId = Number(route.params.id);

const user = ref<UserDetail | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);
const departments = ref<Department[]>([]);
const costCenters = ref<CostCenter[]>([]);

async function load() {
  loading.value = true;
  loadError.value = null;
  try {
    user.value = await adminApi.getUser(userId);
  } catch (err) {
    loadError.value =
      err instanceof ApiError && err.status === 404
        ? 'User not found.'
        : err instanceof ApiError && err.status === 403
          ? 'You are not allowed to view this account.'
          : 'Unable to load the account.';
  } finally {
    loading.value = false;
  }
}

async function loadLookups() {
  const [d, c] = await Promise.allSettled([adminApi.departments(), adminApi.costCenters()]);
  if (d.status === 'fulfilled') departments.value = d.value;
  if (c.status === 'fulfilled') costCenters.value = c.value;
}

onMounted(async () => {
  if (!Number.isInteger(userId) || userId <= 0) {
    loading.value = false;
    loadError.value = 'User not found.';
    return;
  }
  await Promise.all([load(), loadLookups()]);
});

// A 409 means another administrator changed the account; reload so the user sees the current state.
async function handleMutationError(err: unknown, fallback: string): Promise<Record<string, string[]> | null> {
  snack.fromError(err, fallback);
  if (err instanceof ApiError && err.status === 409) {
    await load();
  }
  return err instanceof ApiError && err.problem?.errors ? err.problem.errors : null;
}

const isSelf = computed(() => user.value?.id === session.user?.userId);
const crumbs = computed<Crumb[]>(() => [{ title: 'Users', to: { name: 'admin-users' } }, { title: user.value?.fullName ?? 'User' }]);

const activeRoles = computed(() => user.value?.roleGrants.filter((g) => g.status === 'Active').map((g) => roleLabel(g.role)) ?? []);

// ---- profile ----------------------------------------------------------------
const editOpen = ref(false);
const savingProfile = ref(false);
const profileErrors = ref<Record<string, string[]> | null>(null);

async function saveProfile(input: CreateUserInput) {
  if (!user.value) return;
  savingProfile.value = true;
  profileErrors.value = null;
  try {
    user.value = await adminApi.updateUser(userId, {
      fullName: input.fullName,
      position: input.position,
      jobGrade: input.jobGrade,
      email: input.email,
      mobileNo: input.mobileNo,
      departmentId: input.departmentId,
      costCenterId: input.costCenterId,
      workWithEnabled: input.workWithEnabled,
      rowVersion: user.value.rowVersion,
    });
    editOpen.value = false;
    snack.success('Profile saved.');
  } catch (err) {
    profileErrors.value = await handleMutationError(err, 'Unable to save the profile.');
  } finally {
    savingProfile.value = false;
  }
}

// ---- active / inactive ---------------------------------------------------------
const activeDialog = ref(false);
const togglingActive = ref(false);

async function toggleActive() {
  if (!user.value) return;
  togglingActive.value = true;
  try {
    const next = !user.value.isActive;
    user.value = await adminApi.setActive(userId, { isActive: next, rowVersion: user.value.rowVersion });
    activeDialog.value = false;
    snack.success(next ? 'Account activated.' : 'Account deactivated.');
  } catch (err) {
    await handleMutationError(err, 'Unable to change the account status.');
  } finally {
    togglingActive.value = false;
  }
}

// ---- password ---------------------------------------------------------------
const passwordOpen = ref(false);
const savingPassword = ref(false);
const passwordErrors = ref<Record<string, string[]> | null>(null);

async function resetPassword(newPassword: string) {
  savingPassword.value = true;
  passwordErrors.value = null;
  try {
    await adminApi.resetPassword(userId, newPassword);
    passwordOpen.value = false;
    snack.success('Password reset.');
  } catch (err) {
    passwordErrors.value = await handleMutationError(err, 'Unable to reset the password.');
  } finally {
    savingPassword.value = false;
  }
}

// ---- signature --------------------------------------------------------------
const uploadingSignature = ref(false);

async function uploadSignature(file: File) {
  uploadingSignature.value = true;
  try {
    user.value = await adminApi.uploadSignature(userId, file);
    snack.success('Signature saved.');
  } catch (err) {
    await handleMutationError(err, 'Unable to upload the signature.');
  } finally {
    uploadingSignature.value = false;
  }
}

// ---- roles ------------------------------------------------------------------
const rolesCard = ref<InstanceType<typeof RoleGrantsCard> | null>(null);
const actingRole = ref(false);
const grantErrors = ref<Record<string, string[]> | null>(null);

async function grantRole(role: UserRole, note: string | null) {
  actingRole.value = true;
  grantErrors.value = null;
  try {
    user.value = await adminApi.grantRole(userId, { role, note });
    rolesCard.value?.closeGrant();
    const grant = user.value.roleGrants.find((g) => g.role === role);
    snack.success(grant?.status === 'PendingActivation' ? `${roleLabel(role)} granted; awaiting countersignature.` : `${roleLabel(role)} granted.`);
  } catch (err) {
    grantErrors.value = await handleMutationError(err, 'Unable to grant the role.');
  } finally {
    actingRole.value = false;
  }
}

async function countersign(grant: RoleGrant, note: string | null) {
  actingRole.value = true;
  try {
    user.value = await adminApi.countersignRole(userId, grant.role, { note, rowVersion: grant.rowVersion });
    rolesCard.value?.closeAction();
    snack.success(`${roleLabel(grant.role)} is now active.`);
  } catch (err) {
    await handleMutationError(err, 'Unable to countersign the role.');
  } finally {
    actingRole.value = false;
  }
}

async function revoke(grant: RoleGrant, note: string | null) {
  actingRole.value = true;
  try {
    user.value = await adminApi.revokeRole(userId, grant.role, { note, rowVersion: grant.rowVersion });
    rolesCard.value?.closeAction();
    snack.success(`${roleLabel(grant.role)} revoked.`);
  } catch (err) {
    await handleMutationError(err, 'Unable to revoke the role.');
  } finally {
    actingRole.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="crumbs" :back="{ name: 'admin-users' }">
    <template v-if="user && !isSelf" #actions>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-lock-reset" @click="passwordOpen = true">Reset password</v-btn>
      <v-btn class="press" :color="user.isActive ? 'error' : 'success'" variant="tonal" :prepend-icon="user.isActive ? 'mdi-account-off-outline' : 'mdi-account-check-outline'" @click="activeDialog = true">
        {{ user.isActive ? 'Deactivate' : 'Activate' }}
      </v-btn>
    </template>
    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-else-if="loadError" type="error" class="mt-2">
      {{ loadError }}
      <template #append>
        <v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn>
        <v-btn class="press" size="small" variant="text" @click="router.push({ name: 'admin-users' })">Back to users</v-btn>
      </template>
    </v-alert>

    <template v-else-if="user">
      <div class="d-flex align-start ga-3 flex-wrap mb-5">
        <div class="flex-grow-1" style="min-width: 0">
          <div class="d-flex align-center ga-2 flex-wrap">
            <h1 class="page-title mb-0">{{ user.fullName }}</h1>
            <span class="pill" :class="user.isActive ? 'pill--success' : 'pill--neutral'">{{ user.isActive ? 'Active' : 'Inactive' }}</span>
            <span v-if="isSelf" class="pill pill--neutral">This is you</span>
          </div>
          <div class="text-body-2 text-medium-emphasis mt-1">
            <span class="mono">{{ user.username }}</span>
            <template v-if="user.position"> · {{ user.position }}</template>
            <template v-if="user.departmentName"> · {{ user.departmentName }}</template>
          </div>
        </div>
      </div>

      <v-alert v-if="isSelf" type="info" variant="tonal" density="compact" class="mb-4">
        An administrator cannot change their own account. Ask another administrator for profile, role or status changes.
      </v-alert>
      <v-alert v-else-if="!user.coolingPeriodElapsed" type="warning" variant="tonal" density="compact" class="mb-4">
        This account is still inside its cooling period and cannot file expense reports yet.
      </v-alert>

      <div class="detail" :class="{ 'detail--wide': lgAndUp }">
        <div class="detail__main">
          <!-- Profile -->
          <v-card>
            <div class="card-h">
              <span class="card-t">Profile</span>
              <v-btn v-if="!editOpen" class="press" size="small" variant="outlined" prepend-icon="mdi-pencil-outline" :disabled="isSelf" @click="editOpen = true">Edit</v-btn>
            </div>
            <div v-if="editOpen" class="pa-4">
              <UserForm
                mode="edit"
                :initial="user"
                :departments="departments"
                :cost-centers="costCenters"
                :saving="savingProfile"
                :errors="profileErrors"
                @submit="saveProfile"
                @cancel="editOpen = false"
              />
            </div>
            <div v-else class="profile">
              <div class="field"><span class="lbl">Full name</span><span class="val">{{ user.fullName }}</span></div>
              <div class="field"><span class="lbl">Username</span><span class="val mono">{{ user.username }}</span></div>
              <div class="field"><span class="lbl">Position</span><span class="val" :class="{ muted: !user.position }">{{ user.position || '—' }}</span></div>
              <div class="field"><span class="lbl">Job grade</span><span class="val" :class="{ muted: !user.jobGrade }">{{ user.jobGrade || '—' }}</span></div>
              <div class="field"><span class="lbl">Email</span><span class="val" :class="{ muted: !user.email }">{{ user.email || '—' }}</span></div>
              <div class="field"><span class="lbl">Mobile no.</span><span class="val" :class="{ muted: !user.mobileNo }">{{ user.mobileNo || '—' }}</span></div>
              <div class="field"><span class="lbl">Department</span><span class="val" :class="{ muted: !user.departmentName }">{{ user.departmentName || '—' }}</span></div>
              <div class="field">
                <span class="lbl">Cost center</span>
                <span class="val" :class="{ muted: user.costCenterId === null }">
                  {{ costCenters.find((c) => c.id === user?.costCenterId)?.name ?? (user.costCenterId === null ? '—' : `#${user.costCenterId}`) }}
                </span>
              </div>
              <div class="field"><span class="lbl">Work With</span><span class="val">{{ user.workWithEnabled ? 'Listed on expense lines' : 'Not listed' }}</span></div>
              <div class="field"><span class="lbl">Created</span><span class="val">{{ formatDateTime(user.createdAt) }}</span></div>
            </div>
          </v-card>

          <RoleGrantsCard
            ref="rolesCard"
            :grants="user.roleGrants"
            :current-user-id="session.user?.userId ?? 0"
            :user-id="user.id"
            :acting="actingRole"
            :grant-errors="grantErrors"
            @grant="grantRole"
            @countersign="countersign"
            @revoke="revoke"
            @clear-grant-errors="grantErrors = null"
          />

          <SignatureUpload v-if="!lgAndUp" :has-signature="user.hasSignature" :uploading="uploadingSignature" :disabled="isSelf" @upload="uploadSignature" />
        </div>

        <aside v-if="lgAndUp" class="detail__side">
          <v-card>
            <div class="card-h"><span class="card-t">Summary</span></div>
            <div class="profile profile--stack">
              <div class="field"><span class="lbl">Active roles</span><span class="val" :class="{ muted: !activeRoles.length }">{{ activeRoles.length ? activeRoles.join(', ') : 'Employee only' }}</span></div>
              <div class="field"><span class="lbl">Signature</span><span class="val">{{ user.hasSignature ? 'On file' : 'Not on file' }}</span></div>
              <div class="field"><span class="lbl">Cooling period</span><span class="val">{{ user.coolingPeriodElapsed ? 'Over' : 'In progress' }}</span></div>
            </div>
          </v-card>
          <SignatureUpload :has-signature="user.hasSignature" :uploading="uploadingSignature" :disabled="isSelf" @upload="uploadSignature" />
        </aside>
      </div>

      <AppConfirmDialog
        v-model="activeDialog"
        :title="user.isActive ? 'Deactivate account' : 'Activate account'"
        :message="
          user.isActive
            ? `${user.fullName} will no longer be able to sign in. Their reports and approvals stay on record.`
            : `${user.fullName} will be able to sign in again.`
        "
        :confirm-text="user.isActive ? 'Deactivate' : 'Activate'"
        :confirm-color="user.isActive ? 'error' : 'success'"
        :loading="togglingActive"
        @confirm="toggleActive"
      />

      <ResetPasswordDialog v-model="passwordOpen" :full-name="user.fullName" :saving="savingPassword" :errors="passwordErrors" @submit="resetPassword" />
    </template>

    <template v-if="xs && user && !isSelf && !editOpen" #bottom>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-lock-reset" @click="passwordOpen = true">Reset password</v-btn>
      <v-btn class="press" :color="user.isActive ? 'error' : 'success'" variant="tonal" @click="activeDialog = true">
        {{ user.isActive ? 'Deactivate' : 'Activate' }}
      </v-btn>
    </template>
  </AuthenticatedLayout>
</template>

<style scoped>
.detail {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.detail--wide {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  align-items: start;
}
.detail__main,
.detail__side {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
.profile {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px 24px;
  padding: 16px 20px;
}
.profile--stack {
  grid-template-columns: 1fr;
}
@media (max-width: 599.98px) {
  .profile {
    grid-template-columns: 1fr;
  }
}
</style>
