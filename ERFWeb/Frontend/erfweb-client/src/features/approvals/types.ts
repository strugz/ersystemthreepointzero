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
  /** Set when the step is a signatory's I stand in for (pending) or acted on for them (acted). */
  onBehalfOfName?: string | null;
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

/** One CRM activity on a day of the claimant's calendar (GET /reports/{id}/crm-calendar). */
export interface CrmCalendarEntry {
  activityId: string;
  activityType: string | null;
  status: string | null;
  completed: boolean;
  startAt: string | null;
  endAt: string | null;
  subject: string | null;
  serviceNumber: string | null;
  customerName: string | null;
  instrumentName: string | null;
  serialNumber: string | null;
  role: string;
  workWith: string | null;
  claimedOnThisReport: boolean;
  /** A whole-day leave or other activity from the CRM calendar (VL, SL, Holiday, ...); never claimable. */
  isLeave: boolean;
}

export interface CrmCalendarDay {
  date: string;
  expenseLineCount: number;
  entries: CrmCalendarEntry[];
  /** The CRM has the claimant on vacation leave, sick leave or a holiday that day. */
  timeOff: boolean;
}

/** The claimant's CRM calendar over the report period; linked is false when the account has no CRM link. */
export interface CrmCalendar {
  linked: boolean;
  employeeName: string;
  dateFrom: string | null;
  dateTo: string | null;
  days: CrmCalendarDay[];
}
