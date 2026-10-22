import { api } from '@/shared/api/client';
import type { ExpenseReportDetail, PagedResult } from '@/features/expenses/types';
import type { ApprovalActionKind, ApprovalActionResult, ApprovalInboxItem, ApprovalInboxQuery } from './types';

const a = (id: string) => `/approvals/${encodeURIComponent(id)}`;

export const approvalsApi = {
  inbox: (query: ApprovalInboxQuery) => {
    const search = new URLSearchParams({ view: query.view, page: String(query.page), pageSize: String(query.pageSize) });
    if (query.search) search.set('search', query.search);
    return api.get<PagedResult<ApprovalInboxItem>>(`/approvals?${search.toString()}`);
  },
  /** The whole inbox view (no paging) as CSV. */
  exportInbox: (view: ApprovalInboxQuery['view'], search?: string) => {
    const params = new URLSearchParams({ view });
    if (search) params.set('search', search);
    return api.getFile(`/approvals/export?${params.toString()}`, `ApprovalInbox_${view}.csv`);
  },
  /** Approve (note optional), or return, reject, override (reason required). rowVersion is the report's. */
  act: (id: string, kind: ApprovalActionKind, note: string | null, rowVersion: number) =>
    api.post<ApprovalActionResult>(`${a(id)}/${kind}`, { note, rowVersion }),
  /** Lowers one line before the first approval. rowVersion is the line's. */
  adjustLine: (id: string, lineId: number, amount: number, reason: string, rowVersion: number) =>
    api.put<ExpenseReportDetail>(`${a(id)}/lines/${lineId}`, { amount, reason, rowVersion }),
};
