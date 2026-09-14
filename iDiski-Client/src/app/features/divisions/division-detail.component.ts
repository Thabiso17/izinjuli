import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DivisionService, MatchService, StandingsService } from '../../core/services';
import { DivisionDto, MatchResultDto, StandingDto, TopScorerDto } from '../../core/models';
import { BracketComponent } from './bracket.component';
import { StandingsTableComponent } from './standings-table.component';
import { getImageUrl } from '../../core/utils/image.utils';
import { ScopedArticlesComponent } from '../../shared/components/scoped-articles.component';
import { ScopedVideosComponent } from '../../shared/components/scoped-videos.component';

@Component({
  selector: 'app-division-detail',
  imports: [
    CommonModule,
    RouterLink,
    ScopedArticlesComponent,
    ScopedVideosComponent,
    BracketComponent,
    StandingsTableComponent,
  ],
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
          <!-- The bracket, for a competition that has one. A league table says nothing about
               a cup: knockout ties are deliberately kept out of the standings, so this page
               used to tell a knockout division it had no matches however many were played. -->
          @if (division()?.format !== 'League') {
            <div class="col-12">
              <section class="card shadow-sm mb-4">
                <div class="card-body">
                  <h2 class="h4 mb-4">
                    <i class="bi bi-diagram-3 text-primary me-2"></i>
                    {{ division()?.format === 'GroupAndKnockout' ? 'Knockout Stage' : 'Bracket' }}
                  </h2>

                  @if (fixturesLoading()) {
                    <div class="text-center py-4">
                      <div class="spinner-border spinner-border-sm" role="status"></div>
                    </div>
                  } @else {
                    <app-bracket [matches]="fixtures()" />
                  }
                </div>
              </section>
            </div>
          }

          <div class="col-lg-8">
            <!-- A knockout has no table. Every tie in it is excluded from the standings by
                 design, so the section would only ever say the division had played nothing. -->
            @if (division()?.format !== 'Knockout') {
            <section class="card shadow-sm mb-4">
              <div class="card-body">
                <h2 class="h4 mb-4">
                  <i class="bi bi-table text-primary me-2"></i>
                  {{ division()?.format === 'GroupAndKnockout' ? 'Group Stage' : 'League Table' }}
                </h2>

                @if (standingsLoading()) {
                  <div class="text-center py-4">
                    <div class="spinner-border spinner-border-sm" role="status"></div>
                  </div>
                } @else if (division()?.format === 'GroupAndKnockout') {
                  <!-- One table per group. Merging them would rank teams against opponents
                       they have never played, which is not a table of anything. -->
                  @for (group of groupTables(); track group.name) {
                    <h3 class="h6 text-uppercase text-muted mt-3 mb-2" data-testid="group-heading">
                      Group {{ group.name }}
                    </h3>
                    <app-standings-table
                      [rows]="group.table"
                      emptyMessage="No matches played in this group yet."
                    />
                  } @empty {
                    <p class="text-muted text-center py-3 mb-0">
                      The groups have not been drawn yet.
                    </p>
                  }
                } @else {
                  <app-standings-table
                    [rows]="standings()"
                    emptyMessage="No matches played in this division yet."
                  />
                }
              </div>
            </section>
            }

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
  private readonly matchService = inject(MatchService);

  division = signal<DivisionDto | null>(null);
  standings = signal<StandingDto[]>([]);
  fixtures = signal<MatchResultDto[]>([]);
  groupTables = signal<{ name: string; table: StandingDto[] }[]>([]);
  topScorers = signal<TopScorerDto[]>([]);

  loading = signal(true);
  standingsLoading = signal(true);
  fixturesLoading = signal(true);
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
        // A knockout has no table, and a group stage builds one per group from its fixtures.
        if (division.format === 'League') this.loadStandings(division);
        else this.standingsLoading.set(false);

        this.loadTopScorers(division);

        if (division.format !== 'League') this.loadBracket(division);
        else this.fixturesLoading.set(false);
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

  private loadBracket(division: DivisionDto): void {
    // A page size that holds a bracket of sixty-four and its group stage. Paging this would
    // mean a bracket missing its later rounds, which is worse than a slow page.
    this.matchService
      .getAll(division.season, undefined, undefined, undefined, division.id, 1, 200)
      .subscribe({
        next: (page) => {
          this.fixtures.set(page.items);
          this.fixturesLoading.set(false);

          if (division.format === 'GroupAndKnockout') this.loadGroupTables(division, page.items);
        },
        error: () => this.fixturesLoading.set(false),
      });
  }

  /**
   * A table per group. Which teams are in a group is carried by its fixtures rather than by
   * the team, so the groups themselves are read off the fixtures already loaded for the
   * bracket rather than asked for separately.
   */
  private loadGroupTables(division: DivisionDto, fixtures: MatchResultDto[]): void {
    const names = [
      ...new Set(
        fixtures
          .filter((f) => f.stage === 'Group' && f.groupName)
          .map((f) => f.groupName as string),
      ),
    ].sort();

    if (names.length === 0) {
      this.standingsLoading.set(false);
      return;
    }

    let outstanding = names.length;
    const tables = new Map<string, StandingDto[]>();

    const settle = () => {
      if (--outstanding > 0) return;

      this.groupTables.set(names.map((name) => ({ name, table: tables.get(name) ?? [] })));
      this.standingsLoading.set(false);
    };

    for (const name of names) {
      this.standingsService
        .getLeagueTable(division.season, division.id, undefined, name)
        .subscribe({
          next: (table) => {
            tables.set(name, table.table);
            settle();
          },
          // One group failing should not cost the reader the other groups.
          error: () => settle(),
        });
    }
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
