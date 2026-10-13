// Mirrors ERFWeb.Application.Features.Verification (plan Phase 6, Flow 6). Enums travel as strings.

export type VerificationStatus = 'Open' | 'InProgress' | 'Found' | 'Queried' | 'Closed';
export type VerificationFinding = 'Verified' | 'VerifiedWithNote' | 'NotVerified';
export type QueryDisposition = 'Resume' | 'Return' | 'Reject';
export type VerificationQueueView = 'Open' | 'Mine' | 'Done';
export type QueryListView = 'Pending' | 'Decided';

/** One request on the report. The claimant gets reason, notes and names as null. */
export interface Verification {
  id: number;
  lineId: number;
  lineParticulars: string | null;
  lineTotal: number;
  status: VerificationStatus;
  reason: string | null;
  requestedByName: string;
  requestedAt: string;
  verifierName: string | null;
  pickedUpAt: string | null;
  finding: VerificationFinding | null;
  findingNote: string | null;
  foundAt: string | null;
  queryToName: string | null;
  queryNote: string | null;
  queriedAt: string | null;
  disposition: QueryDisposition | null;
  dispositionNote: string | null;
  decidedAt: string | null;
  rowVersion: number;
  canPickUp: boolean;
  canRecordFinding: boolean;
  canQuery: boolean;
  canDecide: boolean;
}

export interface VerificationPanel {
  canRequest: boolean;
  underVerification: boolean;
  requests: Verification[];
}

export interface VerificationQueueItem {
  id: number;
  reportId: string;
  referenceNo: string | null;
  employeeName: string;
  departmentName: string | null;
  lineId: number;
  lineDate: string;
  lineParticulars: string | null;
  lineTotal: number;
  reason: string;
  requestedByName: string;
  requestedAt: string;
  status: VerificationStatus;
  verifierName: string | null;
  finding: VerificationFinding | null;
  findingNote: string | null;
  foundAt: string | null;
  queryNote: string | null;
  queriedAt: string | null;
  disposition: QueryDisposition | null;
  decidedAt: string | null;
  daysWaiting: number;
}
