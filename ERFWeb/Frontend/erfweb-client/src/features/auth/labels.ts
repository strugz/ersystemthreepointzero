import type { UserRole } from './types';

export const roleLabels: Record<UserRole, string> = {
  Employee: 'Employee',
  SignatoryLevel1: 'Signatory Level 1',
  SignatoryLevel2: 'Signatory Level 2',
  Finance: 'Finance',
  ExpenseVerifier: 'Expense Verifier',
  SystemAdministrator: 'System Administrator',
  BusinessOwner: 'Business Owner',
};

/** Human label for a role enum value; an unknown value (newer server) falls back to the raw name. */
export function roleLabel(role: UserRole | string): string {
  return roleLabels[role as UserRole] ?? role;
}

/** Roles an administrator can grant. Employee is implicit for every account and never granted. */
export const grantableRoles: UserRole[] = [
  'SignatoryLevel1',
  'SignatoryLevel2',
  'Finance',
  'ExpenseVerifier',
  'SystemAdministrator',
  'BusinessOwner',
];

export const roleOptions = (Object.keys(roleLabels) as UserRole[]).map((value) => ({ value, title: roleLabels[value] }));
