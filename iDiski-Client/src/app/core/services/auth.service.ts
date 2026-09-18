import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, firstValueFrom, switchMap, map, of } from 'rxjs';
import {
  LoginRequest,
  LoginResponse,
  CurrentUserDto,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  CreateUserRequest,
  Role
} from '../models/auth.model';
import { environment } from '../../../environments/environment';
import { LoggerService } from './logger.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiBaseUrl}/auth`;
  private logger = inject(LoggerService);

  constructor(private http: HttpClient, private router: Router) {
    this.logger.log('🔐 AuthService initialized with API URL: ' + this.apiUrl);
  }

  // Signals for reactive state
  currentUser = signal<CurrentUserDto | null>(null);
  isLoading = signal(false);
  error = signal<string | null>(null);

  // Computed signals
  isAuthenticated = computed(() => this.currentUser() !== null);
  isSuperAdmin = computed(() => this.currentUser()?.isSuperAdmin ?? false);
  hasAnyAdminRole = computed(() => {
    const user = this.currentUser();
    return user?.roles.includes(Role.SuperAdmin) ||
           user?.roles.includes(Role.CompetitionAdmin) ||
           user?.roles.includes(Role.TeamAdmin) ||
           false;
  });

  /**
   * Whether this user's scope is known yet. A freshly signed-in user whose profile request
   * failed has roles but no assignments, and narrowing a screen on that would show them an
   * empty page rather than what they run. Unknown means show everything, exactly as before —
   * the API still refuses anything they may not do.
   */
  scopeIsKnown = computed(() => {
    const user = this.currentUser();
    return !!user && (user.isSuperAdmin || user.administeredCompetitionIds !== undefined);
  });

  administeredCompetitionIds = computed(() => this.currentUser()?.administeredCompetitionIds ?? []);
  administeredTeamIds = computed(() => this.currentUser()?.administeredTeamIds ?? []);

  /** A super admin runs every competition; anybody else, the ones they were assigned. */
  canAdministerCompetition(competitionId: string | null | undefined): boolean {
    if (this.isSuperAdmin()) return true;
    if (!this.scopeIsKnown()) return true;
    if (!competitionId) return false;

    return this.administeredCompetitionIds().includes(competitionId);
  }

  /**
   * Creating or deleting a club is SuperAdmin work at the API. A competition admin runs
   * competitions and administers no clubs; a team admin manages the club they were given but
   * cannot add or remove one.
   */
  canCreateTeam(): boolean {
    return this.isSuperAdmin();
  }

  canDeleteTeam(team: { id: string; divisionId?: string | null }): boolean {
    return this.canCreateTeam() && this.canAdministerTeam(team);
  }

  /**
   * A club is administered by its own team admins, and below super admin by nobody else —
   * the same rule the API enforces, rather than a second opinion about it. Running a
   * competition the club is entered in does not come with the right to edit the club.
   */
  canAdministerTeam(team: { id: string; divisionId?: string | null }): boolean {
    if (this.isSuperAdmin()) return true;
    if (!this.scopeIsKnown()) return true;

    return this.administeredTeamIds().includes(team.id);
  }


  login(request: LoginRequest): Observable<LoginResponse> {
    this.isLoading.set(true);
    this.error.set(null);
    this.logger.log('📡 Attempting login for:', { email: request.email });

    const loginUrl = `${this.apiUrl}/login`;
    this.logger.log('🔗 POST to:', loginUrl);

    return this.http.post<LoginResponse>(loginUrl, request).pipe(
      tap(response => {
        this.logger.log('✅ Login successful for:', { email: response.user.email, roles: response.user.roles });
        sessionStorage.setItem('auth_token', response.accessToken);
        sessionStorage.setItem('token_expires', response.expiresAt);

        this.currentUser.set({
          ...response.user,
          isSuperAdmin: response.user.roles.includes(Role.SuperAdmin)
        });

        this.isLoading.set(false);
      }),
      // The login response says who somebody is but not what they administer — /me is the
      // only payload carrying that. Without this step a division admin who signs in and goes
      // straight to an admin page has no assignments loaded, and the screens that scope
      // themselves to those assignments would have nothing to scope by. Session restore
      // already calls /me; this is the other way in.
      switchMap(response =>
        this.getCurrentUser().pipe(
          map(() => response),
          // A profile that fails to load must not fail the sign-in: the token is good and the
          // roles are known. The screens fall back to showing everything, as they did before.
          catchError(() => of(response)),
        ),
      ),
      catchError(err => {
        this.isLoading.set(false);
        this.logger.error('Login request failed', err);
        this.error.set('Login failed. Please check your credentials.');
        throw err;
      })
    );
  }

  /**
   * Get current user profile
   */
  getCurrentUser(): Observable<CurrentUserDto> {
    return this.http.get<CurrentUserDto>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.currentUser.set(user);
      })
    );
  }

  /**
   * Logout user and clear session
   */
  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  /**
   * Request password reset email
   */
  forgotPassword(request: ForgotPasswordRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/forgot-password`, request);
  }

  /**
   * Reset password with token
   */
  resetPassword(request: ResetPasswordRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, request);
  }

  /**
   * Create new user (Super Admin only)
   */
  createUser(request: CreateUserRequest): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.apiUrl}/create-user`, request);
  }

  /**
   * Get stored JWT token
   */
  getToken(): string | null {
    return sessionStorage.getItem('auth_token');
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(token?: string): boolean {
    const expiresAt = sessionStorage.getItem('token_expires');
    if (!expiresAt) return true;

    try {
      const expireDate = new Date(expiresAt);
      return new Date() > expireDate;
    } catch {
      return true;
    }
  }

  /**
   * Check if user has a specific role
   */
  hasRole(role: Role): boolean {
    return this.currentUser()?.roles.includes(role) ?? false;
  }

  /**
   * Check if user has any of the provided roles
   */
  hasAnyRole(roles: Role[]): boolean {
    const userRoles = this.currentUser()?.roles ?? [];
    return roles.some(role => userRoles.includes(role));
  }

  /**
   * Restores the signed-in user from the token held in sessionStorage.
   *
   * This runs as an app initializer, which matters twice over. It has to run at all — it used
   * to be private and was never called, so a refresh left a valid token in storage with no user
   * behind it, and adminGuard reads isAuthenticated() synchronously and sent the admin
   * straight back to the login page. And it has to finish before routing starts, or the guard
   * would run while the profile request was still in flight and bounce them just the same.
   *
   * It never rejects: a token the API refuses is cleared and the app carries on as signed out,
   * rather than failing to start.
   */
  restoreSession(): Promise<void> {
    const token = this.getToken();

    if (!token) {
      return Promise.resolve();
    }

    if (this.isTokenExpired(token)) {
      this.clearSession();
      return Promise.resolve();
    }

    return firstValueFrom(this.getCurrentUser())
      .then(() => undefined)
      .catch(() => {
        // The token did not survive the server's scrutiny. Drop it quietly; navigating from
        // here would interrupt whatever the visitor was opening.
        this.clearSession();
      });
  }

  private clearSession(): void {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('token_expires');
    this.currentUser.set(null);
  }
}
