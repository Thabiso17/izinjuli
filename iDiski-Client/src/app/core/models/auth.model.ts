export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileImageUrl?: string;
  roles: string[];
  isSuperAdmin?: boolean;
}

export interface CurrentUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileImageUrl?: string;
  roles: string[];
  isSuperAdmin: boolean;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roles: string[];
  assignedTeamIds?: string[];
  assignedDivisionIds?: string[];
}

export enum Role {
  TeamAdmin = 'TeamAdmin',
  DivisionAdmin = 'DivisionAdmin',
  SuperAdmin = 'SuperAdmin'
}
