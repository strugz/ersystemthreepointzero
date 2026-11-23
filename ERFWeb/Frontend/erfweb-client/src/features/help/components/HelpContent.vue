<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { HELP_VIDEO_SRC, formatStart, helpTopics } from '@/features/help/topics';
import type { HelpTopic } from '@/features/help/topics';

const props = defineProps<{ topicId: string | null }>();
const emit = defineEmits<{ 'update:topicId': [id: string | null] }>();

const video = ref<HTMLVideoElement | null>(null);
const search = ref('');
const openId = ref<string | null>(null);

const groups = computed(() => {
  const q = search.value.trim().toLowerCase();
  const matches = q
    ? helpTopics.filter((t) => [t.question, t.group, t.keywords ?? '', ...t.answer].join(' ').toLowerCase().includes(q))
    : helpTopics;
  const byGroup = new Map<string, HelpTopic[]>();
  for (const t of matches) byGroup.set(t.group, [...(byGroup.get(t.group) ?? []), t]);
  return [...byGroup].map(([label, topics]) => ({ label, topics }));
});

// Seeking before the metadata has loaded is ignored by the browser, so wait for it.
function seek(seconds: number, play: boolean) {
  const v = video.value;
  if (!v) return;
  const go = () => {
    v.currentTime = seconds;
    if (play) void v.play().catch(() => undefined);
  };
  if (v.readyState >= HTMLMediaElement.HAVE_METADATA) go();
  else v.addEventListener('loadedmetadata', go, { once: true });
}

function choose(topic: HelpTopic) {
  const wasOpen = openId.value === topic.id;
  openId.value = wasOpen ? null : topic.id;
  if (wasOpen) return;
  emit('update:topicId', topic.id);
  seek(topic.start, true);
  video.value?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// From the address (e.g. the ? in the expense line window): open the topic and cue the video. No
// autoplay, since a page opened in a new tab has no click to allow it.
function openFromAddress(id: string | null) {
  const topic = helpTopics.find((t) => t.id === id);
  if (!topic || openId.value === topic.id) return;
  openId.value = topic.id;
  seek(topic.start, false);
  void nextTick(() => document.getElementById(`help-${topic.id}`)?.scrollIntoView({ block: 'center' }));
}

onMounted(() => openFromAddress(props.topicId));
watch(() => props.topicId, openFromAddress);
</script>

<template>
  <div class="help">
    <div class="help__video">
      <video ref="video" :src="HELP_VIDEO_SRC" controls preload="metadata" playsinline class="help__player">
        Your browser cannot play this video.
      </video>
      <p class="help__hint">Select a question to play the video from that part.</p>
    </div>

    <div class="help__topics">
      <v-text-field
        v-model="search"
        density="compact"
        hide-details
        clearable
        prepend-inner-icon="mdi-magnify"
        placeholder="Search, e.g. Representation, OR no., cash advance"
        class="mb-4"
      />

      <p v-if="!groups.length" class="help__empty">No question matches "{{ search }}".</p>

      <section v-for="g in groups" :key="g.label" class="mb-5">
        <h2 class="help__group">{{ g.label }}</h2>
        <v-card>
          <article v-for="t in g.topics" :id="`help-${t.id}`" :key="t.id" class="topic" :class="{ 'topic--open': openId === t.id }">
            <button type="button" class="topic__head press" :aria-expanded="openId === t.id" @click="choose(t)">
              <span class="topic__q">{{ t.question }}</span>
              <span class="topic__time"><v-icon icon="mdi-play" size="14" />{{ formatStart(t.start) }}</span>
            </button>
            <ul v-if="openId === t.id" class="topic__answer">
              <li v-for="(line, i) in t.answer" :key="i">{{ line }}</li>
            </ul>
          </article>
        </v-card>
      </section>
    </div>
  </div>
</template>

<style scoped>
.help {
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(0, 2fr);
  gap: 24px;
  align-items: start;
}
.help__video {
  position: sticky;
  top: calc(var(--shell-header) + 28px);
}
.help__player {
  display: block;
  width: 100%;
  aspect-ratio: 16 / 9;
  border-radius: var(--radius);
  background: #000;
  box-shadow: var(--shadow-1);
}
.help__hint {
  margin: 8px 0 0;
  font-size: 12px;
  color: var(--ink-3);
}
.help__group {
  margin: 0 0 8px;
  font-size: 14px;
  font-weight: 600;
}
.help__empty {
  font-size: 13px;
  color: var(--ink-2);
}
.topic + .topic {
  border-top: 1px solid var(--line);
}
.topic__head {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 16px;
  text-align: left;
  font: inherit;
  color: inherit;
  background: none;
  border: 0;
  cursor: pointer;
}
.topic__head:focus-visible {
  outline: 2px solid rgb(var(--v-theme-primary));
  outline-offset: -2px;
}
.topic__q {
  flex: 1;
  font-size: 14px;
}
.topic--open .topic__q {
  font-weight: 600;
}
.topic__time {
  flex: none;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 2px 8px;
  border-radius: 999px;
  font-family: var(--font-mono);
  font-size: 12px;
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.12);
}
.topic__answer {
  margin: 0;
  padding: 0 16px 14px 34px;
  font-size: 13px;
  line-height: 1.5;
  color: var(--ink-2);
}
.topic__answer li + li {
  margin-top: 4px;
}
@media (max-width: 959.98px) {
  .help {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  .help__video {
    position: static;
  }
}
</style>
