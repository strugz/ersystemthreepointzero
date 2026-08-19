export const colors = {
  brand: '#123A63', brandDeep: '#0B2945', brandSoft: '#DCE8F3', accent: '#C58B2A',
  canvas: '#E8EFF5', surface: '#F7FAFC', surfaceElevated: '#FCFDFE', border: '#C9D6E2',
  ink: '#172B3D', muted: '#5E7286',
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
