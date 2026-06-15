import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, throwError } from 'rxjs';
import {
  LoginRequest,
  LoginResponse,
  CurrentUserDto,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  CreateUserRequest,
  Role
} from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5000/api/auth';

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
           user?.roles.includes(Role.DivisionAdmin) ||
           user?.roles.includes(Role.TeamAdmin) ||
           false;
  });

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.restoreSession();
  }

  /**
   * Login with email and password
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    this.isLoading.set(true);
    this.error.set(null);

    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        // Store JWT token in sessionStorage
        sessionStorage.setItem('auth_token', response.accessToken);
        sessionStorage.setItem('token_expires', response.expiresAt);

        // Update current user signal
        this.currentUser.set({
          ...response.user,
          isSuperAdmin: response.user.roles.includes(Role.SuperAdmin)
        });

        this.isLoading.set(false);
      }),
      throwError => {
        this.isLoading.set(false);
        this.error.set('Login failed. Please check your credentials.');
        return throwError;
      }
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
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('token_expires');
    this.currentUser.set(null);
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
   * Restore session from sessionStorage on page refresh
   */
  private restoreSession(): void {
    const token = this.getToken();

    if (token && !this.isTokenExpired(token)) {
      // Token exists and is valid, fetch current user
      this.getCurrentUser().subscribe({
        error: () => {
          // Token is invalid, clear it
          this.logout();
        }
      });
    } else if (token) {
      // Token expired, clear it
      this.logout();
    }
  }
}
