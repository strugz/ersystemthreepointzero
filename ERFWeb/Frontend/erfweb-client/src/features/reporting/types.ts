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
