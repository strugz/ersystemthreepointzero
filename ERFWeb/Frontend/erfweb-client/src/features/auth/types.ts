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
  /** The password was set by an administrator: the user must change it before anything else (the API enforces it). */
  mustChangePassword: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/** Same rule as the API (AccountRules.MinPasswordLength). */
export const MIN_PASSWORD_LENGTH = 8;

export interface LoginRequest {
  username: string;
  password: string;
}
