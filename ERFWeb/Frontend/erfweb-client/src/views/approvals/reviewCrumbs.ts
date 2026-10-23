import type { LocationQuery, RouteLocationRaw } from 'vue-router';
import type { Crumb } from '@/layouts/AuthenticatedLayout.vue';

/** The queue a review was opened from (`?from=`), as the first breadcrumb of the review and its R47 PDF. */
export function reviewOriginCrumb(from: LocationQuery[string]): Crumb {
  switch (from) {
    case 'finance':
      return { title: 'Finance queue', to: { name: 'finance-queue' } };
    case 'verifications':
      return { title: 'Verifications', to: { name: 'verifications' } };
    case 'queries':
      return { title: 'Approvals', to: { name: 'approvals', query: { view: 'Queries' } } };
    default:
      return { title: 'Approvals', to: { name: 'approvals' } };
  }
}

/**
 * The R47 PDF opened from a review. `via=review` makes the PDF page lead back to the review (with its
 * Approve/Return buttons) instead of the claimant's editor; `from` keeps the review's own origin.
 */
export function reviewPdfLink(reportId: string, from: LocationQuery[string]): RouteLocationRaw {
  return { name: 'report-pdf', params: { id: reportId }, query: typeof from === 'string' ? { via: 'review', from } : { via: 'review' } };
}

/** The review a PDF opened with {@link reviewPdfLink} returns to. */
export function reviewLink(reportId: string, from: LocationQuery[string]): RouteLocationRaw {
  return { name: 'report-review', params: { id: reportId }, query: typeof from === 'string' ? { from } : {} };
}
