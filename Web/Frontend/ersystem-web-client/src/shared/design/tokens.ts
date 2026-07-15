export const colors = {
  brand: '#123A63', brandSoft: '#E8F0F8', accent: '#C58B2A', surface: '#F6F8FB',
  success: '#237A57', warning: '#A96800', error: '#B3261E', info: '#2766A3',
  pending: '#A96800', returned: '#B3261E', approved: '#237A57', received: '#2766A3'
}

export const statusPresentation: Record<string, { label: string; color: string }> = {
  Pending: { label: 'Pending', color: 'warning' },
  'For Approval': { label: 'For approval', color: 'warning' },
  Approved: { label: 'Approved', color: 'success' },
  Returned: { label: 'Returned', color: 'error' },
  'Receipts Received': { label: 'Receipts received', color: 'info' },
  Missing: { label: 'Missing', color: 'warning' },
  Received: { label: 'Received', color: 'success' }
}
