<script setup lang="ts">import { useSnackbar } from '@/shared/composables/useSnackbar'; const { state } = useSnackbar()</script>
<template>
  <v-snackbar
    v-model="state.open"
    :color="state.color"
    location="top right"
    timeout="4500"
  >
    {{ state.message }}<template #actions>
      <v-btn
        icon="mdi-close"
        aria-label="Close notification"
        @click="state.open = false"
      />
    </template>
  </v-snackbar>
</template>
