import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LeagueService } from '../../../core/services/league.service';
import { DivisionService } from '../../../core/services';
import { StandingDto } from '../../../core/models';

@Component({
  selector: 'app-standings-section',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="card shadow-sm mb-4">
      <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h2 class="card-title h4 mb-0">
            <i class="bi bi-trophy me-2"></i>League Standings
          </h2>
          @if (divisionName()) {
            <small class="text-muted">{{ divisionName() }}</small>
          }
        </div>

        @if (loading()) {
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
          </div>
        }

        @if (!loading() && standings().length > 0) {
          <div class="table-responsive">
            <table class="table table-sm table-hover standings-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Team</th>
                  <th class="text-center">P</th>
                  <th class="text-center">W</th>
                  <th class="text-center">D</th>
                  <th class="text-center">L</th>
                  <th class="text-center fw-bold">Pts</th>
                </tr>
              </thead>
              <tbody>
                @for (team of standings().slice(0, 6); track team.teamId) {
                  <tr>
                    <td>{{ team.position }}</td>
                    <td>
                      <a [routerLink]="['/teams', team.teamId]" class="text-decoration-none text-dark fw-semibold team-link">
                        {{ team.teamName }}
                      </a>
                    </td>
                    <td class="text-center">{{ team.played }}</td>
                    <td class="text-center">{{ team.won }}</td>
                    <td class="text-center">{{ team.drawn }}</td>
                    <td class="text-center">{{ team.lost }}</td>
                    <td class="text-center fw-bold">{{ team.points }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- See More Link -->
          <div class="text-center mt-3">
            <a [routerLink]="['/standings']" class="btn btn-outline-primary btn-sm">
              <i class="bi bi-arrow-right-circle me-2"></i>See Full Standings
            </a>
          </div>
        }

        @if (!loading() && standings().length === 0) {
          <div class="text-center py-4 text-muted">
            <i class="bi bi-inbox display-4"></i>
            <p class="mt-2">No standings data available</p>
          </div>
        }

        @if (error()) {
          <div class="alert alert-danger" role="alert">
            {{ error() }}
          </div>
        }
      </div>
    </section>
  `,
  styles: [`
    .standings-table {
      margin-bottom: 0;
    }

    .team-link {
      transition: color 0.2s ease;
    }

    .team-link:hover {
      color: #0d6efd !important;
      text-decoration: underline !important;
    }

    .standings-table tbody tr {
      cursor: pointer;
      transition: background-color 0.15s ease;
    }

    .standings-table tbody tr:hover {
      background-color: rgba(0, 0, 0, 0.02);
    }
  `],
})
export class StandingsSectionComponent implements OnInit {
  private leagueService = inject(LeagueService);
  private divisionService = inject(DivisionService);

  standings = signal<StandingDto[]>([]);
  divisionName = signal<string | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadStandings();
  }

  loadStandings() {
    this.loading.set(true);

    // Get latest season from database
    this.divisionService.getAvailableSeasons().subscribe({
      next: (seasons) => {
        if (seasons.length === 0) {
          this.loading.set(false);
          return;
        }

        const latestSeason = seasons[0];

        // Get divisions for that season
        this.divisionService.getAll(latestSeason, undefined).subscribe({
          next: (divisions) => {
            if (divisions.length === 0) {
              this.loading.set(false);
              return;
            }

            // Use first division
            const firstDivision = divisions[0];
            this.divisionName.set(firstDivision.name);

            // Load standings for that division
            this.leagueService.getLeagueTable(latestSeason, firstDivision.id).subscribe({
              next: (data) => {
                this.standings.set(data.table);
                this.loading.set(false);
              },
              error: (err) => {
                this.error.set('Failed to load standings');
                this.loading.set(false);
                console.error('Error loading standings:', err);
              },
            });
          },
          error: (err) => {
            this.loading.set(false);
            console.error('Error loading divisions:', err);
          },
        });
      },
      error: (err) => {
        this.loading.set(false);
        console.error('Error loading seasons:', err);
      },
    });
  }
}
