<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useSessionStore } from '@/app/stores/session';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import AuthenticatedLayout from '@/layouts/AuthenticatedLayout.vue';
import HelpContent from '@/features/help/components/HelpContent.vue';
import type { HelpVideoKey } from '@/features/help/topics';

const route = useRoute();
const router = useRouter();
const session = useSessionStore();
const { compact } = useLayoutMode();

const topicId = computed(() => (typeof route.query.topic === 'string' ? route.query.topic : null));

// The approving video is shown to signatories only; it holds nothing secret, this only keeps Help relevant.
const isSignatory = computed(() => session.hasRole('SignatoryLevel1') || session.hasRole('SignatoryLevel2'));
const videos = computed<HelpVideoKey[]>(() => (isSignatory.value ? ['filing', 'approving'] : ['filing']));

function setTopic(id: string | null) {
  void router.replace({ query: { ...route.query, topic: id ?? undefined } });
}
</script>

<template>
  <AuthenticatedLayout :crumbs="[{ title: 'Help' }]">
    <div v-if="!compact" class="d-flex align-baseline ga-3 flex-wrap mb-5">
      <h1 class="page-title">Help</h1>
      <span class="text-body-2 text-medium-emphasis">
        {{ isSignatory ? 'How to file an expense report, what each category needs, and how to approve reports.' : 'How to file an expense report, and what each expense category needs.' }}
      </span>
    </div>
    <HelpContent :videos="videos" :topic-id="topicId" @update:topic-id="setTopic" />
  </AuthenticatedLayout>
</template>
