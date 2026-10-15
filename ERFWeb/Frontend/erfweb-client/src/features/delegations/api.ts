import { api } from '@/shared/api/client';
import type { CreateDelegationInput, Delegation, DelegationOptions, MyDelegations } from './types';

export const delegationsApi = {
  mine: () => api.get<MyDelegations>('/delegations'),
  options: () => api.get<DelegationOptions>('/delegations/options'),
  create: (input: CreateDelegationInput) => api.post<Delegation>('/delegations', input),
  cancel: (id: number, rowVersion: number) => api.post<Delegation>(`/delegations/${id}/cancel`, { rowVersion }),
};
