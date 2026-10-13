import { api } from '@/shared/api/client';
import type { PagedResult } from '@/features/expenses/types';
import type { FinanceQueueItem, FinanceQueueQuery, ReceiptStatus } from './types';

const f = (id: string) => `/finance/${encodeURIComponent(id)}`;

export const financeApi = {
  queue: (query: FinanceQueueQuery) => {
    const search = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize), openOnly: String(query.openOnly) });
    if (query.status) search.set('status', query.status);
    if (query.search) search.set('search', query.search);
    return api.get<PagedResult<FinanceQueueItem>>(`/finance/queue?${search.toString()}`);
  },
  /** rowVersion is the tracking row's (from the queue item). */
  recordReceipts: (id: string, status: ReceiptStatus, remarks: string | null, rowVersion: number) =>
    api.post<FinanceQueueItem>(`${f(id)}/receipts`, { status, remarks, rowVersion }),
  clear: (id: string, remarks: string | null, rowVersion: number) =>
    api.post<FinanceQueueItem>(`${f(id)}/clear`, { remarks, rowVersion }),
  followUp: (id: string, message: string | null) => api.post<FinanceQueueItem>(`${f(id)}/follow-up`, { message }),
};
