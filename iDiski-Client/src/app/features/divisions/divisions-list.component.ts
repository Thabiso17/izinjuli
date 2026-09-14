import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DivisionService } from '../../core/services';
import {
  DivisionDto,
  COMPETITION_STATUS_LABEL,
  COMPETITION_STATUS_CLASS,
  isCurrentCompetition,
} from '../../core/models';

/** Which competitions the page is showing. */
type Scope = 'current' | 'completed' | 'all';

@Component({
  selector: 'app-divisions-list',
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="container py-5">
      <div class="text-center mb-5">
        <h1 class="display-4 fw-bold">Divisions</h1>
        <p class="lead text-muted">Browse every division in the league</p>
      </div>

      @if (seasons().length > 1) {
        <div class="row mb-4">
          <div class="col-md-4 mx-auto">
            <select
              class="form-select form-select-lg"
              [(ngModel)]="selectedSeason"
              (ngModelChange)="loadDivisions()"
            >
              @for (season of seasons(); track season) {
                <option [ngValue]="season">{{ season }} Season</option>
              }
            </select>
          </div>
        </div>
      }

      <!-- Current by default. A competition that ended two years ago and one starting next
           week looked identical here, and the one a visitor came for is almost always the one
           still being played. The other two are a click away rather than gone. -->
      <div class="d-flex justify-content-center mb-4">
        <div class="btn-group" role="group" aria-label="Filter competitions">
          @for (option of scopes; track option.value) {
            <input
              type="radio"
              class="btn-check"
              name="scope"
              [id]="'scope-' + option.value"
              [value]="option.value"
              [(ngModel)]="scope"
              (ngModelChange)="applyScope()"
            />
            <label
              class="btn btn-outline-primary"
              [for]="'scope-' + option.value"
              [attr.data-testid]="'scope-' + option.value"
            >
              {{ option.label }}
              <span class="badge bg-light text-dark ms-1">{{ countFor(option.value) }}</span>
            </label>
          }
        </div>
      </div>

      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
      }

      @if (error()) {
        <div class="alert alert-danger text-center">{{ error() }}</div>
      }

      @if (!loading() && divisions().length > 0) {
        <div class="row g-4">
          @for (division of divisions(); track division.id) {
            <div class="col-md-6 col-lg-4">
              <a [routerLink]="['/divisions', division.id]" class="text-decoration-none">
                <div class="card h-100 shadow-sm division-card">
                  <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                      <span class="badge bg-primary">{{ division.shortCode }}</span>
                      <small class="text-muted">{{ division.season }}</small>
                    </div>

                    <div class="mb-2">
                      <span
                        [class]="'badge ' + statusClass[division.status]"
                        data-testid="division-status"
                      >
                        {{ statusLabel[division.status] }}
                      </span>
                    </div>

                    <h2 class="h5 fw-bold text-dark">{{ division.name }}</h2>

                    @if (division.description) {
                      <p class="text-muted small mb-3">{{ division.description }}</p>
                    }

                    <div class="d-flex gap-2 flex-wrap mb-3">
                      @if (division.gender) {
                        <span class="badge bg-light text-dark">{{ division.gender }}</span>
                      }
                      @if (division.ageGroup) {
                        <span class="badge bg-light text-dark">{{ division.ageGroup }}</span>
                      }
                    </div>

                    <div class="d-flex gap-3 text-muted small">
                      <span>
                        <i class="bi bi-shield-fill me-1"></i>{{ division.teamCount }} teams
                      </span>
                      <span>
                        <i class="bi bi-calendar-event me-1"></i>{{ division.matchCount }} matches
                      </span>
                    </div>

                    <!-- How far through it is, for anything that has started. A bare fixture
                         count says nothing about whether there is still something to watch. -->
                    @if (division.playedCount > 0 && division.status !== 'Completed') {
                      <div class="text-muted small mt-2" data-testid="division-progress">
                        {{ division.playedCount }} of {{ division.matchCount }} played
                      </div>
                    }
                  </div>
                </div>
              </a>
            </div>
          }
        </div>
      }

      @if (!loading() && !error() && divisions().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-trophy display-1 text-muted"></i>
            <!-- An empty filter and an empty league read very differently, and telling the
                 reader "no divisions yet" when there are four finished ones behind the next
                 tab is simply wrong. -->
            @if (all().length > 0) {
              <h2 class="h4 mt-3">Nothing {{ emptyWord() }}</h2>
              <p class="text-muted mb-0">
                There are {{ all().length }} competitions in this season under the other tabs.
              </p>
            } @else {
              <h2 class="h4 mt-3">No divisions yet</h2>
              <p class="text-muted mb-0">Divisions will appear here once they are set up.</p>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .division-card {
        transition: transform 0.2s ease, box-shadow 0.2s ease;
      }

      .division-card:hover {
        transform: translateY(-4px);
        box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
      }
    `,
  ],
})
export class DivisionsListComponent implements OnInit {
  private readonly divisionService = inject(DivisionService);

  /** Everything loaded for the chosen season, before the scope filter. */
  all = signal<DivisionDto[]>([]);
  /** What the page actually shows: `all()` narrowed to the chosen scope. */
  divisions = signal<DivisionDto[]>([]);
  seasons = signal<number[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  selectedSeason: number | undefined;

  // Current first, and selected by default: the competition somebody came to look at is
  // almost always the one still being played.
  readonly scopes: { value: Scope; label: string }[] = [
    { value: 'current', label: 'Current' },
    { value: 'completed', label: 'Past' },
    { value: 'all', label: 'All' },
  ];

  scope: Scope = 'current';

  readonly statusLabel = COMPETITION_STATUS_LABEL;
  readonly statusClass = COMPETITION_STATUS_CLASS;

  /** How many divisions each tab would show, so the reader can see where the rest are. */
  countFor(scope: Scope): number {
    return this.matching(scope).length;
  }

  applyScope(): void {
    this.divisions.set(this.matching(this.scope));
  }

  /** Reads naturally after "Nothing ..." in the empty state. */
  emptyWord(): string {
    return this.scope === 'completed' ? 'finished yet' : 'under way';
  }

  private matching(scope: Scope): DivisionDto[] {
    const divisions = this.all();

    if (scope === 'all') return divisions;

    // One definition of current, shared with the API, rather than each screen deciding for
    // itself what counts as finished.
    return divisions.filter((d) =>
      scope === 'current' ? isCurrentCompetition(d.status) : !isCurrentCompetition(d.status),
    );
  }

  ngOnInit(): void {
    this.divisionService.getAvailableSeasons().subscribe({
      next: (seasons) => {
        const ordered = [...seasons].sort((a, b) => b - a);
        this.seasons.set(ordered);
        this.selectedSeason = ordered[0];
        this.loadDivisions();
      },
      error: () => this.loadDivisions(),
    });
  }

  loadDivisions(): void {
    this.loading.set(true);
    this.error.set(null);

    this.divisionService.getAll(this.selectedSeason, true).subscribe({
      next: (divisions) => {
        this.all.set(divisions);
        this.applyScope();
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          err.error?.detail || err.error?.title || 'Failed to load divisions.'
        );
        this.loading.set(false);
      },
    });
  }
}
