<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import { useThemeStore } from '@/app/stores/theme';
import { ApiError } from '@/shared/api/client';
import { describeError } from '@/shared/composables/useSnackbar';
import { appVersionLabel } from '@/shared/appVersion';

const session = useSessionStore();
const theme = useThemeStore();
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
    // A lockout (423) or too many attempts (429) carries the server's own wording, e.g. how long to wait.
    error.value =
      err instanceof ApiError && err.status === 401
        ? 'Invalid username or password.'
        : err instanceof ApiError && (err.status === 423 || err.status === 429)
          ? describeError(err)
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
    <!-- Brand panel: desktop only; phones go straight to the form. -->
    <aside class="login__brand" aria-hidden="true">
      <div class="login__brand-inner">
        <span class="login__eyebrow">MDMPI · Expense Report</span>
        <p class="login__pitch">File, approve and settle expense reports without chasing paper.</p>
        <ul class="login__points">
          <li><v-icon icon="mdi-file-document-edit-outline" size="18" /> Prepare and file reports online</li>
          <li><v-icon icon="mdi-check-decagram-outline" size="18" /> Route approvals to your signatories</li>
          <li><v-icon icon="mdi-printer-outline" size="18" /> Print the R47 when it is final</li>
        </ul>
      </div>
    </aside>

    <section class="login__side">
      <button
        type="button"
        class="login__theme press"
        :aria-label="theme.isDark ? 'Switch to light mode' : 'Switch to dark mode'"
        @click="theme.toggle()"
      >
        <v-icon :icon="theme.isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night'" size="20" />
      </button>

      <div class="login__panel page-enter">
        <div class="login__mark-plate">
          <img
            src="/erms-wordmark.png"
            srcset="/erms-wordmark.png 1x, /erms-wordmark@2x.png 2x"
            alt="ERMS — Expense Report Management System"
            class="login__mark"
          />
        </div>
        <h1 class="login__title">Sign in</h1>
        <p class="login__sub">Use your ER System username and password.</p>

        <v-form class="login__form" @submit.prevent="submit">
          <v-alert v-if="session.unavailable && !error" type="warning" density="compact" class="mb-2">
            The server is not responding right now. You can still try to sign in.
          </v-alert>
          <v-alert v-if="error" type="error" density="compact" class="mb-2">{{ error }}</v-alert>
          <v-text-field v-model="username" label="Username" prepend-inner-icon="mdi-account-outline" autocomplete="username" autofocus :disabled="loading" hide-details="auto" />
          <v-text-field v-model="password" label="Password" prepend-inner-icon="mdi-lock-outline" type="password" autocomplete="current-password" :disabled="loading" hide-details="auto" />
          <v-btn type="submit" class="press mt-2" color="primary" variant="flat" block size="large" :loading="loading">Sign in</v-btn>
        </v-form>
      </div>

      <p class="login__version">{{ appVersionLabel }}</p>
    </section>
  </v-main>
</template>

<style scoped>
.login {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(0, 5fr) minmax(0, 6fr);
}

.login__brand {
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: flex-end;
  padding: 48px;
  color: var(--on-brand);
  background:
    radial-gradient(640px 360px at 0% 100%, rgba(226, 183, 101, 0.2), transparent 70%),
    linear-gradient(160deg, var(--brand-2), var(--brand-1));
}
.login__brand::before {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(0deg, transparent 0 31px, rgba(255, 255, 255, 0.045) 31px 32px);
  pointer-events: none;
}
.login__brand-inner {
  position: relative;
  max-width: 420px;
}
.login__eyebrow {
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--brand-accent);
}
.login__pitch {
  margin: 12px 0 24px;
  font-size: 30px;
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.2;
}
.login__points {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  font-size: 14px;
  color: var(--on-brand-2);
}
.login__points li {
  display: flex;
  align-items: center;
  gap: 10px;
}
.login__points :deep(.v-icon) {
  color: var(--brand-accent);
}

.login__side {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.login__version {
  position: absolute;
  bottom: 16px;
  left: 0;
  right: 0;
  margin: 0;
  text-align: center;
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), 0.55);
  font-variant-numeric: tabular-nums;
}
.login__theme {
  position: absolute;
  top: 20px;
  right: 20px;
  width: 38px;
  height: 38px;
  border-radius: 10px;
  border: 1px solid var(--line);
  background: var(--card);
  color: var(--ink-2);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}
.login__theme:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
.login__panel {
  width: 100%;
  max-width: 400px;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 16px;
  padding: 32px 28px 28px;
  box-shadow: var(--shadow-2);
}
/* The wordmark is navy ink on white, so it sits on a white plate that disappears in light mode. */
.login__mark-plate {
  background: #ffffff;
  border-radius: 12px;
  padding: 10px 14px;
  margin: 0 auto 22px;
  max-width: 300px;
}
.login__mark {
  display: block;
  width: 100%;
  height: auto;
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

@media (max-width: 959.98px) {
  .login {
    grid-template-columns: 1fr;
  }
  .login__brand {
    display: none;
  }
}
@media (max-width: 599.98px) {
  .login__side {
    align-items: flex-start;
    padding: 12vh 16px 24px;
  }
  .login__panel {
    border: 0;
    background: transparent;
    box-shadow: none;
    padding: 0;
  }
}
</style>
