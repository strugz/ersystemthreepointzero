<script setup lang="ts">
import { useSessionStore } from '@/app/stores/session';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import HealthyLivingContent from '@/features/wellness/components/HealthyLivingContent.vue';
import { appVersionLabel } from '@/shared/appVersion';

// Public page: signed-in users see it inside the app shell, everyone else in a plain page with a sign-in link.
const session = useSessionStore();
</script>

<template>
  <AuthenticatedLayout v-if="session.isAuthenticated" :crumbs="[{ title: 'Healthy living' }]">
    <HealthyLivingContent />
  </AuthenticatedLayout>

  <v-main v-else class="public">
    <header class="public__bar">
      <span class="public__brand">ERMS · Healthy living</span>
      <v-btn class="press" color="primary" variant="flat" prepend-icon="mdi-login" :to="{ name: 'login' }">Sign in</v-btn>
    </header>
    <div class="public__body">
      <HealthyLivingContent />
      <p class="public__version">{{ appVersionLabel }}</p>
    </div>
  </v-main>
</template>

<style scoped>
.public {
  min-height: 100vh;
  background: var(--paper);
}
.public__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  max-width: 1100px;
  margin: 0 auto;
  padding: 16px;
}
.public__brand {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: -0.01em;
}
.public__body {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0 16px 24px;
}
.public__version {
  margin: 24px 0 0;
  font-size: 11px;
  text-align: center;
  color: var(--ink-3);
}
</style>
