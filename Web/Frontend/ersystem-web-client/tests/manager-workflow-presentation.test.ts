import { describe, expect, it } from 'vitest'
import { canCurrentManagerAct } from '@/features/manager-approvals/workflowPresentation'
import type { ApprovalTrailItem } from '@/features/manager-approvals/types'

function step(approverUserId: number, status: string): ApprovalTrailItem {
  return { approverUserId, approverName: 'Manager', sort: 1, occurredAtUtc: null, status }
}

describe('Manager workflow action presentation', () => {
  it('allows actions only for the current manager pending an active approval', () => {
    expect(canCurrentManagerAct({ status: 'For Approval', approvalTrail: [step(7, 'Pending')] }, 7)).toBe(true)
  })

  it('hides actions after the report is approved', () => {
    expect(canCurrentManagerAct({ status: 'Approved', approvalTrail: [step(7, 'Approved')] }, 7)).toBe(false)
  })

  it('hides actions after the manager assignment is processed', () => {
    expect(canCurrentManagerAct({ status: 'For Approval', approvalTrail: [step(7, 'Returned')] }, 7)).toBe(false)
    expect(canCurrentManagerAct({ status: 'For Approval', approvalTrail: [step(7, 'Approved')] }, 7)).toBe(false)
  })

  it('does not expose another manager assignment', () => {
    expect(canCurrentManagerAct({ status: 'For Approval', approvalTrail: [step(8, 'Pending')] }, 7)).toBe(false)
  })
})
