<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { ApiError } from '@/shared/api/client';
import { describeError } from '@/shared/composables/useSnackbar';
import { adminApi } from '../api';
import type { CrmEmployee } from '../types';

/**
 * Links an account to its CRM employee. The admin searches the CRM (through the ERFWeb server) by name,
 * initials or employee ID and picks one person; only the employeeId is stored. Names are never matched
 * automatically: two employees can share a name. When the CRM is down the admin can still save unlinked.
 */
const props = withDefaults(
  defineProps<{
    modelValue: string | null;
    disabled?: boolean;
    errorMessages?: string[];
  }>(),
  { disabled: false, errorMessages: undefined },
);

const emit = defineEmits<{
  'update:modelValue': [value: string | null];
  /** The picked CRM person, so the form can offer their name. */
  picked: [employee: CrmEmployee];
}>();

const MIN_SEARCH = 2;
const DEBOUNCE_MS = 350;

const search = ref('');
const includeInactive = ref(false);
const results = ref<CrmEmployee[]>([]);
const loading = ref(false);
const lookupError = ref<string | null>(null);
/** The linked person's details, when the CRM could tell us. */
const selected = ref<CrmEmployee | null>(null);

let timer: ReturnType<typeof setTimeout> | undefined;
let sequence = 0;

function label(e: CrmEmployee): string {
  const name = e.fullName?.trim() || [e.firstName, e.lastName].filter(Boolean).join(' ') || e.employeeId;
  return e.nickname ? `${name} (${e.nickname})` : name;
}

function detail(e: CrmEmployee): string {
  return [e.employeeId, e.department, e.section, e.isActive ? null : 'Left the company'].filter(Boolean).join(' · ');
}

const items = computed(() => {
  const list = [...results.value];
  if (selected.value && !list.some((e) => e.employeeId === selected.value?.employeeId)) list.unshift(selected.value);
  return list.map((e) => ({ value: e.employeeId, title: label(e), subtitle: detail(e), raw: e }));
});

// An ID loaded with the account: show who it is. A CRM that cannot answer just leaves the bare ID.
watch(
  () => props.modelValue,
  async (id) => {
    if (!id) {
      selected.value = null;
      return;
    }
    if (selected.value?.employeeId === id) return;
    selected.value = { employeeId: id, firstName: null, middleName: null, lastName: null, nickname: null, fullName: null, department: null, section: null, workflows: [], isActive: true };
    try {
      const employee = await adminApi.getCrmEmployee(id);
      if (props.modelValue === id) selected.value = employee;
    } catch (err) {
      if (props.modelValue === id && err instanceof ApiError && err.status === 404) {
        lookupError.value = 'The CRM no longer has this employee ID. Search and pick the person again.';
      }
    }
  },
  { immediate: true },
);

async function runSearch(term: string) {
  const mine = ++sequence;
  loading.value = true;
  lookupError.value = null;
  try {
    const page = await adminApi.searchCrmEmployees(term, includeInactive.value);
    if (mine === sequence) results.value = page.items;
  } catch (err) {
    if (mine === sequence) {
      results.value = [];
      lookupError.value = describeError(err, 'The CRM could not be searched.');
    }
  } finally {
    if (mine === sequence) loading.value = false;
  }
}

function schedule() {
  clearTimeout(timer);
  const term = search.value.trim();
  // Typing the selected item's label back into the box is not a new search.
  if (term.length < MIN_SEARCH || (selected.value && term === label(selected.value))) {
    sequence++;
    loading.value = false;
    return;
  }
  timer = setTimeout(() => void runSearch(term), DEBOUNCE_MS);
}

watch(search, schedule);
watch(includeInactive, () => {
  results.value = [];
  schedule();
});
onBeforeUnmount(() => clearTimeout(timer));

function onSelect(id: string | null) {
  const employee = results.value.find((e) => e.employeeId === id) ?? null;
  selected.value = employee;
  lookupError.value = null;
  emit('update:modelValue', id);
  if (employee) emit('picked', employee);
}

const messages = computed(() => props.errorMessages ?? (lookupError.value ? [lookupError.value] : undefined));
const noDataText = computed(() =>
  search.value.trim().length < MIN_SEARCH ? `Type at least ${MIN_SEARCH} characters` : loading.value ? 'Searching the CRM…' : 'No CRM employee matches',
);
</script>

<template>
  <div>
    <v-autocomplete
      v-model:search="search"
      :model-value="modelValue"
      :items="items"
      item-props
      :loading="loading"
      :disabled="disabled"
      label="CRM employee"
      placeholder="Search by name, initials or employee ID"
      prepend-inner-icon="mdi-magnify"
      no-filter
      clearable
      :no-data-text="noDataText"
      hint="Links the account to the CRM so the employee's CRM activities can be claimed. Optional."
      persistent-hint
      :error-messages="messages"
      @update:model-value="onSelect"
    />
    <v-checkbox
      v-model="includeInactive"
      :disabled="disabled"
      density="compact"
      hide-details
      label="Include people who have left the company"
      class="mt-1"
    />
  </div>
</template>
