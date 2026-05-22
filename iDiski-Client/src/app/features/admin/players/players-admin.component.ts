import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlayerService } from '../../../core/services/player.service';
import { TeamService } from '../../../core/services/team.service';
import { DivisionService } from '../../../core/services/division.service';
import { UploadService } from '../../../core/services/upload.service';
import {
  PlayerDto,
  CreatePlayerRequest,
  UpdatePlayerRequest,
  TeamDto,
  DivisionDto,
  PlayerPosition,
  PreferredFoot,
} from '../../../core/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-players-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Players Management</h1>
          <p class="text-muted">Manage player rosters and details</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Add Player
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-3">
              <label class="form-label">Filter by Division</label>
              <select
                class="form-select"
                [(ngModel)]="filterDivisionId"
                (ngModelChange)="loadTeams(); loadPlayers()"
              >
                <option [ngValue]="undefined">All Divisions</option>
                @for (division of divisions(); track division.id) {
                  <option [ngValue]="division.id">{{ division.name }}</option>
                }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label">Filter by Team</label>
              <select
                class="form-select"
                [(ngModel)]="filterTeamId"
                (ngModelChange)="loadPlayers()"
              >
                <option [ngValue]="undefined">All Teams</option>
                @for (team of teams(); track team.id) {
                  <option [ngValue]="team.id">{{ team.name }}</option>
                }
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label">Status</label>
              <select
                class="form-select"
                [(ngModel)]="filterActiveOnly"
                (ngModelChange)="loadPlayers()"
              >
                <option [ngValue]="true">Active Only</option>
                <option [ngValue]="false">All Players</option>
              </select>
            </div>
            <div class="col-md-3 d-flex align-items-end">
              <button class="btn btn-outline-secondary" (click)="clearFilters()">
                Clear Filters
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

      <!-- Players Table -->
      @if (!loading() && players().length > 0) {
        <div class="card">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Photo</th>
                  <th>Player Name</th>
                  <th>Team</th>
                  <th>Jersey</th>
                  <th>Position</th>
                  <th>Foot</th>
                  <th>Age</th>
                  <th>Stats</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (player of players(); track player.id) {
                  <tr
                    class="player-row"
                    [class.selected]="selectedPlayerId() === player.id"
                    (click)="selectPlayer(player.id)"
                  >
                    <td>
                      @if (getImageUrl(player.profileImageUrl)) {
                        <img
                          [src]="getImageUrl(player.profileImageUrl)"
                          [alt]="player.fullName"
                          class="rounded-circle"
                          style="width: 40px; height: 40px; object-fit: cover"
                          (error)="$event.target.style.display='none'"
                        />
                      } @else {
                        <div
                          class="rounded-circle bg-secondary d-inline-flex align-items-center justify-content-center"
                          style="width: 40px; height: 40px"
                        >
                          <i class="bi bi-person text-white"></i>
                        </div>
                      }
                    </td>
                    <td class="fw-semibold">{{ player.fullName }}</td>
                    <td>{{ player.teamName }}</td>
                    <td>
                      <span class="badge bg-secondary">{{ player.jerseyNumber }}</span>
                    </td>
                    <td>{{ player.position }}</td>
                    <td>
                      <span class="badge bg-info">{{ player.preferredFoot }}</span>
                    </td>
                    <td>{{ player.ageYears }}</td>
                    <td>
                      <small class="text-muted">
                        <i class="bi bi-trophy-fill text-warning"></i> {{ player.goals }} |
                        <i class="bi bi-hand-thumbs-up-fill text-info"></i> {{ player.assists }} |
                        <i class="bi bi-square-fill text-warning"></i> {{ player.yellowCards }} |
                        <i class="bi bi-square-fill text-danger"></i> {{ player.redCards }}
                      </small>
                    </td>
                    <td>
                      @if (player.isCurrentlySuspended) {
                        <span class="badge bg-danger">Suspended</span>
                      } @else if (player.isActive) {
                        <span class="badge bg-success">Active</span>
                      } @else {
                        <span class="badge bg-secondary">Inactive</span>
                      }
                    </td>
                    <td (click)="$event.stopPropagation()">
                      <div class="btn-group" role="group">
                        <button
                          type="button"
                          class="btn btn-sm btn-outline-primary"
                          (click)="showEditModal(player)"
                          title="Edit player details"
                        >
                          <i class="bi bi-pencil"></i> Edit
                        </button>
                        <button
                          type="button"
                          class="btn btn-sm btn-outline-info"
                          (click)="showTransferModalFor(player)"
                          [disabled]="!player.isActive"
                          [title]="player.isActive ? 'Transfer to another team' : 'Cannot transfer inactive player'"
                        >
                          <i class="bi bi-arrow-left-right"></i> Transfer
                        </button>
                        <button
                          type="button"
                          class="btn btn-sm btn-outline-danger"
                          (click)="confirmDeactivate(player)"
                          [disabled]="!player.isActive"
                          [title]="player.isActive ? 'Deactivate player' : 'Already inactive'"
                        >
                          <i class="bi bi-person-x"></i> Deactivate
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && players().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-person display-1 text-muted"></i>
            <h3 class="mt-3">No Players Found</h3>
            <p class="text-muted">Add your first player to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Add Player
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

    <!-- Add/Edit Modal -->
    @if (showModal()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">
                {{ editingPlayer() ? 'Edit Player' : 'Add Player' }}
              </h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #playerForm="ngForm">
                <div class="row g-3">
                  <div class="col-md-6">
                    <label class="form-label">First Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.firstName"
                      name="firstName"
                      required
                      placeholder="e.g., Lebo"
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Last Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.lastName"
                      name="lastName"
                      required
                      placeholder="e.g., Molefe"
                    />
                  </div>

                  <div class="col-md-4">
                    <label class="form-label">Division (Filter)</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formDivisionFilter"
                      name="formDivisionFilter"
                      (ngModelChange)="filterTeamsByDivision()"
                    >
                      <option [ngValue]="undefined">All Divisions</option>
                      @for (division of divisions(); track division.id) {
                        <option [ngValue]="division.id">{{ division.name }}</option>
                      }
                    </select>
                    <small class="text-muted">Filter teams by division</small>
                  </div>
                  <div class="col-md-8">
                    <label class="form-label">Team *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.teamId"
                      name="teamId"
                      required
                    >
                      <option [ngValue]="null">Select Team</option>
                      @for (team of getFilteredTeams(); track team.id) {
                        <option [ngValue]="team.id">{{ team.name }} ({{ team.divisionName }})</option>
                      }
                    </select>
                  </div>
                  <div class="col-md-3">
                    <label class="form-label">Jersey Number *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="formData.jerseyNumber"
                      name="jerseyNumber"
                      required
                      min="1"
                      max="99"
                      placeholder="10"
                    />
                  </div>
                  <div class="col-md-3">
                    <label class="form-label">Position *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.position"
                      name="position"
                      required
                    >
                      <option value="">Select Position</option>
                      <optgroup label="Goalkeeper">
                        <option [value]="PlayerPosition.GK">GK - Goalkeeper</option>
                      </optgroup>
                      <optgroup label="Defenders">
                        <option [value]="PlayerPosition.CB">CB - Center Back</option>
                        <option [value]="PlayerPosition.SW">SW - Sweeper</option>
                        <option [value]="PlayerPosition.RB">RB - Right Back</option>
                        <option [value]="PlayerPosition.LB">LB - Left Back</option>
                        <option [value]="PlayerPosition.RWB">RWB - Right Wing-Back</option>
                        <option [value]="PlayerPosition.LWB">LWB - Left Wing-Back</option>
                      </optgroup>
                      <optgroup label="Midfielders">
                        <option [value]="PlayerPosition.CDM">CDM - Defensive Midfielder</option>
                        <option [value]="PlayerPosition.CM">CM - Central Midfielder</option>
                        <option [value]="PlayerPosition.CAM">CAM - Attacking Midfielder</option>
                        <option [value]="PlayerPosition.RM">RM - Right Midfielder</option>
                        <option [value]="PlayerPosition.LM">LM - Left Midfielder</option>
                      </optgroup>
                      <optgroup label="Forwards">
                        <option [value]="PlayerPosition.ST">ST - Striker</option>
                        <option [value]="PlayerPosition.CF">CF - Center Forward</option>
                        <option [value]="PlayerPosition.RW">RW - Right Winger</option>
                        <option [value]="PlayerPosition.LW">LW - Left Winger</option>
                      </optgroup>
                    </select>
                  </div>

                  <div class="col-md-3">
                    <label class="form-label">Preferred Foot *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.preferredFoot"
                      name="preferredFoot"
                      required
                    >
                      <option value="">Select Foot</option>
                      <option [value]="PreferredFoot.Right">Right</option>
                      <option [value]="PreferredFoot.Left">Left</option>
                      <option [value]="PreferredFoot.Both">Both</option>
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Date of Birth *</label>
                    <input
                      type="date"
                      class="form-control"
                      [(ngModel)]="formData.dateOfBirth"
                      name="dateOfBirth"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Nationality</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.nationality"
                      name="nationality"
                      placeholder="e.g., South African"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Profile Image</label>

                    @if (formData.profileImageUrl) {
                      <div class="mb-2">
                        <img
                          [src]="getImageUrl(formData.profileImageUrl) || formData.profileImageUrl"
                          alt="Preview"
                          class="img-thumbnail"
                          style="max-width: 150px; max-height: 150px"
                          (error)="$event.target.style.display='none'"
                        />
                        <button
                          type="button"
                          class="btn btn-sm btn-danger ms-2"
                          (click)="formData.profileImageUrl = ''"
                        >
                          <i class="bi bi-trash"></i> Remove
                        </button>
                      </div>
                    }

                    <div class="input-group">
                      <input
                        type="file"
                        class="form-control"
                        accept="image/*"
                        (change)="onPlayerImageSelected($event)"
                        #playerImageInput
                      />
                      <button
                        type="button"
                        class="btn btn-outline-secondary"
                        (click)="playerImageInput.value = ''; formData.profileImageUrl = ''"
                      >
                        Clear
                      </button>
                    </div>
                    <small class="text-muted">Upload player photo (JPG, PNG, max 5MB)</small>

                    @if (uploadingImage()) {
                      <div class="mt-2">
                        <div class="progress">
                          <div
                            class="progress-bar progress-bar-striped progress-bar-animated"
                            role="progressbar"
                            style="width: 100%"
                          >
                            Uploading...
                          </div>
                        </div>
                      </div>
                    }

                    <div class="mt-2">
                      <small class="text-muted">Or paste URL:</small>
                      <input
                        type="url"
                        class="form-control form-control-sm mt-1"
                        [(ngModel)]="formData.profileImageUrl"
                        name="profileImageUrl"
                        placeholder="https://example.com/player.jpg"
                      />
                    </div>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Biography</label>
                    <textarea
                      class="form-control"
                      [(ngModel)]="formData.bio"
                      name="bio"
                      rows="4"
                      maxlength="2000"
                      placeholder="Player biography for features like Player of the Month"
                    ></textarea>
                    <small class="text-muted">
                      {{ formData.bio?.length || 0 }}/2000 characters
                    </small>
                  </div>

                  @if (editingPlayer()) {
                    <div class="col-12">
                      <div class="form-check">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          [(ngModel)]="formData.isActive"
                          name="isActive"
                          id="isActive"
                        />
                        <label class="form-check-label" for="isActive">
                          Active Player
                        </label>
                      </div>
                    </div>
                  }
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-primary"
                (click)="savePlayer()"
                [disabled]="playerForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingPlayer() ? 'Update' : 'Create' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Transfer Modal -->
    @if (showTransferModal()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header bg-info text-white">
              <h5 class="modal-title">
                <i class="bi bi-arrow-left-right me-2"></i>
                Transfer Player
              </h5>
              <button
                type="button"
                class="btn-close btn-close-white"
                (click)="closeTransferModal()"
              ></button>
            </div>
            <div class="modal-body">
              @if (transferringPlayer()) {
                <div class="alert alert-info mb-3">
                  <strong>{{ transferringPlayer()!.fullName }}</strong>
                  <br />
                  <small>Current Team: {{ transferringPlayer()!.teamName }} (Jersey #{{ transferringPlayer()!.jerseyNumber }})</small>
                </div>
              }

              <form #transferForm="ngForm">
                <div class="mb-3">
                  <label class="form-label">New Team *</label>
                  <select
                    class="form-select"
                    [(ngModel)]="transferFormData.newTeamId"
                    name="newTeamId"
                    required
                  >
                    <option [ngValue]="null">Select Team</option>
                    @for (team of teams(); track team.id) {
                      @if (team.id !== transferringPlayer()?.teamId) {
                        <option [ngValue]="team.id">{{ team.name }} ({{ team.divisionName }})</option>
                      }
                    }
                  </select>
                  <small class="text-muted">Select the team to transfer this player to</small>
                </div>

                <div class="mb-3">
                  <label class="form-label">New Jersey Number *</label>
                  <input
                    type="number"
                    class="form-control"
                    [(ngModel)]="transferFormData.newJerseyNumber"
                    name="newJerseyNumber"
                    required
                    min="1"
                    max="99"
                    placeholder="10"
                  />
                  <small class="text-muted">The player's jersey number in the new team</small>
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeTransferModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-info"
                (click)="executeTransfer()"
                [disabled]="transferForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                <i class="bi bi-arrow-left-right me-2"></i>
                Transfer Player
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

      .table td button {
        cursor: pointer;
        position: relative;
        z-index: 1;
      }

      .table-responsive {
        overflow-x: auto;
      }

      .player-row {
        cursor: pointer;
        transition: background-color 0.2s ease;
      }

      .player-row:hover {
        background-color: #f8f9fa !important;
      }

      .player-row.selected {
        background-color: #e7f1ff !important;
        border-left: 4px solid #0d6efd;
      }

      .player-row.selected td:first-child {
        padding-left: 8px;
      }
    `,
  ],
})
export class PlayersAdminComponent implements OnInit {
  private playerService = inject(PlayerService);
  private teamService = inject(TeamService);
  private divisionService = inject(DivisionService);
  private uploadService = inject(UploadService);

  // Expose enums to template
  PlayerPosition = PlayerPosition;
  PreferredFoot = PreferredFoot;

  players = signal<PlayerDto[]>([]);
  teams = signal<TeamDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  uploadingImage = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  showTransferModal = signal(false);
  editingPlayer = signal<PlayerDto | null>(null);
  transferringPlayer = signal<PlayerDto | null>(null);
  selectedPlayerId = signal<string | null>(null);

  filterTeamId: string | undefined;
  filterDivisionId: string | undefined;
  filterActiveOnly = true;

  formDivisionFilter: string | undefined;
  formData: any = this.getEmptyForm();

  // Transfer form data
  transferFormData = {
    newTeamId: null as string | null,
    newJerseyNumber: 1,
  };

  ngOnInit() {
    this.loadDivisions();
    this.loadTeams();
    this.loadPlayers();
  }

  loadPlayers() {
    this.loading.set(true);
    this.error.set(null);

    this.playerService.getAll(this.filterTeamId, this.filterActiveOnly).subscribe({
      next: (data) => {
        this.players.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load players: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  loadDivisions() {
    // Load all divisions (active and inactive) for admin panel
    this.divisionService.getAll().subscribe({
      next: (data) => this.divisions.set(data),
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  loadTeams() {
    this.teamService.getAll().subscribe({
      next: (data) => this.teams.set(data),
      error: (err) => console.error('Failed to load teams:', err),
    });
  }

  clearFilters() {
    this.filterTeamId = undefined;
    this.filterDivisionId = undefined;
    this.filterActiveOnly = true;
    this.loadPlayers();
  }

  filterTeamsByDivision() {
    // Just a trigger for the template to re-render getFilteredTeams()
  }

  getFilteredTeams(): TeamDto[] {
    if (!this.formDivisionFilter) {
      return this.teams();
    }
    return this.teams().filter(team => team.divisionId === this.formDivisionFilter);
  }

  selectPlayer(playerId: string) {
    this.selectedPlayerId.set(playerId);
  }

  getImageUrl(url: string | null): string | null {
    if (!url) return null;
    // If it's already a full URL, return as-is
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    // Otherwise, prepend the API server URL (http://localhost:5207)
    // environment.apiBaseUrl is "http://localhost:5207/api", we need just "http://localhost:5207"
    const apiBase = environment.apiBaseUrl; // "http://localhost:5207/api"
    const serverUrl = apiBase.substring(0, apiBase.lastIndexOf('/api')); // "http://localhost:5207"

    // Ensure url starts with /
    const path = url.startsWith('/') ? url : `/${url}`;

    console.log('Image URL:', `${serverUrl}${path}`);
    return `${serverUrl}${path}`;
  }

  showAddModal() {
    this.editingPlayer.set(null);
    this.formData = this.getEmptyForm();
    this.formDivisionFilter = undefined;
    this.showModal.set(true);
  }

  showEditModal(player: PlayerDto) {
    console.log('Edit button clicked for player:', player.fullName);
    this.editingPlayer.set(player);
    this.formData = {
      firstName: player.firstName,
      lastName: player.lastName,
      profileImageUrl: player.profileImageUrl || '',
      bio: player.bio || '',
      dateOfBirth: player.dateOfBirth,
      nationality: player.nationality || '',
      jerseyNumber: player.jerseyNumber,
      position: player.position,
      preferredFoot: player.preferredFoot,
      teamId: player.teamId,
      isActive: player.isActive,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingPlayer.set(null);
    this.formData = this.getEmptyForm();
  }

  savePlayer() {
    this.saving.set(true);
    this.error.set(null);

    const command = {
      firstName: this.formData.firstName,
      lastName: this.formData.lastName,
      dateOfBirth: this.formData.dateOfBirth,
      jerseyNumber: this.formData.jerseyNumber,
      position: this.formData.position as PlayerPosition,
      preferredFoot: this.formData.preferredFoot as PreferredFoot,
      teamId: this.formData.teamId,
      profileImageUrl: this.formData.profileImageUrl || undefined,
      bio: this.formData.bio || undefined,
      nationality: this.formData.nationality || undefined,
    };

    if (this.editingPlayer()) {
      this.playerService.update(this.editingPlayer()!.id, {
        ...command,
        id: this.editingPlayer()!.id,
        isActive: this.formData.isActive,
      } as UpdatePlayerRequest).subscribe({
        next: () => {
          this.success.set('Player updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadPlayers();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save player: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      this.playerService.create(command as CreatePlayerRequest).subscribe({
        next: () => {
          this.success.set('Player created successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadPlayers();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save player: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  showTransferModalFor(player: PlayerDto) {
    console.log('Transfer button clicked for player:', player.fullName);
    this.transferringPlayer.set(player);
    this.transferFormData = {
      newTeamId: null,
      newJerseyNumber: player.jerseyNumber, // Default to same jersey number
    };
    this.showTransferModal.set(true);
  }

  closeTransferModal() {
    this.showTransferModal.set(false);
    this.transferringPlayer.set(null);
    this.transferFormData = {
      newTeamId: null,
      newJerseyNumber: 1,
    };
  }

  executeTransfer() {
    const player = this.transferringPlayer();
    if (!player || !this.transferFormData.newTeamId) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.playerService
      .transfer(
        player.id,
        this.transferFormData.newTeamId,
        this.transferFormData.newJerseyNumber
      )
      .subscribe({
        next: () => {
          const newTeam = this.teams().find(t => t.id === this.transferFormData.newTeamId);
          this.success.set(
            `${player.fullName} transferred to ${newTeam?.name} successfully`
          );
          this.saving.set(false);
          this.closeTransferModal();
          this.loadPlayers();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(
            `Failed to transfer player: ${err.error?.message || err.message}`
          );
          this.saving.set(false);
        },
      });
  }

  confirmDeactivate(player: PlayerDto) {
    if (
      !confirm(
        `Are you sure you want to deactivate "${player.fullName}"? They will be marked as inactive but stats will be preserved.`
      )
    ) {
      return;
    }

    this.playerService.delete(player.id).subscribe({
      next: () => {
        this.success.set('Player deactivated successfully');
        this.loadPlayers();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to deactivate player: ${err.error?.message || err.message}`);
      },
    });
  }

  onPlayerImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.uploadingImage.set(true);
    this.error.set(null);

    this.uploadService.uploadImage(file, 'players').subscribe({
      next: (response) => {
        this.formData.profileImageUrl = response.fileUrl;
        this.uploadingImage.set(false);
        this.success.set('Image uploaded successfully');
        setTimeout(() => this.success.set(null), 2000);
      },
      error: (err) => {
        this.error.set(`Failed to upload image: ${err.error?.message || err.message}`);
        this.uploadingImage.set(false);
      },
    });
  }

  private getEmptyForm() {
    return {
      firstName: '',
      lastName: '',
      profileImageUrl: '',
      bio: '',
      dateOfBirth: '',
      nationality: '',
      jerseyNumber: 1,
      position: '',
      preferredFoot: PreferredFoot.Right,
      teamId: null,
      isActive: true,
    };
  }
}
