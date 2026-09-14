<script setup lang="ts">
import { ref } from 'vue';
import type { ReportType } from '../types';

// The report type decides which money block the form shows, so it is a visible choice, not a
// dropdown: no menu can cover the fields below, and all three options are readable at once.
// Behaves as a radio group: Arrow keys move, Space/Enter select.
defineProps<{ disabled?: boolean }>();
const model = defineModel<ReportType>({ required: true });

interface Option {
  value: ReportType;
  title: string;
  hint: string;
}

const options: Option[] = [
  { value: 'Reimbursement', title: 'Reimbursement', hint: 'No cash advance or revolving fund. The full total is due to you.' },
  { value: 'CashAdvanceLiquidation', title: 'Liquidation of Cash Advance', hint: 'You received an advance; the R47 reconciles it against your expenses.' },
  { value: 'RevolvingFund', title: 'Replenishment of Revolving Fund', hint: 'Prints the fund amount in the "IF WITH REVOLVING FUND" box.' },
];

const buttons = ref<HTMLButtonElement[]>([]);

function select(value: ReportType) {
  model.value = value;
}

function onKeydown(event: KeyboardEvent, index: number) {
  const keys: Record<string, number> = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 };
  const step = keys[event.key];
  if (step === undefined) {
    return;
  }
  event.preventDefault();
  const next = (index + step + options.length) % options.length;
  const opt = options[next];
  if (opt) {
    select(opt.value);
    buttons.value[next]?.focus();
  }
}
</script>

<template>
  <div class="picker" role="radiogroup" aria-label="Report type">
    <button
      v-for="(opt, i) in options"
      :key="opt.value"
      ref="buttons"
      type="button"
      role="radio"
      class="picker__opt press"
      :class="{ 'picker__opt--on': model === opt.value }"
      :aria-checked="model === opt.value"
      :tabindex="model === opt.value ? 0 : -1"
      :disabled="disabled"
      @click="select(opt.value)"
      @keydown="onKeydown($event, i)"
    >
      <span class="picker__dot" aria-hidden="true"></span>
      <span class="picker__text">
        <span class="picker__title">{{ opt.title }}</span>
        <span class="picker__hint">{{ opt.hint }}</span>
      </span>
    </button>
  </div>
</template>

<style scoped>
.picker {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}
@media (max-width: 959.98px) {
  .picker {
    grid-template-columns: 1fr;
  }
}
.picker__opt {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px 14px;
  min-height: 64px;
  border: 1px solid var(--line-2);
  border-radius: 10px;
  background: var(--card);
  color: var(--ink);
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: border-color 120ms ease, background-color 120ms ease, transform var(--dur-press) var(--ease-out);
}
.picker__opt:disabled {
  cursor: default;
  opacity: 0.6;
}
.picker__opt--on {
  border-color: var(--navy);
  background: var(--navy-soft);
  box-shadow: inset 0 0 0 1px var(--navy);
}
.picker__opt:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: 2px;
}
@media (hover: hover) and (pointer: fine) {
  .picker__opt:not(:disabled):not(.picker__opt--on):hover {
    border-color: var(--ink-3);
  }
}
.picker__dot {
  flex: none;
  width: 16px;
  height: 16px;
  margin-top: 2px;
  border-radius: 50%;
  border: 1.5px solid var(--line-2);
  background: var(--card);
  transition: border-color 120ms ease, box-shadow 120ms ease;
}
.picker__opt--on .picker__dot {
  border-color: var(--navy);
  box-shadow: inset 0 0 0 4px var(--navy);
}
.picker__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.picker__title {
  font-size: 14px;
  font-weight: 500;
  line-height: 1.3;
}
.picker__hint {
  font-size: 12px;
  color: var(--ink-3);
  line-height: 1.35;
}
</style>
