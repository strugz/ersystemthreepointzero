<script setup lang="ts">
import { useDisplay } from 'vuetify'

withDefaults(defineProps<{
  loading?: boolean
  accessibleLabel: string
}>(), {
  loading: false
})

const emit = defineEmits<{
  refresh: []
}>()

const { smAndDown } = useDisplay()
</script>

<template>
  <v-btn
    v-if="smAndDown"
    icon="mdi-refresh"
    :loading="loading"
    :aria-label="accessibleLabel"
    title="Refresh"
    :width="44"
    :height="44"
    @click="emit('refresh')"
  />
  <v-btn
    v-else
    prepend-icon="mdi-refresh"
    :loading="loading"
    :aria-label="accessibleLabel"
    @click="emit('refresh')"
  >
    Refresh
  </v-btn>
</template>
