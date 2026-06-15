import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ResetPasswordRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="reset-password-container">
      <div class="reset-password-card">
        <h1>Reset Password</h1>

        <form [formGroup]="resetForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="password">New Password</label>
            <input
              id="password"
              type="password"
              formControlName="newPassword"
              placeholder="Enter new password"
              [class.error]="resetForm.get('newPassword')?.invalid && resetForm.get('newPassword')?.touched"
            />
          </div>

          <div class="form-group">
            <label for="confirm">Confirm Password</label>
            <input
              id="confirm"
              type="password"
              formControlName="confirmPassword"
              placeholder="Confirm password"
              [class.error]="resetForm.get('confirmPassword')?.invalid && resetForm.get('confirmPassword')?.touched"
            />
            <span class="error-message" *ngIf="resetForm.get('confirmPassword')?.invalid && resetForm.get('confirmPassword')?.touched">
              Passwords must match
            </span>
          </div>

          <button
            type="submit"
            class="submit-button"
            [disabled]="resetForm.invalid || authService.isLoading()"
          >
            {{ authService.isLoading() ? 'Resetting...' : 'Reset Password' }}
          </button>
        </form>

        <div *ngIf="successMessage" class="success-message">
          {{ successMessage }}
          <p><a href="/login">Login here</a></p>
        </div>

        <div class="links">
          <a href="/login">Back to Login</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .reset-password-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .reset-password-card {
      background: white;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      padding: 2rem;
      width: 100%;
      max-width: 400px;
    }

    h1 {
      text-align: center;
      color: #333;
      margin-bottom: 1.5rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    label {
      display: block;
      margin-bottom: 0.5rem;
      color: #555;
      font-weight: 500;
    }

    input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 1rem;
    }

    input:focus {
      outline: none;
      border-color: #667eea;
    }

    input.error {
      border-color: #e74c3c;
    }

    .error-message {
      color: #e74c3c;
      font-size: 0.875rem;
      margin-top: 0.25rem;
      display: block;
    }

    .submit-button {
      width: 100%;
      padding: 0.75rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }

    .submit-button:disabled {
      opacity: 0.7;
    }

    .success-message {
      background: #d4edda;
      color: #155724;
      padding: 1rem;
      border-radius: 4px;
      margin-top: 1rem;
      text-align: center;
    }

    .success-message a {
      color: #155724;
      text-decoration: underline;
    }

    .links {
      text-align: center;
      margin-top: 1.5rem;
    }

    .links a {
      color: #667eea;
      text-decoration: none;
    }
  `]
})
export class ResetPasswordComponent {
  resetForm: FormGroup;
  successMessage = '';

  constructor(
    private formBuilder: FormBuilder,
    public authService: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.resetForm = this.formBuilder.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(group: FormGroup) {
    const password = group.get('newPassword');
    const confirm = group.get('confirmPassword');

    if (!password || !confirm) return null;
    return password.value === confirm.value ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.resetForm.invalid) return;

    const token = this.route.snapshot.queryParams['token'];
    if (!token) {
      console.error('No reset token provided');
      return;
    }

    const request: ResetPasswordRequest = {
      token,
      newPassword: this.resetForm.get('newPassword')?.value,
      confirmPassword: this.resetForm.get('confirmPassword')?.value
    };

    this.authService.resetPassword(request).subscribe({
      next: () => {
        this.successMessage = 'Password reset successfully!';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (error) => {
        console.error('Error:', error);
      }
    });
  }
}
