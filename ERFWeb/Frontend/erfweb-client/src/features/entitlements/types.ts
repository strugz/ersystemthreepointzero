import type { ExpenseCategory } from '@/features/expenses/types';

// Dates are the API's DateOnly strings (yyyy-MM-dd); timestamps are ISO strings; money is a number.

// ---- lookups ------------------------------------------------------------------

/** An active department from GET /api/lookups/departments (any signed-in user). */
export interface DepartmentOption {
  id: number;
  code: string | null;
  name: string;
}

/** A person selectable in pickers, derived from the coverage list (every active employee). */
export interface PersonOption {
  userId: number;
  fullName: string;
  departmentName: string | null;
}

// ---- scope ----------------------------------------------------------------------

export type ScopeLevel = 1 | 2;
export type ScopeAssignmentType = 'Department' | 'Individual' | 'Exclusion';

export interface ScopeMember {
  userId: number;
  fullName: string;
}

export interface ScopeAssignment {
  id: number;
  holderUserId: number;
  holderName: string;
  level: ScopeLevel;
  type: ScopeAssignmentType;
  departmentId: number | null;
  departmentName: string | null;
  members: ScopeMember[];
  approvalLimit: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  isBootstrap: boolean;
  createdByName: string | null;
  createdAt: string;
  note: string | null;
  rowVersion: number;
}

export interface ScopeAssignmentQuery {
  holderUserId?: number;
  departmentId?: number;
  level?: ScopeLevel;
  includeInactive?: boolean;
}

export interface CreateScopeAssignmentInput {
  holderUserId: number;
  level: ScopeLevel;
  type: ScopeAssignmentType;
  departmentId?: number | null;
  memberUserIds?: number[];
  approvalLimit?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  note?: string | null;
}

/** Holder, level, type and department are fixed; to change them, end the assignment and create another. */
export interface UpdateScopeAssignmentInput {
  memberUserIds?: number[];
  approvalLimit?: number | null;
  effectiveTo?: string | null;
  isActive: boolean;
  note?: string | null;
  rowVersion: number;
}

export interface CoverageRow {
  userId: number;
  fullName: string;
  departmentName: string | null;
  level1UserId: number | null;
  level1Name: string | null;
  /** Human-readable reason when Level 1 is unresolved or ambiguous. */
  level1Problem: string | null;
  level2UserId: number | null;
  level2Name: string | null;
  level2Problem: string | null;
}

// ---- rates ----------------------------------------------------------------------

export type RateType = 'FixedPerDay' | 'PerKm' | 'CapPerTransaction' | 'ActualWithCeiling';
export type RateTargetType = 'Individual' | 'Grade' | 'Department' | 'Company';
export type MealSubCategory = 'Breakfast' | 'Lunch' | 'Dinner' | 'OTMeal';
export type RateCategory = ExpenseCategory;

export interface RateLine {
  id: number;
  profileId: number;
  category: RateCategory;
  subCategory: MealSubCategory | null;
  rateType: RateType;
  amount: number;
  currency: string;
  frequencyCap: number | null;
  requiresReceipt: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  changedByName: string | null;
  changeReason: string | null;
  backdateOverride: boolean;
  createdAt: string;
}

export interface RateAssignment {
  id: number;
  profileId: number;
  profileName: string;
  targetType: RateTargetType;
  userId: number | null;
  userName: string | null;
  jobGrade: string | null;
  departmentId: number | null;
  departmentName: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  assignedByName: string | null;
  assignedAt: string;
  isBootstrap: boolean;
  changeReason: string | null;
  rowVersion: number;
}

export interface RateProfileListItem {
  id: number;
  name: string;
  isActive: boolean;
  currentLineCount: number;
  activeAssignmentCount: number;
  rowVersion: number;
}

export interface RateProfileDetail {
  id: number;
  name: string;
  isActive: boolean;
  /** Every version, newest first per category/sub-category key. */
  lines: RateLine[];
  assignments: RateAssignment[];
  rowVersion: number;
}

export interface UpdateRateProfileInput {
  name: string;
  isActive: boolean;
  rowVersion: number;
}

export interface AddRateLineInput {
  category: RateCategory;
  /** Required for Meals, forbidden otherwise. */
  subCategory: MealSubCategory | null;
  rateType: RateType;
  amount: number;
  currency: string | null;
  frequencyCap: number | null;
  requiresReceipt: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  changeReason: string | null;
  backdateOverride: boolean;
}

export interface RateAssignmentQuery {
  profileId?: number;
  targetType?: RateTargetType;
  includeEnded?: boolean;
}

export interface CreateRateAssignmentInput {
  profileId: number;
  targetType: RateTargetType;
  userId?: number | null;
  jobGrade?: string | null;
  departmentId?: number | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  changeReason?: string | null;
  backdateOverride: boolean;
}

export interface EndRateAssignmentInput {
  effectiveTo: string;
  changeReason?: string | null;
  rowVersion: number;
}

export interface ResolvedRates {
  userId: number;
  date: string;
  profileId: number | null;
  profileName: string | null;
  via: RateTargetType | null;
  lines: RateLine[];
}
