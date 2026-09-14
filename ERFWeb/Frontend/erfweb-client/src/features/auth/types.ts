export type UserRole = 'Employee' | 'Manager' | 'Finance' | 'Admin';

export interface AuthenticatedUser {
  userId: number;
  username: string;
  fullName: string;
  roles: UserRole[];
}

export interface LoginRequest {
  username: string;
  password: string;
}
