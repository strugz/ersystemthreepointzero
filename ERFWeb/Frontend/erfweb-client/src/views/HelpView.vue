<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import HelpContent from '@/features/help/components/HelpContent.vue';

const route = useRoute();
const router = useRouter();
const { compact } = useLayoutMode();

const topicId = computed(() => (typeof route.query.topic === 'string' ? route.query.topic : null));

function setTopic(id: string | null) {
  void router.replace({ query: { ...route.query, topic: id ?? undefined } });
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Help' }]">
    <div v-if="!compact" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Help</h1>
      <span class="text-body-2 text-medium-emphasis">How to file an expense report, and what each expense category needs.</span>
    </div>
    <HelpContent :topic-id="topicId" @update:topic-id="setTopic" />
  </AuthenticatedLayout>
</template>
