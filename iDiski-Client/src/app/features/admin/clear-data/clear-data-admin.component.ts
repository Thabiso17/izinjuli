import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClearDataService } from '../../../core/services/clear-data.service';
import { TeamService } from '../../../core/services/team.service';
import { DivisionService } from '../../../core/services/division.service';
import { TeamDto, DivisionDto } from '../../../core/models';

type ClearScope = 'players' | 'team' | 'division';

@Component({
  selector: 'app-clear-data-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Clear Data</h1>
          <p class="text-muted">
            Permanently remove test or seed data before launch. These actions cannot be undone.
          </p>
        </div>
      </div>

      <div class="alert alert-warning">
        <i class="bi bi-exclamation-triangle-fill me-2"></i>
        Everything on this page performs a hard, permanent delete. There is no undo.
      </div>

      <div class="row g-4">
        <!-- Clear Players -->
        <div class="col-md-4">
          <div class="card h-100 border-danger-subtle">
            <div class="card-body d-flex flex-column">
              <h5 class="card-title">Remove Players from a Team</h5>
              <p class="text-muted small">
                Deletes every player on the selected team, along with their suspensions and match
                events. The team itself is kept.
              </p>
              <div class="mb-3 mt-auto">
                <label class="form-label">Team</label>
                <select class="form-select" [(ngModel)]="selectedPlayersTeamId">
                  <option [ngValue]="null">Select Team</option>
                  @for (team of teams(); track team.id) {
                    <option [ngValue]="team.id">
                      {{ team.name }} ({{ team.playerCount }} players)
                    </option>
                  }
                </select>
              </div>
              <button
                class="btn btn-outline-danger"
                [disabled]="!selectedPlayersTeamId"
                (click)="startClear('players', selectedPlayersTeamId)"
              >
                <i class="bi bi-person-x"></i> Remove All Players
              </button>
            </div>
          </div>
        </div>

        <!-- Clear Team -->
        <div class="col-md-4">
          <div class="card h-100 border-danger-subtle">
            <div class="card-body d-flex flex-column">
              <h5 class="card-title">Delete a Team</h5>
              <p class="text-muted small">
                Deletes the selected team and everything tied to it: its players, suspensions,
                match events and match history.
              </p>
              <div class="mb-3 mt-auto">
                <label class="form-label">Team</label>
                <select class="form-select" [(ngModel)]="selectedTeamId">
                  <option [ngValue]="null">Select Team</option>
                  @for (team of teams(); track team.id) {
                    <option [ngValue]="team.id">{{ team.name }}</option>
                  }
                </select>
              </div>
              <button
                class="btn btn-outline-danger"
                [disabled]="!selectedTeamId"
                (click)="startClear('team', selectedTeamId)"
              >
                <i class="bi bi-trash"></i> Delete Team
              </button>
            </div>
          </div>
        </div>

        <!-- Clear Division -->
        <div class="col-md-4">
          <div class="card h-100 border-danger-subtle">
            <div class="card-body d-flex flex-column">
              <h5 class="card-title">Delete a Division</h5>
              <p class="text-muted small">
                Deletes the selected division and everything nested under it: its teams,
                players, suspensions, match events and match history.
              </p>
              <div class="mb-3 mt-auto">
                <label class="form-label">Division</label>
                <select class="form-select" [(ngModel)]="selectedDivisionId">
                  <option [ngValue]="null">Select Division</option>
                  @for (division of divisions(); track division.id) {
                    <option [ngValue]="division.id">
                      {{ division.name }} ({{ division.season }})
                    </option>
                  }
                </select>
              </div>
              <button
                class="btn btn-outline-danger"
                [disabled]="!selectedDivisionId"
                (click)="startClear('division', selectedDivisionId)"
              >
                <i class="bi bi-trash"></i> Delete Division
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Error / Success Alerts -->
      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show mt-4" role="alert">
          {{ error() }}
          <button type="button" class="btn-close" (click)="error.set(null)"></button>
        </div>
      }
      @if (success()) {
        <div class="alert alert-success alert-dismissible fade show mt-4" role="alert">
          {{ success() }}
          <button type="button" class="btn-close" (click)="success.set(null)"></button>
        </div>
      }
    </div>

    <!-- Confirmation Modal -->
    @if (pendingAction()) {
      <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5)">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header bg-danger text-white">
              <h5 class="modal-title">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                Confirm Permanent Deletion
              </h5>
              <button type="button" class="btn-close btn-close-white" (click)="cancelClear()"></button>
            </div>
            <div class="modal-body">
              <p>{{ pendingAction()!.description }}</p>
              <p class="fw-semibold">
                Type <code>{{ pendingAction()!.name }}</code> below to confirm.
              </p>
              <input
                type="text"
                class="form-control"
                [(ngModel)]="confirmText"
                [placeholder]="pendingAction()!.name"
                autofocus
              />
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="cancelClear()">
                Cancel
              </button>
              <button
                type="button"
                class="btn btn-danger"
                [disabled]="confirmText !== pendingAction()!.name || clearing()"
                (click)="executeClear()"
              >
                @if (clearing()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Permanently Delete
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
export class ClearDataAdminComponent implements OnInit {
  private clearDataService = inject(ClearDataService);
  private teamService = inject(TeamService);
  private divisionService = inject(DivisionService);

  teams = signal<TeamDto[]>([]);
  divisions = signal<DivisionDto[]>([]);

  selectedPlayersTeamId: string | null = null;
  selectedTeamId: string | null = null;
  selectedDivisionId: string | null = null;

  error = signal<string | null>(null);
  success = signal<string | null>(null);
  clearing = signal(false);

  pendingAction = signal<{ scope: ClearScope; id: string; name: string; description: string } | null>(
    null
  );
  confirmText = '';

  ngOnInit() {
    this.loadTeams();
    this.loadDivisions();
  }

  loadTeams() {
    this.teamService.getAll().subscribe({
      next: (data) => this.teams.set(data),
      error: (err) => console.error('Failed to load teams:', err),
    });
  }

  loadDivisions() {
    this.divisionService.getAll().subscribe({
      next: (data) => this.divisions.set(data),
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  startClear(scope: ClearScope, id: string | null) {
    if (!id) return;

    if (scope === 'players' || scope === 'team') {
      const team = this.teams().find((t) => t.id === id);
      if (!team) return;

      this.pendingAction.set({
        scope,
        id,
        name: team.name,
        description:
          scope === 'players'
            ? `This will permanently delete all ${team.playerCount} player(s) on "${team.name}".`
            : `This will permanently delete "${team.name}" and all of its players and match history.`,
      });
    } else {
      const division = this.divisions().find((d) => d.id === id);
      if (!division) return;

      this.pendingAction.set({
        scope,
        id,
        name: division.name,
        description: `This will permanently delete "${division.name}" and all ${division.teamCount} team(s), their players, and match history under it.`,
      });
    }

    this.confirmText = '';
    this.error.set(null);
  }

  cancelClear() {
    this.pendingAction.set(null);
    this.confirmText = '';
  }

  executeClear() {
    const action = this.pendingAction();
    if (!action || this.confirmText !== action.name) return;

    this.clearing.set(true);
    this.error.set(null);

    const request$ =
      action.scope === 'players'
        ? this.clearDataService.clearPlayers(action.id)
        : action.scope === 'team'
          ? this.clearDataService.clearTeam(action.id)
          : this.clearDataService.clearDivision(action.id);

    request$.subscribe({
      next: (count) => {
        this.clearing.set(false);
        this.pendingAction.set(null);
        this.confirmText = '';
        this.success.set(this.buildSuccessMessage(action.scope, action.name, count));
        this.selectedPlayersTeamId = null;
        this.selectedTeamId = null;
        this.selectedDivisionId = null;
        this.loadTeams();
        this.loadDivisions();
        setTimeout(() => this.success.set(null), 5000);
      },
      error: (err) => {
        this.error.set(`Failed to clear data: ${err.error?.message || err.message}`);
        this.clearing.set(false);
      },
    });
  }

  private buildSuccessMessage(scope: ClearScope, name: string, count: number): string {
    switch (scope) {
      case 'players':
        return `Removed ${count} player(s) from "${name}".`;
      case 'team':
        return `Deleted team "${name}".`;
      case 'division':
        return `Deleted division "${name}" and ${count} team(s) under it.`;
    }
  }
}
