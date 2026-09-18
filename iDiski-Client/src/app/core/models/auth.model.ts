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
  /**
   * What this administrator actually administers. A role says what kind of administrator
   * somebody is, never which competitions — so without these every admin screen showed the
   * whole league and offered Edit on rows the API would refuse to save.
   *
   * Both are empty for a super admin, who is not scoped to a set: `isSuperAdmin` says so
   * instead, and treating the empty list as their scope would lock them out of everything.
   *
   * Optional because the login response is a smaller payload than /me. Undefined means "not
   * known yet", which is not the same as "assigned to nothing".
   */
  administeredCompetitionIds?: string[];
  administeredTeamIds?: string[];
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
  assignedCompetitionIds?: string[];
}

export enum Role {
  TeamAdmin = 'TeamAdmin',
  CompetitionAdmin = 'CompetitionAdmin',
  SuperAdmin = 'SuperAdmin'
}
