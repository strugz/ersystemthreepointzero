import { api } from '@/shared/api/client';
import type { SendSmsRequest, SmsSent } from './types';

export const smsApi = {
  send: (request: SendSmsRequest) => api.post<SmsSent>('/sms', request),
};

/** A fresh idempotency key for one message. */
export function newIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `sms-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
}
