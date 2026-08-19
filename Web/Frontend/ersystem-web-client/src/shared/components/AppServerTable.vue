<script setup lang="ts">
import { computed, useSlots } from 'vue'
import { useDisplay } from 'vuetify'
import AppEmptyState from '@/shared/components/AppEmptyState.vue'

const props = defineProps<{
  headers: Array<Record<string, unknown>>
  items: unknown[]
  total: number
  loading?: boolean
  page?: number
  itemsPerPage?: number
}>()
const emit = defineEmits<{ updateOptions: [options: { page: number; itemsPerPage: number; sortBy?: Array<{ key: string; order: 'asc' | 'desc' }> }]; clickRow: [item: unknown] }>()
const slots = useSlots()
const { smAndDown } = useDisplay()
const forwardedSlots = computed(() => Object.fromEntries(Object.entries(slots).filter(([name]) => name !== 'no-data')))
const totalPages = computed(() => Math.max(1, Math.ceil(props.total / (props.itemsPerPage ?? 25))))
function onClickRow(_event: Event, row: { item: unknown }) { emit('clickRow', row.item) }
function updateMobilePage(page: number) { emit('updateOptions', { page, itemsPerPage: props.itemsPerPage ?? 25 }) }
</script>
<template>
  <v-data-table-server
    v-if="!smAndDown || !slots['mobile-item']"
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
  <v-card
    v-else
    class="mobile-server-list border"
    variant="flat"
  >
    <v-progress-linear
      v-if="loading"
      indeterminate
      color="primary"
      aria-label="Loading records"
    />
    <div
      v-if="items.length"
      class="mobile-server-list__items"
    >
      <slot
        v-for="(item, index) in items"
        :key="index"
        name="mobile-item"
        :item="item"
      />
    </div>
    <slot
      v-else-if="!loading"
      name="no-data"
    >
      <AppEmptyState
        title="No records found"
        message="Try changing the filters or refresh the list."
      />
    </slot>
    <template v-if="total > 0">
      <v-divider />
      <div class="mobile-server-list__footer">
        <span class="text-caption muted">{{ total }} {{ total === 1 ? 'record' : 'records' }}</span>
        <v-pagination
          :model-value="page ?? 1"
          :length="totalPages"
          :total-visible="4"
          density="comfortable"
          aria-label="Queue pages"
          @update:model-value="updateMobilePage"
        />
      </div>
    </template>
  </v-card>
</template>
