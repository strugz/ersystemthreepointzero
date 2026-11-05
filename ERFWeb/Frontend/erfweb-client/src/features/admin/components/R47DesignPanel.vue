<script setup lang="ts">
// Exact values of the R47 layout, one expansion panel per section of the form. The parent owns the
// design; every edit is emitted whole. Focusing a field records an undo step first.
import { computed } from 'vue';
import type { R47Design } from '../types';
import {
  GRID_COLUMN_NAMES,
  RANGES,
  SECTION_FIELDS,
  SECTION_TITLES,
  contentWidth,
  withSize,
  type R47Section,
  type R47SizeKey,
} from '../r47Design';

const props = defineProps<{
  modelValue: R47Design;
  section: R47Section | null;
  /** Server validation errors keyed r47Design.<field>. */
  errors: Record<string, string[]> | null;
  disabled?: boolean;
}>();
const emit = defineEmits<{
  'update:modelValue': [value: R47Design];
  'update:section': [value: R47Section | null];
  checkpoint: [];
}>();

const sections = Object.keys(SECTION_TITLES) as R47Section[];

const d = computed(() => props.modelValue);
const columnSum = computed(() => d.value.gridColumns.reduce((a, b) => a + b, 0));

function fieldErrors(field: string): string[] | undefined {
  return props.errors?.[`r47Design.${field}`];
}

function toNumber(value: string | number): number {
  return typeof value === 'number' ? value : value.trim() === '' ? Number.NaN : Number(value);
}

function setSize(key: R47SizeKey, value: string | number) {
  emit('update:modelValue', withSize(d.value, key, toNumber(value)));
}

function setColumn(i: number, value: string | number) {
  const cols = [...d.value.gridColumns];
  cols[i] = toNumber(value);
  emit('update:modelValue', { ...d.value, gridColumns: cols });
}

function setText(key: 'promiseText' | 'promiseAlign' | 'revolvingFundText' | 'balanceNoteText' | 'labelColor' | 'noteColor', value: string) {
  emit('update:modelValue', { ...d.value, [key]: value });
}

function rangeHint(key: R47SizeKey): string {
  const [min, max] = RANGES[key];
  return `${min} to ${max} pt`;
}
</script>

<template>
  <v-expansion-panels
    :model-value="section"
    variant="accordion"
    :disabled="disabled"
    @update:model-value="(v: unknown) => emit('update:section', (v as R47Section | null) ?? null)"
  >
    <v-expansion-panel v-for="s in sections" :key="s" :value="s" :title="SECTION_TITLES[s]">
      <v-expansion-panel-text>
        <v-row dense>
          <v-col v-for="f in SECTION_FIELDS[s]" :key="f.key" cols="12" sm="6">
            <v-text-field
              :model-value="d[f.key]"
              :label="f.label"
              type="number"
              step="0.5"
              suffix="pt"
              density="compact"
              :hint="rangeHint(f.key)"
              :error-messages="fieldErrors(f.key)"
              @focus="emit('checkpoint')"
              @update:model-value="(v: string | number) => setSize(f.key, v)"
            />
          </v-col>
        </v-row>

        <template v-if="s === 'page'">
          <p class="text-caption text-medium-emphasis mt-1">
            The page is US Letter landscape (792 × 612 pt). A margin change is taken from or given to the PARTICULARS column, so the grid keeps filling the page.
          </p>
        </template>

        <template v-if="s === 'grid'">
          <div class="text-subtitle-2 mt-3 mb-1">Column widths</div>
          <p class="text-caption mb-2" :class="Math.abs(columnSum - contentWidth(d)) > 0.5 ? 'text-error' : 'text-medium-emphasis'">
            {{ Math.round(columnSum * 100) / 100 }} of {{ contentWidth(d) }} pt. Drag a divider in the drawing to move width between two columns.
          </p>
          <v-row dense>
            <v-col v-for="(name, i) in GRID_COLUMN_NAMES" :key="name" cols="6" sm="4">
              <v-text-field
                :model-value="d.gridColumns[i]"
                :label="name"
                type="number"
                step="0.5"
                suffix="pt"
                density="compact"
                hide-details="auto"
                @focus="emit('checkpoint')"
                @update:model-value="(v: string | number) => setColumn(i, v)"
              />
            </v-col>
          </v-row>
          <p v-if="fieldErrors('gridColumns')" class="text-caption text-error mt-1">{{ fieldErrors('gridColumns')!.join(' ') }}</p>
          <p class="text-caption text-medium-emphasis mt-2">
            Page breaks follow these widths: wider PARTICULARS or Invoice/OR columns wrap less, so more lines fit on a page.
          </p>
        </template>

        <template v-if="s === 'totals'">
          <v-text-field
            :model-value="d.revolvingFundText"
            label="Revolving fund line"
            density="compact"
            counter="80"
            :error-messages="fieldErrors('revolvingFundText')"
            @focus="emit('checkpoint')"
            @update:model-value="(v: string) => setText('revolvingFundText', v)"
          />
        </template>

        <template v-if="s === 'cashAdvance'">
          <v-text-field
            :model-value="d.balanceNoteText"
            label="Note after BAL DUE TO"
            density="compact"
            counter="100"
            hint="{ShortName} prints the company short name. May be blank."
            persistent-hint
            :error-messages="fieldErrors('balanceNoteText')"
            @focus="emit('checkpoint')"
            @update:model-value="(v: string) => setText('balanceNoteText', v)"
          />
        </template>

        <template v-if="s === 'footer'">
          <v-textarea
            :model-value="d.promiseText"
            label="Promise (one printed line per line)"
            rows="5"
            auto-grow
            density="compact"
            hint="Up to 6 lines of 70 characters"
            persistent-hint
            :error-messages="fieldErrors('promiseText')"
            @focus="emit('checkpoint')"
            @update:model-value="(v: string) => setText('promiseText', v)"
          />
          <div class="d-flex align-center flex-wrap ga-3 mt-3">
            <span class="text-body-2">Promise alignment</span>
            <v-btn-toggle
              :model-value="d.promiseAlign"
              density="compact"
              variant="outlined"
              divided
              mandatory
              @update:model-value="(v: 'Left' | 'Center') => { emit('checkpoint'); setText('promiseAlign', v); }"
            >
              <v-btn value="Left" prepend-icon="mdi-format-align-left">Left</v-btn>
              <v-btn value="Center" prepend-icon="mdi-format-align-center">Center</v-btn>
            </v-btn-toggle>
          </div>
          <p v-if="fieldErrors('promiseAlign')" class="text-caption text-error mt-1">{{ fieldErrors('promiseAlign')!.join(' ') }}</p>
          <p class="text-caption text-medium-emphasis mt-1">Left is the standard form; Center lines the promise up over the employee's signature.</p>
        </template>

        <template v-if="s === 'type'">
          <v-row dense>
            <v-col cols="12" sm="6">
              <v-text-field
                :model-value="d.labelColor"
                label="Headings and labels colour"
                type="color"
                density="compact"
                :error-messages="fieldErrors('labelColor')"
                @focus="emit('checkpoint')"
                @update:model-value="(v: string) => setText('labelColor', v)"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                :model-value="d.noteColor"
                label="Notes and stamps colour"
                type="color"
                density="compact"
                :error-messages="fieldErrors('noteColor')"
                @focus="emit('checkpoint')"
                @update:model-value="(v: string) => setText('noteColor', v)"
              />
            </v-col>
          </v-row>
          <p class="text-caption text-medium-emphasis">
            The grid stays in Tahoma: page breaks measure text with Tahoma widths. Font families are set on the server.
          </p>
        </template>
      </v-expansion-panel-text>
    </v-expansion-panel>
  </v-expansion-panels>
</template>
