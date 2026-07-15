<script setup lang="ts">defineProps<{ loading: boolean }>()</script>
<template>
  <v-overlay
    :model-value="loading"
    contained
    persistent
    class="align-center justify-center"
  >
    <v-progress-circular
      indeterminate
      color="primary"
    />
  </v-overlay>
</template>
