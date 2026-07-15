<script setup lang="ts">defineProps<{ items: Array<{ title: string; to?: string }> }>()</script>
<template>
  <v-breadcrumbs
    :items="items"
    density="compact"
    class="px-0 mb-2"
  />
</template>
