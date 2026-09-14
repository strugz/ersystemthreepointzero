import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { authApi } from '@/features/auth/api';
import { ApiError } from '@/shared/api/client';
import type { AuthenticatedUser, LoginRequest, UserRole } from '@/features/auth/types';

export const useSessionStore = defineStore('session', () => {
  const user = ref<AuthenticatedUser | null>(null);
  const initialized = ref(false);
  /** Set when the session check itself failed (API down, 500); the login page shows it. */
  const unavailable = ref(false);

  const isAuthenticated = computed(() => user.value !== null);

  function hasRole(role: UserRole): boolean {
    return user.value?.roles.includes(role) ?? false;
  }

  /**
   * Loads the current user once. 401 means "not signed in". Any other failure (server down,
   * 500 while it restarts) also resolves to signed-out so the app still renders the login page
   * instead of a blank screen; the flag lets that page explain what happened.
   */
  async function initialize(): Promise<void> {
    if (initialized.value) {
      return;
    }
    try {
      user.value = await authApi.me();
      unavailable.value = false;
    } catch (error) {
      user.value = null;
      unavailable.value = !(error instanceof ApiError && error.status === 401);
    } finally {
      initialized.value = true;
    }
  }

  async function login(request: LoginRequest): Promise<void> {
    user.value = await authApi.login(request);
    initialized.value = true;
    unavailable.value = false;
  }

  async function logout(): Promise<void> {
    await authApi.logout();
    user.value = null;
  }

  return { user, initialized, unavailable, isAuthenticated, hasRole, initialize, login, logout };
});
