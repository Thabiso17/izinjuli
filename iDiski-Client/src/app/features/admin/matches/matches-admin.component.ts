import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatchService, GenerateFixturesCommand, GenerateFixturesResult } from '../../../core/services/match.service';
import { MatchEventService } from '../../../core/services/match-event.service';
import { TeamService } from '../../../core/services/team.service';
import { DivisionService } from '../../../core/services/division.service';
import {
  MatchResultDto,
  CreateMatchCommand,
  UpdateMatchScoreCommand,
  TeamDto,
  DivisionDto,
  MatchStatus,
} from '../../../core/models';

@Component({
  selector: 'app-matches-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Matches Management</h1>
          <p class="text-muted">Create fixtures, enter results, and manage match events</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-success me-2" (click)="showGenerateModal()">
            <i class="bi bi-lightning-charge"></i> Generate Fixtures
          </button>
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Create Fixture
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-2">
              <label class="form-label">Season *</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterSeason"
                (ngModelChange)="loadMatches()"
                placeholder="2025"
              />
            </div>
            <div class="col-md-2">
              <label class="form-label">Matchweek</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterMatchweek"
                (ngModelChange)="loadMatches()"
                placeholder="All"
              />
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
            <div class="col-md-2 d-flex align-items-end">
              <button class="btn btn-outline-secondary" (click)="clearFilters()">
                Clear
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
      }

      <!-- Matches List -->
      @if (!loading() && matches().length > 0) {
        <div class="row g-3">
          @for (match of matches(); track match.id) {
            <div class="col-12">
              <div class="card shadow-sm">
                <div class="card-body">
                  <div class="row align-items-center">
                    <div class="col-md-2">
                      <div class="text-muted small mb-1">
                        MW {{ match.matchweekNumber }} · {{ match.season }}
                      </div>
                      <div class="fw-bold">
                        {{ match.matchDate | date: 'MMM d, y' }}
                      </div>
                      <div class="text-muted small">
                        {{ match.matchDate | date: 'HH:mm' }}
                      </div>
                      @if (match.divisionName) {
                        <div class="badge bg-info text-dark mt-2">
                          {{ match.divisionName }}
                        </div>
                      }
                    </div>

                    <div class="col-md-7">
                      <div class="d-flex align-items-center justify-content-between">
                        <!-- Home Team -->
                        <div class="text-end" style="flex: 1">
                          <div class="d-flex align-items-center justify-content-end gap-2">
                            @if (match.homeTeamLogo) {
                              <img
                                [src]="match.homeTeamLogo"
                                [alt]="match.homeTeamName"
                                style="width: 30px; height: 30px; object-fit: contain"
                              />
                            }
                            <span class="fw-semibold">{{ match.homeTeamName }}</span>
                          </div>
                        </div>

                        <!-- Score -->
                        <div class="text-center px-4">
                          <div class="fs-4 fw-bold">
                            {{ match.scoreDisplay }}
                          </div>
                          <span
                            class="badge"
                            [class.bg-success]="match.status === 'Completed'"
                            [class.bg-primary]="match.status === 'InProgress'"
                            [class.bg-secondary]="match.status === 'Scheduled'"
                            [class.bg-warning]="match.status === 'Postponed'"
                            [class.bg-danger]="match.status === 'Cancelled'"
                          >
                            {{ match.status }}
                          </span>
                        </div>

                        <!-- Away Team -->
                        <div class="text-start" style="flex: 1">
                          <div class="d-flex align-items-center gap-2">
                            <span class="fw-semibold">{{ match.awayTeamName }}</span>
                            @if (match.awayTeamLogo) {
                              <img
                                [src]="match.awayTeamLogo"
                                [alt]="match.awayTeamName"
                                style="width: 30px; height: 30px; object-fit: contain"
                              />
                            }
                          </div>
                        </div>
                      </div>

                      @if (match.venue) {
                        <div class="text-center text-muted small mt-2">
                          <i class="bi bi-geo-alt"></i> {{ match.venue }}
                        </div>
                      }
                    </div>

                    <div class="col-md-3 text-end">
                      <button
                        class="btn btn-sm btn-outline-primary me-2"
                        (click)="showScoreModal(match)"
                        [disabled]="match.status === 'Cancelled'"
                      >
                        <i class="bi bi-pencil-square"></i> Enter Score
                      </button>
                      <button
                        class="btn btn-sm btn-outline-secondary"
                        (click)="showEventsModal(match)"
                      >
                        <i class="bi bi-list-check"></i> Events
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <div class="d-flex justify-content-center mt-4">
            <nav>
              <ul class="pagination">
                <li class="page-item" [class.disabled]="currentPage === 1">
                  <button
                    class="page-link"
                    (click)="goToPage(currentPage - 1)"
                    [disabled]="currentPage === 1"
                  >
                    Previous
                  </button>
                </li>
                @for (page of pageNumbers(); track page) {
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
          </div>
        }
      }

      <!-- Empty State -->
      @if (!loading() && matches().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-calendar-event display-1 text-muted"></i>
            <h3 class="mt-3">No Matches Found</h3>
            <p class="text-muted">Create your first fixture to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Create Fixture
            </button>
          </div>
        </div>
      }

      <!-- Error Alert -->
      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
          {{ error() }}
          <button
            type="button"
            class="btn-close"
            (click)="error.set(null)"
          ></button>
        </div>
      }

      <!-- Success Alert -->
      @if (success()) {
        <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
          {{ success() }}
          <button
            type="button"
            class="btn-close"
            (click)="success.set(null)"
          ></button>
        </div>
      }
    </div>

    <!-- Create Fixture Modal -->
    @if (showCreateModal()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Create Fixture</h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeCreateModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #matchForm="ngForm">
                <div class="row g-3">
                  <div class="col-md-4">
                    <label class="form-label">Season *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="createFormData.season"
                      name="season"
                      required
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Matchweek *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="createFormData.matchweekNumber"
                      name="matchweekNumber"
                      required
                      min="1"
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Division</label>
                    <select
                      class="form-select"
                      [(ngModel)]="createFormData.divisionId"
                      name="divisionId"
                    >
                      <option [ngValue]="null">Select</option>
                      @for (division of divisions(); track division.id) {
                        <option [ngValue]="division.id">
                          {{ division.name }}
                        </option>
                      }
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Home Team *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="createFormData.homeTeamId"
                      name="homeTeamId"
                      required
                    >
                      <option [ngValue]="null">Select Team</option>
                      @for (team of teams(); track team.id) {
                        <option
                          [ngValue]="team.id"
                          [disabled]="team.id === createFormData.awayTeamId"
                        >
                          {{ team.name }}
                        </option>
                      }
                    </select>
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Away Team *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="createFormData.awayTeamId"
                      name="awayTeamId"
                      required
                    >
                      <option [ngValue]="null">Select Team</option>
                      @for (team of teams(); track team.id) {
                        <option
                          [ngValue]="team.id"
                          [disabled]="team.id === createFormData.homeTeamId"
                        >
                          {{ team.name }}
                        </option>
                      }
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Match Date & Time *</label>
                    <input
                      type="datetime-local"
                      class="form-control"
                      [(ngModel)]="createFormData.matchDate"
                      name="matchDate"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Status *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="createFormData.status"
                      name="status"
                      required
                    >
                      <option value="Scheduled">Scheduled</option>
                      <option value="InProgress">In Progress</option>
                      <option value="Completed">Completed</option>
                      <option value="Postponed">Postponed</option>
                      <option value="Cancelled">Cancelled</option>
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Venue</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="createFormData.venue"
                      name="venue"
                      placeholder="Stadium name"
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Referee</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="createFormData.referee"
                      name="referee"
                      placeholder="Referee name"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Notes</label>
                    <textarea
                      class="form-control"
                      [(ngModel)]="createFormData.notes"
                      name="notes"
                      rows="2"
                      placeholder="Optional match notes"
                    ></textarea>
                  </div>
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeCreateModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-primary"
                (click)="createMatch()"
                [disabled]="matchForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Create Fixture
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Generate Fixtures Modal -->
    @if (showGenerateFixturesModal()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header bg-success text-white">
              <h5 class="modal-title">
                <i class="bi bi-lightning-charge me-2"></i>
                Generate Fixtures (Round-Robin)
              </h5>
              <button
                type="button"
                class="btn-close btn-close-white"
                (click)="closeGenerateModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #generateForm="ngForm">
                <div class="alert alert-info">
                  <i class="bi bi-info-circle me-2"></i>
                  <strong>Auto-generate all fixtures</strong> for a division using the round-robin algorithm.
                  Each team will play every other team once (single) or twice (home & away).
                </div>

                <div class="mb-3">
                  <label class="form-label">Division *</label>
                  <select
                    class="form-select"
                    [(ngModel)]="generateFormData.divisionId"
                    name="divisionId"
                    required
                  >
                    <option value="">Select Division</option>
                    @for (division of divisions(); track division.id) {
                      <option [value]="division.id">{{ division.name }}</option>
                    }
                  </select>
                  <small class="text-muted">Choose which division to generate fixtures for</small>
                </div>

                <div class="row g-3">
                  <div class="col-md-6">
                    <label class="form-label">Season *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="generateFormData.season"
                      name="season"
                      required
                      min="2020"
                      max="2100"
                      placeholder="2025"
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Start Date *</label>
                    <input
                      type="date"
                      class="form-control"
                      [(ngModel)]="generateFormData.startDate"
                      name="startDate"
                      required
                    />
                  </div>
                </div>

                <div class="row g-3 mt-2">
                  <div class="col-md-6">
                    <label class="form-label">Days Between Matchweeks *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="generateFormData.daysBetweenMatchweeks"
                      name="daysBetweenMatchweeks"
                      required
                      min="1"
                      max="30"
                      placeholder="7"
                    />
                    <small class="text-muted">Typically 7 days (weekly)</small>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Format *</label>
                    <div class="btn-group w-100" role="group">
                      <input
                        type="radio"
                        class="btn-check"
                        name="isHomeAndAway"
                        id="singleRound"
                        [value]="false"
                        [(ngModel)]="generateFormData.isHomeAndAway"
                      />
                      <label class="btn btn-outline-primary" for="singleRound">
                        Single Round
                      </label>

                      <input
                        type="radio"
                        class="btn-check"
                        name="isHomeAndAway"
                        id="homeAndAway"
                        [value]="true"
                        [(ngModel)]="generateFormData.isHomeAndAway"
                      />
                      <label class="btn btn-outline-primary" for="homeAndAway">
                        Home & Away
                      </label>
                    </div>
                    <small class="text-muted d-block mt-1">
                      {{ generateFormData.isHomeAndAway ? 'Each team plays twice (home/away)' : 'Each team plays once' }}
                    </small>
                  </div>
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeGenerateModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-success"
                (click)="executeGenerate()"
                [disabled]="generateForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                <i class="bi bi-lightning-charge me-2"></i>
                Generate Fixtures
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Update Score Modal -->
    @if (showUpdateScoreModal() && selectedMatch()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Update Match Score</h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeScoreModal()"
              ></button>
            </div>
            <div class="modal-body">
              <div class="text-center mb-4">
                <h6>{{ selectedMatch()!.homeTeamName }} vs {{ selectedMatch()!.awayTeamName }}</h6>
                <small class="text-muted">
                  MW {{ selectedMatch()!.matchweekNumber }} · {{ selectedMatch()!.matchDate | date }}
                </small>
              </div>

              <form #scoreForm="ngForm">
                <div class="row g-3">
                  <div class="col-6">
                    <label class="form-label">Home Score *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="scoreFormData.homeScore"
                      name="homeScore"
                      required
                      min="0"
                    />
                  </div>
                  <div class="col-6">
                    <label class="form-label">Away Score *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="scoreFormData.awayScore"
                      name="awayScore"
                      required
                      min="0"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Status *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="scoreFormData.status"
                      name="status"
                      required
                    >
                      <option value="InProgress">In Progress</option>
                      <option value="Completed">Completed</option>
                    </select>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Notes</label>
                    <textarea
                      class="form-control"
                      [(ngModel)]="scoreFormData.notes"
                      name="notes"
                      rows="2"
                    ></textarea>
                  </div>
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeScoreModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-primary"
                (click)="updateScore()"
                [disabled]="scoreForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Update Score
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Events Modal (placeholder) -->
    @if (showMatchEventsModal() && selectedMatch()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Match Events</h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeEventsModal()"
              ></button>
            </div>
            <div class="modal-body">
              <div class="text-center py-4">
                <p class="text-muted">Match events functionality coming next...</p>
                <p class="text-muted">Match ID: {{ selectedMatch()!.id }}</p>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeEventsModal()">
                Close
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
  styles: [
    `
      .modal.show {
        display: block;
      }
    `,
  ],
})
export class MatchesAdminComponent implements OnInit {
  private matchService = inject(MatchService);
  private teamService = inject(TeamService);
  private divisionService = inject(DivisionService);

  matches = signal<MatchResultDto[]>([]);
  teams = signal<TeamDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showCreateModal = signal(false);
  showGenerateFixturesModal = signal(false);
  showUpdateScoreModal = signal(false);
  showMatchEventsModal = signal(false);
  selectedMatch = signal<MatchResultDto | null>(null);
  totalPages = signal(1);

  filterSeason = new Date().getFullYear();
  filterMatchweek: number | undefined;
  filterDivisionId: string | undefined;
  filterStatus: string | undefined;
  currentPage = 1;
  pageSize = 20;

  createFormData: any = this.getEmptyCreateForm();
  scoreFormData: any = { homeScore: 0, awayScore: 0, status: 'Completed', notes: '' };
  generateFormData = {
    divisionId: '',
    season: new Date().getFullYear(),
    isHomeAndAway: true,
    startDate: '',
    daysBetweenMatchweeks: 7
  };

  ngOnInit() {
    this.loadTeams();
    this.loadDivisions();
    this.loadMatches();
  }

  loadMatches() {
    this.loading.set(true);
    this.error.set(null);

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
          this.error.set('Failed to load matches: ' + err.message);
          this.loading.set(false);
        },
      });
  }

  loadTeams() {
    this.teamService.getAll().subscribe({
      next: (data) => this.teams.set(data),
      error: (err) => console.error('Failed to load teams:', err),
    });
  }

  loadDivisions() {
    // Load all divisions for admin panel
    this.divisionService.getAll().subscribe({
      next: (data) => {
        const sorted = data.sort((a, b) => {
          if (b.season !== a.season) return b.season - a.season;
          return a.name.localeCompare(b.name);
        });
        this.divisions.set(sorted);
      },
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  clearFilters() {
    this.filterMatchweek = undefined;
    this.filterDivisionId = undefined;
    this.filterStatus = undefined;
    this.currentPage = 1;
    this.loadMatches();
  }

  goToPage(page: number) {
    this.currentPage = page;
    this.loadMatches();
  }

  pageNumbers() {
    const pages = [];
    for (let i = 1; i <= Math.min(this.totalPages(), 10); i++) {
      pages.push(i);
    }
    return pages;
  }

  showAddModal() {
    this.createFormData = this.getEmptyCreateForm();
    this.showCreateModal.set(true);
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
  }

  showGenerateModal() {
    this.generateFormData = {
      divisionId: this.filterDivisionId || '',
      season: this.filterSeason,
      isHomeAndAway: true,
      startDate: new Date().toISOString().split('T')[0],
      daysBetweenMatchweeks: 7
    };
    this.showGenerateFixturesModal.set(true);
  }

  closeGenerateModal() {
    this.showGenerateFixturesModal.set(false);
  }

  executeGenerate() {
    this.saving.set(true);
    this.error.set(null);

    const command: GenerateFixturesCommand = {
      divisionId: this.generateFormData.divisionId,
      season: this.generateFormData.season,
      isHomeAndAway: this.generateFormData.isHomeAndAway,
      startDate: this.generateFormData.startDate,
      daysBetweenMatchweeks: this.generateFormData.daysBetweenMatchweeks
    };

    this.matchService.generateFixtures(command).subscribe({
      next: (result: GenerateFixturesResult) => {
        const formatType = command.isHomeAndAway ? 'home & away' : 'single round-robin';
        this.success.set(
          `✓ Generated ${result.fixturesGenerated} fixtures (${result.matchweeksCreated} matchweeks) in ${formatType} format. ` +
          `Season runs from ${new Date(result.firstMatchDate).toLocaleDateString()} to ${new Date(result.lastMatchDate).toLocaleDateString()}.`
        );
        this.saving.set(false);
        this.closeGenerateModal();
        this.loadMatches();
        setTimeout(() => this.success.set(null), 8000);
      },
      error: (err: any) => {
        this.error.set(`Failed to generate fixtures: ${err.error?.message || err.message}`);
        this.saving.set(false);
      }
    });
  }

  createMatch() {
    this.saving.set(true);
    this.error.set(null);

    const command: CreateMatchCommand = {
      season: this.createFormData.season,
      matchweekNumber: this.createFormData.matchweekNumber,
      matchDate: new Date(this.createFormData.matchDate).toISOString(),
      homeTeamId: this.createFormData.homeTeamId,
      awayTeamId: this.createFormData.awayTeamId,
      venue: this.createFormData.venue || undefined,
      referee: this.createFormData.referee || undefined,
      divisionId: this.createFormData.divisionId || undefined,
    };

    this.matchService.create(command).subscribe({
      next: () => {
        this.success.set('Fixture created successfully');
        this.saving.set(false);
        this.closeCreateModal();
        this.loadMatches();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to create fixture: ${err.error?.message || err.message}`);
        this.saving.set(false);
      },
    });
  }

  showScoreModal(match: MatchResultDto) {
    this.selectedMatch.set(match);
    this.scoreFormData = {
      homeScore: match.homeScore,
      awayScore: match.awayScore,
      status: match.status === 'Scheduled' ? 'Completed' : match.status,
      notes: match.notes || '',
    };
    this.showUpdateScoreModal.set(true);
  }

  closeScoreModal() {
    this.showUpdateScoreModal.set(false);
    this.selectedMatch.set(null);
  }

  updateScore() {
    if (!this.selectedMatch()) return;

    this.saving.set(true);
    this.error.set(null);

    const command: UpdateMatchScoreCommand = {
      id: this.selectedMatch()!.id,
      homeScore: this.scoreFormData.homeScore,
      awayScore: this.scoreFormData.awayScore,
      status: this.scoreFormData.status as MatchStatus,
      notes: this.scoreFormData.notes || undefined,
    };

    this.matchService.updateScore(this.selectedMatch()!.id, command).subscribe({
      next: () => {
        this.success.set('Match score updated successfully');
        this.saving.set(false);
        this.closeScoreModal();
        this.loadMatches();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to update score: ${err.error?.message || err.message}`);
        this.saving.set(false);
      },
    });
  }

  showEventsModal(match: MatchResultDto) {
    this.selectedMatch.set(match);
    this.showMatchEventsModal.set(true);
  }

  closeEventsModal() {
    this.showMatchEventsModal.set(false);
    this.selectedMatch.set(null);
  }

  private getEmptyCreateForm() {
    return {
      season: new Date().getFullYear(),
      matchweekNumber: 1,
      divisionId: null,
      homeTeamId: null,
      awayTeamId: null,
      matchDate: '',
      status: 'Scheduled',
      venue: '',
      referee: '',
      notes: '',
    };
  }
}
