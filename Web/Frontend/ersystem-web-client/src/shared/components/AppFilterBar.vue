<script setup lang="ts">
import { ref, useSlots } from 'vue'
import { useDisplay } from 'vuetify'

defineProps<{ mobileTitle?: string }>()
const slots = useSlots()
const { smAndDown } = useDisplay()
const filtersOpen = ref(false)
</script>

<template>
  <v-card
    v-if="!smAndDown || !slots.primary"
    class="pa-4 mb-4 border"
    variant="flat"
  >
    <div class="filter-grid">
      <template v-if="slots.primary || slots.filters">
        <slot name="primary" />
        <slot name="filters" />
      </template>
      <slot v-else />
    </div>
  </v-card>
  <template v-else>
    <v-card
      class="pa-3 mb-4 border"
      variant="flat"
    >
      <div class="mobile-filter-primary">
        <slot name="primary" />
        <v-btn
          variant="tonal"
          prepend-icon="mdi-filter-variant"
          @click="filtersOpen = true"
        >
          Filters
        </v-btn>
      </div>
    </v-card>
    <v-bottom-sheet v-model="filtersOpen">
      <v-card class="mobile-filter-sheet">
        <v-card-title class="d-flex align-center">
          {{ mobileTitle ?? 'Filters' }}
          <v-spacer />
          <v-btn
            icon="mdi-close"
            variant="text"
            aria-label="Close filters"
            @click="filtersOpen = false"
          />
        </v-card-title>
        <v-divider />
        <v-card-text class="filter-grid">
          <slot name="filters" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            color="primary"
            @click="filtersOpen = false"
          >
            Done
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-bottom-sheet>
  </template>
</template>
