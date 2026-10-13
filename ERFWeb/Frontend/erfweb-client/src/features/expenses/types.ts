// Mirrors ERFWeb.Application.Features.Expenses contracts. Enums travel as strings.

export type ReportType = 'RevolvingFund' | 'CashAdvanceLiquidation' | 'Reimbursement';
export type ReportStatus = 'New' | 'ForApproval' | 'Returned' | 'Approved';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Returned' | 'Superseded';
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
}

export interface ApprovalStep {
  stepOrder: number;
  approverUserId: number;
  approverName: string;
  status: ApprovalStatus;
  actedAt: string | null;
  note: string | null;
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
