<script setup lang="ts">
// "Before you file" — every precondition, green when met, amber when still open.
export interface ChecklistItem {
  ok: boolean;
  text: string;
}

defineProps<{ items: ChecklistItem[]; approverNames: string[] }>();
</script>

<template>
  <v-card class="checklist">
    <div class="checklist__body">
      <div class="card-t">Before you file</div>
      <ul class="checklist__list">
        <li v-for="item in items" :key="item.text" class="checklist__row">
          <svg v-if="item.ok" class="checklist__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: rgb(var(--v-theme-success))"><path d="M5 12l5 5L20 7"></path></svg>
          <svg v-else class="checklist__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" style="color: rgb(var(--v-theme-warning))"><circle cx="12" cy="12" r="8"></circle></svg>
          <span>{{ item.text }}</span>
        </li>
      </ul>
      <div v-if="approverNames.length" class="checklist__route">
        <span class="lbl">Approval route</span>
        <span class="text-body-2">{{ approverNames.join(' → ') }}</span>
      </div>
    </div>
  </v-card>
</template>

<style scoped>
.checklist__body {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.checklist__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.checklist__row {
  display: flex;
  gap: 10px;
  align-items: flex-start;
  font-size: 13px;
  line-height: 1.45;
}
.checklist__icon {
  width: 18px;
  height: 18px;
  flex: none;
}
.checklist__route {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding-top: 8px;
  border-top: 1px solid var(--line);
}
</style>
