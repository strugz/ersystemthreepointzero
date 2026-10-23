import type { RoleGrantStatus } from './types';

export const grantStatusLabel: Record<RoleGrantStatus, string> = {
  Active: 'Active',
  Revoked: 'Revoked',
};

/** Tone class from main.css (.pill--*). */
export const grantStatusTone: Record<RoleGrantStatus, string> = {
  Active: 'pill--success',
  Revoked: 'pill--neutral',
};

/** Vuetify color name, for components that take a `color` prop rather than a pill class. */
export const grantStatusColor: Record<RoleGrantStatus, string> = {
  Active: 'success',
  Revoked: 'grey',
};

export const activeOptions: { value: boolean; title: string }[] = [
  { value: true, title: 'Active' },
  { value: false, title: 'Inactive' },
];

export const userSortOptions = [
  { value: 'fullname', title: 'Name' },
  { value: 'username', title: 'Username' },
  { value: 'department', title: 'Department' },
  { value: 'created', title: 'Created' },
] as const;

/** Known audit event names, used to seed the event filter combobox; the server may emit more. */
export const knownAuditEvents = [
  'UserCreated',
  'UserUpdated',
  'UserActivated',
  'UserDeactivated',
  'PasswordReset',
  'SignatureUploaded',
  'RoleGranted',
  'RoleCountersigned',
  'RoleRevoked',
  'SettingsUpdated',
  'MasterDataChanged',
  'ReportFiled',
  'ReportApproved',
  'ReportReturned',
];
