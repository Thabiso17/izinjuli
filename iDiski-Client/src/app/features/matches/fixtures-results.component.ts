import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatchService, DivisionService } from '../../core/services';
import { MatchResultDto, DivisionDto, MatchStatus } from '../../core/models';

@Component({
  selector: 'app-fixtures-results',
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="container py-5">
      <!-- Header -->
      <div class="text-center mb-5">
        <h1 class="display-4 fw-bold">Fixtures & Results</h1>
        <p class="lead text-muted">Browse matches, upcoming fixtures, and results</p>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-3">
              <label class="form-label">Season</label>
              <select
                class="form-select"
                [(ngModel)]="filterSeason"
                (ngModelChange)="onSeasonChange()"
              >
                @for (season of availableSeasons(); track season) {
                  <option [ngValue]="season">{{ season }}/{{ season + 1 }}</option>
                }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label">Division</label>
              <select
                class="form-select"
                [(ngModel)]="filterDivisionId"
                (ngModelChange)="loadMatches()"
              >
                <option [ngValue]="undefined">All Divisions</option>
                @for (division of divisions(); track division.id) {
                  <option [ngValue]="division.id">
                    {{ division.name }}
                  </option>
                }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label">Status</label>
              <select
                class="form-select"
                [(ngModel)]="filterStatus"
                (ngModelChange)="loadMatches()"
              >
                <option [ngValue]="undefined">All Statuses</option>
                <option value="Scheduled">Scheduled</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
                <option value="Postponed">Postponed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label">Matchweek</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterMatchweek"
                (ngModelChange)="loadMatches()"
                placeholder="All"
                min="1"
                max="38"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      <!-- Matches List -->
      @if (!loading() && matches().length > 0) {
        <div class="row">
          @for (match of matches(); track match.id) {
            <div class="col-12 mb-3">
              <a [routerLink]="['/matches', match.id]" class="text-decoration-none">
                <div class="card match-card">
                  <div class="card-body">
                    <div class="row align-items-center">
                      <!-- Match Date & Status -->
                      <div class="col-md-2 text-center mb-3 mb-md-0">
                        <div class="text-muted">
                          <small>{{ match.matchDate | date: 'EEE' }}</small>
                        </div>
                        <div class="fw-bold">{{ match.matchDate | date: 'MMM d' }}</div>
                        <div class="text-muted">
                          <small>{{ match.matchDate | date: 'h:mm a' }}</small>
                        </div>
                        @if (match.status !== 'Scheduled' && match.status !== 'Completed') {
                          <div class="mt-2">
                            @switch (match.status) {
                              @case ('InProgress') {
                                <span class="badge bg-success">Live</span>
                              }
                              @case ('Postponed') {
                                <span class="badge bg-warning">Postponed</span>
                              }
                              @case ('Cancelled') {
                                <span class="badge bg-danger">Cancelled</span>
                              }
                            }
                          </div>
                        }
                      </div>

                      <!-- Home Team -->
                      <div class="col-5 text-end">
                        <div class="d-flex align-items-center justify-content-end">
                          <div class="me-3">
                            <h5 class="mb-0 text-dark">{{ match.homeTeamName }}</h5>
                            <small class="text-muted">{{ match.homeTeamShortCode }}</small>
                          </div>
                          @if (match.homeTeamLogo) {
                            <img
                              [src]="match.homeTeamLogo"
                              [alt]="match.homeTeamName"
                              class="team-logo-sm"
                            />
                          } @else {
                            <div class="team-logo-sm-placeholder">
                              <i class="bi bi-shield"></i>
                            </div>
                          }
                        </div>
                      </div>

                      <!-- Score -->
                      <div class="col-2 text-center">
                        @if (match.status === 'Completed') {
                          <div class="score-display">
                            <span class="score-number">{{ match.homeScore }}</span>
                            <span class="score-separator">-</span>
                            <span class="score-number">{{ match.awayScore }}</span>
                          </div>
                        } @else {
                          <div class="vs-display">VS</div>
                        }
                        <div class="mt-2">
                          <small class="badge bg-light text-dark">
                            MW {{ match.matchweekNumber }}
                          </small>
                        </div>
                      </div>

                      <!-- Away Team -->
                      <div class="col-5 text-start">
                        <div class="d-flex align-items-center">
                          @if (match.awayTeamLogo) {
                            <img
                              [src]="match.awayTeamLogo"
                              [alt]="match.awayTeamName"
                              class="team-logo-sm"
                            />
                          } @else {
                            <div class="team-logo-sm-placeholder">
                              <i class="bi bi-shield"></i>
                            </div>
                          }
                          <div class="ms-3">
                            <h5 class="mb-0 text-dark">{{ match.awayTeamName }}</h5>
                            <small class="text-muted">{{ match.awayTeamShortCode }}</small>
                          </div>
                        </div>
                      </div>
                    </div>

                    <!-- Match Details -->
                    @if (match.venue || match.divisionName) {
                      <div class="row mt-3">
                        <div class="col-12 text-center">
                          @if (match.venue) {
                            <small class="text-muted me-3">
                              <i class="bi bi-geo-alt"></i> {{ match.venue }}
                            </small>
                          }
                          @if (match.divisionName) {
                            <small class="text-muted">
                              <i class="bi bi-trophy"></i> {{ match.divisionName }}
                            </small>
                          }
                        </div>
                      </div>
                    }
                  </div>
                </div>
              </a>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <nav class="mt-4">
            <ul class="pagination justify-content-center">
              <li class="page-item" [class.disabled]="currentPage === 1">
                <button
                  class="page-link"
                  (click)="goToPage(currentPage - 1)"
                  [disabled]="currentPage === 1"
                >
                  Previous
                </button>
              </li>
              @for (page of getPageNumbers(); track page) {
                <li class="page-item" [class.active]="page === currentPage">
                  <button class="page-link" (click)="goToPage(page)">
                    {{ page }}
                  </button>
                </li>
              }
              <li class="page-item" [class.disabled]="currentPage === totalPages()">
                <button
                  class="page-link"
                  (click)="goToPage(currentPage + 1)"
                  [disabled]="currentPage === totalPages()"
                >
                  Next
                </button>
              </li>
            </ul>
          </nav>
        }
      }

      <!-- Empty State -->
      @if (!loading() && matches().length === 0) {
        <div class="text-center py-5">
          <i class="bi bi-calendar-x display-1 text-muted"></i>
          <h3 class="mt-3">No Matches Found</h3>
          <p class="text-muted">Try adjusting your filters</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .match-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .match-card:hover {
      transform: translateY(-3px);
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    }

    .team-logo-sm {
      width: 50px;
      height: 50px;
      object-fit: contain;
    }

    .team-logo-sm-placeholder {
      width: 50px;
      height: 50px;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border-radius: 8px;
    }

    .team-logo-sm-placeholder i {
      font-size: 1.5rem;
      color: #adb5bd;
    }

    .score-display {
      font-size: 2rem;
      font-weight: bold;
      color: #212529;
    }

    .score-number {
      display: inline-block;
      min-width: 40px;
    }

    .score-separator {
      margin: 0 10px;
      color: #6c757d;
    }

    .vs-display {
      font-size: 1.5rem;
      font-weight: bold;
      color: #6c757d;
    }

    .page-link {
      cursor: pointer;
    }
  `],
})
export class FixturesResultsComponent implements OnInit {
  private matchService = inject(MatchService);
  private divisionService = inject(DivisionService);

  matches = signal<MatchResultDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  totalPages = signal(1);
  currentPage = 1;
  pageSize = 20;
  availableSeasons = signal<number[]>([]);

  currentYear = new Date().getFullYear();
  filterSeason = this.currentYear;
  filterDivisionId?: string;
  filterStatus?: string;
  filterMatchweek?: number;

  ngOnInit() {
    this.loadAvailableSeasons();
  }

  loadAvailableSeasons() {
    this.divisionService.getAvailableSeasons().subscribe({
      next: (seasons) => {
        this.availableSeasons.set(seasons);
        if (seasons.length > 0) {
          this.filterSeason = seasons[0];
        }
        this.loadDivisions();
        this.loadMatches();
      },
      error: (err) => {
        console.error('Failed to load seasons:', err);
        this.loadDivisions();
        this.loadMatches();
      },
    });
  }

  loadDivisions() {
    this.divisionService.getAll(this.filterSeason, undefined).subscribe({
      next: (data) => this.divisions.set(data),
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  onSeasonChange() {
    this.filterDivisionId = undefined;
    this.loadDivisions();
    this.loadMatches();
  }

  loadMatches() {
    this.loading.set(true);
    this.currentPage = 1;

    this.matchService
      .getAll(
        this.filterSeason,
        this.filterMatchweek,
        undefined,
        this.filterStatus,
        this.filterDivisionId,
        this.currentPage,
        this.pageSize
      )
      .subscribe({
        next: (data) => {
          this.matches.set(data.items);
          this.totalPages.set(data.totalPages);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Failed to load matches:', err);
          this.loading.set(false);
        },
      });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage = page;

    this.loading.set(true);
    this.matchService
      .getAll(
        this.filterSeason,
        this.filterMatchweek,
        undefined,
        this.filterStatus,
        this.filterDivisionId,
        this.currentPage,
        this.pageSize
      )
      .subscribe({
        next: (data) => {
          this.matches.set(data.items);
          this.loading.set(false);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (err) => {
          console.error('Failed to load matches:', err);
          this.loading.set(false);
        },
      });
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage;
    const pages: number[] = [];

    // Show max 5 page numbers
    let start = Math.max(1, current - 2);
    let end = Math.min(total, start + 4);

    if (end - start < 4) {
      start = Math.max(1, end - 4);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }
}
