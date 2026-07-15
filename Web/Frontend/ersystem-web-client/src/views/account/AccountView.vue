<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/app/stores/session'
import AppPageHeader from '@/shared/components/AppPageHeader.vue'

const session = useSessionStore()
const router = useRouter()

async function logout() {
  await session.logout()
  await router.replace('/login')
}
</script>

<template>
  <div class="account-page">
    <AppPageHeader
      title="Account"
      subtitle="Your ER System identity and access"
    />
    <v-card
      class="border"
      variant="flat"
    >
      <v-card-text class="pa-6">
        <div class="d-flex align-center ga-4 mb-6">
          <v-avatar
            color="primary"
            size="56"
          >
            <v-icon size="30">
              mdi-account-outline
            </v-icon>
          </v-avatar>
          <div>
            <div class="text-h6 font-weight-bold">
              {{ session.user?.fullName }}
            </div>
            <div class="muted">
              {{ session.user?.username }}
            </div>
          </div>
        </div>
        <div class="detail-stack">
          <div>
            <span class="field-label">Roles</span>
            <div class="d-flex flex-wrap ga-2">
              <v-chip
                v-for="role in session.user?.roles"
                :key="role"
                color="primary"
                variant="tonal"
              >
                {{ role }}
              </v-chip>
            </div>
          </div>
          <div>
            <span class="field-label">User level</span>
            {{ session.user?.userLevel || '—' }}
          </div>
        </div>
      </v-card-text>
      <v-divider />
      <v-card-actions class="pa-4">
        <v-btn
          block
          color="primary"
          variant="tonal"
          prepend-icon="mdi-logout"
          @click="logout"
        >
          Sign out
        </v-btn>
      </v-card-actions>
    </v-card>
  </div>
</template>
