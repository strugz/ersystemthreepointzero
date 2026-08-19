<script setup lang="ts">import { formatMoney } from '@/shared/utils/format'; defineProps<{ value?: number | null }>()</script>
<template><span>{{ formatMoney(value) }}</span></template>
