<script setup lang="ts">
defineProps<{ title: string; subtitle?: string }>()
</script>
<template>
  <div class="d-flex flex-wrap justify-space-between align-center ga-4 mb-6">
    <div>
      <h1 class="text-h4 font-weight-bold">
        {{ title }}
      </h1><p
        v-if="subtitle"
        class="muted mt-1"
      >
        {{ subtitle }}
      </p>
    </div><div><slot /></div>
  </div>
</template>
