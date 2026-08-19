<script setup lang="ts">
defineProps<{
  to: string
  label: string
  accessibleLabel: string
}>()
</script>

<template>
  <nav
    class="report-detail-mobile-nav"
    :aria-label="label"
  >
    <v-btn
      icon="mdi-arrow-left"
      variant="text"
      :to="to"
      :aria-label="accessibleLabel"
    />
    <span>{{ label }}</span>
  </nav>
</template>
