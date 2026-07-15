<script setup lang="ts">
import { computed, useSlots } from 'vue'
import AppEmptyState from '@/shared/components/AppEmptyState.vue'

defineProps<{ headers: Array<Record<string, unknown>>; items: unknown[]; total: number; loading?: boolean; itemsPerPage?: number }>()
const emit = defineEmits<{ updateOptions: [options: { page: number; itemsPerPage: number; sortBy?: Array<{ key: string; order: 'asc' | 'desc' }> }]; clickRow: [item: unknown] }>()
const slots = useSlots()
const forwardedSlots = computed(() => Object.fromEntries(Object.entries(slots).filter(([name]) => name !== 'no-data')))
function onClickRow(_event: Event, row: { item: unknown }) { emit('clickRow', row.item) }
</script>
<template>
  <v-data-table-server
    :headers="headers"
    :items="items"
    :items-length="total"
    :loading="loading"
    :items-per-page="itemsPerPage ?? 25"
    hover
    class="border rounded-xl"
    @update:options="emit('updateOptions', $event)"
    @click:row="onClickRow"
  >
    <template
      v-for="(_, name) in forwardedSlots"
      #[name]="slotData"
    >
      <slot
        :name="name"
        v-bind="slotData ?? {}"
      />
    </template>
    <template #no-data>
      <slot name="no-data">
        <AppEmptyState
          title="No records found"
          description="Try changing the filters or refresh the list."
        />
      </slot>
    </template>
  </v-data-table-server>
</template>
