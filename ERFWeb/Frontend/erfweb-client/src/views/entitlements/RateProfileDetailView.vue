<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useDisplay } from 'vuetify';
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import type { Crumb } from '@/layouts/AuthenticatedLayout.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { entitlementLookupsApi, ratesApi, scopeApi } from '@/features/entitlements/api';
import { hasAnyRole, rateWriterRoles } from '@/features/entitlements/access';
import { fieldErrors, peopleFromCoverage } from '@/features/entitlements/forms';
import { rateKeyLabel } from '@/features/entitlements/labels';
import RateProfileLinesCard from '@/features/entitlements/components/RateProfileLinesCard.vue';
import RateLineDialog from '@/features/entitlements/components/RateLineDialog.vue';
import RateAssignmentsCard from '@/features/entitlements/components/RateAssignmentsCard.vue';
import RateAssignmentDialog from '@/features/entitlements/components/RateAssignmentDialog.vue';
import ResolveRatesPanel from '@/features/entitlements/components/ResolveRatesPanel.vue';
import type {
  AddRateLineInput,
  CreateRateAssignmentInput,
  DepartmentOption,
  PersonOption,
  RateAssignment,
  RateLine,
  RateProfileDetail,
  ResolvedRates,
  UpdateRateAssignmentInput,
  UpdateRateLineInput,
} from '@/features/entitlements/types';

const route = useRoute();
const router = useRouter();
const session = useSessionStore();
const snack = useSnackbar();
const { xs, lgAndUp } = useDisplay();
const profileId = Number(route.params.id);

// Presentation only: the API refuses rate changes that would apply to the actor.
const canWrite = computed(() => hasAnyRole(session.user?.roles, rateWriterRoles));

const profile = ref<RateProfileDetail | null>(null);
const loading = ref(true);
const loadError = ref<string | null>(null);

async function load() {
  loading.value = profile.value === null;
  loadError.value = null;
  try {
    profile.value = await ratesApi.profile(profileId);
  } catch (err) {
    loadError.value =
      err instanceof ApiError && err.status === 404
        ? 'Rate profile not found.'
        : err instanceof ApiError && err.status === 403
          ? describeError(err)
          : 'Unable to load the rate profile.';
  } finally {
    loading.value = false;
  }
}

const people = ref<PersonOption[]>([]);
const departments = ref<DepartmentOption[]>([]);

async function loadLookups() {
  // Pickers are optional: the profile still renders without them.
  const [c, d] = await Promise.allSettled([scopeApi.coverage(), canWrite.value ? entitlementLookupsApi.departments() : Promise.resolve([])]);
  if (c.status === 'fulfilled') people.value = peopleFromCoverage(c.value);
  else snack.fromError(c.reason, 'Unable to load the people list.');
  if (d.status === 'fulfilled') departments.value = d.value;
  else snack.fromError(d.reason, 'Unable to load departments.');
}

onMounted(async () => {
  if (!Number.isInteger(profileId) || profileId <= 0) {
    loading.value = false;
    loadError.value = 'Rate profile not found.';
    return;
  }
  await Promise.all([load(), loadLookups()]);
});

// A 409 means someone else changed the profile; reload so the user sees the current state.
async function handleMutationError(err: unknown, fallback: string): Promise<Record<string, string[]> | null> {
  snack.fromError(err, fallback);
  if (err instanceof ApiError && err.status === 409) {
    await load();
  }
  return err instanceof ApiError && err.problem?.errors ? err.problem.errors : null;
}

const crumbs = computed<Crumb[]>(() => [{ title: 'Rates', to: { name: 'rate-profiles' } }, { title: profile.value?.name ?? 'Profile' }]);

// ---- rename ------------------------------------------------------------------
const renameOpen = ref(false);
const renameValue = ref('');
const savingProfile = ref(false);
const profileErrors = ref<Record<string, string[]> | null>(null);

function openRename() {
  renameValue.value = profile.value?.name ?? '';
  profileErrors.value = null;
  renameOpen.value = true;
}

async function saveProfile(name: string, isActive: boolean, success: string): Promise<boolean> {
  if (!profile.value) return false;
  savingProfile.value = true;
  profileErrors.value = null;
  try {
    profile.value = await ratesApi.updateProfile(profileId, { name, isActive, rowVersion: profile.value.rowVersion });
    snack.success(success);
    return true;
  } catch (err) {
    profileErrors.value = await handleMutationError(err, 'Unable to save the profile.');
    return false;
  } finally {
    savingProfile.value = false;
  }
}

async function rename() {
  const name = renameValue.value.trim();
  if (!profile.value || !name) return;
  if (await saveProfile(name, profile.value.isActive, 'Profile renamed.')) renameOpen.value = false;
}

// ---- activate / deactivate ------------------------------------------------------
const activeDialog = ref(false);

async function toggleActive() {
  if (!profile.value) return;
  const next = !profile.value.isActive;
  // The server refuses (409) to deactivate a profile that is still assigned; the message says so.
  await saveProfile(profile.value.name, next, next ? 'Profile activated.' : 'Profile deactivated.');
  activeDialog.value = false;
}

// ---- lines --------------------------------------------------------------------
const lineDialog = ref(false);
const lineFrom = ref<RateLine | null>(null);
const savingLine = ref(false);
const lineErrors = ref<Record<string, string[]> | null>(null);

const lineEditing = ref<RateLine | null>(null);

function openLine(from: RateLine | null) {
  lineFrom.value = from;
  lineEditing.value = null;
  lineErrors.value = null;
  lineDialog.value = true;
}

function openEditLine(line: RateLine) {
  lineFrom.value = null;
  lineEditing.value = line;
  lineErrors.value = null;
  lineDialog.value = true;
}

async function updateLine(input: UpdateRateLineInput) {
  const line = lineEditing.value;
  if (!line) return;
  savingLine.value = true;
  lineErrors.value = null;
  try {
    profile.value = await ratesApi.updateLine(profileId, line.id, input);
    lineDialog.value = false;
    snack.success(`${rateKeyLabel(line.category, line.subCategory)} rate corrected.`);
  } catch (err) {
    lineErrors.value = await handleMutationError(err, 'Unable to correct the rate line.');
  } finally {
    savingLine.value = false;
  }
}

const lineDialogRef = ref<InstanceType<typeof RateLineDialog> | null>(null);

// Several meals sharing one amount arrive as several inputs. The API takes one line per call, so
// they go one by one; on a failure the saved ones leave the dialog and a retry sends only the rest.
async function addLine(inputs: AddRateLineInput[]) {
  savingLine.value = true;
  lineErrors.value = null;
  const saved: AddRateLineInput[] = [];
  try {
    for (const input of inputs) {
      profile.value = await ratesApi.addLine(profileId, input);
      saved.push(input);
    }
    lineDialog.value = false;
    const first = inputs[0];
    snack.success(inputs.length === 1 && first ? `${rateKeyLabel(first.category, first.subCategory)} rate saved.` : `${inputs.length} rate lines saved.`);
  } catch (err) {
    const failed = inputs[saved.length];
    const what = failed ? rateKeyLabel(failed.category, failed.subCategory) : 'the rate line';
    lineErrors.value = await handleMutationError(err, saved.length ? `${saved.length} saved; unable to save ${what}.` : `Unable to save ${what}.`);
    lineDialogRef.value?.dropMeals(saved.flatMap((s) => (s.subCategory ? [s.subCategory] : [])));
  } finally {
    savingLine.value = false;
  }
}

// ---- assignments ------------------------------------------------------------------
const assignmentsCard = ref<InstanceType<typeof RateAssignmentsCard> | null>(null);
const assignDialog = ref(false);
const savingAssignment = ref(false);
const assignErrors = ref<Record<string, string[]> | null>(null);
const ending = ref(false);
const endErrors = ref<Record<string, string[]> | null>(null);
const existingAssignments = ref<RateAssignment[] | null>(null);
const assignEditing = ref<RateAssignment | null>(null);

async function openAssign() {
  assignEditing.value = null;
  assignErrors.value = null;
  existingAssignments.value = null;
  assignDialog.value = true;
  // Every target's assignments (any profile), so the dialog asks for the backdate override only
  // when it would end one. If this fails the dialog stays strict and the server still decides.
  try {
    existingAssignments.value = await ratesApi.assignments({ includeEnded: true });
  } catch {
    existingAssignments.value = null;
  }
}

async function assign(input: CreateRateAssignmentInput) {
  savingAssignment.value = true;
  assignErrors.value = null;
  try {
    await ratesApi.createAssignment(input);
    assignDialog.value = false;
    snack.success('Profile assigned.');
    await load();
  } catch (err) {
    assignErrors.value = await handleMutationError(err, 'Unable to assign the profile.');
  } finally {
    savingAssignment.value = false;
  }
}

function openEditAssignment(a: RateAssignment) {
  assignEditing.value = a;
  assignErrors.value = null;
  assignDialog.value = true;
}

async function updateAssignment(input: UpdateRateAssignmentInput) {
  const a = assignEditing.value;
  if (!a) return;
  savingAssignment.value = true;
  assignErrors.value = null;
  try {
    await ratesApi.updateAssignment(a.id, input);
    assignDialog.value = false;
    snack.success('Assignment corrected.');
    await load();
  } catch (err) {
    assignErrors.value = await handleMutationError(err, 'Unable to correct the assignment.');
  } finally {
    savingAssignment.value = false;
  }
}

async function endAssignment(a: RateAssignment, effectiveTo: string, changeReason: string | null) {
  ending.value = true;
  endErrors.value = null;
  try {
    await ratesApi.endAssignment(a.id, { effectiveTo, changeReason, rowVersion: a.rowVersion });
    assignmentsCard.value?.closeEnd();
    snack.success('Assignment ended.');
    await load();
  } catch (err) {
    endErrors.value = await handleMutationError(err, 'Unable to end the assignment.');
    if (err instanceof ApiError && err.status === 409) assignmentsCard.value?.closeEnd();
  } finally {
    ending.value = false;
  }
}

// ---- resolve ---------------------------------------------------------------------
const resolved = ref<ResolvedRates | null>(null);
const resolving = ref(false);
const resolveError = ref<string | null>(null);

async function resolve(userId: number, date: string) {
  resolving.value = true;
  resolveError.value = null;
  try {
    resolved.value = await ratesApi.resolve(userId, date);
  } catch (err) {
    resolveError.value = describeError(err, 'Unable to resolve the rates.');
  } finally {
    resolving.value = false;
  }
}
</script>

<template>
  <AuthenticatedLayout :crumbs="crumbs" :back="{ name: 'rate-profiles' }">
    <template v-if="profile && canWrite" #actions>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-pencil-outline" @click="openRename">Rename</v-btn>
      <v-btn
        class="press"
        :color="profile.isActive ? 'error' : 'success'"
        variant="tonal"
        :prepend-icon="profile.isActive ? 'mdi-cancel' : 'mdi-check-circle-outline'"
        @click="activeDialog = true"
      >
        {{ profile.isActive ? 'Deactivate' : 'Activate' }}
      </v-btn>
    </template>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-else-if="loadError" type="error" class="mt-2">
      {{ loadError }}
      <template #append>
        <v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn>
        <v-btn class="press" size="small" variant="text" @click="router.push({ name: 'rate-profiles' })">Back to rates</v-btn>
      </template>
    </v-alert>

    <template v-else-if="profile">
      <div class="d-flex align-center ga-2 flex-wrap mb-5">
        <h1 class="page-title mb-0">{{ profile.name }}</h1>
        <span class="pill" :class="profile.isActive ? 'pill--success' : 'pill--neutral'">{{ profile.isActive ? 'Active' : 'Inactive' }}</span>
      </div>

      <v-alert v-if="canWrite" type="info" variant="tonal" density="compact" class="mb-4">
        Use New version for a rate change: it closes the current one the day before. Use Edit only to correct a mistake; filed reports keep their amounts.
        You cannot change rates that apply to you.
      </v-alert>

      <div class="detail" :class="{ 'detail--wide': lgAndUp }">
        <div class="detail__main">
          <RateProfileLinesCard :lines="profile.lines" :can-write="canWrite" :disabled="savingLine" @add="openLine" @edit="openEditLine" />
          <RateAssignmentsCard
            ref="assignmentsCard"
            :assignments="profile.assignments"
            :can-write="canWrite"
            :disabled="savingAssignment"
            :acting="ending"
            :end-errors="endErrors"
            @create="openAssign"
            @edit="openEditAssignment"
            @end="endAssignment"
            @clear-end-errors="endErrors = null"
          />
          <ResolveRatesPanel v-if="!lgAndUp" :people="people" :result="resolved" :loading="resolving" :error="resolveError" :current-profile-id="profile.id" @resolve="resolve" />
        </div>
        <aside v-if="lgAndUp" class="detail__side">
          <ResolveRatesPanel :people="people" :result="resolved" :loading="resolving" :error="resolveError" :current-profile-id="profile.id" @resolve="resolve" />
        </aside>
      </div>

      <template v-if="canWrite">
        <RateLineDialog
          ref="lineDialogRef"
          v-model="lineDialog"
          :from="lineFrom"
          :editing="lineEditing"
          :lines="profile.lines"
          :saving="savingLine"
          :errors="lineErrors"
          @save="addLine"
          @update="updateLine"
        />
        <RateAssignmentDialog
          v-model="assignDialog"
          :profile-id="profile.id"
          :profile-name="profile.name"
          :people="people"
          :departments="departments"
          :existing="existingAssignments"
          :editing="assignEditing"
          :saving="savingAssignment"
          :errors="assignErrors"
          @save="assign"
          @update="updateAssignment"
        />

        <v-dialog v-model="renameOpen" max-width="460" :persistent="savingProfile">
          <v-card>
            <v-card-title class="text-subtitle-1 font-weight-bold">Rename profile</v-card-title>
            <v-card-text>
              <v-form :disabled="savingProfile" @submit.prevent="rename">
                <v-text-field v-model="renameValue" label="Name *" maxlength="100" autofocus :error-messages="fieldErrors(profileErrors, 'name')" />
              </v-form>
            </v-card-text>
            <v-divider />
            <v-card-actions class="px-4 py-3">
              <v-spacer />
              <v-btn class="press" variant="text" :disabled="savingProfile" @click="renameOpen = false">Cancel</v-btn>
              <v-btn class="press" color="primary" variant="flat" :loading="savingProfile" :disabled="!renameValue.trim()" @click="rename">Save</v-btn>
            </v-card-actions>
          </v-card>
        </v-dialog>

        <AppConfirmDialog
          v-model="activeDialog"
          :title="profile.isActive ? 'Deactivate profile' : 'Activate profile'"
          :message="
            profile.isActive
              ? `${profile.name} can no longer be assigned. A profile that is still assigned cannot be deactivated; end its assignments first.`
              : `${profile.name} can be assigned again.`
          "
          :confirm-text="profile.isActive ? 'Deactivate' : 'Activate'"
          :confirm-color="profile.isActive ? 'error' : 'success'"
          :loading="savingProfile"
          @confirm="toggleActive"
        />
      </template>
    </template>

    <template v-if="xs && profile && canWrite" #bottom>
      <v-btn class="press" variant="outlined" prepend-icon="mdi-pencil-outline" @click="openRename">Rename</v-btn>
      <v-btn class="press" :color="profile.isActive ? 'error' : 'success'" variant="tonal" @click="activeDialog = true">
        {{ profile.isActive ? 'Deactivate' : 'Activate' }}
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
  grid-template-columns: minmax(0, 1fr) 340px;
  align-items: start;
}
.detail__main,
.detail__side {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
</style>
