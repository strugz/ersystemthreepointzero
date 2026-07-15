<script setup lang="ts">import { formatDateTime } from '@/shared/utils/format'; defineProps<{ value?: string | null }>()</script>
<template><span>{{ formatDateTime(value) }}</span></template>
