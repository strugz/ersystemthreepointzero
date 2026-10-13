// Mirrors ERFWeb.Application.Features.Notifications.SmsContracts.

/** The recipient is named by user id only; the server texts the mobile number(s) on file. */
export interface SendSmsRequest {
  contactId: number;
  message: string;
  /** Reuse the same key when retrying the same message so it is never sent twice. */
  idempotencyKey: string | null;
  /** The report the SMS is about: its Level 1 and Level 2 signatories are texted too (server-side). */
  reportId?: string | null;
}

export interface SmsSent {
  status: 'Sent';
  recipientCount: number;
  /** True when a retry found the SMS already sent and nothing new went out. */
  replayed: boolean;
}

/** Same limit as the API (SmsRules.MaxMessageLength). */
export const SMS_MAX_LENGTH = 320;
