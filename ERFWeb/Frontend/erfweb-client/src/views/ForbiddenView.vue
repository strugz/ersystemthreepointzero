<script setup lang="ts"></script>

<template>
  <v-main class="d-flex align-center justify-center" style="min-height: 100vh; background: var(--paper)">
    <div class="text-center px-6">
      <v-icon size="48" color="warning" icon="mdi-lock-alert-outline" class="mb-3" />
      <h1 class="page-title">Access denied</h1>
      <p class="text-body-2 text-medium-emphasis mt-2">You do not have permission to view this page.</p>
      <v-btn class="press mt-4" color="primary" variant="flat" to="/">Go to dashboard</v-btn>
    </div>
  </v-main>
</template>
