import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DivisionService } from '../../../core/services/division.service';
import { DivisionDto, CreateDivisionCommand, UpdateDivisionCommand } from '../../../core/models';

@Component({
  selector: 'app-divisions-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Divisions Management</h1>
          <p class="text-muted">Manage league divisions, age groups, and seasons</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Add Division
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="card mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-3">
              <label class="form-label">Season</label>
              <input
                type="number"
                class="form-control"
                [(ngModel)]="filterSeason"
                (ngModelChange)="loadDivisions()"
                placeholder="e.g., 2025"
              />
            </div>
            <div class="col-md-3">
              <label class="form-label">Status</label>
              <select
                class="form-select"
                [(ngModel)]="filterActive"
                (ngModelChange)="loadDivisions()"
              >
                <option [ngValue]="undefined">All</option>
                <option [ngValue]="true">Active Only</option>
                <option [ngValue]="false">Inactive Only</option>
              </select>
            </div>
            <div class="col-md-6 d-flex align-items-end">
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

      <!-- Divisions Table -->
      @if (!loading() && divisions().length > 0) {
        <div class="card">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Name</th>
                  <th>Code</th>
                  <th>Season</th>
                  <th>Age Group</th>
                  <th>Gender</th>
                  <th>Teams</th>
                  <th>Matches</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (division of divisions(); track division.id) {
                  <tr>
                    <td class="fw-semibold">{{ division.name }}</td>
                    <td>
                      <span class="badge bg-secondary">{{ division.shortCode }}</span>
                    </td>
                    <td>{{ division.season }}</td>
                    <td>{{ division.ageGroup || '-' }}</td>
                    <td>{{ division.gender || '-' }}</td>
                    <td>{{ division.teamCount }}</td>
                    <td>{{ division.matchCount }}</td>
                    <td>
                      <span
                        class="badge"
                        [class.bg-success]="division.isActive"
                        [class.bg-secondary]="!division.isActive"
                      >
                        {{ division.isActive ? 'Active' : 'Inactive' }}
                      </span>
                    </td>
                    <td>
                      <button
                        class="btn btn-sm btn-outline-primary me-2"
                        (click)="showEditModal(division)"
                        title="Edit"
                      >
                        <i class="bi bi-pencil"></i>
                      </button>
                      <button
                        class="btn btn-sm btn-outline-danger"
                        (click)="confirmDelete(division)"
                        [disabled]="division.teamCount > 0 || division.matchCount > 0"
                        [title]="
                          division.teamCount > 0 || division.matchCount > 0
                            ? 'Cannot delete: has teams or matches'
                            : 'Delete'
                        "
                      >
                        <i class="bi bi-trash"></i>
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && divisions().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-inbox display-1 text-muted"></i>
            <h3 class="mt-3">No Divisions Found</h3>
            <p class="text-muted">Create your first division to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Add Division
            </button>
          </div>
        </div>
      }

      <!-- Error Alert -->
      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
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
        <div class="alert alert-success alert-dismissible fade show" role="alert">
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
                {{ editingDivision() ? 'Edit Division' : 'Add Division' }}
              </h5>
              <button
                type="button"
                class="btn-close"
                (click)="closeModal()"
              ></button>
            </div>
            <div class="modal-body">
              <form #divisionForm="ngForm">
                <div class="row g-3">
                  <div class="col-md-8">
                    <label class="form-label">Division Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.name"
                      name="name"
                      required
                      placeholder="e.g., U15 Boys League"
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
                      maxlength="20"
                      placeholder="e.g., U15B"
                    />
                  </div>

                  <div class="col-md-4">
                    <label class="form-label">Season *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="formData.season"
                      name="season"
                      required
                      placeholder="2025"
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Age Group</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.ageGroup"
                      name="ageGroup"
                      placeholder="e.g., U15, U17, Senior"
                    />
                  </div>
                  <div class="col-md-4">
                    <label class="form-label">Gender</label>
                    <select class="form-select" [(ngModel)]="formData.gender" name="gender">
                      <option value="">Not specified</option>
                      <option value="Male">Male</option>
                      <option value="Female">Female</option>
                      <option value="Mixed">Mixed</option>
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Start Date</label>
                    <input
                      type="date"
                      class="form-control"
                      [(ngModel)]="formData.startDate"
                      name="startDate"
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">End Date</label>
                    <input
                      type="date"
                      class="form-control"
                      [(ngModel)]="formData.endDate"
                      name="endDate"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Description</label>
                    <textarea
                      class="form-control"
                      [(ngModel)]="formData.description"
                      name="description"
                      rows="3"
                      placeholder="Optional description of the division"
                    ></textarea>
                  </div>

                  @if (editingDivision()) {
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
                          Active Division
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
                (click)="saveDivision()"
                [disabled]="divisionForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingDivision() ? 'Update' : 'Create' }}
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
      .bi {
        font-size: 1rem;
      }
    `,
  ],
})
export class DivisionsAdminComponent implements OnInit {
  private divisionService = inject(DivisionService);

  divisions = signal<DivisionDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  editingDivision = signal<DivisionDto | null>(null);

  filterSeason: number | undefined;
  filterActive: boolean | undefined;

  formData: any = this.getEmptyForm();

  ngOnInit() {
    this.loadDivisions();
  }

  loadDivisions() {
    this.loading.set(true);
    this.error.set(null);

    this.divisionService.getAll(this.filterSeason, this.filterActive).subscribe({
      next: (data) => {
        this.divisions.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load divisions: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  clearFilters() {
    this.filterSeason = undefined;
    this.filterActive = undefined;
    this.loadDivisions();
  }

  showAddModal() {
    this.editingDivision.set(null);
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  showEditModal(division: DivisionDto) {
    this.editingDivision.set(division);
    this.formData = {
      name: division.name,
      shortCode: division.shortCode,
      season: division.season,
      ageGroup: division.ageGroup || '',
      gender: division.gender || '',
      startDate: division.startDate || '',
      endDate: division.endDate || '',
      description: division.description || '',
      isActive: division.isActive,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingDivision.set(null);
    this.formData = this.getEmptyForm();
  }

  saveDivision() {
    this.saving.set(true);
    this.error.set(null);

    const command = {
      name: this.formData.name,
      shortCode: this.formData.shortCode,
      season: this.formData.season,
      ageGroup: this.formData.ageGroup || undefined,
      gender: this.formData.gender || undefined,
      startDate: this.formData.startDate || undefined,
      endDate: this.formData.endDate || undefined,
      description: this.formData.description || undefined,
    };

    if (this.editingDivision()) {
      this.divisionService.update({
        ...command,
        id: this.editingDivision()!.id,
        isActive: this.formData.isActive,
      } as UpdateDivisionCommand).subscribe({
        next: () => {
          this.success.set('Division updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadDivisions();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save division: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      this.divisionService.create(command as CreateDivisionCommand).subscribe({
        next: () => {
          this.success.set('Division created successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadDivisions();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save division: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  confirmDelete(division: DivisionDto) {
    if (
      !confirm(
        `Are you sure you want to delete "${division.name}"? This action cannot be undone.`
      )
    ) {
      return;
    }

    this.divisionService.delete(division.id).subscribe({
      next: () => {
        this.success.set('Division deleted successfully');
        this.loadDivisions();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to delete division: ${err.error?.message || err.message}`);
      },
    });
  }

  private getEmptyForm() {
    return {
      name: '',
      shortCode: '',
      season: new Date().getFullYear(),
      ageGroup: '',
      gender: '',
      startDate: '',
      endDate: '',
      description: '',
      isActive: true,
    };
  }
}
