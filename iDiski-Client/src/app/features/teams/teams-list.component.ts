import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TeamService, DivisionService } from '../../core/services';
import { TeamDto, DivisionDto } from '../../core/models';

@Component({
  selector: 'app-teams-list',
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="container py-5">
      <!-- Header -->
      <div class="text-center mb-5">
        <h1 class="display-4 fw-bold">Teams</h1>
        <p class="lead text-muted">Browse all football clubs in the league</p>
      </div>

      <!-- Division Filter -->
      <div class="row mb-4">
        <div class="col-md-4 mx-auto">
          <select
            class="form-select form-select-lg"
            [(ngModel)]="selectedDivisionId"
            (ngModelChange)="filterTeams()"
          >
            <option [ngValue]="null">All Divisions</option>
            @for (division of divisions(); track division.id) {
              <option [ngValue]="division.id">
                {{ division.name }} ({{ division.season }})
              </option>
            }
          </select>
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
      @if (!loading() && filteredTeams().length > 0) {
        <div class="row g-4">
          @for (team of filteredTeams(); track team.id) {
            <div class="col-md-6 col-lg-4 col-xl-3">
              <a
                [routerLink]="['/teams', team.id]"
                class="text-decoration-none"
              >
                <div class="card h-100 shadow-sm team-card">
                  <div class="card-body text-center">
                    <!-- Team Logo -->
                    <div class="mb-3">
                      @if (team.logoUrl) {
                        <img
                          [src]="team.logoUrl"
                          [alt]="team.name"
                          class="team-logo"
                        />
                      } @else {
                        <div class="team-logo-placeholder">
                          <i class="bi bi-shield-fill"></i>
                        </div>
                      }
                    </div>

                    <!-- Team Name -->
                    <h5 class="card-title mb-1 text-dark">{{ team.name }}</h5>
                    <p class="text-muted mb-3">
                      <span class="badge bg-secondary">{{ team.shortCode }}</span>
                    </p>

                    <!-- Team Colors -->
                    @if (team.primaryColour || team.secondaryColour) {
                      <div class="d-flex justify-content-center gap-2 mb-3">
                        @if (team.primaryColour) {
                          <div
                            class="color-swatch"
                            [style.background-color]="team.primaryColour"
                            [title]="'Primary: ' + team.primaryColour"
                          ></div>
                        }
                        @if (team.secondaryColour) {
                          <div
                            class="color-swatch"
                            [style.background-color]="team.secondaryColour"
                            [title]="'Secondary: ' + team.secondaryColour"
                          ></div>
                        }
                      </div>
                    }

                    <!-- Team Info -->
                    <div class="team-info">
                      @if (team.city) {
                        <div class="mb-2">
                          <i class="bi bi-geo-alt text-muted me-2"></i>
                          <small class="text-muted">{{ team.city }}</small>
                        </div>
                      }
                      @if (team.divisionName) {
                        <div class="mb-2">
                          <i class="bi bi-trophy text-muted me-2"></i>
                          <small class="text-muted">{{ team.divisionName }}</small>
                        </div>
                      }
                      <div>
                        <i class="bi bi-people text-muted me-2"></i>
                        <small class="text-muted">{{ team.playerCount }} players</small>
                      </div>
                    </div>
                  </div>

                  <!-- Card Footer -->
                  <div class="card-footer bg-transparent border-top-0">
                    <div class="text-center">
                      <small class="text-primary fw-semibold">
                        View Details <i class="bi bi-arrow-right"></i>
                      </small>
                    </div>
                  </div>
                </div>
              </a>
            </div>
          }
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && filteredTeams().length === 0) {
        <div class="text-center py-5">
          <i class="bi bi-shield display-1 text-muted"></i>
          <h3 class="mt-3">No Teams Found</h3>
          <p class="text-muted">
            {{ selectedDivisionId ? 'Try selecting a different division' : 'Check back later for teams' }}
          </p>
        </div>
      }
    </div>
  `,
  styles: [`
    .team-card {
      transition: transform 0.2s, box-shadow 0.2s;
      cursor: pointer;
    }

    .team-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    }

    .team-logo {
      width: 100px;
      height: 100px;
      object-fit: contain;
    }

    .team-logo-placeholder {
      width: 100px;
      height: 100px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border-radius: 50%;
    }

    .team-logo-placeholder i {
      font-size: 3rem;
      color: #adb5bd;
    }

    .color-swatch {
      width: 30px;
      height: 30px;
      border-radius: 50%;
      border: 2px solid #dee2e6;
    }

    .team-info {
      font-size: 0.875rem;
    }

    .card-footer {
      padding: 0.75rem 1rem;
    }
  `],
})
export class TeamsListComponent implements OnInit {
  private teamService = inject(TeamService);
  private divisionService = inject(DivisionService);

  teams = signal<TeamDto[]>([]);
  divisions = signal<DivisionDto[]>([]);
  filteredTeams = signal<TeamDto[]>([]);
  loading = signal(false);
  selectedDivisionId: string | null = null;

  ngOnInit() {
    this.loadDivisions();
    this.loadTeams();
  }

  loadTeams() {
    this.loading.set(true);
    this.teamService.getAll().subscribe({
      next: (data) => {
        this.teams.set(data);
        this.filterTeams();
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load teams:', err);
        this.loading.set(false);
      },
    });
  }

  loadDivisions() {
    this.divisionService.getAll(undefined, true).subscribe({
      next: (data) => this.divisions.set(data),
      error: (err) => console.error('Failed to load divisions:', err),
    });
  }

  filterTeams() {
    if (!this.selectedDivisionId) {
      this.filteredTeams.set(this.teams());
    } else {
      this.filteredTeams.set(
        this.teams().filter(t => t.divisionId === this.selectedDivisionId)
      );
    }
  }
}
