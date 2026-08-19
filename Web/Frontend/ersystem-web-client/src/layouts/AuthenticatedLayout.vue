<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import { resolveNavigationDestination } from '@/app/router/navigation'
import { usePermissions } from '@/shared/composables/usePermissions'

const drawer = ref(true)
const route = useRoute()
const { mdAndUp } = useDisplay()
const { isManager, isFinance } = usePermissions()
const activeDestination = computed(() => resolveNavigationDestination(route.path))
</script>

<template>
  <v-navigation-drawer
    v-if="mdAndUp"
    v-model="drawer"
    class="authenticated-drawer"
  >
    <div class="pa-5">
      <div class="text-overline text-primary">
        ER System
      </div>
      <div class="text-h6 font-weight-bold">
        Workflow Portal
      </div>
    </div>
    <v-divider />
    <v-list
      nav
      density="comfortable"
    >
      <v-list-item
        v-if="isManager"
        prepend-icon="mdi-file-check-outline"
        title="Manager approvals"
        to="/manager/reports"
      />
      <v-list-item
        v-if="isFinance"
        prepend-icon="mdi-receipt-text-check-outline"
        title="Finance receipts"
        to="/finance/receipts"
      />
      <v-list-item
        prepend-icon="mdi-account-circle-outline"
        title="Account"
        to="/account"
      />
    </v-list>
  </v-navigation-drawer>
  <v-app-bar
    class="authenticated-app-bar"
    color="primary"
    flat
    border
  >
    <v-app-bar-nav-icon
      v-if="mdAndUp"
      aria-label="Toggle navigation"
      @click="drawer = !drawer"
    />
    <div
      class="app-brand-icon-tile"
      aria-hidden="true"
    >
      <img
        src="/er-system-icon.png"
        alt=""
      >
    </div>
    <v-app-bar-title class="app-brand-title">
      Expense Report System
    </v-app-bar-title>
  </v-app-bar>
  <v-main :class="{ 'mobile-authenticated-main': !mdAndUp }">
    <router-view />
  </v-main>
  <v-bottom-navigation
    v-if="!mdAndUp"
    :model-value="activeDestination"
    class="mobile-bottom-navigation"
    grow
    mandatory
    color="white"
    height="72"
  >
    <v-btn
      v-if="isManager"
      value="manager"
      to="/manager/reports"
    >
      <v-icon>mdi-file-check-outline</v-icon>
      <span>Approvals</span>
    </v-btn>
    <v-btn
      v-if="isFinance"
      value="finance"
      to="/finance/receipts"
    >
      <v-icon>mdi-receipt-text-check-outline</v-icon>
      <span>Receipts</span>
    </v-btn>
    <v-btn
      value="account"
      to="/account"
    >
      <v-icon>mdi-account-circle-outline</v-icon>
      <span>Account</span>
    </v-btn>
  </v-bottom-navigation>
</template>
