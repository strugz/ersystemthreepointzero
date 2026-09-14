<script setup lang="ts">
import { reportStatusLabel } from '../labels';
import type { ReportStatus } from '../types';

defineProps<{ status: ReportStatus }>();

const tone: Record<ReportStatus, string> = {
  New: 'pill--neutral',
  ForApproval: 'pill--warning',
  Returned: 'pill--error',
  Approved: 'pill--success',
};
</script>

<template>
  <span class="pill" :class="tone[status]">{{ reportStatusLabel[status] }}</span>
</template>
