<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import { ApiError } from '@/shared/api/client';

const session = useSessionStore();
const router = useRouter();
const route = useRoute();

const username = ref('');
const password = ref('');
const loading = ref(false);
const error = ref<string | null>(null);

async function submit() {
  error.value = null;
  loading.value = true;
  try {
    await session.login({ username: username.value, password: password.value });
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
    await router.push(redirect);
  } catch (err) {
    error.value =
      err instanceof ApiError && err.status === 401
        ? 'Invalid username or password.'
        : err instanceof ApiError && err.status >= 500
          ? 'The server is not responding. Please try again in a moment.'
          : 'Unable to sign in. Please try again.';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <v-main class="login">
    <div class="login__panel page-enter">
      <img
        src="/erms-wordmark.png"
        srcset="/erms-wordmark.png 1x, /erms-wordmark@2x.png 2x"
        alt="ERMS — Expense Report Management System"
        class="login__mark"
      />
      <h1 class="login__title">Sign in</h1>
      <p class="login__sub">Expense Report Forms · MDMPI</p>

      <v-form class="login__form" @submit.prevent="submit">
        <v-alert v-if="session.unavailable && !error" type="warning" density="compact" class="mb-2">
          The server is not responding right now. You can still try to sign in.
        </v-alert>
        <v-alert v-if="error" type="error" density="compact" class="mb-2">{{ error }}</v-alert>
        <v-text-field v-model="username" label="Username" autocomplete="username" autofocus :disabled="loading" hide-details="auto" />
        <v-text-field v-model="password" label="Password" type="password" autocomplete="current-password" :disabled="loading" hide-details="auto" />
        <v-btn type="submit" class="press mt-2" color="primary" variant="flat" block size="large" :loading="loading">Sign in</v-btn>
      </v-form>
    </div>
  </v-main>
</template>

<style scoped>
.login {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--paper);
  padding: 24px;
}
.login__panel {
  width: 100%;
  max-width: 400px;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 16px;
  padding: 32px 28px 28px;
}
.login__mark {
  display: block;
  width: 100%;
  max-width: 280px;
  height: auto;
  margin: 0 auto 22px;
}
.login__title {
  margin: 0;
  text-align: center;
  font-size: 22px;
  font-weight: 600;
  letter-spacing: -0.01em;
}
.login__sub {
  margin: 4px 0 24px;
  text-align: center;
  font-size: 13px;
  color: var(--ink-3);
}
.login__form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
@media (max-width: 599.98px) {
  .login {
    align-items: flex-start;
    padding-top: 12vh;
  }
  .login__panel {
    border: 0;
    background: transparent;
    padding: 0;
  }
}
</style>
