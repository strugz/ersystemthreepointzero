import { api } from '@/shared/api/client';
import type {
  AddRateLineInput,
  CoverageRow,
  CreateRateAssignmentInput,
  CreateScopeAssignmentInput,
  DepartmentOption,
  EndRateAssignmentInput,
  RateAssignment,
  RateAssignmentQuery,
  RateProfileDetail,
  RateProfileListItem,
  ResolvedRates,
  ScopeAssignment,
  ScopeAssignmentQuery,
  UpdateRateLineInput,
  UpdateRateProfileInput,
  UpdateScopeAssignmentInput,
} from './types';

function qs(params: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value));
    }
  }
  const s = search.toString();
  return s ? `?${s}` : '';
}

export const scopeApi = {
  list: (query: ScopeAssignmentQuery = {}) => api.get<ScopeAssignment[]>(`/scope/assignments${qs({ ...query })}`),
  get: (id: number) => api.get<ScopeAssignment>(`/scope/assignments/${id}`),
  create: (input: CreateScopeAssignmentInput) => api.post<ScopeAssignment>('/scope/assignments', input),
  update: (id: number, input: UpdateScopeAssignmentInput) => api.put<ScopeAssignment>(`/scope/assignments/${id}`, input),
  /** Every active employee and who signs for them on the date (today by default). */
  coverage: (date?: string) => api.get<CoverageRow[]>(`/scope/coverage${qs({ date })}`),
};

const p = (id: number) => `/rates/profiles/${id}`;

export const ratesApi = {
  profiles: (includeInactive = true) => api.get<RateProfileListItem[]>(`/rates/profiles${qs({ includeInactive })}`),
  profile: (id: number) => api.get<RateProfileDetail>(p(id)),
  createProfile: (name: string) => api.post<RateProfileDetail>('/rates/profiles', { name }),
  updateProfile: (id: number, input: UpdateRateProfileInput) => api.put<RateProfileDetail>(p(id), input),
  addLine: (id: number, input: AddRateLineInput) => api.post<RateProfileDetail>(`${p(id)}/lines`, input),
  updateLine: (id: number, lineId: number, input: UpdateRateLineInput) => api.put<RateProfileDetail>(`${p(id)}/lines/${lineId}`, input),

  assignments: (query: RateAssignmentQuery = {}) => api.get<RateAssignment[]>(`/rates/assignments${qs({ ...query })}`),
  createAssignment: (input: CreateRateAssignmentInput) => api.post<RateAssignment>('/rates/assignments', input),
  endAssignment: (id: number, input: EndRateAssignmentInput) => api.post<RateAssignment>(`/rates/assignments/${id}/end`, input),

  resolve: (userId: number, date?: string) => api.get<ResolvedRates>(`/rates/resolve${qs({ userId, date })}`),
};

export const entitlementLookupsApi = {
  /** Active departments, readable by any signed-in user. */
  departments: () => api.get<DepartmentOption[]>('/lookups/departments'),
};
