// Mirrors ERFWeb.Application.Features.Approvals (approver inbox and actions). Enums travel as strings.
import type { ApprovalStatus, ExpenseReportDetail, ReportStatus, ReportType } from '@/features/expenses/types';

export type ApprovalInboxView = 'Pending' | 'Acted';
export type ApprovalActionKind = 'approve' | 'return' | 'reject' | 'override';

export interface ApprovalInboxItem {
  reportId: string;
  referenceNo: string | null;
  employeeUserId: number;
  employeeName: string;
  departmentName: string | null;
  reportType: ReportType;
  purpose: string | null;
  status: ReportStatus;
  totalAmount: number;
  filedAt: string | null;
  cycle: number;
  myStepOrder: number;
  myLevel: number | null;
  myStepStatus: ApprovalStatus;
  myActedAt: string | null;
  currentStepOrder: number | null;
  currentApproverName: string | null;
  /** My step is the first pending one, so I can act now. */
  isMyTurn: boolean;
  /** A Level 2 may approve past a pending Level 1, with a reason. */
  canOverride: boolean;
  daysWaiting: number;
}

export interface ApprovalInboxQuery {
  view: ApprovalInboxView;
  page: number;
  pageSize: number;
  search?: string;
}

export interface ApprovalActionResult {
  reportId: string;
  status: ReportStatus;
  nextApproverName: string | null;
  report: ExpenseReportDetail;
}
