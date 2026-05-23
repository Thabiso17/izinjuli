import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface SponsorDto {
  id: string;
  name: string;
  logoUrl?: string;
  websiteUrl?: string;
  adImageUrl?: string;
  adLinkUrl?: string;
  tier: string;
  placement: string;
  isActive: boolean;
  contractStart?: string;
  contractEnd?: string;
  displayOrder: number;
}

@Component({
  selector: 'app-sponsors-section',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="card shadow-sm mb-4">
      <div class="card-body">
        <h2 class="card-title h4 mb-4">
          <i class="bi bi-star-fill me-2"></i>Our Sponsors
        </h2>

        @if (loading()) {
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
          </div>
        }

        @if (!loading() && sponsors().length > 0) {
          <div class="row g-3">
            @for (sponsor of sponsors(); track sponsor.id) {
              <div class="col-6 col-md-4 col-lg-3">
                <a
                  [href]="sponsor.websiteUrl || '#'"
                  [target]="sponsor.websiteUrl ? '_blank' : '_self'"
                  [class.pe-none]="!sponsor.websiteUrl"
                  class="text-decoration-none"
                >
                  <div class="sponsor-card bg-light rounded p-3 d-flex align-items-center justify-content-center">
                    @if (sponsor.logoUrl) {
                      <img
                        [src]="sponsor.logoUrl"
                        [alt]="sponsor.name"
                        class="sponsor-logo"
                        [title]="sponsor.name"
                      />
                    } @else {
                      <span class="text-muted">{{ sponsor.name }}</span>
                    }
                  </div>
                </a>
              </div>
            }
          </div>
        }

        @if (!loading() && sponsors().length === 0) {
          <div class="text-center py-4 text-muted">
            <i class="bi bi-star display-4"></i>
            <p class="mt-2">No sponsors available</p>
          </div>
        }
      </div>
    </section>
  `,
  styles: [`
    .sponsor-card {
      min-height: 100px;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    a:not(.pe-none) .sponsor-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 0.25rem 0.5rem rgba(0, 0, 0, 0.1);
    }

    .sponsor-logo {
      max-width: 100%;
      max-height: 80px;
      object-fit: contain;
    }
  `],
})
export class SponsorsSectionComponent implements OnInit {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiBaseUrl}/sponsors`;

  sponsors = signal<SponsorDto[]>([]);
  loading = signal(false);

  ngOnInit() {
    this.loadSponsors();
  }

  loadSponsors() {
    this.loading.set(true);

    // Fetch sponsors for Homepage placement
    this.http.get<SponsorDto[]>(`${this.baseUrl}?placement=Homepage`).subscribe({
      next: (data) => {
        this.sponsors.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load sponsors:', err);
        this.loading.set(false);
      },
    });
  }
}
