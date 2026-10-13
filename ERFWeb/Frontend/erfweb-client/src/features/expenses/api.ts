import { api } from '@/shared/api/client';
import type {
  Attachment,
  CashAdvanceLine,
  EmployeeRates,
  ExpenseLine,
  ExpenseReportDetail,
  ExpenseReportListItem,
  FileReportResult,
  FilingReadiness,
  PagedResult,
  ReportHeaderInput,
  ReportListQuery,
  SaveCashAdvanceLineInput,
  SaveExpenseLineInput,
  WorkWithOption,
} from './types';

function qs(params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value));
    }
  }
  const s = search.toString();
  return s ? `?${s}` : '';
}

const r = (id: string) => `/reports/${encodeURIComponent(id)}`;

export const expensesApi = {
  list: (query: ReportListQuery) => api.get<PagedResult<ExpenseReportListItem>>(`/reports${qs({ ...query })}`),
  get: (id: string) => api.get<ExpenseReportDetail>(r(id)),
  create: (input: ReportHeaderInput) => api.post<ExpenseReportDetail>('/reports', input),
  update: (id: string, input: ReportHeaderInput & { rowVersion: number }) => api.put<ExpenseReportDetail>(r(id), input),
  remove: (id: string) => api.del<void>(r(id)),
  file: (id: string) => api.post<FileReportResult>(`${r(id)}/file`),
  reopen: (id: string) => api.post<ExpenseReportDetail>(`${r(id)}/reopen`),

  addLine: (id: string, input: SaveExpenseLineInput) => api.post<ExpenseLine>(`${r(id)}/lines`, input),
  updateLine: (id: string, lineId: number, input: SaveExpenseLineInput) =>
    api.put<ExpenseLine>(`${r(id)}/lines/${lineId}`, input),
  removeLine: (id: string, lineId: number) => api.del<void>(`${r(id)}/lines/${lineId}`),

  addCashAdvance: (id: string, input: SaveCashAdvanceLineInput) => api.post<CashAdvanceLine>(`${r(id)}/cash-advances`, input),
  updateCashAdvance: (id: string, lineId: number, input: SaveCashAdvanceLineInput) =>
    api.put<CashAdvanceLine>(`${r(id)}/cash-advances/${lineId}`, input),
  removeCashAdvance: (id: string, lineId: number) => api.del<void>(`${r(id)}/cash-advances/${lineId}`),

  attachments: (id: string) => api.get<Attachment[]>(`${r(id)}/attachments`),
  upload: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file, file.name);
    return api.postForm<Attachment>(`${r(id)}/attachments`, form);
  },
  attachmentUrl: (id: string, attachmentId: number, download = false) =>
    `/api${r(id)}/attachments/${attachmentId}/content${download ? '?download=true' : ''}`,
  removeAttachment: (id: string, attachmentId: number) => api.del<void>(`${r(id)}/attachments/${attachmentId}`),

  workWith: (search?: string) => api.get<WorkWithOption[]>(`/lookups/work-with${qs({ search })}`),
  /** The rates in force on the date (today when omitted). */
  myRates: (date?: string) => api.get<EmployeeRates>(`/lookups/my-rates${qs({ date })}`),
  filingReadiness: () => api.get<FilingReadiness>('/lookups/my-filing-readiness'),
};
