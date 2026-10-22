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
