import { api, apiUrl } from '@/shared/api/client';
import type { UserRole } from '@/features/auth/types';
import type {
  AuditEntry,
  AuditQuery,
  CostCenter,
  CostCenterInput,
  CreateUserInput,
  CrmEmployee,
  Currency,
  CurrencyInput,
  Department,
  DepartmentInput,
  ExpenseCategoryInfo,
  FareMode,
  FareModeInput,
  GrantRoleInput,
  PagedResult,
  ReminderStatus,
  RoleActionInput,
  SetActiveInput,
  Site,
  SiteInput,
  SystemSettings,
  SystemSettingsInput,
  UpdateUserInput,
  UserDetail,
  UserListItem,
  UserListQuery,
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

const u = (id: number) => `/admin/users/${id}`;
const md = '/admin/master-data';

export const adminApi = {
  // ---- users ---------------------------------------------------------------
  listUsers: (query: UserListQuery) => api.get<PagedResult<UserListItem>>(`/admin/users${qs({ ...query })}`),
  getUser: (id: number) => api.get<UserDetail>(u(id)),
  createUser: (input: CreateUserInput) => api.post<UserDetail>('/admin/users', input),
  updateUser: (id: number, input: UpdateUserInput) => api.put<UserDetail>(u(id), input),
  setActive: (id: number, input: SetActiveInput) => api.post<UserDetail>(`${u(id)}/active`, input),
  resetPassword: (id: number, newPassword: string) => api.post<void>(`${u(id)}/password`, { newPassword }),
  uploadSignature: (id: number, file: File) => {
    const form = new FormData();
    form.append('file', file, file.name);
    return api.postForm<UserDetail>(`${u(id)}/signature`, form);
  },
  /** Direct URL of the signature on file, for an <img>; the browser sends the session cookie. */
  signatureUrl: (id: number, version = 0) => apiUrl(`${u(id)}/signature?v=${version}`),
  grantRole: (id: number, input: GrantRoleInput) => api.post<UserDetail>(`${u(id)}/roles`, input),
  revokeRole: (id: number, role: UserRole, input: RoleActionInput) =>
    api.post<UserDetail>(`${u(id)}/roles/${encodeURIComponent(role)}/revoke`, input),

  // ---- CRM employee lookup (the server calls the CRM) ----------------------
  searchCrmEmployees: (search: string, includeInactive = false) =>
    api.get<PagedResult<CrmEmployee>>(`/admin/crm/employees${qs({ search, includeInactive, pageSize: 20 })}`),
  getCrmEmployee: (employeeId: string) => api.get<CrmEmployee>(`/admin/crm/employees/${encodeURIComponent(employeeId)}`),

  // ---- master data ---------------------------------------------------------
  departments: (includeInactive = true) => api.get<Department[]>(`${md}/departments${qs({ includeInactive })}`),
  createDepartment: (input: DepartmentInput) => api.post<Department>(`${md}/departments`, input),
  updateDepartment: (id: number, input: DepartmentInput) => api.put<Department>(`${md}/departments/${id}`, input),

  costCenters: (includeInactive = true) => api.get<CostCenter[]>(`${md}/cost-centers${qs({ includeInactive })}`),
  createCostCenter: (input: CostCenterInput) => api.post<CostCenter>(`${md}/cost-centers`, input),
  updateCostCenter: (id: number, input: CostCenterInput) => api.put<CostCenter>(`${md}/cost-centers/${id}`, input),

  currencies: (includeInactive = true) => api.get<Currency[]>(`${md}/currencies${qs({ includeInactive })}`),
  createCurrency: (input: CurrencyInput) => api.post<Currency>(`${md}/currencies`, input),
  updateCurrency: (code: string, input: CurrencyInput) =>
    api.put<Currency>(`${md}/currencies/${encodeURIComponent(code)}`, input),

  sites: (includeInactive = true) => api.get<Site[]>(`${md}/sites${qs({ includeInactive })}`),
  createSite: (input: SiteInput) => api.post<Site>(`${md}/sites`, input),
  updateSite: (id: number, input: SiteInput) => api.put<Site>(`${md}/sites/${id}`, input),

  fareModes: (includeInactive = true) => api.get<FareMode[]>(`${md}/fare-modes${qs({ includeInactive })}`),
  createFareMode: (input: FareModeInput) => api.post<FareMode>(`${md}/fare-modes`, input),
  updateFareMode: (id: number, input: FareModeInput) => api.put<FareMode>(`${md}/fare-modes/${id}`, input),

  expenseCategories: () => api.get<ExpenseCategoryInfo[]>(`${md}/expense-categories`),

  // ---- settings ------------------------------------------------------------
  settings: () => api.get<SystemSettings>('/admin/settings'),
  updateSettings: (input: SystemSettingsInput) => api.put<SystemSettings>('/admin/settings', input),
  reminderStatus: () => api.get<ReminderStatus>('/admin/reminders/status'),

  // ---- audit ---------------------------------------------------------------
  audit: (query: AuditQuery) => api.get<PagedResult<AuditEntry>>(`/admin/audit${qs({ ...query })}`),
};
