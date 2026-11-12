import { computed } from 'vue';
import { useDisplay } from 'vuetify';

/**
 * Tablets (600-959px) use the phone layout: bottom bar, phone header, row lists, full-screen dialogs. This switch
 * is the tablet's own setting; tablet-only tweaks sit in `tablet` checks and 600-959px media queries, so changing
 * them never changes phones (`phone`). Desktop (960px and up) never gets `compact`.
 */
const TABLET_PHONE_LAYOUT = true;

export function useLayoutMode() {
  const { xs, sm } = useDisplay();
  const tablet = computed(() => TABLET_PHONE_LAYOUT && sm.value);
  /** Phone layout: phones always, tablets by the switch above. */
  const compact = computed(() => xs.value || tablet.value);
  return { compact, phone: xs, tablet };
}
