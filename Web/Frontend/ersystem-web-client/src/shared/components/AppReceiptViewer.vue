<script setup lang="ts">defineProps<{ url: string; contentType: string; title: string }>()</script>
<template>
  <div class="border rounded-lg overflow-hidden">
    <img
      v-if="contentType.startsWith('image/')"
      :src="url"
      :alt="title"
      style="display:block;max-width:100%;margin:auto"
    ><iframe
      v-else-if="contentType === 'application/pdf'"
      :src="url"
      :title="title"
      style="width:100%;height:70vh;border:0"
    /><v-alert
      v-else
      type="warning"
    >
      This receipt type cannot be previewed.
    </v-alert>
  </div>
</template>
