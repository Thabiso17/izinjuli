import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TeamService, PlayerService, MatchService } from '../../core/services';
import { TeamDto, PlayerDto, MatchResultDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';

@Component({
  selector: 'app-team-detail',
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-5">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      @if (team()) {
        <!-- Team Header -->
        <div class="row mb-5">
          <div class="col-md-12">
            <div class="card shadow-sm">
              <div class="card-body">
                <div class="row align-items-center">
                  <div class="col-md-2 text-center">
                    @if (team()!.logoUrl) {
                      <img
                        [src]="team()!.logoUrl"
                        [alt]="team()!.name"
                        class="team-logo-large"
                      />
                    } @else {
                      <div class="team-logo-large-placeholder">
                        <i class="bi bi-shield-fill"></i>
                      </div>
                    }
                  </div>
                  <div class="col-md-10">
                    <h1 class="display-4 mb-2">{{ team()!.name }}</h1>
                    <p class="lead text-muted mb-3">
                      <span class="badge bg-secondary me-2">{{ team()!.shortCode }}</span>
                      @if (team()!.divisionName) {
                        <span class="badge bg-primary">{{ team()!.divisionName }}</span>
                      }
                    </p>
                    <div class="row text-muted">
                      @if (team()!.city) {
                        <div class="col-md-3 mb-2">
                          <i class="bi bi-geo-alt me-2"></i>
                          <strong>City:</strong> {{ team()!.city }}
                        </div>
                      }
                      @if (team()!.homeGround) {
                        <div class="col-md-4 mb-2">
                          <i class="bi bi-building me-2"></i>
                          <strong>Home Ground:</strong> {{ team()!.homeGround }}
                        </div>
                      }
                      <div class="col-md-3 mb-2">
                        <i class="bi bi-calendar me-2"></i>
                        <strong>Founded:</strong> {{ team()!.founded }}
                      </div>
                      <div class="col-md-2 mb-2">
                        <i class="bi bi-people me-2"></i>
                        <strong>Squad:</strong> {{ team()!.playerCount }}
                      </div>
                    </div>
                    @if (team()!.primaryColour || team()!.secondaryColour) {
                      <div class="mt-3">
                        <small class="text-muted me-2">Team Colors:</small>
                        @if (team()!.primaryColour) {
                          <span
                            class="color-swatch me-2"
                            [style.background-color]="team()!.primaryColour"
                            [title]="'Primary: ' + team()!.primaryColour"
                          ></span>
                        }
                        @if (team()!.secondaryColour) {
                          <span
                            class="color-swatch"
                            [style.background-color]="team()!.secondaryColour"
                            [title]="'Secondary: ' + team()!.secondaryColour"
                          ></span>
                        }
                      </div>
                    }
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Tabs Navigation -->
        <ul class="nav nav-tabs mb-4" role="tablist">
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'squad'"
              (click)="activeTab.set('squad')"
            >
              <i class="bi bi-people-fill me-2"></i>Squad
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'fixtures'"
              (click)="setTab('fixtures')"
            >
              <i class="bi bi-calendar-event me-2"></i>Fixtures
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'results'"
              (click)="setTab('results')"
            >
              <i class="bi bi-trophy me-2"></i>Results
            </button>
          </li>
        </ul>

        <!-- Tab Content -->
        <div class="tab-content">
          <!-- Squad Tab -->
          @if (activeTab() === 'squad') {
            <div class="row">
              @if (loadingPlayers()) {
                <div class="col-12 text-center py-5">
                  <div class="spinner-border text-primary" role="status"></div>
                </div>
              }

              @if (!loadingPlayers() && players().length > 0) {
                @for (position of ['Goalkeeper', 'Defender', 'Midfielder', 'Forward']; track position) {
                  @if (getPlayersByPosition(position).length > 0) {
                    <div class="col-12 mb-4">
                      <h4 class="mb-3">{{ position }}s</h4>
                      <div class="row g-3">
                        @for (player of getPlayersByPosition(position); track player.id) {
                          <div class="col-md-6 col-lg-4">
                            <a
                              [routerLink]="['/players', player.id]"
                              class="text-decoration-none"
                            >
                              <div class="card player-card">
                                <div class="card-body">
                                  <div class="d-flex align-items-center">
                                    <div class="position-relative me-3">
                                      @if (getImageUrl(player.profileImageUrl)) {
                                        <img
                                          [src]="getImageUrl(player.profileImageUrl)"
                                          [alt]="player.fullName"
                                          class="player-avatar"
                                        />
                                      } @else {
                                        <div class="player-avatar-placeholder">
                                          <i class="bi bi-person"></i>
                                        </div>
                                      }
                                      <span class="jersey-badge">{{ player.jerseyNumber }}</span>
                                    </div>
                                    <div class="flex-grow-1">
                                      <h6 class="mb-1 text-dark">{{ player.fullName }}</h6>
                                      <small class="text-muted d-block">
                                        Age: {{ player.ageYears }}
                                        @if (player.nationality) {
                                          | {{ player.nationality }}
                                        }
                                      </small>
                                      <div class="mt-2">
                                        <small class="text-muted me-3">
                                          <i class="bi bi-trophy-fill text-warning"></i> {{ player.goals }}
                                        </small>
                                        <small class="text-muted me-3">
                                          <i class="bi bi-hand-thumbs-up-fill text-info"></i> {{ player.assists }}
                                        </small>
                                        <small class="text-muted me-3">
                                          <i class="bi bi-square-fill text-warning"></i> {{ player.yellowCards }}
                                        </small>
                                        <small class="text-muted">
                                          <i class="bi bi-square-fill text-danger"></i> {{ player.redCards }}
                                        </small>
                                      </div>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </a>
                          </div>
                        }
                      </div>
                    </div>
                  }
                }
              }

              @if (!loadingPlayers() && players().length === 0) {
                <div class="col-12 text-center py-5">
                  <i class="bi bi-person display-1 text-muted"></i>
                  <h5 class="mt-3 text-muted">No players in squad yet</h5>
                </div>
              }
            </div>
          }

          <!-- Fixtures Tab -->
          @if (activeTab() === 'fixtures') {
            <div class="row">
              @if (loadingMatches()) {
                <div class="col-12 text-center py-5">
                  <div class="spinner-border text-primary" role="status"></div>
                </div>
              }

              @if (!loadingMatches() && upcomingMatches().length > 0) {
                @for (match of upcomingMatches(); track match.id) {
                  <div class="col-md-6 mb-3">
                    <a [routerLink]="['/matches', match.id]" class="text-decoration-none">
                      <div class="card match-card">
                        <div class="card-body">
                          <div class="text-center mb-2">
                            <small class="text-muted">
                              {{ match.matchDate | date: 'EEE, MMM d, y - h:mm a' }}
                            </small>
                          </div>
                          <div class="d-flex justify-content-between align-items-center">
                            <div class="text-center flex-grow-1">
                              <div class="fw-bold text-dark">{{ match.homeTeamName }}</div>
                              <small class="text-muted">{{ match.homeTeamShortCode }}</small>
                            </div>
                            <div class="text-center px-3">
                              <span class="badge bg-secondary">{{ match.scoreDisplay }}</span>
                            </div>
                            <div class="text-center flex-grow-1">
                              <div class="fw-bold text-dark">{{ match.awayTeamName }}</div>
                              <small class="text-muted">{{ match.awayTeamShortCode }}</small>
                            </div>
                          </div>
                          @if (match.venue) {
                            <div class="text-center mt-2">
                              <small class="text-muted">
                                <i class="bi bi-geo-alt"></i> {{ match.venue }}
                              </small>
                            </div>
                          }
                        </div>
                      </div>
                    </a>
                  </div>
                }
              } @else if (!loadingMatches()) {
                <div class="col-12 text-center py-5">
                  <i class="bi bi-calendar-x display-1 text-muted"></i>
                  <h5 class="mt-3 text-muted">No upcoming fixtures</h5>
                </div>
              }
            </div>
          }

          <!-- Results Tab -->
          @if (activeTab() === 'results') {
            <div class="row">
              @if (loadingMatches()) {
                <div class="col-12 text-center py-5">
                  <div class="spinner-border text-primary" role="status"></div>
                </div>
              }

              @if (!loadingMatches() && pastMatches().length > 0) {
                @for (match of pastMatches(); track match.id) {
                  <div class="col-md-6 mb-3">
                    <a [routerLink]="['/matches', match.id]" class="text-decoration-none">
                      <div class="card match-card">
                        <div class="card-body">
                          <div class="text-center mb-2">
                            <small class="text-muted">
                              {{ match.matchDate | date: 'EEE, MMM d, y' }}
                            </small>
                          </div>
                          <div class="d-flex justify-content-between align-items-center">
                            <div class="text-center flex-grow-1">
                              <div class="fw-bold text-dark">{{ match.homeTeamName }}</div>
                              <small class="text-muted">{{ match.homeTeamShortCode }}</small>
                            </div>
                            <div class="text-center px-3">
                              <div class="fs-4 fw-bold text-dark">{{ match.scoreDisplay }}</div>
                            </div>
                            <div class="text-center flex-grow-1">
                              <div class="fw-bold text-dark">{{ match.awayTeamName }}</div>
                              <small class="text-muted">{{ match.awayTeamShortCode }}</small>
                            </div>
                          </div>
                        </div>
                      </div>
                    </a>
                  </div>
                }
              } @else if (!loadingMatches()) {
                <div class="col-12 text-center py-5">
                  <i class="bi bi-trophy display-1 text-muted"></i>
                  <h5 class="mt-3 text-muted">No match results yet</h5>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .team-logo-large {
      width: 150px;
      height: 150px;
      object-fit: contain;
    }

    .team-logo-large-placeholder {
      width: 150px;
      height: 150px;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border-radius: 10px;
      margin: 0 auto;
    }

    .team-logo-large-placeholder i {
      font-size: 4rem;
      color: #adb5bd;
    }

    .color-swatch {
      display: inline-block;
      width: 25px;
      height: 25px;
      border-radius: 50%;
      border: 2px solid #dee2e6;
    }

    .player-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .player-card:hover {
      transform: translateY(-3px);
      box-shadow: 0 0.25rem 0.5rem rgba(0, 0, 0, 0.1);
    }

    .player-avatar {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      object-fit: cover;
    }

    .player-avatar-placeholder {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background-color: #f8f9fa;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .player-avatar-placeholder i {
      font-size: 2rem;
      color: #adb5bd;
    }

    .jersey-badge {
      position: absolute;
      bottom: -5px;
      right: -5px;
      background-color: #0d6efd;
      color: white;
      font-weight: bold;
      font-size: 0.75rem;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 2px solid white;
    }

    .match-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .match-card:hover {
      transform: translateY(-3px);
      box-shadow: 0 0.25rem 0.5rem rgba(0, 0, 0, 0.1);
    }

    .nav-link {
      cursor: pointer;
    }
  `],
})
export class TeamDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private teamService = inject(TeamService);
  private playerService = inject(PlayerService);
  private matchService = inject(MatchService);

  team = signal<TeamDto | null>(null);
  players = signal<PlayerDto[]>([]);
  upcomingMatches = signal<MatchResultDto[]>([]);
  pastMatches = signal<MatchResultDto[]>([]);

  loading = signal(false);
  loadingPlayers = signal(false);
  loadingMatches = signal(false);
  activeTab = signal<'squad' | 'fixtures' | 'results'>('squad');

  // Use shared utility for image URL handling
  getImageUrl = getImageUrl;

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const teamId = params['id'];
      if (teamId) {
        this.loadTeam(teamId);
        this.loadPlayers(teamId);
        this.loadMatches(teamId);
      }
    });
  }

  loadTeam(id: string) {
    this.loading.set(true);
    this.teamService.getById(id).subscribe({
      next: (data) => {
        this.team.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load team:', err);
        this.loading.set(false);
      },
    });
  }

  loadPlayers(teamId: string) {
    this.loadingPlayers.set(true);
    this.playerService.getAll(teamId, true).subscribe({
      next: (data) => {
        this.players.set(data);
        this.loadingPlayers.set(false);
      },
      error: (err) => {
        console.error('Failed to load players:', err);
        this.loadingPlayers.set(false);
      },
    });
  }

  loadMatches(teamId: string) {
    this.loadingMatches.set(true);
    const currentSeason = new Date().getFullYear();

    this.matchService.getAll(currentSeason, undefined, teamId, undefined, undefined, 1, 50).subscribe({
      next: (data) => {
        const now = new Date();
        const upcoming = data.items.filter(m =>
          (m.status === 'Scheduled' || m.status === 'InProgress') &&
          new Date(m.matchDate) >= now
        );
        const past = data.items.filter(m =>
          m.status === 'Completed' &&
          new Date(m.matchDate) < now
        ).sort((a, b) => new Date(b.matchDate).getTime() - new Date(a.matchDate).getTime());

        this.upcomingMatches.set(upcoming);
        this.pastMatches.set(past);
        this.loadingMatches.set(false);
      },
      error: (err) => {
        console.error('Failed to load matches:', err);
        this.loadingMatches.set(false);
      },
    });
  }

  getPlayersByPosition(positionCategory: string): PlayerDto[] {
    const positionMap: Record<string, string[]> = {
      'Goalkeeper': ['GK'],
      'Defender': ['CB', 'SW', 'RB', 'LB', 'RWB', 'LWB'],
      'Midfielder': ['CDM', 'CM', 'CAM', 'RM', 'LM'],
      'Forward': ['ST', 'CF', 'RW', 'LW']
    };

    const positions = positionMap[positionCategory] || [];
    return this.players().filter(p => positions.includes(p.position));
  }

  setTab(tab: 'squad' | 'fixtures' | 'results') {
    this.activeTab.set(tab);
  }
}
