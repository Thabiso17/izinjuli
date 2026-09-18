import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CompetitionService, DivisionService, StandingsService } from '../../core/services';
import {
  CompetitionDto,
  COMPETITION_FORMAT_LABEL,
  COMPETITION_STATUS_CLASS,
  COMPETITION_STATUS_LABEL,
  DivisionDto,
  TopScorerDto,
} from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';
import { ScopedArticlesComponent } from '../../shared/components/scoped-articles.component';
import { ScopedVideosComponent } from '../../shared/components/scoped-videos.component';

/**
 * A division: the pool of teams, and what they are playing.
 *
 * It used to show a table or a bracket, because a division was the competition. Now it lists
 * the competitions being run from it — the league, the cup, the sponsor's tournament — and each
 * of those has its own page. A single page could not honestly show three at once, and a single
 * status could not describe them.
 */
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
        <!-- ── The pool ───────────────────────────────────────────────────── -->
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
                <div>
                  <i class="bi bi-trophy me-1"></i>{{ div.competitionCount }}
                  {{ div.competitionCount === 1 ? 'competition' : 'competitions' }}
                </div>
              </div>
            </div>

            @if (div.description) {
              <p class="text-muted mt-3 mb-0">{{ div.description }}</p>
            }
          </div>
        </div>

        <div class="row g-4">
          <!-- ── What is being played here ─────────────────────────────────── -->
          <div class="col-12">
            <section class="card shadow-sm">
              <div class="card-body">
                <h2 class="h4 mb-4">
                  <i class="bi bi-trophy text-primary me-2"></i>Competitions
                </h2>

                @if (competitionsLoading()) {
                  <div class="text-center py-4">
                    <div class="spinner-border spinner-border-sm" role="status"></div>
                  </div>
                } @else {
                  <div class="row g-3">
                    @for (competition of competitions(); track competition.id) {
                      <div class="col-md-6 col-lg-4">
                        <a
                          [routerLink]="['/competitions', competition.id]"
                          class="text-decoration-none"
                        >
                          <div
                            class="card h-100 border competition-card"
                            data-testid="competition-card"
                          >
                            <div class="card-body">
                              <div class="d-flex justify-content-between align-items-start mb-2">
                                <span class="badge bg-primary">{{ competition.shortCode }}</span>
                                <span [class]="'badge ' + statusClass[competition.status]">
                                  {{ statusLabel[competition.status] }}
                                </span>
                              </div>

                              <h3 class="h6 fw-bold text-dark mb-2">{{ competition.name }}</h3>

                              <div class="text-muted small">
                                <div>{{ formatLabel[competition.format] }}</div>
                                <div>
                                  {{ competition.entrantCount }} entrants
                                  @if (competition.divisionsRepresented > 1) {
                                    <span class="text-info">
                                      · from {{ competition.divisionsRepresented }} divisions
                                    </span>
                                  }
                                </div>
                                @if (
                                  competition.playedCount > 0 && competition.status !== 'Completed'
                                ) {
                                  <div>
                                    {{ competition.playedCount }} of
                                    {{ competition.matchCount }} played
                                  </div>
                                }
                              </div>
                            </div>
                          </div>
                        </a>
                      </div>
                    } @empty {
                      <div class="col-12">
                        <p class="text-muted mb-0" data-testid="no-competitions">
                          Nothing is being played in this division yet.
                        </p>
                      </div>
                    }
                  </div>
                }
              </div>
            </section>
          </div>

          <div class="col-lg-8">
            <!-- Both hide themselves when this division has no content -->
            <app-scoped-articles [divisionId]="div.id" heading="Division News" />
            <app-scoped-videos [divisionId]="div.id" heading="Division Highlights" />
          </div>

          <!-- ── Top scorers, across everything the division plays ─────────── -->
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

      .competition-card {
        transition: transform 0.15s ease, box-shadow 0.15s ease;
      }

      .competition-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 0.35rem 0.75rem rgba(0, 0, 0, 0.12) !important;
      }
    `,
  ],
})
export class DivisionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly divisionService = inject(DivisionService);
  private readonly competitionService = inject(CompetitionService);
  private readonly standingsService = inject(StandingsService);

  division = signal<DivisionDto | null>(null);
  competitions = signal<CompetitionDto[]>([]);
  topScorers = signal<TopScorerDto[]>([]);

  loading = signal(true);
  competitionsLoading = signal(true);
  scorersLoading = signal(true);
  error = signal<string | null>(null);

  readonly formatLabel = COMPETITION_FORMAT_LABEL;
  readonly statusLabel = COMPETITION_STATUS_LABEL;
  readonly statusClass = COMPETITION_STATUS_CLASS;

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
        this.loadTopScorers(division);
      },
      error: (err) => {
        this.error.set(err.error?.detail || err.error?.title || 'Failed to load this division.');
        this.loading.set(false);
      },
    });

    // Asked for by division rather than filtered from a season's worth, and asked for
    // independently of the division itself so a slow list does not hold up the header.
    this.competitionService.getAll(id).subscribe({
      next: (competitions) => {
        this.competitions.set(competitions);
        this.competitionsLoading.set(false);
      },
      error: () => this.competitionsLoading.set(false),
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
