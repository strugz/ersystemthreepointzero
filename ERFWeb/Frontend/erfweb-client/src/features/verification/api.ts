import { api } from '@/shared/api/client';
import type { ExpenseReportDetail, PagedResult } from '@/features/expenses/types';
import type {
  QueryDisposition,
  QueryListView,
  VerificationFinding,
  VerificationQueueItem,
  VerificationQueueView,
} from './types';

const v = (id: number) => `/verifications/${id}`;

function paged(view: string, page: number, pageSize: number): string {
  return new URLSearchParams({ view, page: String(page), pageSize: String(pageSize) }).toString();
}

// Every action returns the report as the actor now sees it. rowVersion is the request's.
export const verificationApi = {
  raise: (reportId: string, lineId: number, reason: string) =>
    api.post<ExpenseReportDetail>('/verifications', { reportId, lineId, reason }),
  queue: (view: VerificationQueueView, page: number, pageSize: number) =>
    api.get<PagedResult<VerificationQueueItem>>(`/verifications?${paged(view, page, pageSize)}`),
  pickUp: (id: number, rowVersion: number) => api.post<ExpenseReportDetail>(`${v(id)}/pick-up`, { rowVersion }),
  recordFinding: (id: number, finding: VerificationFinding, note: string | null, rowVersion: number) =>
    api.post<ExpenseReportDetail>(`${v(id)}/finding`, { finding, note, rowVersion }),
  query: (id: number, note: string, rowVersion: number) => api.post<ExpenseReportDetail>(`${v(id)}/query`, { note, rowVersion }),
  queries: (view: QueryListView, page: number, pageSize: number) =>
    api.get<PagedResult<VerificationQueueItem>>(`/verifications/queries?${paged(view, page, pageSize)}`),
  decide: (id: number, disposition: QueryDisposition, note: string | null, rowVersion: number) =>
    api.post<ExpenseReportDetail>(`${v(id)}/decision`, { disposition, note, rowVersion }),
};
