<script setup lang="ts">
import { computed } from 'vue';
import { formatMoney } from '@/shared/format';
import { balanceDueTo } from '../labels';

// Running totals in navy. `card` is the desktop side card; `strip` is the horizontal tablet/phone form.
const props = withDefaults(
  defineProps<{ total: number; cashAdvance: number; variant?: 'card' | 'strip' }>(),
  { variant: 'card' },
);

const balance = computed(() => props.cashAdvance - props.total);
const balanceLabel = computed(() => `Balance due to ${balanceDueTo(balance.value).toLowerCase() === 'mdmpi' ? 'MDMPI' : 'employee'}`);
</script>

<template>
  <section v-if="variant === 'card'" class="totals">
    <div class="lbl totals__lbl">Total expenses</div>
    <div class="mono totals__big">₱ {{ formatMoney(total) }}</div>
    <div class="totals__rule"></div>
    <div class="totals__row"><span>Cash advance</span><span class="mono">{{ formatMoney(cashAdvance) }}</span></div>
    <div class="totals__row totals__row--strong"><span>{{ balanceLabel }}</span><span class="mono">{{ formatMoney(Math.abs(balance)) }}</span></div>
  </section>

  <section v-else class="totals totals--strip">
    <div class="totals__cell">
      <div class="lbl totals__lbl">Total expenses</div>
      <div class="mono totals__val">₱ {{ formatMoney(total) }}</div>
    </div>
    <div class="totals__cell">
      <div class="lbl totals__lbl">Cash advance</div>
      <div class="mono totals__val">{{ formatMoney(cashAdvance) }}</div>
    </div>
    <div class="totals__cell">
      <div class="lbl totals__lbl">{{ balanceLabel }}</div>
      <div class="mono totals__val">{{ formatMoney(Math.abs(balance)) }}</div>
    </div>
  </section>
</template>

<style scoped>
.totals {
  background: var(--navy);
  color: #fff;
  border-radius: var(--radius);
  padding: 20px;
}
.totals__lbl {
  color: rgba(255, 255, 255, 0.7);
}
.totals__big {
  font-size: 30px;
  font-weight: 500;
  margin-top: 4px;
  line-height: 1.2;
}
.totals__rule {
  height: 1px;
  background: rgba(255, 255, 255, 0.18);
  margin: 16px 0 12px;
}
.totals__row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
  opacity: 0.85;
}
.totals__row + .totals__row {
  margin-top: 8px;
}
.totals__row--strong {
  opacity: 1;
  font-weight: 500;
}

.totals--strip {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  padding: 0;
}
.totals__cell {
  padding: 14px 18px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.totals__cell + .totals__cell {
  border-left: 1px solid rgba(255, 255, 255, 0.18);
}
.totals__val {
  font-size: 20px;
  font-weight: 500;
  white-space: nowrap;
}
@media (max-width: 599.98px) {
  .totals--strip {
    grid-template-columns: 1fr;
  }
  .totals__cell + .totals__cell {
    border-left: 0;
    border-top: 1px solid rgba(255, 255, 255, 0.18);
  }
  .totals__cell:first-child .totals__val {
    font-size: 30px;
  }
}
.v-theme--erDark .totals {
  color: #0f1c33;
}
.v-theme--erDark .totals__lbl {
  color: rgba(15, 28, 51, 0.7);
}
.v-theme--erDark .totals__rule,
.v-theme--erDark .totals__cell + .totals__cell {
  border-color: rgba(15, 28, 51, 0.18);
  background: rgba(15, 28, 51, 0.18);
}
</style>
