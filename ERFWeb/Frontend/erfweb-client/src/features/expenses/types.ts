// Mirrors ERFWeb.Application.Features.Expenses contracts. Enums travel as strings.
import type { VerificationPanel } from '@/features/verification/types';

export type ReportType = 'RevolvingFund' | 'CashAdvanceLiquidation' | 'Reimbursement';
export type ReportStatus = 'New' | 'ForApproval' | 'Returned' | 'Approved' | 'Rejected';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Returned' | 'Superseded' | 'Rejected' | 'Overridden';
export type FinanceStatus = 'Pending' | 'ReceiptsIncomplete' | 'ReceiptsReceived' | 'ClearedForPayment';
export type ExpenseType = 'Local' | 'Foreign';
export type ExpenseCategory = 'Meals' | 'Transportation' | 'Toll' | 'Parking' | 'Others' | 'Representation';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ExpenseReportListItem {
  id: string;
  referenceNo: string | null;
  reportType: ReportType;
  purpose: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  status: ReportStatus;
  totalAmount: number;
  lineCount: number;
  filedAt: string | null;
  updatedAt: string;
}

/** One row of the CASH ADVANCE RECEIVED block. Only a Liquidation of Cash Advance carries them. */
export interface CashAdvanceLine {
  id: number;
  advanceDate: string | null;
  referenceDocument: string | null;
  referenceNo: string | null;
  amount: number;
  rowVersion: number;
}

export interface SaveCashAdvanceLineInput {
  advanceDate: string | null;
  referenceDocument: string | null;
  referenceNo: string | null;
  amount: number;
  rowVersion: number | null;
}

export interface ExpenseMealItem {
  mealType: string;
  paidFor: string | null;
  paidEmployeeId: number | null;
}

export interface ExpenseTransportItem {
  fareFrom: string | null;
  fareTo: string | null;
  fareAmount: number;
  /** How the leg was travelled; null on legs saved before fare modes existed. */
  fareModeId: number | null;
  /** The typed mode when the fare mode asks for it ("Others"). */
  fareModeDetail: string | null;
  /** Filled in by the server on the way out; ignored on save. */
  fareModeCode?: string | null;
  fareModeName?: string | null;
}

/** An active fare mode for the fare-leg picker, in display order. */
export interface FareModeOption {
  id: number;
  code: string;
  name: string;
  requiresDetail: boolean;
  receiptExpected: boolean;
}

export interface ExpenseLine {
  id: number;
  transactionDate: string;
  particulars: string | null;
  workWith: string | null;
  invoiceOrNo: string | null;
  expenseType: ExpenseType;
  category: ExpenseCategory;
  amount: number;
  vatAmount: number;
  totalAmount: number;
  location: string | null;
  serviceNumber: string | null;
  instrument: string | null;
  serialNumber: string | null;
  isPerdiem: boolean;
  multiplier: number;
  minusDays: number | null;
  totalDays: number | null;
  computation: string | null;
  remarks: string | null;
  sortOrder: number;
  rowVersion: number;
  meals: ExpenseMealItem[];
  transports: ExpenseTransportItem[];
  /** Set when an approver lowered the line before the first approval. */
  adjustment?: LineAdjustment | null;
}

export interface LineAdjustment {
  originalAmount: number;
  originalTotal: number;
  adjustedByName: string | null;
  adjustedAt: string;
  reason: string | null;
}

export interface ApprovalStep {
  stepOrder: number;
  approverUserId: number;
  approverName: string;
  status: ApprovalStatus;
  actedAt: string | null;
  note: string | null;
  /** Signatory level (1 or 2) snapshotted when the report was filed. */
  level?: number | null;
  /** Set only when a delegate acted on the signatory's behalf. */
  actedByName?: string | null;
}

/** What the viewer may do as an approver; computed by the same rules the API enforces. */
export interface ReviewActions {
  canApprove: boolean;
  canReturn: boolean;
  canReject: boolean;
  canOverride: boolean;
  canAdjustLines: boolean;
}

export interface FinanceSummary {
  status: FinanceStatus;
  remarks: string | null;
  receivedAt: string | null;
  clearedAt: string | null;
  /** A verification request is unresolved, so the report cannot be cleared yet. */
  underVerification?: boolean;
}

export interface Attachment {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
}

export interface ExpenseReportDetail {
  id: string;
  referenceNo: string | null;
  employeeUserId: number;
  employeeName: string;
  reportType: ReportType;
  purpose: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  status: ReportStatus;
  revolvingFundAmount: number;
  currentCycle: number;
  filedAt: string | null;
  approvedAt: string | null;
  cancelNote: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: number;
  canEdit: boolean;
  canFile: boolean;
  canReopen: boolean;
  canDelete: boolean;
  totalAmount: number;
  cashAdvanceTotal: number;
  cashAdvances: CashAdvanceLine[];
  lines: ExpenseLine[];
  approvalSteps: ApprovalStep[];
  attachments: Attachment[];
  review?: ReviewActions | null;
  finance?: FinanceSummary | null;
  /** Present when there are requests or the viewer (Finance) may raise one. */
  verification?: VerificationPanel | null;
}

export interface FileReportResult {
  reportId: string;
  status: ReportStatus;
  cycle: number;
  nextApproverName: string | null;
}

export interface ReportHeaderInput {
  reportType: ReportType;
  purpose: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  revolvingFundAmount: number;
}

export interface SaveExpenseLineInput {
  transactionDate: string;
  particulars: string | null;
  workWith: string | null;
  invoiceOrNo: string | null;
  expenseType: ExpenseType;
  category: ExpenseCategory;
  amount: number;
  vatAmount: number;
  location: string | null;
  serviceNumber: string | null;
  instrument: string | null;
  serialNumber: string | null;
  isPerdiem: boolean;
  multiplier: number;
  minusDays: number | null;
  totalDays: number | null;
  remarks: string | null;
  meals: ExpenseMealItem[];
  transports: ExpenseTransportItem[];
  rowVersion: number | null;
}

export interface ReportListQuery {
  page: number;
  pageSize: number;
  sort?: string;
  direction?: 'Asc' | 'Desc';
  status?: ReportStatus;
  search?: string;
}

export interface WorkWithOption {
  userId: number;
  fullName: string;
  position: string | null;
  department: string | null;
}

export interface EmployeeRates {
  transportRate: number;
  breakfastRate: number;
  lunchRate: number;
  dinnerRate: number;
  otMealRate: number;
}

export interface FilingReadiness {
  hasSignature: boolean;
  approverCount: number;
  approverNames: string[];
  /** False while a newly created account is still inside the configured cooling period. */
  coolingPeriodElapsed: boolean;
  /** ISO timestamp when the cooling period ends; null once elapsed or when none applies. */
  coolingPeriodEndsAt: string | null;
  /** Why no approval route resolves (missing or ambiguous scope); null when routing is fine. */
  routingProblem: string | null;
}
