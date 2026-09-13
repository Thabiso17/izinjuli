import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DivisionService, StandingsService } from '../../core/services';
import { DivisionDto, StandingDto, TopScorerDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';
import { ScopedArticlesComponent } from '../../shared/components/scoped-articles.component';
import { ScopedVideosComponent } from '../../shared/components/scoped-videos.component';

@Component({
  selector: 'app-division-detail',
  imports: [CommonModule, RouterLink, ScopedArticlesComponent, ScopedVideosComponent],
  template: `
    <div class="container py-5">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
      }

      @if (error()) {
        <div class="alert alert-danger text-center">
          {{ error() }}
          <div class="mt-3">
            <a routerLink="/divisions" class="btn btn-outline-danger">Back to divisions</a>
          </div>
        </div>
      }

      @if (!loading() && division(); as div) {
        <!-- Header -->
        <div class="card shadow-sm mb-4">
          <div class="card-body">
            <nav class="mb-3">
              <a routerLink="/divisions" class="text-decoration-none small">
                <i class="bi bi-arrow-left me-1"></i>All divisions
              </a>
            </nav>

            <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
              <div>
                <h1 class="display-5 fw-bold mb-2">{{ div.name }}</h1>
                <div class="d-flex gap-2 flex-wrap">
                  <span class="badge bg-primary">{{ div.shortCode }}</span>
                  <span class="badge bg-secondary">{{ div.season }} Season</span>
                  @if (div.gender) {
                    <span class="badge bg-light text-dark">{{ div.gender }}</span>
                  }
                  @if (div.ageGroup) {
                    <span class="badge bg-light text-dark">{{ div.ageGroup }}</span>
                  }
                </div>
              </div>

              <div class="text-end text-muted small">
                <div><i class="bi bi-shield-fill me-1"></i>{{ div.teamCount }} teams</div>
                <div><i class="bi bi-calendar-event me-1"></i>{{ div.matchCount }} matches</div>
              </div>
            </div>

            @if (div.description) {
              <p class="text-muted mt-3 mb-0">{{ div.description }}</p>
            }
          </div>
        </div>

        <div class="row g-4">
          <!-- League table -->
          <div class="col-lg-8">
            <section class="card shadow-sm mb-4">
              <div class="card-body">
                <h2 class="h4 mb-4">
                  <i class="bi bi-table text-primary me-2"></i>League Table
                </h2>

                @if (standingsLoading()) {
                  <div class="text-center py-4">
                    <div class="spinner-border spinner-border-sm" role="status"></div>
                  </div>
                }

                @if (!standingsLoading() && standings().length > 0) {
                  <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                      <thead class="table-light">
                        <tr>
                          <th scope="col">#</th>
                          <th scope="col">Team</th>
                          <th scope="col" class="text-center">P</th>
                          <th scope="col" class="text-center">W</th>
                          <th scope="col" class="text-center">D</th>
                          <th scope="col" class="text-center">L</th>
                          <th scope="col" class="text-center">GD</th>
                          <th scope="col" class="text-center">Pts</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (row of standings(); track row.teamId) {
                          <tr>
                            <td class="text-muted">{{ row.position }}</td>
                            <td>
                              <a
                                [routerLink]="['/teams', row.teamId]"
                                class="text-decoration-none text-dark d-flex align-items-center gap-2"
                              >
                                @if (getImageUrl(row.logoUrl)) {
                                  <img
                                    [src]="getImageUrl(row.logoUrl)"
                                    [alt]="row.teamName"
                                    style="width: 24px; height: 24px; object-fit: contain"
                                  />
                                }
                                <span class="fw-semibold">{{ row.teamName }}</span>
                              </a>
                            </td>
                            <td class="text-center">{{ row.played }}</td>
                            <td class="text-center">{{ row.won }}</td>
                            <td class="text-center">{{ row.drawn }}</td>
                            <td class="text-center">{{ row.lost }}</td>
                            <td class="text-center">{{ row.goalDifference }}</td>
                            <td class="text-center fw-bold">{{ row.points }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }

                @if (!standingsLoading() && standings().length === 0) {
                  <p class="text-muted text-center py-3 mb-0">
                    No matches played in this division yet.
                  </p>
                }
              </div>
            </section>

            <!-- Both hide themselves when this division has no content -->
            <app-scoped-articles
              [divisionId]="div.id"
              heading="Division News"
            />

            <app-scoped-videos
              [divisionId]="div.id"
              heading="Division Highlights"
            />
          </div>

          <!-- Top scorers -->
          <aside class="col-lg-4">
            <section class="card shadow-sm">
              <div class="card-body">
                <h2 class="h4 mb-4">
                  <i class="bi bi-trophy-fill text-warning me-2"></i>Top Scorers
                </h2>

                @if (scorersLoading()) {
                  <div class="text-center py-4">
                    <div class="spinner-border spinner-border-sm" role="status"></div>
                  </div>
                }

                @if (!scorersLoading() && topScorers().length > 0) {
                  <ul class="list-group list-group-flush">
                    @for (scorer of topScorers(); track scorer.playerId) {
                      <li class="list-group-item px-0 d-flex align-items-center gap-3">
                        <span class="text-muted small">{{ scorer.rank }}</span>

                        @if (getImageUrl(scorer.profileImageUrl)) {
                          <img
                            [src]="getImageUrl(scorer.profileImageUrl)"
                            [alt]="scorer.fullName"
                            class="rounded-circle"
                            style="width: 36px; height: 36px; object-fit: cover"
                          />
                        } @else {
                          <div
                            class="rounded-circle bg-secondary d-inline-flex align-items-center justify-content-center"
                            style="width: 36px; height: 36px"
                          >
                            <i class="bi bi-person text-white"></i>
                          </div>
                        }

                        <div class="flex-grow-1 min-width-0">
                          <a
                            [routerLink]="['/players', scorer.playerId]"
                            class="text-decoration-none text-dark fw-semibold d-block text-truncate"
                          >
                            {{ scorer.fullName }}
                          </a>
                          <small class="text-muted">{{ scorer.teamName }}</small>
                        </div>

                        <span class="badge bg-primary rounded-pill">{{ scorer.goals }}</span>
                      </li>
                    }
                  </ul>
                }

                @if (!scorersLoading() && topScorers().length === 0) {
                  <p class="text-muted text-center py-3 mb-0">No goals scored yet.</p>
                }
              </div>
            </section>
          </aside>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .min-width-0 {
        min-width: 0;
      }
    `,
  ],
})
export class DivisionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly divisionService = inject(DivisionService);
  private readonly standingsService = inject(StandingsService);

  division = signal<DivisionDto | null>(null);
  standings = signal<StandingDto[]>([]);
  topScorers = signal<TopScorerDto[]>([]);

  loading = signal(true);
  standingsLoading = signal(true);
  scorersLoading = signal(true);
  error = signal<string | null>(null);

  getImageUrl = getImageUrl;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Division not found.');
      this.loading.set(false);
      return;
    }

    this.divisionService.getById(id).subscribe({
      next: (division) => {
        this.division.set(division);
        this.loading.set(false);
        this.loadStandings(division);
        this.loadTopScorers(division);
      },
      error: (err) => {
        this.error.set(
          err.error?.detail || err.error?.title || 'Failed to load this division.'
        );
        this.loading.set(false);
      },
    });
  }

  private loadStandings(division: DivisionDto): void {
    this.standingsService.getLeagueTable(division.season, division.id).subscribe({
      next: (table) => {
        this.standings.set(table.table);
        this.standingsLoading.set(false);
      },
      error: () => this.standingsLoading.set(false),
    });
  }

  private loadTopScorers(division: DivisionDto): void {
    this.standingsService.getTopScorers(division.season, 5, division.id).subscribe({
      next: (scorers) => {
        this.topScorers.set(scorers);
        this.scorersLoading.set(false);
      },
      error: () => this.scorersLoading.set(false),
    });
  }
}
