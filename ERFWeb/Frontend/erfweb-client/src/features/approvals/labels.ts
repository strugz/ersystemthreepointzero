import type { ApprovalActionKind } from './types';

interface ActionCopy {
  title: string;
  confirm: string;
  color: string;
  /** Return, reject and override need a reason; approve takes an optional note. */
  reasonRequired: boolean;
  noteLabel: string;
  message: string;
  done: string;
}

export const approvalActionCopy: Record<ApprovalActionKind, ActionCopy> = {
  approve: {
    title: 'Approve this report?',
    confirm: 'Approve',
    color: 'success',
    reasonRequired: false,
    noteLabel: 'Note (optional)',
    message: 'Line amounts lock once the report has its first approval.',
    done: 'Report approved.',
  },
  return: {
    title: 'Return this report?',
    confirm: 'Return',
    color: 'warning',
    reasonRequired: true,
    noteLabel: 'What should the employee fix?',
    message: 'The employee corrects and re-files it. Approval then restarts from the first approver.',
    done: 'Report returned to the employee.',
  },
  reject: {
    title: 'Reject this report?',
    confirm: 'Reject',
    color: 'error',
    reasonRequired: true,
    noteLabel: 'Reason for rejecting',
    message: 'Rejection is final: the report cannot be re-filed. Use Return if the employee can correct it.',
    done: 'Report rejected.',
  },
  override: {
    title: 'Approve without the Level 1?',
    confirm: 'Override and approve',
    color: 'primary',
    reasonRequired: true,
    noteLabel: 'Why are you approving without the Level 1?',
    message: 'The pending Level 1 step is marked Overridden and the report is approved. The reason is kept in the audit trail.',
    done: 'Report approved by override.',
  },
};
