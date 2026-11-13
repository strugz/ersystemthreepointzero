<script setup lang="ts">
import { useSessionStore } from '@/app/stores/session';
import { useThemeStore } from '@/app/stores/theme';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import { themeOptions, useAppNavigation } from '@/layouts/useAppNavigation';

// The phone "More" tab: the account, every section that is not in the bottom bar, appearance and sign-out.
// Tablet and desktop reach the same entries from the rail and the avatar menu.
const session = useSessionStore();
const theme = useThemeStore();
const { navGroups, roleSummary, initials, logout } = useAppNavigation();
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'More' }]" narrow>
    <section class="profile">
      <span class="profile__avatar" aria-hidden="true">{{ initials }}</span>
      <div class="profile__text">
        <div class="profile__name">{{ session.user?.fullName }}</div>
        <div class="profile__roles">{{ roleSummary }}</div>
      </div>
    </section>

    <section v-for="group in navGroups" :key="group.label" class="group">
      <h2 class="group__label">{{ group.label }}</h2>
      <div class="group__card">
        <router-link v-for="item in group.items" :key="item.title" :to="item.to" class="row press">
          <v-icon :icon="item.icon" size="20" class="row__icon" />
          <span class="row__title">{{ item.title }}</span>
          <v-icon icon="mdi-chevron-right" size="18" class="row__chev" />
        </router-link>
      </div>
    </section>

    <section class="group">
      <h2 class="group__label">Appearance</h2>
      <div class="seg" role="radiogroup" aria-label="Appearance">
        <button
          v-for="o in themeOptions"
          :key="o.value"
          type="button"
          role="radio"
          class="seg__opt press"
          :class="{ 'seg__opt--on': theme.preference === o.value }"
          :aria-checked="theme.preference === o.value"
          @click="theme.setPreference(o.value)"
        >
          <v-icon :icon="o.icon" size="18" />
          <span>{{ o.label }}</span>
        </button>
      </div>
    </section>

    <section class="group">
      <h2 class="group__label">Account</h2>
      <div class="group__card">
        <router-link :to="{ name: 'change-password' }" class="row press">
          <v-icon icon="mdi-lock-reset" size="20" class="row__icon" />
          <span class="row__title">Change password</span>
          <v-icon icon="mdi-chevron-right" size="18" class="row__chev" />
        </router-link>
      </div>
    </section>

    <v-btn class="press signout" block size="large" variant="outlined" color="error" prepend-icon="mdi-logout" @click="logout">Sign out</v-btn>
  </AuthenticatedLayout>
</template>

<style scoped>
.profile {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px;
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: var(--card);
  box-shadow: var(--shadow-1);
}
.profile__avatar {
  flex: none;
  display: grid;
  place-items: center;
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: var(--navy-soft);
  color: var(--navy);
  font-weight: 600;
  font-size: 17px;
}
.profile__text {
  min-width: 0;
}
.profile__name {
  font-size: 16px;
  font-weight: 600;
}
.profile__roles {
  font-size: 12px;
  color: var(--ink-3);
}
.group {
  margin-top: 20px;
}
.group__label {
  margin: 0 4px 8px;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--ink-3);
}
.group__card {
  overflow: hidden;
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: var(--card);
  box-shadow: var(--shadow-1);
}
.row {
  display: flex;
  align-items: center;
  gap: 14px;
  min-height: 52px;
  padding: 0 14px 0 16px;
  border-bottom: 1px solid var(--line);
  color: var(--ink);
  text-decoration: none;
}
.row:last-child {
  border-bottom: 0;
}
.row:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
@media (hover: hover) and (pointer: fine) {
  .row {
    transition: background-color 120ms ease;
  }
  .row:hover {
    background: var(--navy-soft);
  }
}
.row__icon {
  color: var(--ink-2);
}
.row__title {
  flex: 1;
  font-size: 15px;
}
.row__chev {
  color: var(--ink-3);
}
.seg {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 3px;
  padding: 4px;
  border-radius: var(--radius);
  background: var(--line);
}
.seg__opt {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 40px;
  border: 0;
  border-radius: 9px;
  background: none;
  font: inherit;
  font-size: 14px;
  color: var(--ink-2);
  cursor: pointer;
  transition: background-color 120ms ease, color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.seg__opt--on {
  background: var(--card);
  color: var(--ink);
  font-weight: 500;
  box-shadow: var(--shadow-1);
}
.seg__opt:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 1px;
}
.signout {
  margin-top: 28px;
}
</style>
