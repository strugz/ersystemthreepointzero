<script setup lang="ts">defineProps<{ message?: string }>()</script>
<template>
  <v-alert
    v-if="message"
    type="error"
    variant="tonal"
    class="mb-4"
    closable
  >
    {{ message }}
  </v-alert>
</template>
