import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatchService } from '../../../core/services/match.service';
import { MatchResultDto } from '../../../core/models';

@Component({
  selector: 'app-fixtures-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card shadow-sm mb-4">
      <div class="card-body">
        <h2 class="card-title h4 mb-4">
          <i class="bi bi-calendar-event me-2"></i>Recent Fixtures
        </h2>

        @if (loading()) {
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
          </div>
        }

        @if (!loading() && matches().length > 0) {
          @for (match of matches(); track match.id) {
            <div class="d-flex justify-content-between align-items-center p-3 bg-light rounded mb-3">
              <div class="d-flex align-items-center" style="flex: 1">
                @if (match.homeTeamLogo) {
                  <img
                    [src]="match.homeTeamLogo"
                    [alt]="match.homeTeamName"
                    style="width: 32px; height: 32px; object-fit: contain"
                    class="me-3"
                  />
                } @else {
                  <div style="width: 32px; height: 32px;" class="bg-secondary rounded me-3"></div>
                }
                <span class="fw-semibold">{{ match.homeTeamName }}</span>
              </div>
              <div class="text-center px-3">
                <div class="fs-5 fw-bold">{{ match.scoreDisplay }}</div>
                <small class="text-muted">{{ match.matchDate | date: 'shortDate' }}</small>
              </div>
              <div class="d-flex align-items-center justify-content-end" style="flex: 1">
                <span class="fw-semibold">{{ match.awayTeamName }}</span>
                @if (match.awayTeamLogo) {
                  <img
                    [src]="match.awayTeamLogo"
                    [alt]="match.awayTeamName"
                    style="width: 32px; height: 32px; object-fit: contain"
                    class="ms-3"
                  />
                } @else {
                  <div style="width: 32px; height: 32px;" class="bg-secondary rounded ms-3"></div>
                }
              </div>
            </div>
          }
        }

        @if (!loading() && matches().length === 0) {
          <div class="text-center py-4 text-muted">
            <i class="bi bi-inbox display-4"></i>
            <p class="mt-2">No recent fixtures available</p>
          </div>
        }

        @if (error()) {
          <div class="alert alert-danger" role="alert">
            {{ error() }}
          </div>
        }
      </div>
    </section>
  `,
})
export class FixturesSectionComponent implements OnInit {
  private matchService = inject(MatchService);

  matches = signal<MatchResultDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadMatches();
  }

  loadMatches() {
    this.loading.set(true);
    const currentSeason = new Date().getFullYear();

    this.matchService.getAll(currentSeason, undefined, undefined, 'Completed', undefined, 1, 5).subscribe({
      next: (data) => {
        this.matches.set(data.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load fixtures');
        this.loading.set(false);
        console.error('Error loading fixtures:', err);
      },
    });
  }
}
