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
