import { api } from '@/shared/api/client';

/** The record kinds DELETE /api/test-data/{kind}/{id} accepts (TestDataController). */
export type TestDataKind =
  | 'reports'
  | 'expense-lines'
  | 'cash-advances'
  | 'attachments'
  | 'verifications'
  | 'users'
  | 'departments'
  | 'cost-centers'
  | 'cost-center-budgets'
  | 'currencies'
  | 'sites'
  | 'fare-modes'
  | 'rate-profiles'
  | 'rate-profile-lines'
  | 'rate-assignments'
  | 'scope-assignments'
  | 'delegations'
  | 'audit-entries';

export const testDataApi = {
  /** Hard-deletes a test record (System Administrator, API in App:Mode Test). A budget's id is `{costCenterId}-{year}`. */
  delete: (kind: TestDataKind, id: string | number) =>
    api.del<void>(`/test-data/${kind}/${encodeURIComponent(String(id))}`),
};
