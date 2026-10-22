<script setup lang="ts">
// The ERMS "Ledger E": three bars of an E with the middle one in gold, like a line on a ledger.
// `inverse` draws a white tile with navy bars for navy surfaces and dark mode.
withDefaults(defineProps<{ size?: number; inverse?: boolean }>(), { size: 40, inverse: false });
</script>

<template>
  <svg :width="size" :height="size" viewBox="0 0 64 64" aria-hidden="true" focusable="false" class="erms-mark">
    <rect width="64" height="64" rx="15" :fill="inverse ? '#ffffff' : '#16294a'" />
    <g :fill="inverse ? '#16294a' : '#ffffff'">
      <rect x="16" y="15" width="8" height="34" rx="1.5" />
      <rect x="16" y="15" width="32" height="8" rx="1.5" />
      <rect x="16" y="41" width="32" height="8" rx="1.5" />
    </g>
    <rect x="28" y="28" width="16" height="8" rx="1.5" fill="#e2b765" />
  </svg>
</template>

<style scoped>
.erms-mark {
  display: block;
  flex: none;
}
</style>
