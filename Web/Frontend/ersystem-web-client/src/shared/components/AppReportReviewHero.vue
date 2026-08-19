<script setup lang="ts">
import { hasDisplayText } from '@/shared/utils/reportReview'

defineProps<{
  eyebrow: string
  employeeName: string
  department?: string
  reference?: string
}>()
</script>

<template>
  <section class="report-review-hero">
    <div class="d-flex align-center justify-space-between ga-3 mb-3">
      <span class="report-review-hero__eyebrow">{{ eyebrow }}</span>
      <div class="d-flex flex-wrap justify-end ga-2">
        <slot name="status" />
      </div>
    </div>
    <h1
      v-if="hasDisplayText(employeeName)"
      class="report-review-hero__title"
    >
      {{ employeeName }}
    </h1>
    <p
      v-if="hasDisplayText(department)"
      class="report-review-hero__department"
    >
      {{ department }}
    </p>
    <div
      v-if="hasDisplayText(reference)"
      class="report-review-hero__reference"
    >
      <span class="field-label">ERF reference</span>
      <span class="report-review-hero__reference-value">{{ reference }}</span>
    </div>
  </section>
</template>
