import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatchService, GenerateFixturesCommand, GenerateFixturesResult } from '../../../core/services/match.service';
import { MatchEventService } from '../../../core/services/match-event.service';
import { TeamService } from '../../../core/services/team.service';
import { DivisionService } from '../../../core/services/division.service';
import {
  MatchResultDto,
  knockoutRoundName,
  shootoutSuffix,
  CreateMatchCommand,
  UpdateMatchScoreCommand,
  TeamDto,
  DivisionDto,
  MatchStatus,
  CompetitionFormat,
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
                (ngModelChange)="onFiltersChanged()"
                placeholder="2025"
              />
            </div>
            <div class="col-md-2">
              <label class="form-label">Matchweek</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterMatchweek"
                (ngModelChange)="onFiltersChanged()"
                placeholder="All"
              />
            </div>
            <div class="col-md-3">
              <label class="form-label">Division</label>
              <select
                class="form-select"
                [(ngModel)]="filterDivisionId"
                (ngModelChange)="onFilterDivisionChange()"
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
              <label class="form-label">Team</label>
              <select
                class="form-select"
                [(ngModel)]="filterTeamId"
                (ngModelChange)="onFiltersChanged()"
              >
                <option [ngValue]="undefined">All Teams</option>
                @for (team of getTeamsByDivision(filterDivisionId); track team.id) {
                  <option [ngValue]="team.id">{{ team.name }}</option>
                }
              </select>
              <small class="text-muted">Home and away fixtures</small>
            </div>
            <div class="col-md-2">
              <label class="form-label">Status</label>
              <select
                class="form-select"
                [(ngModel)]="filterStatus"
                (ngModelChange)="onFiltersChanged()"
              >
                <option [ngValue]="undefined">All Statuses</option>
                <option value="Scheduled">Scheduled</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
                <option value="Postponed">Postponed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>
            <div class="col-12 d-flex justify-content-end">
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
              <div class="card shadow-sm" data-testid="match-card">
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
                        <div class="badge bg-info text-dark mt-2" data-testid="match-division">
                          {{ match.divisionName }}
                        </div>
                      }
                      @if (roundName(match.knockoutRoundSize); as round) {
                        <div class="badge bg-dark mt-2 ms-1" data-testid="match-round">
                          {{ round }}
                        </div>
                      } @else if (match.groupName) {
                        <div class="badge bg-secondary mt-2 ms-1" data-testid="match-group">
                          Group {{ match.groupName }}
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
                            <span class="fw-semibold" data-testid="match-home">
                              {{ match.homeTeamName || 'To be decided' }}
                            </span>
                          </div>
                        </div>

                        <!-- Score -->
                        <div class="text-center px-4">
                          <div class="fs-4 fw-bold">
                            {{ match.scoreDisplay }}
                            @if (shootout(match); as pens) {
                              <div class="small text-muted" data-testid="shootout-score">
                                {{ pens }}
                              </div>
                            }
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
                            <span class="fw-semibold" data-testid="match-away">
                              {{ match.awayTeamName || 'To be decided' }}
                            </span>
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
                      (ngModelChange)="onCreateDivisionChange()"
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
                      @for (team of getTeamsByDivision(createFormData.divisionId); track team.id) {
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
                      @for (team of getTeamsByDivision(createFormData.divisionId); track team.id) {
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
                <div class="alert alert-info" data-testid="generate-explains-format">
                  <i class="bi bi-info-circle me-2"></i>
                  @switch (generateFormat()) {
                    @case ('Knockout') {
                      <strong>A knockout bracket.</strong> Every round is drawn up now, down to
                      the final — the later rounds wait with empty slots, and each winner moves
                      into the fixture they have earned as results come in. Entrants are seeded
                      so the strongest two can only meet in the final, and if the entry list is
                      not a power of two the strongest get a bye.
                    }
                    @case ('GroupAndKnockout') {
                      <strong>Groups, then a knockout.</strong> Everyone plays their own group
                      in full, and the bracket the qualifiers will contest is drawn up at the
                      same time — empty, because nobody has come through yet.
                    }
                    @default {
                      <strong>A league.</strong> Every team plays every other, once or twice,
                      and the table decides it.
                    }
                  }
                  <div class="mt-2 small">
                    This follows the division's format. To change it, change the division.
                  </div>
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

                  @if (generateFormat() !== 'Knockout') {
                  <div class="col-md-6">
                    <label class="form-label">Meetings *</label>
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
                  }

                  @if (generateFormat() === 'GroupAndKnockout') {
                    <div class="col-md-6">
                      <label class="form-label">Groups *</label>
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="generateFormData.groupCount"
                        name="groupCount"
                        data-testid="group-count"
                        min="2"
                        max="16"
                        required
                      />
                      <small class="text-muted">Each group needs at least two teams.</small>
                    </div>
                    <div class="col-md-6">
                      <label class="form-label">Advancing from each group *</label>
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="generateFormData.teamsAdvancingPerGroup"
                        name="teamsAdvancingPerGroup"
                        data-testid="teams-advancing"
                        min="1"
                        max="8"
                        required
                      />
                      <small class="text-muted">
                        This decides how big the bracket is.
                      </small>
                    </div>
                  }
                </div>
                <div class="form-check mt-3">
                  <input
                    class="form-check-input"
                    type="checkbox"
                    id="replaceExisting"
                    [(ngModel)]="generateFormData.replaceExisting"
                    name="replaceExisting"
                  />
                  <label class="form-check-label" for="replaceExisting">
                    Replace this division's existing fixtures for the season
                  </label>
                  <div class="form-text">
                    Generating adds to what is already there. Leave this unticked and the league
                    will refuse rather than give the division a second copy of its season. Ticking
                    it clears the current fixtures first, and is refused once any of them have
                    been played.
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

                  @if (needsAShootout()) {
                    <div class="col-12">
                      <div class="alert alert-warning py-2 mb-0 small" data-testid="shootout-needed">
                        Level after ninety minutes, and a cup tie has to send somebody through.
                        Record the shootout, or this result cannot be saved.
                      </div>
                    </div>
                    <div class="col-6">
                      <label class="form-label">Home Penalties *</label>
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="scoreFormData.homePenalties"
                        name="homePenalties"
                        data-testid="home-penalties"
                        min="0"
                      />
                    </div>
                    <div class="col-6">
                      <label class="form-label">Away Penalties *</label>
                      <input
                        type="number"
                        class="form-control"
                        [(ngModel)]="scoreFormData.awayPenalties"
                        name="awayPenalties"
                        data-testid="away-penalties"
                        min="0"
                      />
                    </div>
                    <div class="col-12">
                      <small class="text-muted" data-testid="who-goes-through">
                        {{ whoGoesThrough() }}
                      </small>
                    </div>
                  }

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
  filterTeamId: string | undefined;
  filterStatus: string | undefined;
  currentPage = 1;
  pageSize = 20;

  createFormData: any = this.getEmptyCreateForm();
  scoreFormData: any = { homeScore: 0, awayScore: 0, status: 'Completed', notes: '' };
  /**
   * The format of the division chosen in the generate dialog, which decides what the dialog
   * asks for. A bracket has no home and away leg, and only a group competition needs to know
   * how many groups there are.
   */
  generateFormat(): CompetitionFormat {
    const chosen = this.divisions().find((d) => d.id === this.generateFormData.divisionId);
    return chosen?.format ?? 'League';
  }

  generateFormData: {
    divisionId: string;
    season: number;
    isHomeAndAway: boolean;
    startDate: string;
    daysBetweenMatchweeks: number;
    replaceExisting: boolean;
    groupCount: number;
    teamsAdvancingPerGroup: number;
  } = {
    divisionId: '',
    season: new Date().getFullYear(),
    isHomeAndAway: true,
    startDate: '',
    daysBetweenMatchweeks: 7,
    replaceExisting: false,
    groupCount: 2,
    teamsAdvancingPerGroup: 2
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
        // A team plays home and away, so this matches a fixture from either side rather
        // than only the ones where they are listed first.
        this.filterTeamId,
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

  /** Teams of the given division, or every team when none is chosen. */
  /** What a bracket round of that size is called. */
  roundName = knockoutRoundName;

  getTeamsByDivision(divisionId: string | null | undefined) {
    const all = this.teams();
    return divisionId ? all.filter((t) => t.divisionId === divisionId) : all;
  }

  /**
   * Changing the division invalidates team choices from the old one. Two clubs from different
   * divisions should never end up scheduled against each other, and before this the pickers
   * offered every team in the league regardless of the division selected above them.
   */
  onCreateDivisionChange() {
    const eligible = this.getTeamsByDivision(this.createFormData.divisionId);

    if (!eligible.some((t) => t.id === this.createFormData.homeTeamId)) {
      this.createFormData.homeTeamId = null;
    }

    if (!eligible.some((t) => t.id === this.createFormData.awayTeamId)) {
      this.createFormData.awayTeamId = null;
    }
  }

  /**
   * Any filter change starts again at the first page. Staying on page three of the previous
   * result set lands on an empty table and reads as "no matches", which is how a working
   * filter gets reported as broken.
   */
  onFiltersChanged() {
    this.currentPage = 1;
    this.loadMatches();
  }

  onFilterDivisionChange() {
    // The chosen team may not play in the newly chosen division.
    if (this.filterTeamId) {
      const team = this.teams().find((t) => t.id === this.filterTeamId);
      if (!team || team.divisionId !== this.filterDivisionId) {
        this.filterTeamId = undefined;
      }
    }
    this.onFiltersChanged();
  }

  clearFilters() {
    this.filterMatchweek = undefined;
    this.filterDivisionId = undefined;
    this.filterTeamId = undefined;
    this.filterStatus = undefined;
    this.onFiltersChanged();
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
      daysBetweenMatchweeks: 7,
      // Always reopens unticked: replacing a season is a deliberate choice, never a leftover
      // from the last time the modal was open.
      replaceExisting: false,
      groupCount: 2,
      teamsAdvancingPerGroup: 2
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
      daysBetweenMatchweeks: this.generateFormData.daysBetweenMatchweeks,
      replaceExisting: this.generateFormData.replaceExisting,
      // Only meaningful for a group competition, and sending them otherwise would put numbers
      // on the request that the format has no use for.
      ...(this.generateFormat() === 'GroupAndKnockout'
        ? {
            groupCount: this.generateFormData.groupCount,
            teamsAdvancingPerGroup: this.generateFormData.teamsAdvancingPerGroup
          }
        : {})
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
        this.error.set(`Failed to generate fixtures: ${err.error?.detail || err.error?.title || err.message}`);
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
        this.error.set(`Failed to create fixture: ${err.error?.detail || err.error?.title || err.message}`);
        this.saving.set(false);
      },
    });
  }

  /**
   * A knockout tie that finished level. A league match is perfectly happy to end in a draw;
   * a bracket fixture has to name somebody to go into the next round, so the shootout is the
   * only way the result can be recorded at all.
   */
  shootout(match: MatchResultDto): string | null {
    return shootoutSuffix(match.homePenalties, match.awayPenalties);
  }

  needsAShootout(): boolean {
    const match = this.selectedMatch();
    if (!match || match.stage !== 'Knockout') return false;

    return Number(this.scoreFormData.homeScore) === Number(this.scoreFormData.awayScore);
  }

  /** Says who the shootout has sent through, so the entry can be checked before it is saved. */
  whoGoesThrough(): string {
    const match = this.selectedMatch();
    if (!match) return '';

    const home = this.numberOrUndefined(this.scoreFormData.homePenalties);
    const away = this.numberOrUndefined(this.scoreFormData.awayPenalties);

    // Nothing to say until both are filled in.
    if (home === undefined || away === undefined) return '';
    if (home === away) return 'A shootout cannot be level either — somebody has to win it.';

    const through = home > away ? match.homeTeamName : match.awayTeamName;
    return `${through} goes through.`;
  }

  showScoreModal(match: MatchResultDto) {
    this.selectedMatch.set(match);
    this.scoreFormData = {
      homeScore: match.homeScore,
      awayScore: match.awayScore,
      status: match.status === 'Scheduled' ? 'Completed' : match.status,
      notes: match.notes || '',
      homePenalties: match.homePenalties ?? null,
      awayPenalties: match.awayPenalties ?? null,
    };
    this.showUpdateScoreModal.set(true);
  }

  private numberOrUndefined(value: unknown): number | undefined {
    if (value === null || value === undefined || value === '') return undefined;

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : undefined;
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
      // Sent only when the tie is actually level, so a decisive result that was corrected
      // from a draw does not keep the shootout that settled the earlier one.
      ...(this.needsAShootout()
        ? {
            homePenalties: this.numberOrUndefined(this.scoreFormData.homePenalties),
            awayPenalties: this.numberOrUndefined(this.scoreFormData.awayPenalties),
          }
        : {}),
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
        this.error.set(`Failed to update score: ${err.error?.detail || err.error?.title || err.message}`);
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
