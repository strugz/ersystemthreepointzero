import type { ManagerReportDetail } from './types'

export function canCurrentManagerAct(
  report: Pick<ManagerReportDetail, 'status' | 'approvalTrail'>,
  currentUserId: number | null | undefined
): boolean {
  if (!currentUserId || report.status.trim().toLowerCase() !== 'for approval') return false
  return report.approvalTrail.some(step =>
    step.approverUserId === currentUserId && step.status.trim().toLowerCase() === 'pending'
  )
}
