<script setup lang="ts">import LoginForm from '@/features/auth/LoginForm.vue'</script>
<template>
  <v-main class="d-flex align-center justify-center bg-background">
    <v-container style="max-width:440px">
      <v-card class="pa-8">
        <div class="text-overline text-primary">
          ER System 3.0
        </div><h1 class="text-h4 font-weight-bold mb-2">
          Welcome back
        </h1><p class="muted mb-7">
          Use your existing ER System credentials.
        </p><LoginForm />
      </v-card>
    </v-container>
  </v-main>
</template>
