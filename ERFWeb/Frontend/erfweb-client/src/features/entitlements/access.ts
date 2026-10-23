import type { UserRole } from '@/features/auth/types';

// What the UI shows. The API is the authority: only a System Administrator changes scope and
// rates, never a rate assignment naming themselves. Its 403 reason is always surfaced to the user.

/** Roles that may open the scope and rate screens. */
export const entitlementReaderRoles: UserRole[] = [
  'SignatoryLevel1',
  'SignatoryLevel2',
  'Finance',
  'SystemAdministrator',
];

/** Roles that see Rates in the nav. */
export const rateNavRoles: UserRole[] = ['SignatoryLevel1', 'SignatoryLevel2', 'Finance', 'SystemAdministrator'];

export const scopeWriterRoles: UserRole[] = ['SystemAdministrator'];
export const rateWriterRoles: UserRole[] = ['SystemAdministrator'];

export function hasAnyRole(roles: readonly UserRole[] | undefined, allowed: readonly UserRole[]): boolean {
  return !!roles && roles.some((r) => allowed.includes(r));
}
