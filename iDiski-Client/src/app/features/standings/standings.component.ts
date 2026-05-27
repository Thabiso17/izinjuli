import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StandingsService, DivisionService } from '../../core/services';
import { LeagueTableDto, StandingDto, DivisionDto, TopScorerDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';

@Component({
  selector: 'app-standings',
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="container py-5">
      <!-- Header -->
      <div class="text-center mb-5">
        <h1 class="display-4 fw-bold">League Standings</h1>
        <p class="lead text-muted">Current league table and top scorers</p>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-4">
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
            <div class="col-md-4">
              <label class="form-label">Division</label>
              <select
                class="form-select"
                [(ngModel)]="filterDivisionId"
                (ngModelChange)="loadData()"
              >
                @for (division of divisions(); track division.id) {
                  <option [ngValue]="division.id">
                    {{ division.name }}
                  </option>
                }
              </select>
            </div>
            <div class="col-md-4">
              <label class="form-label">Matchweek (Historical)</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterMatchweek"
                (ngModelChange)="loadData()"
                placeholder="Latest"
                min="1"
                max="38"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Tabs -->
      <ul class="nav nav-tabs mb-4" role="tablist">
        <li class="nav-item" role="presentation">
          <button
            class="nav-link"
            [class.active]="activeTab() === 'table'"
            (click)="activeTab.set('table')"
          >
            <i class="bi bi-trophy me-2"></i>League Table
          </button>
        </li>
        <li class="nav-item" role="presentation">
          <button
            class="nav-link"
            [class.active]="activeTab() === 'scorers'"
            (click)="activeTab.set('scorers')"
          >
            <i class="bi bi-star me-2"></i>Top Scorers
          </button>
        </li>
      </ul>

      <!-- Tab Content -->
      <div class="tab-content">
        <!-- League Table Tab -->
        @if (activeTab() === 'table') {
          @if (loadingTable()) {
            <div class="text-center py-5">
              <div class="spinner-border text-primary" role="status"></div>
            </div>
          }

          @if (!loadingTable() && leagueTable()) {
            <div class="card shadow-sm">
              <div class="card-header bg-white">
                <div class="d-flex justify-content-between align-items-center">
                  <div>
                    <h5 class="mb-0">{{ getSelectedDivisionName() }}</h5>
                    <small class="text-muted">Season {{ leagueTable()!.season }}/{{ leagueTable()!.season + 1 }}</small>
                  </div>
                  <small class="text-muted">
                    Matchweeks Played: {{ leagueTable()!.matchweeksPlayed }}
                  </small>
                </div>
              </div>
              <div class="table-responsive">
                <table class="table table-hover standings-table mb-0">
                  <thead class="table-light">
                    <tr>
                      <th class="text-center">#</th>
                      <th>Team</th>
                      <th class="text-center">P</th>
                      <th class="text-center">W</th>
                      <th class="text-center">D</th>
                      <th class="text-center">L</th>
                      <th class="text-center">GF</th>
                      <th class="text-center">GA</th>
                      <th class="text-center">GD</th>
                      <th class="text-center fw-bold">PTS</th>
                      <th>Form</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (standing of leagueTable()!.table; track standing.teamId) {
                      <tr [class.table-success]="standing.position <= 2"
                          [class.table-warning]="standing.position >= leagueTable()!.table.length - 1">
                        <td class="text-center fw-bold">{{ standing.position }}</td>
                        <td>
                          <a [routerLink]="['/teams', standing.teamId]" class="text-decoration-none d-flex align-items-center">
                            @if (standing.logoUrl) {
                              <img
                                [src]="standing.logoUrl"
                                [alt]="standing.teamName"
                                class="team-logo-xs me-2"
                              />
                            }
                            <div>
                              <div class="fw-semibold text-dark">{{ standing.teamName }}</div>
                              <small class="text-muted">{{ standing.shortCode }}</small>
                            </div>
                          </a>
                        </td>
                        <td class="text-center">{{ standing.played }}</td>
                        <td class="text-center">{{ standing.won }}</td>
                        <td class="text-center">{{ standing.drawn }}</td>
                        <td class="text-center">{{ standing.lost }}</td>
                        <td class="text-center">{{ standing.goalsFor }}</td>
                        <td class="text-center">{{ standing.goalsAgainst }}</td>
                        <td class="text-center">{{ standing.goalDifference }}</td>
                        <td class="text-center fw-bold fs-6">{{ standing.points }}</td>
                        <td>
                          <div class="form-dots">
                            @for (result of getFormArray(standing.form); track $index) {
                              @switch (result) {
                                @case ('W') {
                                  <span class="form-dot form-win" title="Win"></span>
                                }
                                @case ('D') {
                                  <span class="form-dot form-draw" title="Draw"></span>
                                }
                                @case ('L') {
                                  <span class="form-dot form-loss" title="Loss"></span>
                                }
                              }
                            }
                          </div>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              <div class="card-footer bg-white text-muted">
                <small>
                  <span class="badge bg-success me-2">Top 2</span>
                  <span class="badge bg-warning">Bottom 2</span>
                </small>
              </div>
            </div>
          }

          @if (!loadingTable() && !leagueTable()) {
            <div class="text-center py-5">
              <i class="bi bi-trophy display-1 text-muted"></i>
              <h3 class="mt-3">No Standings Available</h3>
              <p class="text-muted">Check back after matches are completed</p>
            </div>
          }
        }

        <!-- Top Scorers Tab -->
        @if (activeTab() === 'scorers') {
          @if (loadingScorers()) {
            <div class="text-center py-5">
              <div class="spinner-border text-primary" role="status"></div>
            </div>
          }

          @if (!loadingScorers() && topScorers().length > 0) {
            <div class="row g-4">
              @for (scorer of topScorers(); track scorer.playerId; let idx = $index) {
                <div class="col-md-6 col-lg-4">
                  <a [routerLink]="['/players', scorer.playerId]" class="text-decoration-none">
                    <div class="card scorer-card h-100">
                      <div class="card-body">
                        <div class="d-flex align-items-start">
                          <!-- Rank Badge -->
                          <div class="rank-badge me-3">
                            @if (idx === 0) {
                              <div class="rank-number gold">{{ scorer.rank }}</div>
                              <i class="bi bi-trophy-fill text-warning"></i>
                            } @else if (idx === 1) {
                              <div class="rank-number silver">{{ scorer.rank }}</div>
                              <i class="bi bi-trophy-fill text-secondary"></i>
                            } @else if (idx === 2) {
                              <div class="rank-number bronze">{{ scorer.rank }}</div>
                              <i class="bi bi-trophy-fill" style="color: #cd7f32;"></i>
                            } @else {
                              <div class="rank-number">{{ scorer.rank }}</div>
                            }
                          </div>

                          <!-- Player Info -->
                          <div class="flex-grow-1">
                            @if (getImageUrl(scorer.profileImageUrl)) {
                              <img
                                [src]="getImageUrl(scorer.profileImageUrl)"
                                [alt]="scorer.fullName"
                                class="player-avatar-sm mb-2"
                              />
                            }
                            <h6 class="mb-1 text-dark">{{ scorer.fullName }}</h6>
                            <small class="text-muted d-block mb-2">
                              {{ scorer.teamName }} • {{ scorer.position }}
                            </small>
                            <div class="stats-row">
                              <div class="stat-item">
                                <i class="bi bi-trophy-fill text-warning"></i>
                                <strong>{{ scorer.goals }}</strong>
                                <small class="text-muted">Goals</small>
                              </div>
                              <div class="stat-item">
                                <i class="bi bi-hand-thumbs-up-fill text-info"></i>
                                <strong>{{ scorer.assists }}</strong>
                                <small class="text-muted">Assists</small>
                              </div>
                              <div class="stat-item">
                                <i class="bi bi-star-fill text-primary"></i>
                                <strong>{{ scorer.goalContributions }}</strong>
                                <small class="text-muted">Contributions</small>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </a>
                </div>
              }
            </div>
          }

          @if (!loadingScorers() && topScorers().length === 0) {
            <div class="text-center py-5">
              <i class="bi bi-star display-1 text-muted"></i>
              <h3 class="mt-3">No Scorers Yet</h3>
              <p class="text-muted">Check back after goals are scored</p>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .nav-link {
      cursor: pointer;
    }

    .standings-table {
      font-size: 0.9rem;
    }

    .standings-table td {
      vertical-align: middle;
    }

    .team-logo-xs {
      width: 30px;
      height: 30px;
      object-fit: contain;
    }

    .form-dots {
      display: flex;
      gap: 4px;
    }

    .form-dot {
      width: 18px;
      height: 18px;
      border-radius: 50%;
      display: inline-block;
    }

    .form-win {
      background-color: #198754;
    }

    .form-draw {
      background-color: #6c757d;
    }

    .form-loss {
      background-color: #dc3545;
    }

    .scorer-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .scorer-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    }

    .rank-badge {
      text-align: center;
      min-width: 50px;
    }

    .rank-number {
      font-size: 1.5rem;
      font-weight: bold;
      color: #6c757d;
    }

    .rank-number.gold {
      color: #ffd700;
    }

    .rank-number.silver {
      color: #c0c0c0;
    }

    .rank-number.bronze {
      color: #cd7f32;
    }

    .player-avatar-sm {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      object-fit: cover;
    }

    .stats-row {
      display: flex;
      gap: 1rem;
      margin-top: 0.5rem;
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      font-size: 0.875rem;
    }

    .stat-item i {
      font-size: 1.25rem;
      margin-bottom: 0.25rem;
    }

    .stat-item strong {
      font-size: 1.25rem;
    }

    .stat-item small {
      font-size: 0.75rem;
    }
  `],
})
export class StandingsComponent implements OnInit {
  private standingsService = inject(StandingsService);
  private divisionService = inject(DivisionService);

  leagueTable = signal<LeagueTableDto | null>(null);
  topScorers = signal<TopScorerDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  availableSeasons = signal<number[]>([]);

  loadingTable = signal(false);
  loadingScorers = signal(false);
  activeTab = signal<'table' | 'scorers'>('table');

  currentYear = new Date().getFullYear();
  filterSeason = this.currentYear;
  filterDivisionId?: string;
  filterMatchweek?: number;

  // Use shared utility for image URL handling
  getImageUrl = getImageUrl;

  ngOnInit() {
    this.loadAvailableSeasons();
  }

  loadAvailableSeasons() {
    this.divisionService.getAvailableSeasons().subscribe({
      next: (seasons) => {
        this.availableSeasons.set(seasons);
        // Set default to most recent season
        if (seasons.length > 0) {
          this.filterSeason = seasons[0];
        }
        this.loadDivisions();
      },
      error: (err) => {
        console.error('Failed to load seasons:', err);
        this.loadDivisions();
        this.loadData();
      },
    });
  }

  loadDivisions() {
    this.divisionService.getAll(this.filterSeason, undefined).subscribe({
      next: (data) => {
        this.divisions.set(data);
        // Auto-select first division if none selected
        if (data.length > 0 && !this.filterDivisionId) {
          this.filterDivisionId = data[0].id;
          this.loadData();
        }
      },
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  onSeasonChange() {
    // Reset division filter when season changes
    this.filterDivisionId = undefined;
    this.loadDivisions();
  }

  getSelectedDivisionName(): string {
    const division = this.divisions().find(d => d.id === this.filterDivisionId);
    return division?.name || 'League Standings';
  }

  loadData() {
    this.loadLeagueTable();
    this.loadTopScorers();
  }

  loadLeagueTable() {
    this.loadingTable.set(true);
    this.standingsService
      .getLeagueTable(this.filterSeason, this.filterDivisionId, this.filterMatchweek)
      .subscribe({
        next: (data) => {
          this.leagueTable.set(data);
          this.loadingTable.set(false);
        },
        error: (err) => {
          console.error('Failed to load league table:', err);
          this.loadingTable.set(false);
        },
      });
  }

  loadTopScorers() {
    this.loadingScorers.set(true);
    this.standingsService.getTopScorers(this.filterSeason, 12, this.filterDivisionId).subscribe({
      next: (data) => {
        this.topScorers.set(data);
        this.loadingScorers.set(false);
      },
      error: (err) => {
        console.error('Failed to load top scorers:', err);
        this.loadingScorers.set(false);
      },
    });
  }

  getFormArray(form: string): string[] {
    return form ? form.split(',') : [];
  }
}
