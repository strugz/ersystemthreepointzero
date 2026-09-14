import { api } from '@/shared/api/client';
import type { AuthenticatedUser, LoginRequest } from './types';

export const authApi = {
  me: () => api.get<AuthenticatedUser>('/auth/me'),
  login: (request: LoginRequest) => api.post<AuthenticatedUser>('/auth/login', request),
  logout: () => api.post<void>('/auth/logout'),
};
