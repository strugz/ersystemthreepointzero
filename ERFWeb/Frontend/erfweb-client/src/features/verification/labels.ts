import type { QueryDisposition, VerificationFinding, VerificationStatus } from './types';

export const verificationStatusLabel: Record<VerificationStatus, string> = {
  Open: 'Waiting for a verifier',
  InProgress: 'Being verified',
  Found: 'Not verified: waiting for Finance',
  Queried: 'With Level 2 for a decision',
  Closed: 'Closed',
};

/** Pill tone per status (classes from main.css). */
export const verificationStatusTone: Record<VerificationStatus, string> = {
  Open: 'pill--warning',
  InProgress: 'pill--warning',
  Found: 'pill--error',
  Queried: 'pill--error',
  Closed: 'pill--neutral',
};

export const findingLabel: Record<VerificationFinding, string> = {
  Verified: 'Verified',
  VerifiedWithNote: 'Verified with note',
  NotVerified: 'Not verified',
};

export const findingTone: Record<VerificationFinding, string> = {
  Verified: 'pill--success',
  VerifiedWithNote: 'pill--success',
  NotVerified: 'pill--error',
};

export const dispositionLabel: Record<QueryDisposition, string> = {
  Resume: 'Let through',
  Return: 'Returned to the employee',
  Reject: 'Rejected',
};

export const findingOptions: { value: VerificationFinding; title: string; hint: string }[] = [
  { value: 'Verified', title: 'Verified', hint: 'The facts check out.' },
  { value: 'VerifiedWithNote', title: 'Verified with note', hint: 'It checks out, with something Finance should know.' },
  { value: 'NotVerified', title: 'Not verified', hint: 'It does not check out. Finance will refer it to Level 2.' },
];

export const dispositionOptions: { value: QueryDisposition; title: string; hint: string }[] = [
  { value: 'Resume', title: 'Let it through', hint: 'Finance carries on and can clear the report.' },
  { value: 'Return', title: 'Return for correction', hint: 'The employee corrects and re-files; approval restarts.' },
  { value: 'Reject', title: 'Reject the report', hint: 'Final. The report cannot be re-filed.' },
];
