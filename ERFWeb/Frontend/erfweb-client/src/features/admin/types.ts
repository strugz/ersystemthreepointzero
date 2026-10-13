import type { UserRole } from '@/features/auth/types';

export type RoleGrantStatus = 'PendingActivation' | 'Active' | 'Revoked';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ---- users ------------------------------------------------------------------

export type UserSort = 'fullname' | 'username' | 'department' | 'created';
export type SortDirection = 'Asc' | 'Desc';

export interface UserListQuery {
  page: number;
  pageSize: number;
  sort?: UserSort;
  direction?: SortDirection;
  search?: string;
  departmentId?: number;
  role?: UserRole;
  isActive?: boolean;
}

export interface UserListItem {
  id: number;
  username: string;
  fullName: string;
  position: string | null;
  jobGrade: string | null;
  email: string | null;
  mobileNo: string | null;
  departmentId: number | null;
  departmentName: string | null;
  isActive: boolean;
  hasSignature: boolean;
  workWithEnabled: boolean;
  createdAt: string;
  activeRoles: UserRole[];
  pendingRoles: UserRole[];
}

export interface RoleGrant {
  role: UserRole;
  status: RoleGrantStatus;
  grantedByUserId: number | null;
  grantedByName: string | null;
  grantedAt: string | null;
  countersignedByUserId: number | null;
  countersignedByName: string | null;
  countersignedAt: string | null;
  revokedByUserId: number | null;
  revokedByName: string | null;
  revokedAt: string | null;
  isBootstrap: boolean;
  note: string | null;
  rowVersion: number;
}

export interface UserDetail {
  id: number;
  username: string;
  fullName: string;
  position: string | null;
  jobGrade: string | null;
  email: string | null;
  mobileNo: string | null;
  departmentId: number | null;
  departmentName: string | null;
  costCenterId: number | null;
  isActive: boolean;
  hasSignature: boolean;
  workWithEnabled: boolean;
  createdAt: string;
  coolingPeriodElapsed: boolean;
  roleGrants: RoleGrant[];
  rowVersion: number;
}

/** Profile fields shared by create and edit. */
export interface UserProfileInput {
  fullName: string;
  position: string | null;
  jobGrade: string | null;
  email: string | null;
  mobileNo: string | null;
  departmentId: number | null;
  costCenterId: number | null;
  workWithEnabled: boolean;
}

export interface CreateUserInput extends UserProfileInput {
  username: string;
  password: string;
}

export interface UpdateUserInput extends UserProfileInput {
  rowVersion: number;
}

export interface SetActiveInput {
  isActive: boolean;
  rowVersion: number;
}

export interface GrantRoleInput {
  role: UserRole;
  note?: string | null;
}

export interface RoleActionInput {
  note?: string | null;
  rowVersion: number;
}

// ---- master data --------------------------------------------------------------

export interface Department {
  id: number;
  code: string | null;
  name: string;
  isActive: boolean;
  rowVersion: number;
}

export interface DepartmentInput {
  code: string | null;
  name: string;
  isActive: boolean;
  rowVersion?: number;
}

export interface CostCenter {
  id: number;
  code: string;
  name: string;
  departmentId: number | null;
  departmentName: string | null;
  isActive: boolean;
  rowVersion: number;
}

export interface CostCenterInput {
  code: string;
  name: string;
  departmentId: number | null;
  isActive: boolean;
  rowVersion?: number;
}

export interface Currency {
  code: string;
  name: string;
  isDefault: boolean;
  isActive: boolean;
  rowVersion: number;
}

export interface CurrencyInput {
  code: string;
  name: string;
  isDefault: boolean;
  isActive: boolean;
  rowVersion?: number;
}

export interface Site {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  rowVersion: number;
}

export interface SiteInput {
  code: string;
  name: string;
  isActive: boolean;
  rowVersion?: number;
}

export interface ExpenseCategoryInfo {
  category: string;
  label: string;
  r47Column: string;
}

// ---- settings -----------------------------------------------------------------

export interface SystemSettingsInput {
  level2ThresholdAmount: number;
  level2AlwaysRequired: boolean;
  slaDaysLevel1: number;
  slaDaysLevel2: number;
  escalationAfterDays: number;
  filingCutoffDays: number;
  receiptRequiredAboveAmount: number;
  coolingPeriodDays: number;
  emailNotificationsEnabled: boolean;
  smsNotificationsEnabled: boolean;
  notificationFromAddress: string | null;
}

export interface SystemSettings extends SystemSettingsInput {
  updatedAt: string | null;
  updatedByName: string | null;
}

// ---- audit --------------------------------------------------------------------

export interface AuditQuery {
  page: number;
  pageSize: number;
  direction?: SortDirection;
  event?: string;
  actorUserId?: number;
  /** ISO date (yyyy-MM-dd) bounds, inclusive. */
  from?: string;
  to?: string;
  administrationOnly?: boolean;
}

export interface AuditEntry {
  id: number;
  event: string;
  actorUserId: number | null;
  actorName: string | null;
  reportId: string | null;
  occurredAt: string;
  /** JSON document serialized as a string. */
  detail: string | null;
  correlationId: string | null;
}
