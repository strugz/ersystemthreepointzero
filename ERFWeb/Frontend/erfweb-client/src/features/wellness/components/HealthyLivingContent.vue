<script setup lang="ts">
import { quoteForDate } from '@/features/wellness/quotes';

const now = new Date();
const quote = quoteForDate(now);
const today = new Intl.DateTimeFormat('en-PH', { weekday: 'long', month: 'long', day: 'numeric' }).format(now);

const habits = [
  { icon: 'mdi-cup-water', title: 'Stay hydrated', text: 'Aim for about eight glasses of water a day, more on field days and in the heat.' },
  { icon: 'mdi-walk', title: 'Keep moving', text: 'At least 30 minutes of activity most days. Walking, stairs and stretching all count.' },
  { icon: 'mdi-food-apple-outline', title: 'Eat well', text: 'Fill half your plate with vegetables and fruit. Go easy on salt, sugar and fried food.' },
  { icon: 'mdi-sleep', title: 'Sleep enough', text: 'Seven to nine hours a night. Keep the same bedtime, even on weekends.' },
  { icon: 'mdi-monitor-eye', title: 'Rest your eyes', text: 'Every 20 minutes at the screen, look at something 20 feet away for 20 seconds.' },
  { icon: 'mdi-head-heart-outline', title: 'Mind your mind', text: 'Take short breaks, talk to someone you trust, and ask for help when you need it.' },
] as const;
</script>

<template>
  <div>
    <section class="hero mb-5" aria-labelledby="quote-heading">
      <span class="hero__date">{{ today }}</span>
      <h1 id="quote-heading" class="hero__label">Quote of the day</h1>
      <blockquote class="hero__quote">
        <v-icon icon="mdi-format-quote-open" size="28" class="hero__mark" aria-hidden="true" />
        {{ quote }}
      </blockquote>
      <p class="hero__sub">A new quote every day. Come back tomorrow for the next one.</p>
    </section>

    <h2 class="section-t mb-3">Everyday healthy habits</h2>
    <div class="habits">
      <v-card v-for="(h, i) in habits" :key="h.title" class="habit stagger-in" :style="{ '--i': i }">
        <span class="habit__icon"><v-icon :icon="h.icon" size="22" /></span>
        <div>
          <div class="habit__title">{{ h.title }}</div>
          <p class="habit__text">{{ h.text }}</p>
        </div>
      </v-card>
    </div>

    <p class="note mt-5">These are general wellness tips, not medical advice. Talk to a doctor about your own health.</p>
  </div>
</template>

<style scoped>
.hero {
  position: relative;
  overflow: hidden;
  padding: 28px 32px;
  border-radius: 16px;
  color: var(--on-brand);
  background:
    radial-gradient(520px 220px at 100% 0%, rgba(120, 200, 140, 0.25), transparent 70%),
    linear-gradient(135deg, var(--brand-2), var(--brand-1));
  box-shadow: var(--shadow-2);
}
.hero__date {
  font-size: 12px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--brand-accent);
}
.hero__label {
  margin: 6px 0 10px;
  font-size: 14px;
  font-weight: 500;
  color: var(--on-brand-2);
}
.hero__quote {
  margin: 0 0 12px;
  font-size: 24px;
  font-weight: 500;
  line-height: 1.35;
  letter-spacing: -0.01em;
  text-wrap: balance;
}
.hero__mark {
  vertical-align: top;
  margin-right: 4px;
  color: var(--brand-accent);
}
.hero__sub {
  margin: 0;
  font-size: 13px;
  color: var(--on-brand-2);
}
.section-t {
  font-size: 16px;
  font-weight: 600;
}
.habits {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
.habit {
  display: flex;
  gap: 14px;
  padding: 18px 20px;
}
.habit__icon {
  flex: none;
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(var(--v-theme-success), 0.14);
  color: rgb(var(--v-theme-success));
}
.habit__title {
  font-size: 14px;
  font-weight: 600;
}
.habit__text {
  margin: 2px 0 0;
  font-size: 13px;
  color: var(--ink-2);
}
.note {
  font-size: 12px;
  color: var(--ink-3);
}
@media (max-width: 959.98px) {
  .habits {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
@media (max-width: 599.98px) {
  .hero {
    padding: 20px;
    border-radius: 14px;
  }
  .hero__quote {
    font-size: 19px;
  }
  .habits {
    grid-template-columns: 1fr;
    gap: 10px;
  }
}
</style>
