import { readonly, ref } from 'vue';
import { ApiError } from '@/shared/api/client';

export type SnackKind = 'success' | 'error' | 'info';

export interface SnackMessage {
  id: number;
  kind: SnackKind;
  text: string;
}

// Module-level state so any view can notify and the layout renders the toast.
const queue = ref<SnackMessage[]>([]);
let nextId = 1;

function push(kind: SnackKind, text: string) {
  queue.value.push({ id: nextId++, kind, text });
}

function dismiss(id: number) {
  queue.value = queue.value.filter((m) => m.id !== id);
}

/** Turns an API failure into a short, human message (first validation error wins). */
export function describeError(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (err instanceof ApiError) {
    const errors = err.problem?.errors;
    if (errors) {
      const first = Object.values(errors).flat()[0];
      if (first) {
        return first;
      }
    }
    if (err.status === 409) {
      return err.message || 'This record changed elsewhere. Reload and try again.';
    }
    if (err.status === 403) {
      return 'You are not allowed to do that.';
    }
    return err.message || fallback;
  }
  return fallback;
}

export function useSnackbar() {
  return {
    messages: readonly(queue),
    success: (text: string) => push('success', text),
    error: (text: string) => push('error', text),
    info: (text: string) => push('info', text),
    fromError: (err: unknown, fallback?: string) => push('error', describeError(err, fallback)),
    dismiss,
  };
}
