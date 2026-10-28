<script lang="ts">
// Module scope, shared by every instance: each page mounts its own layout (and so this dialog), and this
// stops a repeat within one page load when browser storage is blocked.
let shownThisLoad = false;
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { localDayNumber, quoteForDate } from '@/features/wellness/quotes';

const props = defineProps<{ userId: number }>();

const now = new Date();
const quote = quoteForDate(now);
const today = new Intl.DateTimeFormat('en-PH', { weekday: 'long', month: 'long', day: 'numeric' }).format(now);
const open = ref(false);

// Once a day per user in this browser: a per-viewer convenience, so storage failures only mean it may show again.
const storageKey = `erfweb.dailyQuote.${props.userId}`;

onMounted(() => {
  if (shownThisLoad) return;
  const day = String(localDayNumber(now));
  try {
    if (localStorage.getItem(storageKey) === day) return;
    localStorage.setItem(storageKey, day);
  } catch {
    // Storage blocked (private window): show it once for this page load.
  }
  shownThisLoad = true;
  open.value = true;
});
</script>

<template>
  <v-dialog v-model="open" max-width="460">
    <v-card class="quote-card">
      <div class="quote-card__head">
        <span class="quote-card__date">{{ today }}</span>
        <h2 class="quote-card__label">Quote of the day</h2>
      </div>
      <v-card-text>
        <blockquote class="quote-card__quote">
          <v-icon icon="mdi-format-quote-open" size="24" class="quote-card__mark" aria-hidden="true" />
          {{ quote.text }}
        </blockquote>
        <p v-if="quote.source" class="quote-card__source">— {{ quote.source }}</p>
      </v-card-text>
      <v-divider />
      <v-card-actions class="px-4 py-3">
        <v-btn class="press" variant="text" color="primary" prepend-icon="mdi-heart-pulse" :to="{ name: 'random-quotes' }" @click="open = false">
          Healthy tips
        </v-btn>
        <v-spacer />
        <v-btn class="press" color="primary" variant="flat" autofocus @click="open = false">Thanks</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.quote-card__head {
  padding: 20px 24px 16px;
  color: var(--on-brand);
  background:
    radial-gradient(320px 140px at 100% 0%, rgba(120, 200, 140, 0.25), transparent 70%),
    linear-gradient(135deg, var(--brand-2), var(--brand-1));
}
.quote-card__date {
  font-size: 12px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--brand-accent);
}
.quote-card__label {
  margin: 4px 0 0;
  font-size: 16px;
  font-weight: 600;
}
.quote-card__quote {
  margin: 8px 0 4px;
  font-size: 19px;
  font-weight: 500;
  line-height: 1.4;
  color: var(--ink);
  text-wrap: balance;
}
.quote-card__mark {
  vertical-align: top;
  margin-right: 4px;
  color: var(--brass);
}
.quote-card__source {
  margin: 4px 0 0;
  font-size: 13px;
  font-weight: 500;
  color: var(--brass);
}
</style>
