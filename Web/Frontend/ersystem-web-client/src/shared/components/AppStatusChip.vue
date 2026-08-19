<script setup lang="ts">
import { computed } from 'vue'
import { statusPresentation } from '@/shared/design/tokens'
const props = defineProps<{ status: string }>()
const presentation = computed(() => statusPresentation[props.status] ?? { label: props.status || 'Unknown', color: 'default' })
</script>
<template>
  <v-chip
    :color="presentation.color"
    size="small"
    variant="tonal"
    label
  >
    {{ presentation.label }}
  </v-chip>
</template>
