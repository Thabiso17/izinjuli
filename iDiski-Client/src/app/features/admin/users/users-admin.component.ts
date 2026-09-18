import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { TeamService } from '../../../core/services/team.service';
import { CompetitionService } from '../../../core/services/competition.service';
import {
  AdminUserDto,
  AdminUserDetailDto,
  CreateAdminUserRequest,
  TeamDto,
  CompetitionDto,
  ROLE_ID,
  ROLE_BY_ID,
  ROLE_LABEL,
} from '../../../core/models';
// Role is not re-exported through the models barrel, so it comes from where it is declared.
import { Role } from '../../../core/models/auth.model';

/**
 * Administrators, and what each of them is allowed to touch.
 *
 * The API for this has existed all along — create, list, update, and grant or revoke roles,
 * teams and divisions — with no screen in front of it, so the only way to add an admin was to
 * call the endpoint by hand or run the development seeder. In production that meant there was
 * no way to onboard one at all.
 *
 * Roles alone are not enough to make an administrator useful: a team admin reaches the teams
 * assigned to them and a division admin the divisions assigned to them, so this keeps the role
 * and its scope on the same screen rather than leaving someone created and powerless.
 */
@Component({
  selector: 'app-users-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Administrators</h1>
          <p class="text-muted">Create administrators and choose what each one can manage</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showCreateModal()" data-testid="add-admin">
            <i class="bi bi-person-plus"></i> Add Administrator
          </button>
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-danger alert-dismissible" role="alert">
          {{ error() }}
          <button type="button" class="btn-close" (click)="error.set(null)"></button>
        </div>
      }

      @if (success()) {
        <div class="alert alert-success alert-dismissible" role="alert">
          {{ success() }}
          <button type="button" class="btn-close" (click)="success.set(null)"></button>
        </div>
      }

      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
      }

      @if (!loading() && users().length > 0) {
        <div class="card">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th>Status</th>
                  <th>Last Signed In</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (user of users(); track user.id) {
                  <tr data-testid="admin-row">
                    <td class="fw-semibold">{{ user.firstName }} {{ user.lastName }}</td>
                    <td>{{ user.email }}</td>
                    <td data-testid="admin-roles">
                      @for (roleId of user.roleIds; track roleId) {
                        <span class="badge bg-primary me-1">{{ roleLabelFor(roleId) }}</span>
                      } @empty {
                        <span class="badge bg-warning text-dark">No role</span>
                      }
                    </td>
                    <td>
                      @if (user.isActive) {
                        <span class="badge bg-success">Active</span>
                      } @else {
                        <span class="badge bg-secondary">Deactivated</span>
                      }
                    </td>
                    <td class="text-muted small">
                      {{ user.lastLoginAt ? (user.lastLoginAt | date: 'MMM d, y HH:mm') : 'Never' }}
                    </td>
                    <td>
                      <button
                        class="btn btn-sm btn-outline-primary"
                        (click)="showEditModal(user)"
                        data-testid="manage-admin"
                      >
                        Manage access
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      @if (!loading() && users().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-people display-1 text-muted"></i>
            <h3 class="mt-3">No administrators yet</h3>
            <p class="text-muted">Add one to let somebody else help run the league</p>
          </div>
        </div>
      }
    </div>

    <!-- Create -->
    @if (createModalOpen()) {
      <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Add Administrator</h5>
              <button type="button" class="btn-close" (click)="closeCreateModal()"></button>
            </div>
            <div class="modal-body">
              <form #createForm="ngForm">
                <div class="row g-3">
                  <div class="col-md-6">
                    <label class="form-label">First Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="form.firstName"
                      name="firstName"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Last Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="form.lastName"
                      name="lastName"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Email *</label>
                    <input
                      type="email"
                      class="form-control"
                      [(ngModel)]="form.email"
                      name="email"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Temporary Password *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="form.password"
                      name="password"
                      required
                      minlength="8"
                    />
                    <small class="text-muted">
                      At least 8 characters with an upper and lower case letter, a digit and a
                      symbol. They can change it from the forgot-password link.
                    </small>
                  </div>

                  <div class="col-12">
                    <label class="form-label">Role *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="form.role"
                      name="role"
                      required
                      (ngModelChange)="onRoleChange()"
                    >
                      <option [ngValue]="null">Select a role</option>
                      @for (role of assignableRoles; track role) {
                        <option [ngValue]="role">{{ label(role) }}</option>
                      }
                    </select>
                    <small class="text-muted">{{ roleExplanation() }}</small>
                  </div>

                  @if (form.role === Role.TeamAdmin) {
                    <div class="col-12">
                      <label class="form-label">Teams they manage *</label>
                      <div class="border rounded p-2" style="max-height: 220px; overflow-y: auto">
                        @for (team of teams(); track team.id) {
                          <div class="form-check">
                            <input
                              class="form-check-input"
                              type="checkbox"
                              [id]="'team-' + team.id"
                              [checked]="form.teamIds.includes(team.id)"
                              (change)="toggle(form.teamIds, team.id)"
                            />
                            <label class="form-check-label" [for]="'team-' + team.id">
                              {{ team.name }}
                              <span class="text-muted">({{ team.divisionName || 'No division' }})</span>
                            </label>
                          </div>
                        }
                      </div>
                    </div>
                  }

                  @if (form.role === Role.CompetitionAdmin) {
                    <div class="col-12">
                      <label class="form-label">Competitions they run *</label>
                      <div class="border rounded p-2" style="max-height: 220px; overflow-y: auto">
                        @for (competition of competitions(); track competition.id) {
                          <div class="form-check">
                            <input
                              class="form-check-input"
                              type="checkbox"
                              [id]="'competition-' + competition.id"
                              [checked]="form.competitionIds.includes(competition.id)"
                              (change)="toggle(form.competitionIds, competition.id)"
                            />
                            <label class="form-check-label" [for]="'competition-' + competition.id">
                              {{ competition.name }} ({{ competition.season }})
                            </label>
                          </div>
                        }
                      </div>
                    </div>
                  }
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
                (click)="create()"
                [disabled]="createForm.invalid || !scopeChosen() || saving()"
                data-testid="save-admin"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Create
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Manage access -->
    @if (editing(); as user) {
      <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">{{ user.firstName }} {{ user.lastName }}</h5>
              <button type="button" class="btn-close" (click)="closeEditModal()"></button>
            </div>
            <div class="modal-body">
              <h6>Roles</h6>
              @for (role of assignableRoles; track role) {
                <div class="form-check">
                  <input
                    class="form-check-input"
                    type="checkbox"
                    [id]="'edit-role-' + role"
                    [checked]="user.roleIds.includes(idFor(role))"
                    (change)="toggleRole(user, role)"
                  />
                  <label class="form-check-label" [for]="'edit-role-' + role">
                    {{ label(role) }}
                  </label>
                </div>
              }

              <hr />

              <h6>Competitions</h6>
              <div class="border rounded p-2 mb-3" style="max-height: 180px; overflow-y: auto">
                @for (competition of competitions(); track competition.id) {
                  <div class="form-check">
                    <input
                      class="form-check-input"
                      type="checkbox"
                      [id]="'edit-competition-' + competition.id"
                      [checked]="user.assignedCompetitionIds.includes(competition.id)"
                      (change)="toggleCompetition(user, competition.id)"
                    />
                    <label class="form-check-label" [for]="'edit-competition-' + competition.id">
                      {{ competition.name }} ({{ competition.season }})
                    </label>
                  </div>
                }
              </div>

              <h6>Teams</h6>
              <div class="border rounded p-2" style="max-height: 180px; overflow-y: auto">
                @for (team of teams(); track team.id) {
                  <div class="form-check">
                    <input
                      class="form-check-input"
                      type="checkbox"
                      [id]="'edit-team-' + team.id"
                      [checked]="user.assignedTeamIds.includes(team.id)"
                      (change)="toggleTeam(user, team.id)"
                    />
                    <label class="form-check-label" [for]="'edit-team-' + team.id">
                      {{ team.name }}
                      <span class="text-muted">({{ team.divisionName || 'No division' }})</span>
                    </label>
                  </div>
                }
              </div>

              <hr />

              <div class="form-check form-switch">
                <input
                  class="form-check-input"
                  type="checkbox"
                  id="edit-active"
                  [checked]="user.isActive"
                  (change)="toggleActive(user)"
                />
                <label class="form-check-label" for="edit-active">
                  Active — a deactivated administrator cannot sign in
                </label>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeEditModal()">
                Done
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class UsersAdminComponent implements OnInit {
  private userService = inject(UserService);
  private teamService = inject(TeamService);
  private competitionService = inject(CompetitionService);

  protected readonly Role = Role;
  protected readonly assignableRoles = [Role.TeamAdmin, Role.CompetitionAdmin, Role.SuperAdmin];

  users = signal<AdminUserDto[]>([]);
  teams = signal<TeamDto[]>([]);
  competitions = signal<CompetitionDto[]>([]);

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  createModalOpen = signal(false);
  editing = signal<AdminUserDetailDto | null>(null);

  form = this.emptyForm();

  /**
   * A role without its scope leaves somebody able to sign in and do nothing, so the create
   * button stays disabled until the scope that role needs has been chosen. A super admin
   * reaches everything and needs none. The API refuses these too — this just says so before
   * the form is submitted rather than after.
   */
  scopeChosen(): boolean {
    switch (this.form.role) {
      case Role.TeamAdmin:
        return this.form.teamIds.length > 0;
      case Role.CompetitionAdmin:
        return this.form.competitionIds.length > 0;
      case Role.SuperAdmin:
        return true;
      default:
        return false;
    }
  }

  ngOnInit() {
    this.loadUsers();
    this.loadTeams();
    this.loadCompetitions();
  }

  loadUsers() {
    this.loading.set(true);
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to load administrators'));
        this.loading.set(false);
      },
    });
  }

  loadTeams() {
    this.teamService.getAll().subscribe({
      next: (data) => this.teams.set(data),
      error: (err) => this.error.set(this.messageFrom(err, 'Failed to load teams')),
    });
  }

  loadCompetitions() {
    this.competitionService.getAll().subscribe({
      next: (data) => this.competitions.set(data),
      error: (err) => this.error.set(this.messageFrom(err, 'Failed to load competitions')),
    });
  }

  showCreateModal() {
    this.form = this.emptyForm();
    this.createModalOpen.set(true);
  }

  closeCreateModal() {
    this.createModalOpen.set(false);
    this.form = this.emptyForm();
  }

  onRoleChange() {
    // Switching role discards a scope that no longer applies, so a team admin cannot be
    // created carrying divisions the API would refuse.
    this.form.teamIds = [];
    this.form.competitionIds = [];
  }

  roleExplanation(): string {
    switch (this.form.role) {
      case Role.TeamAdmin:
        return 'Manages the players of the teams you assign, and can update those teams.';
      case Role.CompetitionAdmin:
        return 'Manages the teams and players inside the divisions you assign.';
      case Role.SuperAdmin:
        return 'Unrestricted. Can manage every division, team and player, and create other administrators.';
      default:
        return '';
    }
  }

  create() {
    if (!this.form.role) return;

    this.saving.set(true);
    this.error.set(null);

    const request: CreateAdminUserRequest = {
      email: this.form.email,
      password: this.form.password,
      firstName: this.form.firstName,
      lastName: this.form.lastName,
      roles: [this.form.role],
      assignedTeamIds: this.form.teamIds.length ? this.form.teamIds : undefined,
      assignedCompetitionIds: this.form.competitionIds.length ? this.form.competitionIds : undefined,
    };

    this.userService.create(request).subscribe({
      next: () => {
        this.success.set(`${this.form.firstName} ${this.form.lastName} can now sign in.`);
        this.saving.set(false);
        this.closeCreateModal();
        this.loadUsers();
      },
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to create the administrator'));
        this.saving.set(false);
      },
    });
  }

  showEditModal(user: AdminUserDto) {
    this.userService.getById(user.id).subscribe({
      next: (detail) => this.editing.set(detail),
      error: (err) => this.error.set(this.messageFrom(err, 'Failed to load that administrator')),
    });
  }

  closeEditModal() {
    this.editing.set(null);
    this.loadUsers();
  }

  toggleRole(user: AdminUserDetailDto, role: Role) {
    const id = this.idFor(role);
    const has = user.roleIds.includes(id);

    const call = has
      ? this.userService.removeRole(user.id, id)
      : this.userService.assignRole(user.id, id);

    call.subscribe({
      next: () => this.refreshEditing(user.id),
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to change that role'));
        this.refreshEditing(user.id);
      },
    });
  }

  toggleTeam(user: AdminUserDetailDto, teamId: string) {
    const has = user.assignedTeamIds.includes(teamId);

    const call = has
      ? this.userService.removeTeam(user.id, teamId)
      : this.userService.assignTeam(user.id, teamId);

    call.subscribe({
      next: () => this.refreshEditing(user.id),
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to change that team'));
        this.refreshEditing(user.id);
      },
    });
  }

  toggleCompetition(user: AdminUserDetailDto, competitionId: string) {
    const has = user.assignedCompetitionIds.includes(competitionId);

    const call = has
      ? this.userService.removeCompetition(user.id, competitionId)
      : this.userService.assignCompetition(user.id, competitionId);

    call.subscribe({
      next: () => this.refreshEditing(user.id),
      error: (err) => {
        this.error.set(this.messageFrom(err, 'Failed to change that competition'));
        this.refreshEditing(user.id);
      },
    });
  }

  toggleActive(user: AdminUserDetailDto) {
    this.userService
      .update({
        id: user.id,
        firstName: user.firstName,
        lastName: user.lastName,
        isActive: !user.isActive,
      })
      .subscribe({
        next: () => this.refreshEditing(user.id),
        error: (err) => {
          this.error.set(this.messageFrom(err, 'Failed to change their status'));
          this.refreshEditing(user.id);
        },
      });
  }

  /**
   * Each grant is its own request, so the screen re-reads the user after every change rather
   * than guessing. A refused change then shows as the checkbox going back rather than staying
   * ticked against a server that said no.
   */
  private refreshEditing(userId: string) {
    this.userService.getById(userId).subscribe({
      next: (detail) => this.editing.set(detail),
      error: () => this.editing.set(null),
    });
  }

  toggle(list: string[], id: string) {
    const at = list.indexOf(id);
    if (at === -1) list.push(id);
    else list.splice(at, 1);
  }

  idFor(role: Role): number {
    return ROLE_ID[role];
  }

  label(role: Role): string {
    return ROLE_LABEL[role];
  }

  roleLabelFor(roleId: number): string {
    const role = ROLE_BY_ID[roleId];
    return role ? ROLE_LABEL[role] : `Role ${roleId}`;
  }

  private messageFrom(err: any, fallback: string): string {
    return err?.error?.detail || err?.error?.title || err?.message || fallback;
  }

  private emptyForm() {
    return {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      role: null as Role | null,
      teamIds: [] as string[],
      competitionIds: [] as string[],
    };
  }
}
