<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSessionStore } from '@/app/stores/session'
import { useAsyncAction } from '@/shared/composables/useAsyncAction'
import AppErrorAlert from '@/shared/components/AppErrorAlert.vue'
import { required } from '@/shared/validation/rules'

const session = useSessionStore()
const router = useRouter()
const form = reactive({ username: '', password: '' })
const valid = ref(false)
const { loading, error, run } = useAsyncAction()
async function submit() {
  if (!valid.value) return
  try {
    await run(() => session.login(form))
    const destination = session.user?.roles.includes('Manager') ? '/manager/reports' : session.user?.roles.includes('Finance') ? '/finance/receipts' : '/forbidden'
    await router.replace(destination)
  } catch { /* the reusable action exposes the API message */ }
}
</script>
<template>
  <v-form
    v-model="valid"
    @submit.prevent="submit"
  >
    <AppErrorAlert :message="error" /><v-text-field
      v-model="form.username"
      label="Username"
      autocomplete="username"
      :rules="[required('Username')]"
      class="mb-3"
    /><v-text-field
      v-model="form.password"
      label="Password"
      type="password"
      autocomplete="current-password"
      :rules="[required('Password')]"
      class="mb-5"
    /><v-btn
      type="submit"
      color="primary"
      block
      size="large"
      :loading="loading"
      :disabled="!valid"
    >
      Sign in
    </v-btn>
  </v-form>
</template>
