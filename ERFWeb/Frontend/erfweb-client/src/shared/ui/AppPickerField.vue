<script setup lang="ts" generic="T extends string | string[] | null">
// A field that opens a pick list in a pop-up (full screen on phones and tablets) instead of a dropdown, which is hard
// to hit on a phone. One choice closes the pop-up; with `multiple` the choices are ticked and confirmed with Done.
// The search box also offers what was typed as an entry of its own when `allowCustom` is on.
import { computed, ref } from 'vue';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';

const props = withDefaults(
  defineProps<{
    /** A string for one choice; a list with `multiple`. */
    modelValue: T;
    items: string[];
    label: string;
    /** Pop-up heading; the label when not given. */
    title?: string;
    hint?: string;
    placeholder?: string;
    multiple?: boolean;
    allowCustom?: boolean;
    errorMessages?: string[];
  }>(),
  { title: undefined, hint: undefined, placeholder: undefined, multiple: false, allowCustom: true, errorMessages: undefined },
);

const emit = defineEmits<{ 'update:modelValue': [value: T] }>();

const { compact } = useLayoutMode();
const open = ref(false);
const search = ref('');
const draft = ref<string[]>([]);

const selected = computed<string[]>(() => {
  const v = props.modelValue;
  if (Array.isArray(v)) return v;
  return v ? [v] : [];
});
const display = computed(() => selected.value.join(', '));

const term = computed(() => search.value.trim());
const choices = computed(() => {
  // Ticked entries typed earlier stay listed so they can be unticked.
  const all = props.multiple ? [...new Set([...props.items, ...draft.value])] : props.items;
  const t = term.value.toLowerCase();
  return t ? all.filter((i) => i.toLowerCase().includes(t)) : all;
});
const customOffer = computed(() => {
  if (!props.allowCustom || !term.value) return null;
  const t = term.value.toLowerCase();
  return choices.value.some((c) => c.toLowerCase() === t) ? null : term.value;
});

function show() {
  search.value = '';
  draft.value = [...selected.value];
  open.value = true;
}

function pick(value: string) {
  if (props.multiple) {
    draft.value = draft.value.includes(value) ? draft.value.filter((d) => d !== value) : [...draft.value, value];
    search.value = '';
    return;
  }
  emit('update:modelValue', value as T);
  open.value = false;
}

function done() {
  emit('update:modelValue', [...draft.value] as T);
  open.value = false;
}

function clear() {
  if (props.multiple) {
    draft.value = [];
    return;
  }
  emit('update:modelValue', '' as T);
  open.value = false;
}
</script>

<template>
  <v-text-field
    :model-value="display"
    :label="label"
    :placeholder="placeholder"
    :hint="hint"
    :persistent-placeholder="!!placeholder"
    :error-messages="errorMessages"
    readonly
    role="button"
    append-inner-icon="mdi-chevron-right"
    hide-details="auto"
    class="picker-field"
    @click="show"
    @keydown.enter.prevent="show"
    @keydown.space.prevent="show"
  />

  <v-dialog v-model="open" :fullscreen="compact" max-width="480" scrollable>
    <v-card>
      <v-card-title class="d-flex align-center ga-2 pt-4">
        <span class="text-subtitle-1 font-weight-bold flex-grow-1">{{ title ?? label }}</span>
        <v-btn icon="mdi-close" variant="text" size="small" aria-label="Close" @click="open = false" />
      </v-card-title>
      <div class="px-4 pb-2">
        <v-text-field
          v-model="search"
          :placeholder="allowCustom ? 'Search or type your own' : 'Search'"
          prepend-inner-icon="mdi-magnify"
          density="compact"
          clearable
          hide-details
          :autofocus="!compact"
          @keydown.enter.prevent="customOffer ? pick(customOffer) : choices.length === 1 && pick(choices[0])"
        />
      </div>
      <v-divider />
      <v-card-text class="pa-0">
        <v-list density="comfortable" class="py-0">
          <v-list-item v-if="customOffer" prepend-icon="mdi-plus" @click="pick(customOffer)">
            <v-list-item-title>Use “{{ customOffer }}”</v-list-item-title>
          </v-list-item>
          <v-list-item
            v-for="item in choices"
            :key="item"
            :active="!multiple && selected.includes(item)"
            color="primary"
            min-height="52"
            @click="pick(item)"
          >
            <template v-if="multiple" #prepend>
              <v-checkbox-btn :model-value="draft.includes(item)" color="primary" tabindex="-1" @click.stop="pick(item)" />
            </template>
            <v-list-item-title class="picker-item">{{ item }}</v-list-item-title>
            <template v-if="!multiple && selected.includes(item)" #append>
              <v-icon icon="mdi-check" color="primary" />
            </template>
          </v-list-item>
          <v-list-item v-if="!choices.length && !customOffer" disabled>
            <v-list-item-title class="text-medium-emphasis">Nothing matches.</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-card-text>
      <v-divider />
      <v-card-actions class="px-4">
        <v-btn class="press" variant="text" :disabled="multiple ? !draft.length : !selected.length" @click="clear">Clear</v-btn>
        <v-spacer />
        <v-btn v-if="multiple" class="press" color="primary" variant="flat" @click="done">
          Done{{ draft.length ? ` (${draft.length})` : '' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.picker-field :deep(input) {
  cursor: pointer;
}
.picker-item {
  white-space: normal;
}
</style>
