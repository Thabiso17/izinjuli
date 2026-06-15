import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error-state">
      <div class="error-icon">⚠️</div>
      <h2>{{ title }}</h2>
      <p>{{ message }}</p>
      <button *ngIf="onRetry" (click)="onRetry()" class="retry-button">
        Try Again
      </button>
    </div>
  `,
  styles: [`
    .error-state {
      padding: 3rem 2rem;
      text-align: center;
      background-color: #fff3cd;
      border: 1px solid #ffc107;
      border-radius: 8px;
      margin: 2rem 0;
    }

    .error-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    h2 {
      color: #333;
      margin: 0.5rem 0;
    }

    p {
      color: #666;
      margin: 1rem 0;
    }

    .retry-button {
      margin-top: 1rem;
      padding: 0.75rem 1.5rem;
      background-color: #ffc107;
      color: #333;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
      transition: background-color 0.3s;
    }

    .retry-button:hover {
      background-color: #ffb300;
    }
  `]
})
export class ErrorStateComponent {
  @Input() title = 'Error Loading Content';
  @Input() message = 'Failed to load the requested content. Please try again.';
  @Input() onRetry: (() => void) | null = null;
}
