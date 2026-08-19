const money = new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' })
const date = new Intl.DateTimeFormat('en-PH', { year: 'numeric', month: 'short', day: '2-digit' })
const dateTime = new Intl.DateTimeFormat('en-PH', { year: 'numeric', month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })

export const formatMoney = (value?: number | null) => value == null ? '—' : money.format(value)
export const formatDate = (value?: string | null) => value ? date.format(new Date(`${value}T00:00:00`)) : '—'
export const formatDateTime = (value?: string | null) => value ? dateTime.format(new Date(value)) : '—'
