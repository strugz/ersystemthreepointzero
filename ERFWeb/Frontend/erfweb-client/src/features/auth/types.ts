export type UserRole =
  | 'Employee'
  | 'SignatoryLevel1'
  | 'SignatoryLevel2'
  | 'Finance'
  | 'ExpenseVerifier'
  | 'SystemAdministrator'
  | 'BusinessOwner';

export interface AuthenticatedUser {
  userId: number;
  username: string;
  fullName: string;
  /** Active role grants plus the implicit Employee role. */
  roles: UserRole[];
}

export interface LoginRequest {
  username: string;
  password: string;
}
