import { refreshAntiforgery } from '@/shared/api/client'

export function useAntiforgery() { return { refresh: refreshAntiforgery } }
