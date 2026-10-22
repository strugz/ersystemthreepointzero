<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import { useThemeStore } from '@/app/stores/theme';
import { ApiError } from '@/shared/api/client';
import { describeError } from '@/shared/composables/useSnackbar';
import { appVersionLabel } from '@/shared/appVersion';
import AppErmsMark from '@/shared/ui/AppErmsMark.vue';

const session = useSessionStore();
const theme = useThemeStore();
const router = useRouter();
const route = useRoute();

const username = ref('');
const password = ref('');
const showPassword = ref(false);
const loading = ref(false);
const error = ref<string | null>(null);
// The company crest's frame is navy; its light variant (white frame, same gold) is for navy and dark surfaces.
const crestSrc = computed(() => (theme.isDark ? '/mdmpi-crest-light.png' : '/mdmpi-crest.png'));

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
      <img
        src="/mdmpi-crest-light.png"
        srcset="/mdmpi-crest-light.png 1x, /mdmpi-crest-light@2x.png 2x"
        alt=""
        class="login__crest"
      />
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
        <div class="login__lockup">
          <AppErmsMark :size="52" :inverse="theme.isDark" />
          <div class="login__wordmark">
            <span class="login__erms">ERMS</span>
            <span class="login__tagline">Expense Report Management System</span>
          </div>
        </div>
        <h1 class="login__title">Sign in</h1>
        <p class="login__sub">Use your ER System username and password.</p>

        <v-form class="login__form" @submit.prevent="submit">
          <v-alert v-if="session.unavailable && !error" type="warning" density="compact" class="mb-2">
            The server is not responding right now. You can still try to sign in.
          </v-alert>
          <v-alert v-if="error" type="error" density="compact" class="mb-2">{{ error }}</v-alert>
          <v-text-field v-model="username" label="Username" prepend-inner-icon="mdi-account-outline" autocomplete="username" autofocus :disabled="loading" hide-details="auto" />
          <v-text-field
            v-model="password"
            label="Password"
            prepend-inner-icon="mdi-lock-outline"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="current-password"
            :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            :disabled="loading"
            hide-details="auto"
            @click:append-inner="showPassword = !showPassword"
          />
          <v-btn type="submit" class="press mt-2" color="primary" variant="flat" block size="large" :loading="loading">Sign in</v-btn>
        </v-form>
      </div>

      <div class="login__footer">
        <!-- Phones and narrow windows hide the brand panel, so the company crest moves down here. -->
        <img :src="crestSrc" :srcset="`${crestSrc} 1x, ${crestSrc.replace('.png', '@2x.png')} 2x`" alt="MDMPI" class="login__footer-crest" />
        <p class="login__version">{{ appVersionLabel }}</p>
      </div>
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
  flex-direction: column;
  justify-content: space-between;
  align-items: flex-start;
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
.login__crest {
  position: relative;
  display: block;
  height: 56px;
  width: auto;
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
.login__footer {
  position: absolute;
  bottom: 16px;
  left: 0;
  right: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
}
.login__footer-crest {
  display: none;
  height: 22px;
  width: auto;
}
.login__version {
  margin: 0;
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
.login__lockup {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 14px;
  margin: 4px 0 26px;
}
.login__wordmark {
  display: flex;
  flex-direction: column;
  gap: 5px;
}
/* Manrope is loaded at weight 800 only, for the wordmark. */
.login__erms {
  font-family: Manrope, var(--font-ui), sans-serif;
  font-size: 40px;
  font-weight: 800;
  letter-spacing: -0.045em;
  line-height: 0.9;
  color: var(--ink);
}
.login__tagline {
  font-size: 8.5px;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  white-space: nowrap;
  color: var(--ink-2);
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
  .login__footer-crest {
    display: block;
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
