// Mirrors the Finance contracts in ERFWeb.Application.Features.Approvals. Enums travel as strings.
import type { FinanceStatus, ReportType } from '@/features/expenses/types';

export interface FinanceQueueItem {
  reportId: string;
  referenceNo: string | null;
  employeeUserId: number;
  employeeName: string;
  departmentName: string | null;
  reportType: ReportType;
  purpose: string | null;
  totalAmount: number;
  approvedAt: string | null;
  status: FinanceStatus;
  remarks: string | null;
  receivedByName: string | null;
  receivedAt: string | null;
  clearedByName: string | null;
  clearedAt: string | null;
  followUpCount: number;
  lastFollowUpAt: string | null;
  attachmentCount: number;
  daysSinceApproval: number;
  /** Finance never processes its own report; the API refuses it too. */
  isOwnReport: boolean;
  rowVersion: number;
  /** Unresolved verification requests; clearance waits until there are none. */
  openVerifications: number;
  /** A Not verified finding waits for Finance to query Level 2. */
  needsQuery: boolean;
}

export interface FinanceQueueQuery {
  page: number;
  pageSize: number;
  status?: FinanceStatus;
  search?: string;
  /** Leave out reports already cleared for payment (the default). */
  openOnly: boolean;
}

/** The receipt statuses Finance records; clearing for payment is its own action. */
export type ReceiptStatus = Exclude<FinanceStatus, 'ClearedForPayment'>;

export type AgingBucket = 'Days0To7' | 'Days8To15' | 'Days16To30' | 'Days31To60' | 'Over60Days';

export interface FinanceAgingBucket {
  bucket: AgingBucket;
  label: string;
  count: number;
  amount: number;
}

/** Open queue items (not cleared for payment) by days since final approval. */
export interface FinanceAging {
  asOf: string;
  buckets: FinanceAgingBucket[];
  count: number;
  amount: number;
  items: FinanceQueueItem[];
}
