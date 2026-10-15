import type { DelegationState } from './types';

export const delegationStateLabel: Record<DelegationState, string> = {
  Scheduled: 'Scheduled',
  Active: 'Active',
  Ended: 'Ended',
  Cancelled: 'Cancelled',
};

export const delegationStateColor: Record<DelegationState, string> = {
  Scheduled: 'info',
  Active: 'success',
  Ended: 'default',
  Cancelled: 'default',
};
