import type { CoverageRow, PersonOption } from './types';

export type FieldErrors = Record<string, string[]> | null | undefined;

/** Server ProblemDetails `errors` for one field, accepting camelCase or PascalCase keys. */
export function fieldErrors(errors: FieldErrors, field: string): string[] | undefined {
  if (!errors) return undefined;
  const pascal = field.charAt(0).toUpperCase() + field.slice(1);
  return errors[field] ?? errors[pascal];
}

/**
 * People pickers are built from the coverage list (every active employee), because the user
 * directory is System Administrator-only. Role eligibility (e.g. who may hold scope) is left to
 * the server, which answers with errors.holderUserId.
 */
export function peopleFromCoverage(rows: CoverageRow[]): PersonOption[] {
  return rows
    .map((r) => ({ userId: r.userId, fullName: r.fullName, departmentName: r.departmentName }))
    .sort((a, b) => a.fullName.localeCompare(b.fullName));
}

export function personItems(people: PersonOption[]) {
  return people.map((p) => ({ value: p.userId, title: p.fullName, subtitle: p.departmentName ?? undefined }));
}

/** Empty / non-numeric input -> null; otherwise the number. */
export function numberOrNull(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

export function textOrNull(value: string | null | undefined): string | null {
  const t = (value ?? '').trim();
  return t ? t : null;
}
