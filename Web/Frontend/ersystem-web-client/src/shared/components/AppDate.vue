<script setup lang="ts">import { formatDate } from '@/shared/utils/format'; defineProps<{ value?: string | null }>()</script>
<template><span>{{ formatDate(value) }}</span></template>
