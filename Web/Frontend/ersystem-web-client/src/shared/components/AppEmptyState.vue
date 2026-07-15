<script setup lang="ts">defineProps<{ title?: string; message?: string }>()</script>
<template>
  <div class="text-center pa-10">
    <v-icon
      size="48"
      color="medium-emphasis"
    >
      mdi-inbox-outline
    </v-icon><h3 class="text-h6 mt-3">
      {{ title ?? 'No records found' }}
    </h3><p class="muted mt-1">
      {{ message ?? 'Try changing the filters or refresh the page.' }}
    </p>
  </div>
</template>
