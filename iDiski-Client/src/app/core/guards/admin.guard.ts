import { Injectable, inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';

@Injectable({ providedIn: 'root' })
class AdminGuardService {
  private router = inject(Router);

  canActivateAdmin(): boolean {
    const isAdmin = sessionStorage.getItem('isAdmin') === 'true';

    if (!isAdmin) {
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}

export const adminGuard: CanActivateFn = () => {
  return inject(AdminGuardService).canActivateAdmin();
};
