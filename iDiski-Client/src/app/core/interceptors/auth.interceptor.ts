import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

/**
 * HTTP Interceptor that:
 * 1. Attaches JWT token to all outgoing requests
 * 2. Catches 401 responses and logs out user
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Get JWT token from session storage
    const token = this.authService.getToken();

    // Clone request and add Authorization header if token exists
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Token invalid or expired
          console.warn('🔐 Unauthorized (401) - logging out');
          this.authService.logout();
          this.router.navigate(['/login']);
        } else if (error.status === 403) {
          // User lacks permission
          console.warn('🚫 Forbidden (403) - insufficient permissions');
        }

        return throwError(() => error);
      })
    );
  }
}
