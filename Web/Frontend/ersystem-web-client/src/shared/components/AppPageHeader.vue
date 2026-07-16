<script setup lang="ts">
defineProps<{ title: string; subtitle?: string }>()
</script>

<template>
  <header class="app-page-header d-flex flex-wrap justify-space-between align-center ga-4 mb-6">
    <div class="app-page-header__copy">
      <h1 class="app-page-header__title text-h4 font-weight-bold">
        {{ title }}
      </h1>
      <p
        v-if="subtitle"
        class="app-page-header__subtitle muted mt-1"
      >
        {{ subtitle }}
      </p>
    </div>
    <div
      v-if="$slots.default"
      class="app-page-header__actions"
    >
      <slot />
    </div>
  </header>
</template>
