import { api, apiUrl } from '@/shared/api/client';
import type { ExportFormat } from '@/shared/download';
import type { ExpenseSummary, ExpenseSummaryQuery } from './types';

function summaryParams(query: ExpenseSummaryQuery) {
  const params = new URLSearchParams({ from: query.from, to: query.to, scope: query.scope, approvedOnly: String(query.approvedOnly) });
  if (query.departmentId) params.set('departmentId', String(query.departmentId));
  return params;
}

export const reportingApi = {
  /** Fetches the R47 expense-report PDF as a Blob (cookie-authenticated). */
  fetchPdf: (reportId: string) => api.getBlob(`/reports/${encodeURIComponent(reportId)}/pdf`),
  /** Direct URL, e.g. for opening in a new tab; the browser sends the session cookie. */
  pdfUrl: (reportId: string, download = false) =>
    apiUrl(`/reports/${encodeURIComponent(reportId)}/pdf${download ? '?download=true' : ''}`),

  /** Employee and category totals for the period. The API checks the scope (403 when not yours to use). */
  expenseSummary: (query: ExpenseSummaryQuery) => api.get<ExpenseSummary>(`/summaries/expenses?${summaryParams(query).toString()}`),
  /** CSV: one row per expense line. PDF: totals by employee, then the lines. */
  exportExpenseSummary: (query: ExpenseSummaryQuery, format: ExportFormat) => {
    const params = summaryParams(query);
    params.set('format', format);
    return api.getFile(`/summaries/expenses/export?${params.toString()}`, `ExpenseSummary.${format.toLowerCase()}`);
  },
};
