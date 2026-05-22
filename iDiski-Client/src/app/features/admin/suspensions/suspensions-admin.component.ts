import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SuspensionService } from '../../../core/services/suspension.service';
import { PlayerService } from '../../../core/services/player.service';
import { DivisionService } from '../../../core/services/division.service';
import {
  SuspensionDto,
  CreateSuspensionCommand,
  PlayerDto,
  DivisionDto,
} from '../../../core/models';

@Component({
  selector: 'app-suspensions-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Suspensions Dashboard</h1>
          <p class="text-muted">View and manage player suspensions</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-danger" (click)="showAddModal()">
            <i class="bi bi-ban"></i> Add Suspension
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">Filter by Division</label>
              <select
                class="form-select"
                [(ngModel)]="filterDivisionId"
                (ngModelChange)="loadSuspensions()"
              >
                <option [ngValue]="undefined">All Divisions</option>
                @for (division of divisions(); track division.id) {
                  <option [ngValue]="division.id">{{ division.name }}</option>
                }
              </select>
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

      <!-- Suspensions Table -->
      @if (!loading() && suspensions().length > 0) {
        <div class="card">
          <div class="card-header bg-danger text-white">
            <h5 class="mb-0">
              <i class="bi bi-ban"></i> Active Suspensions ({{ suspensions().length }})
            </h5>
          </div>
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Player</th>
                  <th>Team</th>
                  <th>Reason</th>
                  <th>Start Date</th>
                  <th>End Date</th>
                  <th>Matches</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (suspension of suspensions(); track suspension.id) {
                  <tr>
                    <td class="fw-semibold">{{ suspension.playerName }}</td>
                    <td>{{ suspension.teamName }}</td>
                    <td>
                      <span class="text-danger">
                        <i class="bi bi-exclamation-triangle"></i>
                        {{ suspension.reason }}
                      </span>
                    </td>
                    <td>{{ suspension.startDate | date: 'MMM d, y' }}</td>
                    <td>{{ suspension.endDate | date: 'MMM d, y' }}</td>
                    <td>
                      <span class="badge bg-danger">{{ suspension.matchesSuspended }} match(es)</span>
                    </td>
                    <td>
                      @if (suspension.isActive) {
                        <span class="badge bg-danger">Active</span>
                      } @else {
                        <span class="badge bg-secondary">Served</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && suspensions().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-check-circle display-1 text-success"></i>
            <h3 class="mt-3">No Active Suspensions</h3>
            <p class="text-muted">All players are eligible to play</p>
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

    <!-- Add Suspension Modal -->
    @if (showModal()) {
      <div
        class="modal fade show d-block"
        tabindex="-1"
        style="background-color: rgba(0,0,0,0.5)"
      >
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header bg-danger text-white">
              <h5 class="modal-title">
                <i class="bi bi-ban"></i> Add Player Suspension
              </h5>
              <button
                type="button"
                class="btn-close btn-close-white"
                (click)="closeModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #suspensionForm="ngForm">
                <div class="mb-3">
                  <label class="form-label">Player *</label>
                  <select
                    class="form-select"
                    [(ngModel)]="formData.playerId"
                    name="playerId"
                    required
                  >
                    <option [ngValue]="null">Select Player</option>
                    @for (player of players(); track player.id) {
                      <option [ngValue]="player.id">
                        {{ player.fullName }} ({{ player.teamName }})
                        @if (player.isCurrentlySuspended) {
                          - Already Suspended
                        }
                      </option>
                    }
                  </select>
                </div>

                <div class="mb-3">
                  <label class="form-label">Reason *</label>
                  <select
                    class="form-select"
                    [(ngModel)]="formData.reason"
                    name="reason"
                    required
                  >
                    <option value="">Select Reason</option>
                    <option value="Red Card">Red Card</option>
                    <option value="Accumulated Yellow Cards">Accumulated Yellow Cards</option>
                    <option value="Violent Conduct">Violent Conduct</option>
                    <option value="Unsporting Behavior">Unsporting Behavior</option>
                    <option value="Other Disciplinary">Other Disciplinary</option>
                  </select>
                </div>

                <div class="mb-3">
                  <label class="form-label">Number of Matches *</label>
                  <input
                    type="number"
                    class="form-control"
                    [(ngModel)]="formData.matchesSuspended"
                    name="matchesSuspended"
                    required
                    min="1"
                    max="99"
                    placeholder="e.g., 1, 2, 3"
                  />
                  <small class="text-muted">Number of matches player will be suspended</small>
                </div>

                <div class="mb-3">
                  <label class="form-label">Start Date</label>
                  <input
                    type="date"
                    class="form-control"
                    [(ngModel)]="formData.startDate"
                    name="startDate"
                  />
                  <small class="text-muted">Leave empty to start immediately</small>
                </div>

                <div class="alert alert-warning" role="alert">
                  <i class="bi bi-exclamation-triangle"></i>
                  <strong>Warning:</strong> Suspended players cannot be selected for matches during the suspension period.
                </div>
              </form>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-danger"
                (click)="addSuspension()"
                [disabled]="suspensionForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Add Suspension
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
export class SuspensionsAdminComponent implements OnInit {
  private suspensionService = inject(SuspensionService);
  private playerService = inject(PlayerService);
  private divisionService = inject(DivisionService);

  suspensions = signal<SuspensionDto[]>([]);
  players = signal<PlayerDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);

  filterDivisionId: string | undefined;

  formData: any = this.getEmptyForm();

  ngOnInit() {
    this.loadDivisions();
    this.loadPlayers();
    this.loadSuspensions();
  }

  loadSuspensions() {
    this.loading.set(true);
    this.error.set(null);

    this.suspensionService.getActive(this.filterDivisionId).subscribe({
      next: (data) => {
        this.suspensions.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load suspensions: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  loadPlayers() {
    this.playerService.getAll(undefined, true).subscribe({
      next: (data) => this.players.set(data),
      error: (err) => console.error('Failed to load players:', err),
    });
  }

  loadDivisions() {
    this.divisionService.getAll(undefined, true).subscribe({
      next: (data) => this.divisions.set(data),
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  showAddModal() {
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.formData = this.getEmptyForm();
  }

  addSuspension() {
    this.saving.set(true);
    this.error.set(null);

    const command: CreateSuspensionCommand = {
      playerId: this.formData.playerId,
      reason: this.formData.reason,
      matchesSuspended: this.formData.matchesSuspended,
      startDate: this.formData.startDate || undefined,
    };

    this.suspensionService.create(command).subscribe({
      next: () => {
        this.success.set('Suspension added successfully');
        this.saving.set(false);
        this.closeModal();
        this.loadSuspensions();
        this.loadPlayers(); // Refresh to update suspension status
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to add suspension: ${err.error?.message || err.message}`);
        this.saving.set(false);
      },
    });
  }

  private getEmptyForm() {
    return {
      playerId: null,
      reason: '',
      matchesSuspended: 1,
      startDate: '',
    };
  }
}
