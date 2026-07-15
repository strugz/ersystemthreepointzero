export type NavigationDestination = 'manager' | 'finance' | 'account' | ''

export function resolveNavigationDestination(path: string): NavigationDestination {
  if (path === '/account') return 'account'
  if (path.startsWith('/finance')) return 'finance'
  if (path.startsWith('/manager')) return 'manager'
  return ''
}
