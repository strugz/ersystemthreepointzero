import { api } from '@/shared/api/client';
import type { AuthenticatedUser, ChangePasswordRequest, LoginRequest } from './types';

export const authApi = {
  me: () => api.get<AuthenticatedUser>('/auth/me'),
  login: (request: LoginRequest) => api.post<AuthenticatedUser>('/auth/login', request),
  logout: () => api.post<void>('/auth/logout'),
  /** Returns the user with mustChangePassword cleared; the session cookie is re-issued. */
  changePassword: (request: ChangePasswordRequest) => api.post<AuthenticatedUser>('/auth/password', request),
};
