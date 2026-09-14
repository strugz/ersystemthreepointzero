// Single fetch layer: cookie credentials, antiforgery token handling with a
// one-time retry, and ProblemDetails -> typed ApiError mapping.

const BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export interface ProblemDetails {
  title?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    readonly problem: ProblemDetails | null,
  ) {
    super(problem?.title ?? `Request failed (${status})`);
    this.name = 'ApiError';
  }
}

let csrfToken: string | null = null;

async function ensureCsrfToken(): Promise<string> {
  if (csrfToken) {
    return csrfToken;
  }
  const res = await fetch(`${BASE}/auth/antiforgery`, { credentials: 'include' });
  if (!res.ok) {
    throw new ApiError(res.status, 'antiforgery_fetch_failed', null);
  }
  const data = (await res.json()) as { token: string };
  csrfToken = data.token;
  return csrfToken;
}

type Method = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

async function request<T>(method: Method, path: string, body?: unknown, allowRetry = true): Promise<T> {
  const isUnsafe = method !== 'GET';
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  if (isUnsafe) {
    headers['X-CSRF-TOKEN'] = await ensureCsrfToken();
  }

  const res = await fetch(`${BASE}${path}`, {
    method,
    credentials: 'include',
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 204) {
    return undefined as T;
  }

  const isJson = res.headers.get('content-type')?.includes('json') ?? false;
  const payload = isJson ? await res.json() : null;

  if (!res.ok) {
    const problem = payload as ProblemDetails | null;
    const code = problem?.code;
    if (code === 'antiforgery_validation_failed' && isUnsafe && allowRetry) {
      csrfToken = null;
      return request<T>(method, path, body, false);
    }
    throw new ApiError(res.status, code, problem);
  }

  return payload as T;
}

/** multipart/form-data POST (file uploads). Same cookie + antiforgery handling as JSON requests. */
async function requestForm<T>(path: string, form: FormData, allowRetry = true): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: { Accept: 'application/json', 'X-CSRF-TOKEN': await ensureCsrfToken() },
    body: form,
  });

  const isJson = res.headers.get('content-type')?.includes('json') ?? false;
  const payload = isJson ? await res.json() : null;

  if (!res.ok) {
    const problem = payload as ProblemDetails | null;
    if (problem?.code === 'antiforgery_validation_failed' && allowRetry) {
      csrfToken = null;
      return requestForm<T>(path, form, false);
    }
    throw new ApiError(res.status, problem?.code, problem);
  }

  return payload as T;
}

async function requestBlob(path: string): Promise<Blob> {
  const res = await fetch(`${BASE}${path}`, {
    credentials: 'include',
    headers: { Accept: 'application/pdf, application/json' },
  });
  if (!res.ok) {
    const isJson = res.headers.get('content-type')?.includes('json') ?? false;
    const problem = isJson ? ((await res.json()) as ProblemDetails) : null;
    throw new ApiError(res.status, problem?.code, problem);
  }
  return res.blob();
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  del: <T>(path: string) => request<T>('DELETE', path),
  postForm: <T>(path: string, form: FormData) => requestForm<T>(path, form),
  getBlob: (path: string) => requestBlob(path),
  ensureCsrfToken,
};
