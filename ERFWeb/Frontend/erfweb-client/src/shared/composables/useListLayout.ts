import { computed } from 'vue';
import { useDisplay } from 'vuetify';

/**
 * Tablets (600-959px) show list screens as rows instead of tables. This switch and the `rows--tablet` styles
 * in main.css are the tablet's own settings: changing them never changes the phone layout (always rows) or
 * desktop (always tables).
 */
const TABLET_ROWS = true;

export function useListLayout() {
  const { xs, sm } = useDisplay();
  const tabletRows = computed(() => TABLET_ROWS && sm.value);
  /** Rows instead of a table: phones always, tablets by the switch above. */
  const rowsLayout = computed(() => xs.value || tabletRows.value);
  return { rowsLayout, tabletRows };
}
