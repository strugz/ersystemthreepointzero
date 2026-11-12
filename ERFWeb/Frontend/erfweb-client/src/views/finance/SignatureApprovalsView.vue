<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';
import AppConfirmDialog from '@/shared/ui/AppConfirmDialog.vue';
import { formatDateTime } from '@/shared/format';
import { describeError, useSnackbar } from '@/shared/composables/useSnackbar';
import { ApiError } from '@/shared/api/client';
import { useSessionStore } from '@/app/stores/session';
import { signatureReviewApi } from '@/features/finance/api';
import type { SignatureReviewItem } from '@/features/finance/types';

// Signature approvals: the R47 prints a person's signature image (with the ELECTRONIC SIGNATURE
// watermark) only after Finance approves it here. The API refuses your own signature (403) and a
// signature that was replaced or already decided since the list loaded (409).
const session = useSessionStore();
const snack = useSnackbar();
const { compact } = useLayoutMode();

const tab = ref<'Pending' | 'Approved'>('Pending');
const loading = ref(false);
const error = ref<string | null>(null);
const items = ref<SignatureReviewItem[]>([]);
const busyId = ref<number | null>(null);
const failedImages = ref(new Set<number>());

const rejecting = ref<SignatureReviewItem | null>(null);
const rejectNote = ref('');

async function load() {
  loading.value = true;
  error.value = null;
  failedImages.value = new Set();
  try {
    items.value = await signatureReviewApi.list(tab.value);
  } catch (err) {
    items.value = [];
    error.value = describeError(err, 'Unable to load the signatures.');
  } finally {
    loading.value = false;
  }
}

function isMine(item: SignatureReviewItem) {
  return item.userId === session.user?.userId;
}

async function handleReviewError(err: unknown, fallback: string) {
  snack.fromError(err, fallback);
  // A replaced or already decided signature: show the list as it is now.
  if (err instanceof ApiError && err.status === 409) await load();
}

async function approve(item: SignatureReviewItem) {
  if (!item.uploadedAt) return;
  busyId.value = item.userId;
  try {
    await signatureReviewApi.approve(item.userId, item.uploadedAt);
    items.value = items.value.filter((i) => i.userId !== item.userId);
    snack.success(`Approved ${item.fullName}'s signature. The R47 prints it from now on.`);
  } catch (err) {
    await handleReviewError(err, 'Unable to approve the signature.');
  } finally {
    busyId.value = null;
  }
}

function askReject(item: SignatureReviewItem) {
  rejectNote.value = '';
  rejecting.value = item;
}

async function confirmReject() {
  const item = rejecting.value;
  if (!item?.uploadedAt) return;
  busyId.value = item.userId;
  try {
    await signatureReviewApi.reject(item.userId, item.uploadedAt, rejectNote.value.trim() || null);
    items.value = items.value.filter((i) => i.userId !== item.userId);
    rejecting.value = null;
    snack.success(`Rejected ${item.fullName}'s signature. The image was removed.`);
  } catch (err) {
    rejecting.value = null;
    await handleReviewError(err, 'Unable to reject the signature.');
  } finally {
    busyId.value = null;
  }
}

function imageFailed(userId: number) {
  failedImages.value = new Set(failedImages.value).add(userId);
}

onMounted(load);
watch(tab, () => void load());
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Signatures' }]">
    <div v-if="!compact" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Signatures</h1>
      <span class="text-body-2 text-medium-emphasis">
        A signature image prints on the R47, with an ELECTRONIC SIGNATURE watermark, only after Finance approves it.
      </span>
    </div>

    <v-card>
      <v-tabs v-model="tab" color="primary" density="comfortable">
        <v-tab value="Pending">Waiting for approval</v-tab>
        <v-tab value="Approved">Approved</v-tab>
      </v-tabs>
      <v-divider />

      <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

      <v-alert v-if="error && !loading" type="error" class="ma-4">
        {{ error }}
        <template #append><v-btn class="press" size="small" variant="text" @click="load">Retry</v-btn></template>
      </v-alert>

      <AppEmptyState
        v-else-if="!loading && items.length === 0"
        icon="mdi-draw-pen"
        :title="tab === 'Pending' ? 'No signatures waiting' : 'No approved signatures yet'"
        :hint="tab === 'Pending' ? 'New signatures the System Administrator loads in Users appear here for approval.' : undefined"
      />

      <div v-else class="sig-list">
        <div v-for="item in items" :key="item.userId" class="sig-item">
          <div class="sig-item__frame">
            <img
              v-if="!failedImages.has(item.userId)"
              :src="signatureReviewApi.imageUrl(item.userId, item.uploadedAt)"
              :alt="`Signature of ${item.fullName}`"
              class="sig-item__img"
              @error="imageFailed(item.userId)"
            />
            <span v-else class="text-caption text-medium-emphasis">Unable to load the signature.</span>
          </div>
          <div class="sig-item__body">
            <div class="font-weight-medium">{{ item.fullName }}</div>
            <div class="text-caption text-medium-emphasis">{{ item.departmentName ?? 'No department' }}</div>
            <div v-if="item.status === 'Approved'" class="text-caption">
              Approved{{ item.approvedByName ? ` by ${item.approvedByName}` : '' }} on {{ formatDateTime(item.approvedAt) }}
            </div>
            <div v-else class="text-caption text-medium-emphasis">Uploaded {{ formatDateTime(item.uploadedAt) }}</div>
          </div>
          <div v-if="item.status === 'Pending'" class="sig-item__actions">
            <span v-if="isMine(item)" class="text-caption text-medium-emphasis">Your own: another Finance user approves it</span>
            <template v-else>
              <v-btn
                class="press"
                color="success"
                variant="flat"
                size="small"
                prepend-icon="mdi-check"
                :loading="busyId === item.userId && !rejecting"
                :disabled="busyId !== null"
                @click="approve(item)"
              >
                Approve
              </v-btn>
              <v-btn class="press" color="error" variant="text" size="small" :disabled="busyId !== null" @click="askReject(item)">Reject</v-btn>
            </template>
          </div>
        </div>
      </div>
    </v-card>

    <AppConfirmDialog
      :model-value="rejecting !== null"
      title="Reject this signature?"
      :message="rejecting ? `${rejecting.fullName}'s signature image will be removed. The R47 prints their electronic status instead until a new image is loaded and approved.` : ''"
      confirm-text="Reject and remove"
      confirm-color="error"
      :loading="busyId !== null"
      @update:model-value="(v: boolean) => !v && (rejecting = null)"
      @confirm="confirmReject"
    >
      <v-textarea v-model="rejectNote" label="Reason (optional)" rows="2" auto-grow counter="500" maxlength="500" />
    </AppConfirmDialog>
  </AuthenticatedLayout>
</template>

<style scoped>
.sig-list {
  display: flex;
  flex-direction: column;
}
.sig-item {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--line);
}
.sig-item:last-child {
  border-bottom: 0;
}
.sig-item__frame {
  width: 180px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 6px;
  border: 1px solid var(--line);
  border-radius: 8px;
  /* White like the printed form, so the preview matches how the R47 shows it. */
  background: #fff;
}
.sig-item__img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}
.sig-item__body {
  flex: 1 1 180px;
  min-width: 0;
}
.sig-item__actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
