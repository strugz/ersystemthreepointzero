<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { HELP_VIDEOS, formatStart, helpTopics, safeVideoUrl } from '@/features/help/topics';
import type { HelpTopic, HelpVideoKey } from '@/features/help/topics';

/** `videos`: the tabs this user sees, first one open by default. */
const props = defineProps<{ videos: HelpVideoKey[]; topicId: string | null }>();
const emit = defineEmits<{ 'update:topicId': [id: string | null] }>();

const SEARCH_HINTS: Record<HelpVideoKey, string> = {
  filing: 'Search, e.g. Representation, OR no., cash advance',
  approving: 'Search, e.g. return, override, delegate',
  finance: 'Search, e.g. receipts, clear, verify, signature',
  verifier: 'Search, e.g. pick up, finding, receipts',
};

const tab = ref<HelpVideoKey>(props.videos[0] ?? 'filing');
const videoSrc = computed(() => safeVideoUrl(HELP_VIDEOS[tab.value].url));
const hdSrc = computed(() => safeVideoUrl(HELP_VIDEOS[tab.value].hd.url));
const loadFailed = ref(false);
const video = ref<HTMLVideoElement | null>(null);
const search = ref('');
const openId = ref<string | null>(null);

watch(tab, () => {
  loadFailed.value = false;
  openId.value = null;
});

function pickTab(key: HelpVideoKey) {
  if (key === tab.value) return;
  tab.value = key;
  emit('update:topicId', null);
}

const groups = computed(() => {
  const q = search.value.trim().toLowerCase();
  const own = helpTopics.filter((t) => t.video === tab.value);
  const matches = q
    ? own.filter((t) => [t.question, t.group, t.level ?? '', t.keywords ?? '', ...t.answer].join(' ').toLowerCase().includes(q))
    : own;
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
  const topic = helpTopics.find((t) => t.id === id && props.videos.includes(t.video));
  if (!topic || openId.value === topic.id) return;
  tab.value = topic.video;
  void nextTick(() => {
    openId.value = topic.id;
    seek(topic.start, false);
    document.getElementById(`help-${topic.id}`)?.scrollIntoView({ block: 'center' });
  });
}

onMounted(() => openFromAddress(props.topicId));
watch(() => props.topicId, openFromAddress);
</script>

<template>
  <div v-if="videos.length > 1" class="help__tabs mb-4" role="tablist" aria-label="Help videos">
    <button
      v-for="key in videos"
      :key="key"
      type="button"
      role="tab"
      class="help__tab press"
      :class="{ 'help__tab--on': tab === key }"
      :aria-selected="tab === key"
      @click="pickTab(key)"
    >
      {{ HELP_VIDEOS[key].title }}
    </button>
  </div>
  <div class="help">
    <div class="help__video">
      <template v-if="videoSrc && !loadFailed">
        <video
          :key="tab"
          ref="video"
          :src="videoSrc"
          controls
          preload="metadata"
          playsinline
          class="help__player"
          @error="loadFailed = true"
        >
          Your browser cannot play this video.
        </video>
        <div class="help__below">
          <p class="help__hint">Select a question to play the video from that part.</p>
          <v-btn
            v-if="hdSrc"
            :href="hdSrc"
            download
            class="press"
            size="small"
            variant="text"
            prepend-icon="mdi-download"
          >
            Download HD ({{ HELP_VIDEOS[tab].hd.size }})
          </v-btn>
        </div>
      </template>
      <div v-else class="help__player help__novideo" role="status">
        <v-icon :icon="loadFailed ? 'mdi-video-off-outline' : 'mdi-video-outline'" size="32" />
        <span>{{ loadFailed ? 'This video could not be loaded.' : 'Video coming soon.' }}</span>
        <span class="help__novideo-sub">The written answers still apply.</span>
      </div>
    </div>

    <div class="help__topics">
      <v-text-field
        v-model="search"
        density="compact"
        hide-details
        clearable
        prepend-inner-icon="mdi-magnify"
        :placeholder="SEARCH_HINTS[tab]"
        class="mb-4"
      />

      <p v-if="!groups.length" class="help__empty">No question matches "{{ search }}".</p>

      <section v-for="g in groups" :key="g.label" class="mb-5">
        <h2 class="help__group">{{ g.label }}</h2>
        <v-card>
          <article v-for="t in g.topics" :id="`help-${t.id}`" :key="t.id" class="topic" :class="{ 'topic--open': openId === t.id }">
            <button type="button" class="topic__head press" :aria-expanded="openId === t.id" @click="choose(t)">
              <span class="topic__q">
                {{ t.question }}
                <span v-if="t.level" class="topic__level">{{ t.level }}</span>
              </span>
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
.help__novideo {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 16px;
  text-align: center;
  font-size: 14px;
  color: var(--ink-2);
  background: var(--card);
  border: 1px solid var(--line);
}
.help__novideo-sub {
  font-size: 12px;
  color: var(--ink-3);
}
.help__below {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 4px 12px;
  margin-top: 8px;
}
.help__hint {
  margin: 0;
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
.topic__level {
  display: inline-block;
  margin-left: 6px;
  padding: 0 6px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 600;
  vertical-align: 1px;
  color: rgb(var(--v-theme-warning));
  background: rgba(var(--v-theme-warning), 0.14);
}
.help__tabs {
  display: inline-flex;
  gap: 4px;
  padding: 4px;
  border-radius: 10px;
  background: var(--card);
  border: 1px solid var(--line);
}
.help__tab {
  padding: 6px 14px;
  border: 0;
  border-radius: 8px;
  font: inherit;
  font-size: 13px;
  font-weight: 500;
  color: var(--ink-2);
  background: none;
  cursor: pointer;
}
.help__tab--on {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.14);
}
.help__tab:focus-visible {
  outline: 2px solid rgb(var(--v-theme-primary));
  outline-offset: 1px;
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
