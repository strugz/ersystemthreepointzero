export interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number }
export interface ProblemDetails { title?: string; detail?: string; status?: number; correlationId?: string; code?: string }
export type SortDirection = 'Ascending' | 'Descending'
