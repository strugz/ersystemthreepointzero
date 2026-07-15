<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/app/stores/session'
import { usePermissions } from '@/shared/composables/usePermissions'
const drawer = ref(true); const session = useSessionStore(); const router = useRouter(); const { isManager, isFinance } = usePermissions()
async function logout() { await session.logout(); await router.replace('/login') }
</script>
<template>
  <v-navigation-drawer v-model="drawer">
    <div class="pa-5">
      <div class="text-overline text-primary">
        ER System
      </div><div class="text-h6 font-weight-bold">
        Workflow Portal
      </div>
    </div><v-divider /><v-list
      nav
      density="comfortable"
    >
      <v-list-item
        v-if="isManager"
        prepend-icon="mdi-file-check-outline"
        title="Manager approvals"
        to="/manager/reports"
      /><v-list-item
        v-if="isFinance"
        prepend-icon="mdi-receipt-text-check-outline"
        title="Finance receipts"
        to="/finance/receipts"
      />
    </v-list><template #append>
      <div class="pa-4">
        <div class="text-body-2 font-weight-medium">
          {{ session.user?.fullName }}
        </div><div class="text-caption muted mb-3">
          {{ session.user?.roles.join(' · ') }}
        </div><v-btn
          variant="tonal"
          block
          prepend-icon="mdi-logout"
          @click="logout"
        >
          Sign out
        </v-btn>
      </div>
    </template>
  </v-navigation-drawer><v-app-bar
    flat
    border
  >
    <v-app-bar-nav-icon
      aria-label="Toggle navigation"
      @click="drawer = !drawer"
    /><v-app-bar-title>Expense Report System</v-app-bar-title>
  </v-app-bar><v-main><router-view /></v-main>
</template>
