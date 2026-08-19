<script setup lang="ts">
import AppDateTime from '@/shared/components/AppDateTime.vue'
import AppStatusChip from '@/shared/components/AppStatusChip.vue'
import type { ApprovalTrailItem } from '@/shared/types/reportReview'

defineProps<{ items: ApprovalTrailItem[] }>()
</script>

<template>
  <v-card
    v-if="items.length"
    title="Approval trail"
    class="border"
    variant="flat"
  >
    <v-list lines="two">
      <v-list-item
        v-for="step in items"
        :key="`${step.sort}-${step.approverUserId}`"
        :title="`${step.sort}. ${step.approverName}`"
      >
        <template #subtitle>
          <AppStatusChip :status="step.status" />
          <AppDateTime
            v-if="step.occurredAtUtc"
            :value="step.occurredAtUtc"
          />
        </template>
      </v-list-item>
    </v-list>
  </v-card>
</template>
