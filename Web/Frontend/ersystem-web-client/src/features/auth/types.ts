export interface AuthenticatedUser { userId: number; username: string; fullName: string; userLevel: string; roles: string[] }
export interface LoginRequest { username: string; password: string }
