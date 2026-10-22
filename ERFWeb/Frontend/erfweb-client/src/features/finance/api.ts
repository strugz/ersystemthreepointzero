import { api } from '@/shared/api/client';
import type { PagedResult } from '@/features/expenses/types';
import type { ExportFormat } from '@/shared/download';
import type { FinanceAging, FinanceQueueItem, FinanceQueueQuery, ReceiptStatus } from './types';

const f = (id: string) => `/finance/${encodeURIComponent(id)}`;

function queueParams(query: Omit<FinanceQueueQuery, 'page' | 'pageSize'>) {
  const search = new URLSearchParams({ openOnly: String(query.openOnly) });
  if (query.status) search.set('status', query.status);
  if (query.search) search.set('search', query.search);
  return search;
}

export const financeApi = {
  queue: (query: FinanceQueueQuery) => {
    const search = queueParams(query);
    search.set('page', String(query.page));
    search.set('pageSize', String(query.pageSize));
    return api.get<PagedResult<FinanceQueueItem>>(`/finance/queue?${search.toString()}`);
  },
  /** The whole queue for the same filters, as CSV. */
  exportQueue: (query: Omit<FinanceQueueQuery, 'page' | 'pageSize'>) =>
    api.getFile(`/finance/queue/export?${queueParams(query).toString()}`, 'FinanceQueue.csv'),
  aging: () => api.get<FinanceAging>('/finance/aging'),
  exportAging: (format: ExportFormat) => api.getFile(`/finance/aging/export?format=${format}`, `FinanceAging.${format.toLowerCase()}`),
  /** rowVersion is the tracking row's (from the queue item). */
  recordReceipts: (id: string, status: ReceiptStatus, remarks: string | null, rowVersion: number) =>
    api.post<FinanceQueueItem>(`${f(id)}/receipts`, { status, remarks, rowVersion }),
  clear: (id: string, remarks: string | null, rowVersion: number) =>
    api.post<FinanceQueueItem>(`${f(id)}/clear`, { remarks, rowVersion }),
  followUp: (id: string, message: string | null) => api.post<FinanceQueueItem>(`${f(id)}/follow-up`, { message }),
};
