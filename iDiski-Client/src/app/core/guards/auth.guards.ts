import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Role } from '../models/auth.model';

/**
 * Guard that requires user to be authenticated
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    console.warn('⚠️ Access denied: not authenticated');
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  return true;
};

/**
 * Guard that requires user to be authenticated and have any admin role
 */
export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    console.warn('⚠️ Access denied: not authenticated');
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  if (!authService.hasAnyAdminRole()) {
    console.warn('⚠️ Access denied: insufficient permissions');
    router.navigate(['/']);
    return false;
  }

  return true;
};

/**
 * Guard that requires user to be Super Admin
 */
export const superAdminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    console.warn('⚠️ Access denied: not authenticated');
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  if (!authService.isSuperAdmin()) {
    console.warn('⚠️ Access denied: Super Admin role required');
    router.navigate(['/']);
    return false;
  }

  return true;
};

/**
 * Guard that requires user to have a specific role
 */
export const roleGuard = (requiredRoles: Role[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      console.warn('⚠️ Access denied: not authenticated');
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (!authService.hasAnyRole(requiredRoles)) {
      console.warn(`⚠️ Access denied: required roles ${requiredRoles.join(', ')}`);
      router.navigate(['/']);
      return false;
    }

    return true;
  };
};

/**
 * Guard that redirects to dashboard if already logged in
 */
export const noAuthGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    router.navigate(['/admin']);
    return false;
  }

  return true;
};
