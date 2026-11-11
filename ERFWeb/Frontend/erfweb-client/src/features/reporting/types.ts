// Mirrors ERFWeb.Application.Features.Reporting (ExportContracts). Enums travel as strings.
import type { ExpenseCategory } from '@/features/expenses/types';

/** Whose reports a summary covers: your own, the ones you sign for, or (Finance) everyone's. */
export type SummaryScope = 'Mine' | 'Approvals' | 'All';

export interface ExpenseSummaryQuery {
  /** DateOnly strings; lines are picked by expense date. */
  from: string;
  to: string;
  scope: SummaryScope;
  departmentId?: number | null;
  approvedOnly: boolean;
}

export interface SummaryEmployeeTotals {
  employeeUserId: number;
  employeeName: string;
  departmentName: string | null;
  lineCount: number;
  byCategory: Record<ExpenseCategory, number>;
  total: number;
}

export interface ExpenseSummary {
  from: string;
  to: string;
  scope: SummaryScope;
  departmentName: string | null;
  approvedOnly: boolean;
  lineCount: number;
  employees: SummaryEmployeeTotals[];
  byCategory: Record<ExpenseCategory, number>;
  total: number;
}

// Mirrors ERFWeb.Application.Features.Reporting.BudgetContracts (cost center budgets).

/** One employee's share of a cost center's year. */
export interface BudgetEmployeeTotals {
  employeeUserId: number;
  employeeName: string;
  lineCount: number;
  byCategory: Record<ExpenseCategory, number>;
  total: number;
}

/** One cost center's year. Remaining and percentUsed are null while no budget is set (percentUsed also for a zero budget). */
export interface CostCenterBudgetRow {
  costCenterId: number;
  code: string;
  name: string;
  departmentId: number;
  departmentName: string;
  budget: number | null;
  used: number;
  remaining: number | null;
  percentUsed: number | null;
  overBudget: boolean;
  lineCount: number;
  byCategory: Record<ExpenseCategory, number>;
  /** Largest spender first. */
  employees: BudgetEmployeeTotals[];
}

export interface CostCenterBudgetReport {
  year: number;
  approvedOnly: boolean;
  generatedAt: string;
  costCenters: CostCenterBudgetRow[];
  totalBudget: number;
  totalUsed: number;
}

// Mirrors ERFWeb.Application.Features.Notifications.EmailContracts (report email).

export interface ReportEmailSent {
  status: 'Sent';
  recipientCount: number;
  /** True when a retry found the email already sent and nothing new went out. */
  replayed: boolean;
}

/** One email about a report (manual "Email R47" or a workflow event). Never the addresses or text. */
export interface ReportEmailDelivery {
  id: number;
  /** "R47" for a manual send, otherwise the workflow event (e.g. ReportApproved). */
  kind: string;
  status: 'Sending' | 'Sent' | 'Failed' | 'Unconfirmed';
  recipientCount: number;
  sentByName: string;
  createdAt: string;
  completedAt: string | null;
}

/** Daily meal claims (Finance only): Meals lines by expense date; filed and approved reports. */
export interface DailyMealClaimQuery {
  from: string;
  to: string;
  departmentId?: number | null;
  approvedOnly: boolean;
}

/** One employee's meals on one day, across their reports. "Other": a meal line with no or an unknown meal type. */
export interface MealClaimRow {
  date: string;
  employeeUserId: number;
  employeeName: string;
  departmentName: string | null;
  referenceNos: string[];
  breakfast: boolean;
  lunch: boolean;
  dinner: boolean;
  otMeal: boolean;
  other: boolean;
  lineCount: number;
  amount: number;
}

/** One day: how many employees claimed each meal, and who. */
export interface MealClaimDay {
  date: string;
  employeeCount: number;
  breakfast: number;
  lunch: number;
  dinner: number;
  otMeal: number;
  other: number;
  amount: number;
  employees: MealClaimRow[];
}

export interface DailyMealClaimReport {
  from: string;
  to: string;
  departmentName: string | null;
  approvedOnly: boolean;
  days: MealClaimDay[];
  /** Employee-days with a meal claim. */
  employeeDays: number;
  total: number;
}
