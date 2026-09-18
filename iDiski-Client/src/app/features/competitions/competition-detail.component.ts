import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CompetitionService, MatchService, StandingsService } from '../../core/services';
import {
  CompetitionDto,
  CompetitionEntrantDto,
  COMPETITION_FORMAT_LABEL,
  COMPETITION_STATUS_CLASS,
  COMPETITION_STATUS_LABEL,
  MatchResultDto,
  StandingDto,
} from '../../core/models';
import { BracketComponent } from '../divisions/bracket.component';
import { StandingsTableComponent } from '../divisions/standings-table.component';

/**
 * One competition: its table, its groups, its bracket, and who won it.
 *
 * All of this used to live on the division page, because a division was the competition. It
 * moved here when a division became able to run several at once — a league, a top-eight cup and
 * a sponsor's tournament are three different pages, not three things stacked on one.
 */
@Component({
  selector: 'app-competition-detail',
  imports: [CommonModule, RouterLink, BracketComponent, StandingsTableComponent],
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
        <div class="alert alert-danger">{{ error() }}</div>
      }

      @if (competition(); as comp) {
        <!-- ── Heading ────────────────────────────────────────────────────── -->
        <div class="card shadow-sm mb-4">
          <div class="card-body">
            <a routerLink="/competitions" class="text-decoration-none text-muted small">
              <i class="bi bi-arrow-left me-1"></i>Competitions
            </a>

            <div class="d-flex justify-content-between align-items-start mt-2">
              <div>
                <h1 class="h3 fw-bold mb-2" data-testid="competition-name">{{ comp.name }}</h1>

                <div class="d-flex gap-2 flex-wrap">
                  <span class="badge bg-primary">{{ comp.shortCode }}</span>
                  <span class="badge bg-light text-dark border" data-testid="competition-format">
                    {{ formatLabel[comp.format] }}
                  </span>
                  <span
                    [class]="'badge ' + statusClass[comp.status]"
                    data-testid="competition-status"
                  >
                    {{ statusLabel[comp.status] }}
                  </span>
                  <span class="badge bg-light text-dark">{{ comp.season }}</span>
                </div>
              </div>

              <div class="text-end text-muted small">
                <div data-testid="entrant-count">
                  <i class="bi bi-shield-fill me-1"></i>{{ comp.entrantCount }} entrants
                </div>
                @if (comp.divisionsRepresented > 1) {
                  <!-- Worth saying plainly: otherwise clubs from three divisions meeting
                       looks like a mistake rather than the point of a cup. -->
                  <div data-testid="divisions-represented">
                    <i class="bi bi-diagram-3 me-1"></i>from
                    {{ comp.divisionsRepresented }} divisions
                  </div>
                }
                <div>
                  <i class="bi bi-calendar-event me-1"></i>{{ comp.matchCount }} fixtures
                </div>
              </div>
            </div>

            @if (comp.description) {
              <p class="text-muted mt-3 mb-0">{{ comp.description }}</p>
            }
          </div>
        </div>

        <!-- ── Who won it ─────────────────────────────────────────────────── -->
        @if (champion(); as winner) {
          <div class="card shadow-sm mb-4 border-warning" data-testid="champion">
            <div class="card-body d-flex align-items-center gap-3">
              <i class="bi bi-trophy-fill text-warning fs-1"></i>
              <div>
                <div class="text-uppercase text-muted small fw-semibold">
                  {{ comp.format === 'League' ? 'Champions' : 'Winners' }}
                </div>
                <div class="h4 mb-0 fw-bold">{{ winner }}</div>
              </div>
            </div>
          </div>
        }

        <div class="row g-4">
          <!-- ── Groups ───────────────────────────────────────────────────── -->
          @if (comp.format === 'GroupAndKnockout') {
            <div class="col-12">
              <section class="card shadow-sm">
                <div class="card-body">
                  <h2 class="h4 mb-4">
                    <i class="bi bi-table text-primary me-2"></i>Group Stage
                  </h2>

                  @if (standingsLoading()) {
                    <div class="text-center py-4">
                      <div class="spinner-border spinner-border-sm" role="status"></div>
                    </div>
                  } @else {
                    <!-- One table per group. Merging them would rank teams against opponents
                         they have never played, which is not a table of anything. -->
                    <div class="row g-4">
                      @for (group of groupTables(); track group.name) {
                        <div class="col-lg-6">
                          <h3 class="h6 text-uppercase text-muted mb-2" data-testid="group-heading">
                            Group {{ group.name }}
                          </h3>
                          <app-standings-table
                            [rows]="group.table"
                            emptyMessage="No matches played in this group yet."
                          />
                        </div>
                      } @empty {
                        <div class="col-12">
                          <p class="text-muted text-center py-3 mb-0">
                            The groups have not been drawn yet.
                          </p>
                        </div>
                      }
                    </div>
                  }
                </div>
              </section>
            </div>
          }

          <!-- ── Bracket ──────────────────────────────────────────────────── -->
          @if (comp.format !== 'League') {
            <div class="col-12">
              <section class="card shadow-sm">
                <div class="card-body">
                  <h2 class="h4 mb-4">
                    <i class="bi bi-diagram-3 text-primary me-2"></i>
                    {{ comp.format === 'GroupAndKnockout' ? 'Knockout Stage' : 'Bracket' }}
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

          <!-- ── Table ────────────────────────────────────────────────────── -->
          @if (comp.format === 'League') {
            <div class="col-12">
              <section class="card shadow-sm">
                <div class="card-body">
                  <h2 class="h4 mb-4">
                    <i class="bi bi-table text-primary me-2"></i>Table
                  </h2>

                  @if (standingsLoading()) {
                    <div class="text-center py-4">
                      <div class="spinner-border spinner-border-sm" role="status"></div>
                    </div>
                  } @else {
                    <app-standings-table
                      [rows]="standings()"
                      emptyMessage="No matches played in this competition yet."
                    />
                  }
                </div>
              </section>
            </div>
          }

          <!-- ── Who is in it ─────────────────────────────────────────────── -->
          <div class="col-12">
            <section class="card shadow-sm">
              <div class="card-body">
                <h2 class="h4 mb-4">
                  <i class="bi bi-people-fill text-primary me-2"></i>Entrants
                </h2>

                <div class="row g-2">
                  @for (entrant of entrants(); track entrant.teamId) {
                    <div class="col-md-6 col-lg-4">
                      <div
                        class="d-flex align-items-center gap-2 border rounded p-2"
                        data-testid="entrant"
                      >
                        <span class="badge bg-secondary">{{ entrant.shortCode }}</span>
                        <a
                          [routerLink]="['/teams', entrant.teamId]"
                          class="text-decoration-none text-dark flex-grow-1 text-truncate"
                        >
                          {{ entrant.teamName }}
                        </a>
                        @if (entrant.divisionName) {
                          <span
                            class="badge bg-light text-dark border"
                            data-testid="entrant-division"
                          >
                            {{ entrant.divisionName }}
                          </span>
                        }
                      </div>
                    </div>
                  } @empty {
                    <div class="col-12">
                      <p class="text-muted mb-0">Nobody has been entered yet.</p>
                    </div>
                  }
                </div>
              </div>
            </section>
          </div>
        </div>
      }
    </div>
  `,
})
export class CompetitionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly competitions = inject(CompetitionService);
  private readonly standingsService = inject(StandingsService);
  private readonly matchService = inject(MatchService);

  competition = signal<CompetitionDto | null>(null);
  entrants = signal<CompetitionEntrantDto[]>([]);
  standings = signal<StandingDto[]>([]);
  fixtures = signal<MatchResultDto[]>([]);
  groupTables = signal<{ name: string; table: StandingDto[] }[]>([]);

  loading = signal(true);
  standingsLoading = signal(true);
  fixturesLoading = signal(true);
  error = signal<string | null>(null);

  readonly formatLabel = COMPETITION_FORMAT_LABEL;
  readonly statusLabel = COMPETITION_STATUS_LABEL;
  readonly statusClass = COMPETITION_STATUS_CLASS;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Competition not found.');
      this.loading.set(false);
      return;
    }

    this.competitions.getById(id).subscribe({
      next: (competition) => {
        this.competition.set(competition);
        this.loading.set(false);

        if (competition.format === 'League') this.loadTable(competition);
        else this.standingsLoading.set(false);

        if (competition.format !== 'League') this.loadBracket(competition);
        else this.fixturesLoading.set(false);
      },
      error: (err) => {
        this.error.set(
          err.error?.detail || err.error?.title || 'Failed to load this competition.',
        );
        this.loading.set(false);
      },
    });

    this.competitions.getEntrants(id).subscribe({
      next: (entrants) => this.entrants.set(entrants),
      error: () => this.entrants.set([]),
    });
  }

  /**
   * Who won it, or null while it is still being played.
   *
   * Read off what the page has already loaded rather than asked for: a cup's winner is the side
   * that won the final, and a league's is whoever finished top.
   */
  champion(): string | null {
    const competition = this.competition();
    if (!competition || competition.status !== 'Completed') return null;

    if (competition.format === 'League') return this.standings()[0]?.teamName ?? null;

    // The final is the round of two. The API identifies it as the tie nothing follows, which
    // is the same fixture by construction — a bracket is built down to two and stops.
    const final = this.fixtures().find(
      (f) => f.stage === 'Knockout' && f.knockoutRoundSize === 2 && f.status === 'Completed',
    );

    if (!final) return null;

    const side = this.wonBy(final);
    if (!side) return null;

    return (side === 'home' ? final.homeTeamName : final.awayTeamName) ?? null;
  }

  private wonBy(tie: MatchResultDto): 'home' | 'away' | null {
    if (tie.homeScore !== tie.awayScore) return tie.homeScore > tie.awayScore ? 'home' : 'away';

    const home = tie.homePenalties;
    const away = tie.awayPenalties;

    if (home === null || away === null || home === undefined || away === undefined) return null;
    if (home === away) return null;

    return home > away ? 'home' : 'away';
  }

  private loadTable(competition: CompetitionDto): void {
    this.standingsService
      .getLeagueTable(competition.season, undefined, undefined, undefined, competition.id)
      .subscribe({
        next: (table) => {
          this.standings.set(table.table);
          this.standingsLoading.set(false);
        },
        error: () => this.standingsLoading.set(false),
      });
  }

  private loadBracket(competition: CompetitionDto): void {
    // A page size that holds a bracket of sixty-four and its group stage. Paging this would
    // mean a bracket missing its later rounds, which is worse than a slow page.
    this.matchService
      .getAll(competition.season, undefined, undefined, undefined, undefined, 1, 200, competition.id)
      .subscribe({
        next: (page) => {
          this.fixtures.set(page.items);
          this.fixturesLoading.set(false);

          if (competition.format === 'GroupAndKnockout') {
            this.loadGroupTables(competition, page.items);
          }
        },
        error: () => this.fixturesLoading.set(false),
      });
  }

  /**
   * A table per group. Which teams are in a group is carried by its fixtures rather than by
   * the entry list, so the groups themselves are read off the fixtures already loaded for the
   * bracket rather than asked for separately.
   */
  private loadGroupTables(competition: CompetitionDto, fixtures: MatchResultDto[]): void {
    const names = [
      ...new Set(
        fixtures.filter((f) => f.stage === 'Group' && f.groupName).map((f) => f.groupName as string),
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
        .getLeagueTable(competition.season, undefined, undefined, name, competition.id)
        .subscribe({
          // One group failing should not cost the reader the other groups.
          next: (table) => {
            tables.set(name, table.table);
            settle();
          },
          error: () => settle(),
        });
    }
  }
}
