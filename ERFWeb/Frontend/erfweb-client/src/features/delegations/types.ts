// Mirrors ERFWeb.Application.Features.Delegations. Enums travel as strings; dates as yyyy-MM-dd.

export type DelegationState = 'Scheduled' | 'Active' | 'Ended' | 'Cancelled';

export interface Delegation {
  id: number;
  signatoryUserId: number;
  signatoryName: string;
  signatoryLevel: number | null;
  delegateUserId: number;
  delegateName: string;
  startsOn: string;
  endsOn: string;
  reason: string | null;
  /** Assigned by the Level 2 for an absent Level 1. */
  isForced: boolean;
  createdByName: string;
  createdAt: string;
  cancelledByName: string | null;
  cancelledAt: string | null;
  state: DelegationState;
  canCancel: boolean;
  rowVersion: number;
}

export interface MyDelegations {
  /** I am the signatory. */
  given: Delegation[];
  /** I am the delegate. */
  received: Delegation[];
  /** I assigned them, as Level 2, for a Level 1. */
  assigned: Delegation[];
}

export interface DelegateCandidate {
  userId: number;
  fullName: string;
  departmentName: string | null;
  level: number;
}

export interface CoveredSignatory {
  userId: number;
  fullName: string;
  departmentName: string | null;
  pendingReports: number;
}

export interface DelegationOptions {
  myLevel: number | null;
  today: string;
  maxDays: number;
  candidates: DelegateCandidate[];
  coveredLevel1s: CoveredSignatory[];
}

export interface CreateDelegationInput {
  /** Null to delegate my own approvals; a Level 1's id when a Level 2 assigns for them. */
  signatoryUserId: number | null;
  delegateUserId: number;
  startsOn: string;
  endsOn: string;
  reason: string | null;
}
