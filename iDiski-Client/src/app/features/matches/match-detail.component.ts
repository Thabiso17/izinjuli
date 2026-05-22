import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatchService, MatchEventService } from '../../core/services';
import { MatchResultDto, MatchEventDto } from '../../core/models';

@Component({
  selector: 'app-match-detail',
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-5">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      @if (match()) {
        <!-- Match Header -->
        <div class="card shadow-sm mb-4">
          <div class="card-body">
            <div class="text-center mb-4">
              <small class="text-muted d-block mb-2">
                {{ match()!.matchDate | date: 'EEEE, MMMM d, y - h:mm a' }}
              </small>
              @if (match()!.divisionName) {
                <span class="badge bg-primary mb-3">{{ match()!.divisionName }}</span>
              }
              @switch (match()!.status) {
                @case ('InProgress') {
                  <span class="badge bg-success ms-2 mb-3">Live</span>
                }
                @case ('Postponed') {
                  <span class="badge bg-warning ms-2 mb-3">Postponed</span>
                }
                @case ('Cancelled') {
                  <span class="badge bg-danger ms-2 mb-3">Cancelled</span>
                }
              }
            </div>

            <div class="row align-items-center">
              <!-- Home Team -->
              <div class="col-5 text-center">
                <a [routerLink]="['/teams', match()!.homeTeamId]" class="text-decoration-none">
                  @if (match()!.homeTeamLogo) {
                    <img
                      [src]="match()!.homeTeamLogo"
                      [alt]="match()!.homeTeamName"
                      class="team-logo-large mb-3"
                    />
                  } @else {
                    <div class="team-logo-large-placeholder mb-3">
                      <i class="bi bi-shield-fill"></i>
                    </div>
                  }
                  <h3 class="text-dark">{{ match()!.homeTeamName }}</h3>
                  <p class="text-muted">{{ match()!.homeTeamShortCode }}</p>
                </a>
              </div>

              <!-- Score -->
              <div class="col-2 text-center">
                @if (match()!.status === 'Completed') {
                  <div class="score-display-large">
                    <div class="score">{{ match()!.homeScore }}</div>
                    <div class="separator">-</div>
                    <div class="score">{{ match()!.awayScore }}</div>
                  </div>
                  <small class="text-muted d-block mt-2">Full Time</small>
                } @else if (match()!.status === 'InProgress') {
                  <div class="score-display-large">
                    <div class="score">{{ match()!.homeScore }}</div>
                    <div class="separator">-</div>
                    <div class="score">{{ match()!.awayScore }}</div>
                  </div>
                  <small class="text-success d-block mt-2">
                    <i class="bi bi-circle-fill blink"></i> Live
                  </small>
                } @else {
                  <div class="vs-display-large">VS</div>
                  <small class="text-muted d-block mt-2">Matchweek {{ match()!.matchweekNumber }}</small>
                }
              </div>

              <!-- Away Team -->
              <div class="col-5 text-center">
                <a [routerLink]="['/teams', match()!.awayTeamId]" class="text-decoration-none">
                  @if (match()!.awayTeamLogo) {
                    <img
                      [src]="match()!.awayTeamLogo"
                      [alt]="match()!.awayTeamName"
                      class="team-logo-large mb-3"
                    />
                  } @else {
                    <div class="team-logo-large-placeholder mb-3">
                      <i class="bi bi-shield-fill"></i>
                    </div>
                  }
                  <h3 class="text-dark">{{ match()!.awayTeamName }}</h3>
                  <p class="text-muted">{{ match()!.awayTeamShortCode }}</p>
                </a>
              </div>
            </div>

            @if (match()!.venue || match()!.referee) {
              <div class="row mt-4">
                <div class="col-12 text-center">
                  @if (match()!.venue) {
                    <div class="text-muted mb-2">
                      <i class="bi bi-geo-alt me-2"></i>
                      <strong>Venue:</strong> {{ match()!.venue }}
                    </div>
                  }
                  @if (match()!.referee) {
                    <div class="text-muted">
                      <i class="bi bi-person-badge me-2"></i>
                      <strong>Referee:</strong> {{ match()!.referee }}
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        </div>

        <!-- Match Events -->
        @if (events().length > 0) {
          <div class="card shadow-sm mb-4">
            <div class="card-header bg-white">
              <h5 class="mb-0">
                <i class="bi bi-activity me-2"></i>Match Events
              </h5>
            </div>
            <div class="card-body">
              <div class="timeline">
                @for (event of events(); track event.id) {
                  <div class="timeline-item">
                    <div class="timeline-marker">
                      @switch (event.eventType) {
                        @case ('Goal') {
                          <i class="bi bi-trophy-fill text-success"></i>
                        }
                        @case ('Assist') {
                          <i class="bi bi-hand-thumbs-up-fill text-info"></i>
                        }
                        @case ('YellowCard') {
                          <i class="bi bi-square-fill text-warning"></i>
                        }
                        @case ('RedCard') {
                          <i class="bi bi-square-fill text-danger"></i>
                        }
                        @case ('OwnGoal') {
                          <i class="bi bi-trophy text-secondary"></i>
                        }
                        @case ('Substitution') {
                          <i class="bi bi-arrow-left-right text-primary"></i>
                        }
                      }
                    </div>
                    <div class="timeline-content">
                      <div class="d-flex justify-content-between align-items-start">
                        <div>
                          <span class="badge bg-dark me-2">{{ event.minute }}'</span>
                          <strong>
                            <a [routerLink]="['/players', event.playerId]" class="text-decoration-none">
                              {{ event.playerName }}
                            </a>
                          </strong>
                          <small class="text-muted d-block">{{ event.teamName }}</small>
                          @if (event.additionalInfo) {
                            <small class="text-muted fst-italic d-block mt-1">
                              {{ event.additionalInfo }}
                            </small>
                          }
                        </div>
                        <div>
                          @switch (event.eventType) {
                            @case ('Goal') {
                              <span class="badge bg-success">Goal</span>
                            }
                            @case ('Assist') {
                              <span class="badge bg-info">Assist</span>
                            }
                            @case ('YellowCard') {
                              <span class="badge bg-warning">Yellow Card</span>
                            }
                            @case ('RedCard') {
                              <span class="badge bg-danger">Red Card</span>
                            }
                            @case ('OwnGoal') {
                              <span class="badge bg-secondary">Own Goal</span>
                            }
                            @case ('Substitution') {
                              <span class="badge bg-primary">Sub</span>
                            }
                          }
                        </div>
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>
          </div>
        }

        <!-- Match Notes -->
        @if (match()!.notes) {
          <div class="card shadow-sm">
            <div class="card-header bg-white">
              <h5 class="mb-0">
                <i class="bi bi-file-text me-2"></i>Match Notes
              </h5>
            </div>
            <div class="card-body">
              <p class="mb-0 text-muted">{{ match()!.notes }}</p>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .team-logo-large {
      width: 120px;
      height: 120px;
      object-fit: contain;
    }

    .team-logo-large-placeholder {
      width: 120px;
      height: 120px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border-radius: 10px;
    }

    .team-logo-large-placeholder i {
      font-size: 3.5rem;
      color: #adb5bd;
    }

    .score-display-large {
      font-size: 4rem;
      font-weight: bold;
      color: #212529;
      display: flex;
      justify-content: center;
      align-items: center;
    }

    .score {
      min-width: 80px;
      text-align: center;
    }

    .separator {
      margin: 0 20px;
      color: #6c757d;
    }

    .vs-display-large {
      font-size: 3rem;
      font-weight: bold;
      color: #6c757d;
    }

    @keyframes blink {
      0%, 50%, 100% { opacity: 1; }
      25%, 75% { opacity: 0.3; }
    }

    .blink {
      animation: blink 2s infinite;
    }

    .timeline {
      position: relative;
      padding-left: 0;
    }

    .timeline-item {
      display: flex;
      margin-bottom: 1.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .timeline-item:last-child {
      border-bottom: none;
      margin-bottom: 0;
      padding-bottom: 0;
    }

    .timeline-marker {
      flex-shrink: 0;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background-color: #f8f9fa;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 1rem;
      font-size: 1.25rem;
    }

    .timeline-content {
      flex-grow: 1;
      padding-top: 0.5rem;
    }
  `],
})
export class MatchDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private matchService = inject(MatchService);
  private matchEventService = inject(MatchEventService);

  match = signal<MatchResultDto | null>(null);
  events = signal<MatchEventDto[]>([]);
  loading = signal(false);

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const matchId = params['id'];
      if (matchId) {
        this.loadMatch(matchId);
        this.loadEvents(matchId);
      }
    });
  }

  loadMatch(id: string) {
    this.loading.set(true);
    this.matchService.getById(id).subscribe({
      next: (data) => {
        this.match.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load match:', err);
        this.loading.set(false);
      },
    });
  }

  loadEvents(matchId: string) {
    this.matchEventService.getByMatch(matchId).subscribe({
      next: (data) => {
        // Sort by minute ascending
        const sorted = data.sort((a, b) => a.minute - b.minute);
        this.events.set(sorted);
      },
      error: (err) => console.error('Failed to load events:', err),
    });
  }
}
