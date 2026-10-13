// Display formatters shared across features. Dependency-free.

const money = new Intl.NumberFormat('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const shortDate = new Intl.DateTimeFormat('en-PH', { year: 'numeric', month: 'short', day: 'numeric' });
const dateTime = new Intl.DateTimeFormat('en-PH', {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
});

/** 1601 -> "1,601.00" (the R47 shows plain amounts, no currency symbol). */
export function formatMoney(value: number | null | undefined): string {
  return money.format(value ?? 0);
}

/** "2026-08-17" (a DateOnly from the API) -> "Aug 17, 2026". */
export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }
  const [y, m, d] = value.split('-').map(Number);
  if (!y || !m || !d) {
    return value;
  }
  return shortDate.format(new Date(y, m - 1, d));
}

/** ISO timestamp -> "Aug 17, 2026, 3:04 PM". */
export function formatDateTime(value: string | null | undefined): string {
  return value ? dateTime.format(new Date(value)) : '—';
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(0)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Today's date as the API's DateOnly string (local calendar day). */
export function todayIso(): string {
  const now = new Date();
  const mm = String(now.getMonth() + 1).padStart(2, '0');
  const dd = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${mm}-${dd}`;
}

/** Shifts a DateOnly string by whole days (negative for earlier), on the local calendar. */
export function addDaysIso(value: string, days: number): string {
  const [y, m, d] = value.split('-').map(Number);
  const date = new Date(y, m - 1, d + days);
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${mm}-${dd}`;
}
