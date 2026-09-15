import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CompetitionService } from '../../../core/services/competition.service';
import { DivisionService } from '../../../core/services/division.service';
import { TeamService } from '../../../core/services/team.service';
import {
  CompetitionDto,
  CompetitionEntrantDto,
  CompetitionFormat,
  COMPETITION_FORMAT_HINT,
  COMPETITION_FORMAT_LABEL,
  COMPETITION_STATUS_CLASS,
  COMPETITION_STATUS_LABEL,
  CreateCompetitionCommand,
  DivisionDto,
  TeamDto,
} from '../../../core/models';

/**
 * What a division is running, and who is in each of them.
 *
 * A division used to be the competition, so this screen did not exist — there was nothing to
 * manage. Now an under-seventeen division runs its league, a top-eight cup and a sponsor's
 * tournament at once, and each needs its own entry list: twelve of the twenty, plus four clubs
 * invited from another division.
 */
@Component({
  selector: 'app-competitions-admin',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <a routerLink="/admin/divisions" class="text-decoration-none small">
            <i class="bi bi-arrow-left me-1"></i>Divisions
          </a>
          <h1 class="display-6 mt-2">{{ division()?.name || 'Competitions' }}</h1>
          <p class="text-muted">
            What this division is playing. A league, a cup and a sponsor's tournament can run
            side by side, each with its own entrants.
          </p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" data-testid="add-competition" (click)="showAdd()">
            <i class="bi bi-plus-circle"></i> Add Competition
          </button>
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show">
          {{ error() }}
          <button type="button" class="btn-close" (click)="error.set(null)"></button>
        </div>
      }

      @if (success()) {
        <div class="alert alert-success alert-dismissible fade show">
          {{ success() }}
          <button type="button" class="btn-close" (click)="success.set(null)"></button>
        </div>
      }

      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      @if (!loading()) {
        <div class="row g-4">
          @for (competition of competitions(); track competition.id) {
            <div class="col-lg-6">
              <div class="card shadow-sm h-100" data-testid="competition-row">
                <div class="card-body">
                  <div class="d-flex justify-content-between align-items-start mb-2">
                    <div>
                      <h2 class="h5 mb-1">{{ competition.name }}</h2>
                      <span class="badge bg-secondary">{{ competition.shortCode }}</span>
                      <span class="badge bg-light text-dark border ms-1">
                        {{ formatLabel[competition.format] }}
                      </span>
                      <span [class]="'badge ms-1 ' + statusClass[competition.status]">
                        {{ statusLabel[competition.status] }}
                      </span>
                    </div>
                    <div class="text-end">
                      <a
                        [routerLink]="['/competitions', competition.id]"
                        class="btn btn-sm btn-outline-secondary"
                        title="View the public page"
                      >
                        <i class="bi bi-box-arrow-up-right"></i>
                      </a>
                      @if (competition.matchCount === 0) {
                        <button
                          class="btn btn-sm btn-outline-danger ms-1"
                          data-testid="delete-competition"
                          (click)="remove(competition)"
                          title="Delete"
                        >
                          <i class="bi bi-trash"></i>
                        </button>
                      }
                    </div>
                  </div>

                  <div class="text-muted small mb-3">
                    {{ competition.entrantCount }} entrants
                    @if (competition.externalEntrantCount > 0) {
                      <span class="text-info">
                        · {{ competition.externalEntrantCount }} invited
                      </span>
                    }
                    · {{ competition.matchCount }} fixtures · {{ competition.season }}
                  </div>

                  <button
                    class="btn btn-sm btn-outline-primary"
                    data-testid="manage-entrants"
                    (click)="openEntrants(competition)"
                  >
                    <i class="bi bi-people me-1"></i>Entrants
                  </button>
                </div>
              </div>
            </div>
          } @empty {
            <div class="col-12">
              <div class="card">
                <div class="card-body text-center py-5">
                  <i class="bi bi-trophy display-1 text-muted"></i>
                  <h3 class="mt-3">Nothing being played yet</h3>
                  <p class="text-muted mb-0">
                    Add a competition to give this division's clubs something to play.
                  </p>
                </div>
              </div>
            </div>
          }
        </div>
      }

      <!-- ── Add ──────────────────────────────────────────────────────────── -->
      @if (showAddModal()) {
        <div class="modal show d-block" tabindex="-1" style="background: rgba(0,0,0,.5)">
          <div class="modal-dialog modal-lg">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title">Add Competition</h5>
                <button type="button" class="btn-close" (click)="showAddModal.set(false)"></button>
              </div>
              <div class="modal-body">
                <div class="row g-3">
                  <div class="col-md-8">
                    <label class="form-label">Name *</label>
                    <input
                      class="form-control"
                      [(ngModel)]="form.name"
                      name="name"
                      placeholder="e.g. Top Eight Cup"
                      required
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Short code *</label>
                    <input
                      class="form-control"
                      [(ngModel)]="form.shortCode"
                      name="shortCode"
                      placeholder="T8C"
                      required
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Format *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="form.format"
                      name="format"
                      data-testid="competition-format"
                    >
                      @for (format of formats; track format) {
                        <option [value]="format">{{ formatLabel[format] }}</option>
                      }
                    </select>
                    <small class="form-text text-muted">{{ hintFor(form.format) }}</small>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Season *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="form.season"
                      name="season"
                      required
                    />
                  </div>

                  <div class="col-12">
                    <div class="form-check">
                      <input
                        type="checkbox"
                        class="form-check-input"
                        [(ngModel)]="form.enterAllDivisionTeams"
                        name="enterAll"
                        id="enterAll"
                        data-testid="enter-all"
                      />
                      <label class="form-check-label" for="enterAll">
                        Enter all {{ division()?.teamCount || 0 }} clubs in this division
                      </label>
                    </div>
                    <small class="text-muted">
                      <!-- A league wants everybody; a cup is easier to trim down than to build
                           up, and an invitational starts from nobody. -->
                      Leave ticked for a league. Untick it for a cup only some of them enter,
                      then add the entrants afterwards.
                    </small>
                  </div>
                </div>
              </div>
              <div class="modal-footer">
                <button class="btn btn-secondary" (click)="showAddModal.set(false)">Cancel</button>
                <button
                  class="btn btn-primary"
                  data-testid="save-competition"
                  [disabled]="!form.name || !form.shortCode || saving()"
                  (click)="create()"
                >
                  {{ saving() ? 'Saving…' : 'Create' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- ── Entrants ─────────────────────────────────────────────────────── -->
      @if (entrantsFor(); as competition) {
        <div class="modal show d-block" tabindex="-1" style="background: rgba(0,0,0,.5)">
          <div class="modal-dialog modal-lg">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title">{{ competition.name }} — entrants</h5>
                <button type="button" class="btn-close" (click)="closeEntrants()"></button>
              </div>
              <div class="modal-body">
                @if (competition.matchCount > 0) {
                  <div class="alert alert-warning">
                    <!-- Adding or removing an entrant now would leave somebody with a place
                         and no fixtures, or fixtures against somebody with no place. -->
                    This competition has already been drawn up, so its entry list is fixed.
                    Delete its fixtures to change who is in it.
                  </div>
                }

                <h6 class="text-uppercase text-muted small">In this competition</h6>
                <div class="list-group mb-4">
                  @for (entrant of entrants(); track entrant.teamId) {
                    <div
                      class="list-group-item d-flex align-items-center gap-2"
                      data-testid="entrant-row"
                    >
                      <span class="badge bg-secondary">{{ entrant.shortCode }}</span>
                      <span class="flex-grow-1">{{ entrant.teamName }}</span>
                      @if (entrant.isExternal) {
                        <span class="badge bg-info text-dark" [title]="entrant.divisionName || ''">
                          Invited
                        </span>
                      }
                      @if (competition.matchCount === 0) {
                        <button
                          class="btn btn-sm btn-outline-danger"
                          data-testid="withdraw"
                          (click)="withdraw(competition, entrant)"
                        >
                          Withdraw
                        </button>
                      }
                    </div>
                  } @empty {
                    <div class="list-group-item text-muted">Nobody entered yet.</div>
                  }
                </div>

                @if (competition.matchCount === 0) {
                  <h6 class="text-uppercase text-muted small">Add a club</h6>
                  <p class="text-muted small">
                    Clubs from other divisions can be invited. One from a division of a
                    different gender cannot, and the API refuses it.
                  </p>
                  <div class="input-group">
                    <select class="form-select" [(ngModel)]="teamToAdd" name="teamToAdd">
                      <option [ngValue]="null">Select a club</option>
                      @for (team of addableTeams(); track team.id) {
                        <option [ngValue]="team.id">
                          {{ team.name }}
                          @if (team.divisionId !== competition.divisionId) {
                            — {{ team.divisionName || 'another division' }}
                          }
                        </option>
                      }
                    </select>
                    <button
                      class="btn btn-primary"
                      data-testid="enter-team"
                      [disabled]="!teamToAdd || saving()"
                      (click)="enter(competition)"
                    >
                      Enter
                    </button>
                  </div>
                }
              </div>
              <div class="modal-footer">
                <button class="btn btn-secondary" (click)="closeEntrants()">Done</button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class CompetitionsAdminComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly competitionService = inject(CompetitionService);
  private readonly divisionService = inject(DivisionService);
  private readonly teamService = inject(TeamService);

  divisionId = '';
  division = signal<DivisionDto | null>(null);
  competitions = signal<CompetitionDto[]>([]);
  entrants = signal<CompetitionEntrantDto[]>([]);
  entrantsFor = signal<CompetitionDto | null>(null);
  teams = signal<TeamDto[]>([]);

  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showAddModal = signal(false);

  teamToAdd: string | null = null;

  readonly formats: CompetitionFormat[] = ['League', 'Knockout', 'GroupAndKnockout'];
  readonly formatLabel = COMPETITION_FORMAT_LABEL;
  readonly statusLabel = COMPETITION_STATUS_LABEL;
  readonly statusClass = COMPETITION_STATUS_CLASS;

  form: CreateCompetitionCommand = this.emptyForm();

  ngOnInit(): void {
    this.divisionId = this.route.snapshot.paramMap.get('id') ?? '';

    this.divisionService.getById(this.divisionId).subscribe({
      next: (division) => {
        this.division.set(division);
        this.form = this.emptyForm();
      },
      error: () => this.error.set('Failed to load this division.'),
    });

    this.teamService.getAll().subscribe({
      next: (teams) => this.teams.set(teams),
      error: () => this.teams.set([]),
    });

    this.load();
  }

  hintFor(format: CompetitionFormat): string {
    return COMPETITION_FORMAT_HINT[format] ?? '';
  }

  /** Every club not already entered — including those from other divisions, deliberately. */
  addableTeams(): TeamDto[] {
    const entered = new Set(this.entrants().map((e) => e.teamId));
    return this.teams().filter((t) => !entered.has(t.id));
  }

  load(): void {
    this.loading.set(true);

    this.competitionService.getAll(this.divisionId).subscribe({
      next: (competitions) => {
        this.competitions.set(competitions);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to load competitions.'));
        this.loading.set(false);
      },
    });
  }

  showAdd(): void {
    this.form = this.emptyForm();
    this.showAddModal.set(true);
  }

  create(): void {
    this.saving.set(true);
    this.error.set(null);

    this.competitionService.create({ ...this.form, divisionId: this.divisionId }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showAddModal.set(false);
        this.success.set(`${this.form.name} created.`);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.messageFrom(err, 'Failed to create the competition.'));
      },
    });
  }

  remove(competition: CompetitionDto): void {
    this.competitionService.delete(competition.id).subscribe({
      next: () => {
        this.success.set(`${competition.name} deleted.`);
        this.load();
      },
      error: (err) => this.error.set(this.messageFrom(err, 'Failed to delete it.')),
    });
  }

  openEntrants(competition: CompetitionDto): void {
    this.entrantsFor.set(competition);
    this.teamToAdd = null;
    this.loadEntrants(competition.id);
  }

  closeEntrants(): void {
    this.entrantsFor.set(null);
    this.entrants.set([]);
    // The counts on the cards behind the modal have just changed.
    this.load();
  }

  enter(competition: CompetitionDto): void {
    if (!this.teamToAdd) return;

    this.saving.set(true);
    this.error.set(null);

    this.competitionService.enter(competition.id, this.teamToAdd).subscribe({
      next: () => {
        this.saving.set(false);
        this.teamToAdd = null;
        this.loadEntrants(competition.id);
      },
      error: (err) => {
        this.saving.set(false);
        // The gender refusal arrives here, and it is a sentence worth showing verbatim.
        this.error.set(this.messageFrom(err, 'That club could not be entered.'));
      },
    });
  }

  withdraw(competition: CompetitionDto, entrant: CompetitionEntrantDto): void {
    this.competitionService.withdraw(competition.id, entrant.teamId).subscribe({
      next: () => this.loadEntrants(competition.id),
      error: (err) => this.error.set(this.messageFrom(err, 'That club could not be withdrawn.')),
    });
  }

  private loadEntrants(competitionId: string): void {
    this.competitionService.getEntrants(competitionId).subscribe({
      next: (entrants) => this.entrants.set(entrants),
      error: () => this.entrants.set([]),
    });
  }

  private emptyForm(): CreateCompetitionCommand {
    return {
      divisionId: this.divisionId,
      name: '',
      shortCode: '',
      season: this.division()?.season ?? new Date().getFullYear(),
      format: 'League',
      enterAllDivisionTeams: true,
    };
  }

  private messageFrom(err: unknown, fallback: string): string {
    const body = (err as { error?: { detail?: string; title?: string } })?.error;
    return body?.detail || body?.title || fallback;
  }
}
