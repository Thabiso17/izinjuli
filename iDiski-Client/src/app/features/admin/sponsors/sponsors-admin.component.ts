import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { UploadService } from '../../../core/services/upload.service';

interface SponsorDto {
  id: string;
  name: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  tier: string;
  placement: string;
  displayOrder: number;
  isActive: boolean;
}

interface CreateSponsorCommand {
  name: string;
  logoUrl?: string;
  websiteUrl?: string;
  tier: string;
  placement: string;
  displayOrder: number;
}

interface UpdateSponsorCommand extends CreateSponsorCommand {
  id: string;
  isActive: boolean;
}

@Component({
  selector: 'app-sponsors-admin',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container-fluid py-4">
      <div class="row mb-4">
        <div class="col">
          <h1 class="display-6">Sponsors Management</h1>
          <p class="text-muted">Manage sponsor logos and ad placements</p>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" (click)="showAddModal()">
            <i class="bi bi-plus-circle"></i> Add Sponsor
          </button>
        </div>
      </div>

      <!-- Loading State -->
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      <!-- Sponsors Grid -->
      @if (!loading() && sponsors().length > 0) {
        <div class="row g-4">
          @for (sponsor of sponsors(); track sponsor.id) {
            <div class="col-md-6 col-lg-4 col-xl-3">
              <div class="card h-100">
                <div class="card-body">
                  <div class="text-center mb-3">
                    @if (sponsor.logoUrl) {
                      <img
                        [src]="sponsor.logoUrl"
                        [alt]="sponsor.name"
                        class="sponsor-logo"
                      />
                    } @else {
                      <div class="sponsor-logo-placeholder">
                        <i class="bi bi-image"></i>
                      </div>
                    }
                  </div>

                  <h6 class="card-title text-center mb-3">{{ sponsor.name }}</h6>

                  <div class="mb-2">
                    <small class="text-muted">Tier:</small>
                    <span class="badge bg-primary ms-2">{{ sponsor.tier }}</span>
                  </div>

                  <div class="mb-2">
                    <small class="text-muted">Placement:</small>
                    <span class="badge bg-secondary ms-2">{{ sponsor.placement }}</span>
                  </div>

                  <div class="mb-2">
                    <small class="text-muted">Display Order:</small>
                    <strong class="ms-2">{{ sponsor.displayOrder }}</strong>
                  </div>

                  <div class="mb-3">
                    <small class="text-muted">Status:</small>
                    @if (sponsor.isActive) {
                      <span class="badge bg-success ms-2">Active</span>
                    } @else {
                      <span class="badge bg-warning ms-2">Inactive</span>
                    }
                  </div>

                  @if (sponsor.websiteUrl) {
                    <div class="mb-3">
                      <a
                        [href]="sponsor.websiteUrl"
                        target="_blank"
                        class="btn btn-sm btn-outline-primary w-100"
                      >
                        <i class="bi bi-link-45deg"></i> Visit Website
                      </a>
                    </div>
                  }

                  <div class="d-flex gap-2">
                    <button
                      class="btn btn-sm btn-outline-primary flex-grow-1"
                      (click)="showEditModal(sponsor)"
                    >
                      <i class="bi bi-pencil"></i> Edit
                    </button>
                    <button
                      class="btn btn-sm btn-outline-danger"
                      (click)="confirmDelete(sponsor)"
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
      @if (!loading() && sponsors().length === 0) {
        <div class="card">
          <div class="card-body text-center py-5">
            <i class="bi bi-tag display-1 text-muted"></i>
            <h3 class="mt-3">No Sponsors Found</h3>
            <p class="text-muted">Add your first sponsor to get started</p>
            <button class="btn btn-primary" (click)="showAddModal()">
              <i class="bi bi-plus-circle"></i> Add Sponsor
            </button>
          </div>
        </div>
      }

      <!-- Alerts -->
      @if (error()) {
        <div class="alert alert-danger alert-dismissible fade show mt-3" role="alert">
          {{ error() }}
          <button type="button" class="btn-close" (click)="error.set(null)"></button>
        </div>
      }

      @if (success()) {
        <div class="alert alert-success alert-dismissible fade show mt-3" role="alert">
          {{ success() }}
          <button type="button" class="btn-close" (click)="success.set(null)"></button>
        </div>
      }
    </div>

    <!-- Add/Edit Modal -->
    @if (showModal()) {
      <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">
                {{ editingSponsor() ? 'Edit Sponsor' : 'Add Sponsor' }}
              </h5>
              <button type="button" class="btn-close" (click)="closeModal()"></button>
            </div>
            <div class="modal-body">
              <form #sponsorForm="ngForm">
                <div class="row g-3">
                  <div class="col-12">
                    <label class="form-label">Sponsor Name *</label>
                    <input
                      type="text"
                      class="form-control"
                      [(ngModel)]="formData.name"
                      name="name"
                      required
                      maxlength="100"
                      placeholder="e.g., Nike"
                    />
                  </div>

                  <div class="col-12">
                    <label class="form-label">Sponsor Logo *</label>

                    @if (formData.logoUrl) {
                      <div class="mb-2">
                        <img
                          [src]="formData.logoUrl"
                          alt="Logo Preview"
                          class="img-thumbnail bg-light"
                          style="max-width: 150px; max-height: 80px; object-fit: contain"
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
                        (change)="onSponsorLogoSelected($event)"
                        #sponsorLogoInput
                      />
                      <button
                        type="button"
                        class="btn btn-outline-secondary"
                        (click)="sponsorLogoInput.value = ''; formData.logoUrl = ''"
                      >
                        Clear
                      </button>
                    </div>
                    <small class="text-muted">Upload sponsor logo (JPG, PNG, SVG, max 5MB)</small>

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

                  <div class="col-12">
                    <label class="form-label">Website URL</label>
                    <input
                      type="url"
                      class="form-control"
                      [(ngModel)]="formData.websiteUrl"
                      name="websiteUrl"
                      placeholder="https://example.com"
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Tier *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.tier"
                      name="tier"
                      required
                    >
                      <option value="">Select Tier</option>
                      <option value="Title">Title</option>
                      <option value="Gold">Gold</option>
                      <option value="Silver">Silver</option>
                      <option value="Bronze">Bronze</option>
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Placement *</label>
                    <select
                      class="form-select"
                      [(ngModel)]="formData.placement"
                      name="placement"
                      required
                    >
                      <option value="">Select Placement</option>
                      <option value="Header">Header</option>
                      <option value="Sidebar">Sidebar</option>
                      <option value="Footer">Footer</option>
                      <option value="MatchDay">Match Day</option>
                      <option value="Homepage">Homepage</option>
                      <option value="NewsPage">News Page</option>
                    </select>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label">Display Order *</label>
                    <input
                      type="number"
                      class="form-control"
                      [(ngModel)]="formData.displayOrder"
                      name="displayOrder"
                      required
                      min="0"
                      placeholder="0"
                    />
                    <small class="text-muted">Lower numbers appear first</small>
                  </div>

                  @if (editingSponsor()) {
                    <div class="col-md-6">
                      <label class="form-label">Status</label>
                      <div class="form-check form-switch mt-2">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          [(ngModel)]="formData.isActive"
                          name="isActive"
                          id="isActive"
                        />
                        <label class="form-check-label" for="isActive">
                          Active
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
                (click)="saveSponsor()"
                [disabled]="sponsorForm.invalid || saving()"
              >
                @if (saving()) {
                  <span class="spinner-border spinner-border-sm me-2"></span>
                }
                {{ editingSponsor() ? 'Update' : 'Create' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal.show { display: block; }

    .sponsor-logo {
      max-width: 150px;
      max-height: 100px;
      object-fit: contain;
    }

    .sponsor-logo-placeholder {
      width: 150px;
      height: 100px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border-radius: 8px;
    }

    .sponsor-logo-placeholder i {
      font-size: 3rem;
      color: #adb5bd;
    }
  `],
})
export class SponsorsAdminComponent implements OnInit {
  private http = inject(HttpClient);
  private uploadService = inject(UploadService);
  private readonly baseUrl = `${environment.apiBaseUrl}/sponsors`;

  sponsors = signal<SponsorDto[]>([]);
  loading = signal(false);
  saving = signal(false);
  uploadingImage = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  showModal = signal(false);
  editingSponsor = signal<SponsorDto | null>(null);

  formData: any = this.getEmptyForm();

  ngOnInit() {
    this.loadSponsors();
  }

  loadSponsors() {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<SponsorDto[]>(`${this.baseUrl}/all`).subscribe({
      next: (data) => {
        this.sponsors.set(data.sort((a, b) => a.displayOrder - b.displayOrder));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load sponsors: ' + err.message);
        this.loading.set(false);
      },
    });
  }

  showAddModal() {
    this.editingSponsor.set(null);
    this.formData = this.getEmptyForm();
    this.showModal.set(true);
  }

  showEditModal(sponsor: SponsorDto) {
    this.editingSponsor.set(sponsor);
    this.formData = {
      name: sponsor.name,
      logoUrl: sponsor.logoUrl || '',
      websiteUrl: sponsor.websiteUrl || '',
      tier: sponsor.tier,
      placement: sponsor.placement,
      displayOrder: sponsor.displayOrder,
      isActive: sponsor.isActive,
    };
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingSponsor.set(null);
    this.formData = this.getEmptyForm();
  }

  saveSponsor() {
    this.saving.set(true);
    this.error.set(null);

    const command = {
      name: this.formData.name,
      logoUrl: this.formData.logoUrl || null,
      websiteUrl: this.formData.websiteUrl || null,
      adImageUrl: this.formData.adImageUrl || null,
      adLinkUrl: this.formData.adLinkUrl || null,
      tier: this.formData.tier,
      placement: this.formData.placement,
      contractStart: this.formData.contractStart || null,
      contractEnd: this.formData.contractEnd || null,
      displayOrder: this.formData.displayOrder,
    };

    if (this.editingSponsor()) {
      this.http.put<void>(`${this.baseUrl}/${this.editingSponsor()!.id}`, {
        ...command,
        id: this.editingSponsor()!.id,
        isActive: this.formData.isActive,
      }).subscribe({
        next: () => {
          this.success.set('Sponsor updated successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadSponsors();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save sponsor: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    } else {
      this.http.post<string>(this.baseUrl, command).subscribe({
        next: () => {
          this.success.set('Sponsor created successfully');
          this.saving.set(false);
          this.closeModal();
          this.loadSponsors();
          setTimeout(() => this.success.set(null), 3000);
        },
        error: (err: any) => {
          this.error.set(`Failed to save sponsor: ${err.error?.message || err.message}`);
          this.saving.set(false);
        },
      });
    }
  }

  onSponsorLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.uploadingImage.set(true);
    this.error.set(null);

    this.uploadService.uploadImage(file, 'sponsors').subscribe({
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

  confirmDelete(sponsor: SponsorDto) {
    if (!confirm(`Are you sure you want to delete "${sponsor.name}"? This action cannot be undone.`)) {
      return;
    }

    this.http.delete<void>(`${this.baseUrl}/${sponsor.id}`).subscribe({
      next: () => {
        this.success.set('Sponsor deleted successfully');
        this.loadSponsors();
        setTimeout(() => this.success.set(null), 3000);
      },
      error: (err) => {
        this.error.set(`Failed to delete sponsor: ${err.error?.message || err.message}`);
      },
    });
  }

  private getEmptyForm() {
    return {
      name: '',
      logoUrl: '',
      websiteUrl: '',
      tier: '',
      placement: '',
      displayOrder: 0,
      isActive: true,
    };
  }
}
