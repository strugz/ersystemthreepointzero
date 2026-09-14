<script setup lang="ts">
defineProps<{ icon: string; title: string; hint?: string }>();
</script>

<template>
  <div class="d-flex flex-column align-center text-center py-10 px-4">
    <v-icon :icon="icon" size="40" class="mb-3 text-medium-emphasis" />
    <div class="text-subtitle-1 font-weight-medium">{{ title }}</div>
    <div v-if="hint" class="text-body-2 text-medium-emphasis mt-1" style="max-width: 420px">{{ hint }}</div>
    <div v-if="$slots.default" class="mt-4"><slot /></div>
  </div>
</template>
