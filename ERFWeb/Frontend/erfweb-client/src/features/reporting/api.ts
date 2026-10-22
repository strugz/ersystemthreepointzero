import { api, apiUrl } from '@/shared/api/client';

export const reportingApi = {
  /** Fetches the R47 expense-report PDF as a Blob (cookie-authenticated). */
  fetchPdf: (reportId: string) => api.getBlob(`/reports/${encodeURIComponent(reportId)}/pdf`),
  /** Direct URL, e.g. for opening in a new tab; the browser sends the session cookie. */
  pdfUrl: (reportId: string, download = false) =>
    apiUrl(`/reports/${encodeURIComponent(reportId)}/pdf${download ? '?download=true' : ''}`),
};
