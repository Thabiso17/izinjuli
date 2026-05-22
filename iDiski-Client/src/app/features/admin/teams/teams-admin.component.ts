import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamService } from '../../../core/services/team.service';
import { DivisionService } from '../../../core/services/division.service';
import { UploadService } from '../../../core/services/upload.service';
import { TeamDto, CreateTeamRequest, UpdateTeamRequest, DivisionDto } from '../../../core/models';

@Component({
  selector: 'app-teams-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Teams Management</h1>
          <p class="text-muted">Manage football teams and their details</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Add Team
          </button>
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

      <!-- Teams Grid -->
      @if (!loading() && teams().length > 0) {
        <div class="row g-4">
          @for (team of teams(); track team.id) {
            <div class="col-md-6 col-lg-4">
              <div class="card h-100 shadow-sm">
                <div class="card-body">
                  <div class="d-flex align-items-start mb-3">
                    @if (team.logoUrl) {
                      <img
                        [src]="team.logoUrl"
                        [alt]="team.name"
                        class="rounded me-3"
                        style="width: 60px; height: 60px; object-fit: contain"
                      />
                    } @else {
                      <div
                        class="rounded me-3 d-flex align-items-center justify-content-center"
                        style="
                          width: 60px;
                          height: 60px;
                          background-color: #e9ecef;
                        "
                      >
                        <i class="bi bi-shield text-muted" style="font-size: 2rem"></i>
                      </div>
                    }
                    <div class="flex-grow-1">
                      <h5 class="card-title mb-1">{{ team.name }}</h5>
                      <span class="badge bg-secondary">{{ team.shortCode }}</span>
                    </div>
                  </div>

                  <div class="mb-3">
                    @if (team.divisionName) {
                      <div class="mb-2">
                        <i class="bi bi-trophy text-muted me-2"></i>
                        <span class="text-muted">{{ team.divisionName }}</span>
                      </div>
                    }
                    @if (team.city) {
                      <div class="mb-2">
                        <i class="bi bi-geo-alt text-muted me-2"></i>
                        <span class="text-muted">{{ team.city }}</span>
                      </div>
                    }
                    @if (team.homeGround) {
                      <div class="mb-2">
                        <i class="bi bi-building text-muted me-2"></i>
                        <span class="text-muted">{{ team.homeGround }}</span>
                      </div>
                    }
                    <div class="mb-2">
                      <i class="bi bi-calendar text-muted me-2"></i>
                      <span class="text-muted">Founded {{ team.founded }}</span>
                    </div>
                    <div>
                      <i class="bi bi-people text-muted me-2"></i>
                      <span class="text-muted">{{ team.playerCount }} players</span>
                    </div>
                  </div>

                  @if (team.primaryColour || team.secondaryColour) {
                    <div class="mb-3">
                      <small class="text-muted">Team Colors:</small>
                      <div class="d-flex gap-2 mt-1">
                        @if (team.primaryColour) {
                          <div
                            class="border rounded"
                            [style.background-color]="team.primaryColour"
                            style="width: 30px; height: 30px"
                            [title]="'Primary: ' + team.primaryColour"
                          ></div>
                        }
                        @if (team.secondaryColour) {
                          <div
                            class="border rounded"
                            [style.background-color]="team.secondaryColour"
                            style="width: 30px; height: 30px"
                            [title]="'Secondary: ' + team.secondaryColour"
                          ></div>
                        }
                      </div>
                    </div>
                  }

                  <div class="d-flex gap-2">
                    <button
                      class="btn btn-sm btn-outline-primary flex-grow-1"
                      (click)="showEditModal(team)"
                    >
                      <i class="bi bi-pencil"></i> Edit
                    </button>
                    <button
                      class="btn btn-sm btn-outline-danger"
                      (click)="confirmDelete(team)"
                      [disabled]="team.playerCount > 0"
                      [title]="
                        team.playerCount > 0
                          ? 'Cannot delete: has players'
                          : 'Delete team'
                      "
                    >
                      <i class="bi bi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && teams().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-shield display-1 text-muted"></i>
            <h3 class="mt-3">No Teams Found</h3>
            <p class="text-muted">Add your first team to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Add Team
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
                {{ editingTeam() ? 'Edit Team' : 'Add Team' }}
              </h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #teamForm="ngForm">
                <div class="row g-3">
                  <div class="col-md-8">
                    <label class="form-label">Team Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.name"
                      name="name"
                      required
                      placeholder="e.g., Orlando Pirates"
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Short Code *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.shortCode"
                      name="shortCode"
                      required
                      maxlength="10"
                      placeholder="e.g., ORL"
                      [disabled]="!!editingTeam()"
                    />
                    @if (editingTeam()) {
                      <small class="text-muted">Short code cannot be changed</small>
                    }
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Division</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.divisionId"
                      name="divisionId"
                    >
                      <option [ngValue]="null">No Division</option>
                      @for (division of divisions(); track division.id) {
                        <option [ngValue]="division.id">
                          {{ division.name }} ({{ division.season }})
                        </option>
                      }
                    </select>
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Founded Year *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="formData.founded"
                      name="founded"
                      required
                      min="1800"
                      [max]="currentYear"
                      placeholder="e.g., 1937"
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">City</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.city"
                      name="city"
                      placeholder="e.g., Johannesburg"
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Home Ground</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.homeGround"
                      name="homeGround"
                      placeholder="e.g., Orlando Stadium"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Team Logo</label>

                    @if (formData.logoUrl) {
                      <div class="mb-2">
                        <img
                          [src]="formData.logoUrl"
                          alt="Logo Preview"
                          class="img-thumbnail"
                          style="max-width: 100px; max-height: 100px; object-fit: contain"
                        />
                        <button
                          type="button"
                          class="btn btn-sm btn-danger ms-2"
                          (click)="formData.logoUrl = ''"
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
                        (change)="onLogoSelected($event)"
                        #logoInput
                      />
                      <button
                        type="button"
                        class="btn btn-outline-secondary"
                        (click)="logoInput.value = ''; formData.logoUrl = ''"
                      >
                        Clear
                      </button>
                    </div>
                    <small class="text-muted">Upload team logo (JPG, PNG, SVG, max 5MB)</small>

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
                        [(ngModel)]="formData.logoUrl"
                        name="logoUrl"
                        placeholder="https://example.com/logo.png"
                      />
                    </div>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Primary Color</label>
                    <div class="input-group">
                      <input
                        type="color"
                        class="form-control form-control-color"
                        [(ngModel)]="formData.primaryColour"
                        name="primaryColour"
                      />
                      <input
                        type="text"
                        class="form-control"
                        [(ngModel)]="formData.primaryColour"
                        name="primaryColourText"
                        placeholder="#000000"
                      />
                    </div>
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Secondary Color</label>
                    <div class="input-group">
                      <input
                        type="color"
                        class="form-control form-control-color"
                        [(ngModel)]="formData.secondaryColour"
                        name="secondaryColour"
                      />
                      <input
                        type="text"
                        class="form-control"
                        [(ngModel)]="formData.secondaryColour"
                        name="secondaryColourText"
                        placeholder="#FFFFFF"
                      />
                    </div>
                  </div>
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
                (click)="saveTeam()"
                [disabled]="teamForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingTeam() ? 'Update' : 'Create' }}
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
export class TeamsAdminComponent implements OnInit {
  private teamService = inject(TeamService);
  private divisionService = inject(DivisionService);
  private uploadService = inject(UploadService);

  teams = signal<TeamDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  uploadingImage = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  editingTeam = signal<TeamDto | null>(null);

  currentYear = new Date().getFullYear();

  formData: any = this.getEmptyForm();

  ngOnInit() {
    this.loadTeams();
    this.loadDivisions();
  }

  loadTeams() {
    this.loading.set(true);
    this.error.set(null);

    this.teamService.getAll().subscribe({
      next: (data) => {
        this.teams.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load teams: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  loadDivisions() {
    // Load all divisions (don't filter by isActive) for admin to have full control
    this.divisionService.getAll().subscribe({
      next: (data) => {
        // Sort by season (newest first) and then by name
        const sorted = data.sort((a, b) => {
          if (b.season !== a.season) return b.season - a.season;
          return a.name.localeCompare(b.name);
        });
        this.divisions.set(sorted);
      },
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  showAddModal() {
    this.editingTeam.set(null);
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  showEditModal(team: TeamDto) {
    this.editingTeam.set(team);
    this.formData = {
      name: team.name,
      shortCode: team.shortCode,
      logoUrl: team.logoUrl || '',
      founded: team.founded,
      homeGround: team.homeGround || '',
      city: team.city || '',
      primaryColour: team.primaryColour || '#000000',
      secondaryColour: team.secondaryColour || '#FFFFFF',
      divisionId: team.divisionId || null,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingTeam.set(null);
    this.formData = this.getEmptyForm();
  }

  saveTeam() {
    this.saving.set(true);
    this.error.set(null);

    const command = {
      name: this.formData.name,
      shortCode: this.formData.shortCode,
      founded: this.formData.founded,
      logoUrl: this.formData.logoUrl || undefined,
      homeGround: this.formData.homeGround || undefined,
      city: this.formData.city || undefined,
      primaryColour: this.formData.primaryColour || undefined,
      secondaryColour: this.formData.secondaryColour || undefined,
      divisionId: this.formData.divisionId || undefined,
    };

    if (this.editingTeam()) {
      this.teamService.update(this.editingTeam()!.id, {
        ...command,
        id: this.editingTeam()!.id,
      } as UpdateTeamRequest).subscribe({
        next: () => {
          this.success.set('Team updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadTeams();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save team: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      this.teamService.create(command as CreateTeamRequest).subscribe({
        next: () => {
          this.success.set('Team created successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadTeams();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save team: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  onLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.uploadingImage.set(true);
    this.error.set(null);

    this.uploadService.uploadImage(file, 'teams').subscribe({
      next: (response) => {
        this.formData.logoUrl = response.fileUrl;
        this.uploadingImage.set(false);
        this.success.set('Logo uploaded successfully');
        setTimeout(() => this.success.set(null), 2000);
      },
      error: (err) => {
        this.error.set(`Failed to upload logo: ${err.error?.message || err.message}`);
        this.uploadingImage.set(false);
      },
    });
  }

  confirmDelete(team: TeamDto) {
    if (
      !confirm(
        `Are you sure you want to delete "${team.name}"? This action cannot be undone.`
      )
    ) {
      return;
    }

    this.teamService.delete(team.id).subscribe({
      next: () => {
        this.success.set('Team deleted successfully');
        this.loadTeams();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to delete team: ${err.error?.message || err.message}`);
      },
    });
  }

  private getEmptyForm() {
    return {
      name: '',
      shortCode: '',
      logoUrl: '',
      founded: this.currentYear - 10,
      homeGround: '',
      city: '',
      primaryColour: '#000000',
      secondaryColour: '#FFFFFF',
      divisionId: null,
    };
  }
}
