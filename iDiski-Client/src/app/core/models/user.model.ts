import { Role } from './auth.model';

/**
 * The API is not consistent about how it names roles, and hiding that here is better than
 * discovering it a screen at a time: the user endpoints return and accept the numeric enum
 * (1, 2, 3), while create-user takes the names the rest of the client uses.
 */
export const ROLE_ID: Record<Role, number> = {
  [Role.TeamAdmin]: 1,
  [Role.DivisionAdmin]: 2,
  [Role.SuperAdmin]: 3,
};

export const ROLE_BY_ID: Record<number, Role> = {
  1: Role.TeamAdmin,
  2: Role.DivisionAdmin,
  3: Role.SuperAdmin,
};

export const ROLE_LABEL: Record<Role, string> = {
  [Role.TeamAdmin]: 'Team Admin',
  [Role.DivisionAdmin]: 'Division Admin',
  [Role.SuperAdmin]: 'Super Admin',
};

/** A row in the administrators list. */
export interface AdminUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  roleIds: number[];
}

/** One administrator with everything they have been granted. */
export interface AdminUserDetailDto extends AdminUserDto {
  assignedTeamIds: string[];
  assignedDivisionIds: string[];
}

/**
 * Creating an administrator. The roles decide what they can reach; the assignments decide
 * where. A team admin with no team and a division admin with no division can both sign in to
 * the admin area and touch nothing in it, so the API refuses those outright.
 */
export interface CreateAdminUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  assignedTeamIds?: string[];
  assignedDivisionIds?: string[];
}

export interface UpdateAdminUserRequest {
  id: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
}
