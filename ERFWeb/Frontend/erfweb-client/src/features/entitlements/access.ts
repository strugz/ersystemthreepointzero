import type { UserRole } from '@/features/auth/types';

// What the UI shows. The API is the authority: it further restricts who may edit which scope
// (Level 1 by L2 or Owner; Level 2 by Owner, or the bootstrap exception) and refuses changes that
// apply to the actor. Its 403 reason is always surfaced to the user.

/** Roles that may open the scope and rate screens. */
export const entitlementReaderRoles: UserRole[] = [
  'SignatoryLevel1',
  'SignatoryLevel2',
  'BusinessOwner',
  'Finance',
  'SystemAdministrator',
];

/** Roles that may read rates; a System Administrator only needs scope, so Rates is hidden from them in the nav. */
export const rateNavRoles: UserRole[] = ['SignatoryLevel1', 'SignatoryLevel2', 'BusinessOwner', 'Finance'];

export const scopeWriterRoles: UserRole[] = ['SignatoryLevel2', 'BusinessOwner', 'SystemAdministrator'];
export const rateWriterRoles: UserRole[] = ['SignatoryLevel2', 'BusinessOwner'];

export function hasAnyRole(roles: readonly UserRole[] | undefined, allowed: readonly UserRole[]): boolean {
  return !!roles && roles.some((r) => allowed.includes(r));
}
