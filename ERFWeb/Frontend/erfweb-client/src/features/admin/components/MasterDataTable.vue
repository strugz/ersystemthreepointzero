<script setup lang="ts" generic="T extends { isActive: boolean }">
import { computed, reactive, ref, watch } from 'vue';
import { useDisplay } from 'vuetify';
import AppEmptyState from '@/shared/ui/AppEmptyState.vue';

/**
 * Generic reference-data list with an add/edit dialog, used for departments, cost centers, currencies
 * and sites. The parent describes columns and form fields and maps the untyped draft back to the
 * typed API input; this component owns only the table, the dialog and server field errors.
 */
export interface MasterDataColumn<Row> {
  key: string;
  label: string;
  mono?: boolean;
  /** Custom cell text; defaults to the raw value. */
  render?: (item: Row) => string;
  hideOnPhone?: boolean;
}

export interface MasterDataField {
  key: string;
  label: string;
  type: 'text' | 'select' | 'switch';
  items?: { value: string | number | null; title: string }[];
  required?: boolean;
  maxlength?: number;
  hint?: string;
  /** Natural keys (currency code) cannot be changed once created. */
  readonlyOnEdit?: boolean;
  /** Uppercase as the user types (codes). */
  upper?: boolean;
}

export type Draft = Record<string, string | number | boolean | null>;

const props = withDefaults(
  defineProps<{
    title: string;
    singular: string;
    items: T[];
    columns: MasterDataColumn<T>[];
    fields: MasterDataField[];
    keyOf: (item: T) => string | number;
    /** Produces the dialog draft from an existing row. */
    toDraft: (item: T) => Draft;
    loading?: boolean;
    error?: string | null;
    saving?: boolean;
    errors?: Record<string, string[]> | null;
    emptyIcon?: string;
  }>(),
  { loading: false, error: null, saving: false, errors: null, emptyIcon: 'mdi-database-outline' },
);

const emit = defineEmits<{
  save: [draft: Draft, existing: T | null];
  retry: [];
  'clear-errors': [];
}>();

const { xs } = useDisplay();

const showInactive = ref(false);
const visible = computed(() => (showInactive.value ? props.items : props.items.filter((i) => i.isActive)));
const inactiveCount = computed(() => props.items.filter((i) => !i.isActive).length);
const visibleColumns = computed(() => props.columns.filter((c) => !(xs.value && c.hideOnPhone)));

function cell(item: T, column: MasterDataColumn<T>): string {
  if (column.render) return column.render(item);
  const value = (item as Record<string, unknown>)[column.key];
  return value === null || value === undefined || value === '' ? '—' : String(value);
}

// ---- dialog ---------------------------------------------------------------------
const open = ref(false);
const editing = ref<T | null>(null);
const draft = reactive<Draft>({});

function blankDraft(): Draft {
  const d: Draft = {};
  for (const f of props.fields) {
    d[f.key] = f.type === 'switch' ? f.key === 'isActive' : f.type === 'select' ? null : '';
  }
  return d;
}

function openCreate() {
  editing.value = null;
  Object.keys(draft).forEach((k) => delete draft[k]);
  Object.assign(draft, blankDraft());
  emit('clear-errors');
  open.value = true;
}

function openEdit(item: T) {
  editing.value = item;
  Object.keys(draft).forEach((k) => delete draft[k]);
  Object.assign(draft, blankDraft(), props.toDraft(item));
  emit('clear-errors');
  open.value = true;
}

/** Parent calls this after a successful save. */
function close() {
  open.value = false;
}
defineExpose({ close, openCreate });

watch(open, (v) => {
  if (!v) editing.value = null;
});

function fieldErrors(field: string): string[] | undefined {
  const errors = props.errors;
  if (!errors) return undefined;
  const pascal = field.charAt(0).toUpperCase() + field.slice(1);
  return errors[field] ?? errors[pascal];
}

const canSave = computed(
  () => !props.saving && props.fields.every((f) => !f.required || f.type !== 'text' || String(draft[f.key] ?? '').trim() !== ''),
);

function onText(field: MasterDataField, value: string) {
  draft[field.key] = field.upper ? value.toUpperCase() : value;
}

function save() {
  if (!canSave.value) return;
  const out: Draft = {};
  for (const f of props.fields) {
    const v = draft[f.key];
    out[f.key] = f.type === 'text' ? String(v ?? '').trim() : (v ?? null);
  }
  emit('save', out, editing.value);
}
</script>

<template>
  <v-card>
    <div class="card-h">
      <div class="d-flex align-baseline ga-3 flex-wrap">
        <span class="card-t">{{ title }}</span>
        <span class="card-meta">{{ visible.length }} of {{ items.length }}</span>
      </div>
      <div class="d-flex align-center ga-2">
        <v-switch
          v-if="inactiveCount > 0"
          v-model="showInactive"
          density="compact"
          hide-details
          color="primary"
          :label="xs ? 'Inactive' : `Show inactive (${inactiveCount})`"
          class="mr-2"
        />
        <v-btn class="press" size="small" variant="outlined" prepend-icon="mdi-plus" :disabled="loading" @click="openCreate">
          {{ xs ? 'Add' : `Add ${singular}` }}
        </v-btn>
      </div>
    </div>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="2" />

    <v-alert v-if="error && !loading" type="error" class="ma-4">
      {{ error }}
      <template #append><v-btn class="press" size="small" variant="text" @click="emit('retry')">Retry</v-btn></template>
    </v-alert>

    <AppEmptyState
      v-else-if="!loading && visible.length === 0"
      :icon="emptyIcon"
      :title="items.length === 0 ? `No ${title.toLowerCase()} yet` : `All ${title.toLowerCase()} are inactive`"
      :hint="items.length === 0 ? `Add the first ${singular}.` : 'Turn on “Show inactive” to see them.'"
    >
      <v-btn v-if="items.length === 0" class="press" color="primary" variant="flat" prepend-icon="mdi-plus" @click="openCreate">Add {{ singular }}</v-btn>
    </AppEmptyState>

    <div v-else-if="!loading" class="table-wrap">
      <table class="ledger">
        <thead>
          <tr>
            <th v-for="c in visibleColumns" :key="c.key">{{ c.label }}</th>
            <th>Status</th>
            <th class="r"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, i) in visible" :key="keyOf(item)" class="row-link stagger-in" :style="{ '--i': Math.min(i, 10) }" tabindex="0" @click="openEdit(item)" @keydown.enter="openEdit(item)">
            <td v-for="c in visibleColumns" :key="c.key" :class="{ mono: c.mono, 'text-medium-emphasis': !item.isActive }">{{ cell(item, c) }}</td>
            <td><span class="pill" :class="item.isActive ? 'pill--success' : 'pill--neutral'">{{ item.isActive ? 'Active' : 'Inactive' }}</span></td>
            <td class="r">
              <v-btn class="press" icon="mdi-pencil-outline" size="x-small" variant="text" :aria-label="`Edit ${singular}`" @click.stop="openEdit(item)" />
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <v-dialog v-model="open" max-width="520" :persistent="saving">
      <v-card>
        <v-card-title class="text-subtitle-1 font-weight-bold">{{ editing ? `Edit ${singular}` : `Add ${singular}` }}</v-card-title>
        <v-card-text>
          <v-form :disabled="saving" @submit.prevent="save">
            <template v-for="(f, idx) in fields" :key="f.key">
              <v-text-field
                v-if="f.type === 'text'"
                :model-value="String(draft[f.key] ?? '')"
                :label="f.label + (f.required ? ' *' : '')"
                :maxlength="f.maxlength"
                :hint="f.hint"
                :persistent-hint="!!f.hint"
                :autofocus="idx === 0"
                :disabled="saving || (!!editing && !!f.readonlyOnEdit)"
                :error-messages="fieldErrors(f.key)"
                @update:model-value="(v: string) => onText(f, v)"
              />
              <v-select
                v-else-if="f.type === 'select'"
                v-model="draft[f.key]"
                :items="f.items ?? []"
                :label="f.label"
                :hint="f.hint"
                :persistent-hint="!!f.hint"
                clearable
                :error-messages="fieldErrors(f.key)"
              />
              <v-switch
                v-else
                v-model="draft[f.key]"
                :label="f.label"
                color="primary"
                :hint="f.hint"
                :persistent-hint="!!f.hint"
                :error-messages="fieldErrors(f.key)"
              />
            </template>
          </v-form>
        </v-card-text>
        <v-divider />
        <v-card-actions class="px-4 py-3">
          <v-spacer />
          <v-btn class="press" variant="text" :disabled="saving" @click="open = false">Cancel</v-btn>
          <v-btn class="press" color="primary" variant="flat" :loading="saving" :disabled="!canSave" @click="save">{{ editing ? 'Save' : 'Add' }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
}
.row-link {
  cursor: pointer;
}
.row-link:focus-visible {
  outline: 2px solid var(--navy);
  outline-offset: -2px;
}
</style>
