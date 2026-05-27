import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PlayerService, MatchEventService, SuspensionService } from '../../core/services';
import { PlayerDto, MatchEventDto, SuspensionDto } from '../../core/models';
import { getImageUrl } from '../../core/utils/image.utils';

@Component({
  selector: 'app-player-detail',
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container py-5">
      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
        </div>
      }

      @if (player()) {
        <!-- Player Header -->
        <div class="row mb-5">
          <div class="col-md-12">
            <div class="card shadow-sm">
              <div class="card-body">
                <div class="row align-items-center">
                  <div class="col-md-2 text-center">
                    @if (getImageUrl(player()!.profileImageUrl)) {
                      <img
                        [src]="getImageUrl(player()!.profileImageUrl)"
                        [alt]="player()!.fullName"
                        class="player-photo"
                      />
                    } @else {
                      <div class="player-photo-placeholder">
                        <i class="bi bi-person-fill"></i>
                      </div>
                    }
                    <div class="jersey-number mt-3">
                      <span class="badge bg-primary fs-3">{{ player()!.jerseyNumber }}</span>
                    </div>
                  </div>
                  <div class="col-md-10">
                    <h1 class="display-4 mb-2">{{ player()!.fullName }}</h1>
                    <p class="lead text-muted mb-3">
                      <span class="badge bg-secondary me-2">{{ player()!.position }}</span>
                      <a [routerLink]="['/teams', player()!.teamId]" class="text-decoration-none">
                        <span class="badge bg-primary">{{ player()!.teamName }}</span>
                      </a>
                      @if (!player()!.isActive) {
                        <span class="badge bg-warning ms-2">Inactive</span>
                      }
                      @if (player()!.isCurrentlySuspended) {
                        <span class="badge bg-danger ms-2">Suspended</span>
                      }
                    </p>
                    <div class="row text-muted mb-3">
                      <div class="col-md-3 mb-2">
                        <i class="bi bi-calendar me-2"></i>
                        <strong>Age:</strong> {{ player()!.ageYears }}
                      </div>
                      @if (player()!.nationality) {
                        <div class="col-md-3 mb-2">
                          <i class="bi bi-flag me-2"></i>
                          <strong>Nationality:</strong> {{ player()!.nationality }}
                        </div>
                      }
                      <div class="col-md-3 mb-2">
                        <i class="bi bi-calendar-event me-2"></i>
                        <strong>DOB:</strong> {{ player()!.dateOfBirth | date: 'MMM d, y' }}
                      </div>
                    </div>

                    <!-- Stats -->
                    <div class="row">
                      <div class="col-6 col-md-3 mb-3">
                        <div class="stat-box">
                          <div class="stat-value text-warning">
                            <i class="bi bi-trophy-fill"></i> {{ player()!.goals }}
                          </div>
                          <div class="stat-label">Goals</div>
                        </div>
                      </div>
                      <div class="col-6 col-md-3 mb-3">
                        <div class="stat-box">
                          <div class="stat-value text-info">
                            <i class="bi bi-hand-thumbs-up-fill"></i> {{ player()!.assists }}
                          </div>
                          <div class="stat-label">Assists</div>
                        </div>
                      </div>
                      <div class="col-6 col-md-3 mb-3">
                        <div class="stat-box">
                          <div class="stat-value text-warning">
                            <i class="bi bi-square-fill"></i> {{ player()!.yellowCards }}
                          </div>
                          <div class="stat-label">Yellow Cards</div>
                        </div>
                      </div>
                      <div class="col-6 col-md-3 mb-3">
                        <div class="stat-box">
                          <div class="stat-value text-danger">
                            <i class="bi bi-square-fill"></i> {{ player()!.redCards }}
                          </div>
                          <div class="stat-label">Red Cards</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                @if (player()!.bio) {
                  <div class="row mt-4">
                    <div class="col-12">
                      <hr>
                      <h5 class="mb-3">Biography</h5>
                      <p class="text-muted">{{ player()!.bio }}</p>
                    </div>
                  </div>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Tabs Navigation -->
        <ul class="nav nav-tabs mb-4" role="tablist">
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'events'"
              (click)="activeTab.set('events')"
            >
              <i class="bi bi-activity me-2"></i>Match Events
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button
              class="nav-link"
              [class.active]="activeTab() === 'suspensions'"
              (click)="setTab('suspensions')"
            >
              <i class="bi bi-exclamation-triangle me-2"></i>Suspensions
            </button>
          </li>
        </ul>

        <!-- Tab Content -->
        <div class="tab-content">
          <!-- Match Events Tab -->
          @if (activeTab() === 'events') {
            <div class="row">
              @if (loadingEvents()) {
                <div class="col-12 text-center py-5">
                  <div class="spinner-border text-primary" role="status"></div>
                </div>
              }

              @if (!loadingEvents() && matchEvents().length > 0) {
                <div class="col-12">
                  <div class="card">
                    <div class="table-responsive">
                      <table class="table table-hover mb-0">
                        <thead class="table-light">
                          <tr>
                            <th>Date</th>
                            <th>Event Type</th>
                            <th>Minute</th>
                            <th>Match</th>
                            <th>Additional Info</th>
                          </tr>
                        </thead>
                        <tbody>
                          @for (event of matchEvents(); track event.id) {
                            <tr>
                              <td>{{ event.matchId | date: 'MMM d, y' }}</td>
                              <td>
                                @switch (event.eventType) {
                                  @case ('Goal') {
                                    <span class="badge bg-success">
                                      <i class="bi bi-trophy-fill"></i> Goal
                                    </span>
                                  }
                                  @case ('Assist') {
                                    <span class="badge bg-info">
                                      <i class="bi bi-hand-thumbs-up-fill"></i> Assist
                                    </span>
                                  }
                                  @case ('YellowCard') {
                                    <span class="badge bg-warning">
                                      <i class="bi bi-square-fill"></i> Yellow Card
                                    </span>
                                  }
                                  @case ('RedCard') {
                                    <span class="badge bg-danger">
                                      <i class="bi bi-square-fill"></i> Red Card
                                    </span>
                                  }
                                  @case ('OwnGoal') {
                                    <span class="badge bg-secondary">
                                      <i class="bi bi-trophy"></i> Own Goal
                                    </span>
                                  }
                                  @case ('Substitution') {
                                    <span class="badge bg-primary">
                                      <i class="bi bi-arrow-left-right"></i> Substitution
                                    </span>
                                  }
                                }
                              </td>
                              <td>{{ event.minute }}'</td>
                              <td>
                                <a [routerLink]="['/matches', event.matchId]" class="text-decoration-none">
                                  {{ event.teamName }}
                                </a>
                              </td>
                              <td>
                                <small class="text-muted">{{ event.additionalInfo || '-' }}</small>
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              }

              @if (!loadingEvents() && matchEvents().length === 0) {
                <div class="col-12 text-center py-5">
                  <i class="bi bi-activity display-1 text-muted"></i>
                  <h5 class="mt-3 text-muted">No match events recorded yet</h5>
                </div>
              }
            </div>
          }

          <!-- Suspensions Tab -->
          @if (activeTab() === 'suspensions') {
            <div class="row">
              @if (loadingSuspensions()) {
                <div class="col-12 text-center py-5">
                  <div class="spinner-border text-primary" role="status"></div>
                </div>
              }

              @if (!loadingSuspensions() && suspensions().length > 0) {
                <div class="col-12">
                  @for (suspension of suspensions(); track suspension.id) {
                    <div class="card mb-3">
                      <div class="card-body">
                        <div class="row align-items-center">
                          <div class="col-md-8">
                            <h6 class="mb-2">
                              @if (suspension.isActive) {
                                <span class="badge bg-danger me-2">Active</span>
                              } @else {
                                <span class="badge bg-secondary me-2">Completed</span>
                              }
                              {{ suspension.reason }}
                            </h6>
                            <p class="text-muted mb-0">
                              <small>
                                <strong>Period:</strong>
                                {{ suspension.startDate | date: 'MMM d, y' }} -
                                {{ suspension.endDate | date: 'MMM d, y' }}
                              </small>
                            </p>
                          </div>
                          <div class="col-md-4 text-end">
                            <div class="fs-2 fw-bold text-danger">
                              {{ suspension.matchesSuspended }}
                            </div>
                            <div class="text-muted">
                              <small>Match{{ suspension.matchesSuspended !== 1 ? 'es' : '' }} Banned</small>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  }
                </div>
              }

              @if (!loadingSuspensions() && suspensions().length === 0) {
                <div class="col-12 text-center py-5">
                  <i class="bi bi-check-circle display-1 text-success"></i>
                  <h5 class="mt-3 text-muted">No suspensions on record</h5>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .player-photo {
      width: 150px;
      height: 150px;
      border-radius: 50%;
      object-fit: cover;
      border: 5px solid #f8f9fa;
    }

    .player-photo-placeholder {
      width: 150px;
      height: 150px;
      border-radius: 50%;
      background-color: #f8f9fa;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto;
    }

    .player-photo-placeholder i {
      font-size: 4rem;
      color: #adb5bd;
    }

    .jersey-number {
      font-size: 1.5rem;
    }

    .stat-box {
      text-align: center;
      padding: 1rem;
      background-color: #f8f9fa;
      border-radius: 8px;
    }

    .stat-value {
      font-size: 2rem;
      font-weight: bold;
      margin-bottom: 0.5rem;
    }

    .stat-label {
      font-size: 0.875rem;
      color: #6c757d;
      text-transform: uppercase;
    }

    .nav-link {
      cursor: pointer;
    }
  `],
})
export class PlayerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private playerService = inject(PlayerService);
  private matchEventService = inject(MatchEventService);
  private suspensionService = inject(SuspensionService);

  player = signal<PlayerDto | null>(null);
  matchEvents = signal<MatchEventDto[]>([]);
  suspensions = signal<SuspensionDto[]>([]);

  loading = signal(false);
  loadingEvents = signal(false);
  loadingSuspensions = signal(false);
  activeTab = signal<'events' | 'suspensions'>('events');

  // Use shared utility for image URL handling
  getImageUrl = getImageUrl;

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const playerId = params['id'];
      if (playerId) {
        this.loadPlayer(playerId);
        this.loadMatchEvents(playerId);
        this.loadSuspensions(playerId);
      }
    });
  }

  loadPlayer(id: string) {
    this.loading.set(true);
    this.playerService.getById(id).subscribe({
      next: (data) => {
        this.player.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load player:', err);
        this.loading.set(false);
      },
    });
  }

  loadMatchEvents(playerId: string) {
    this.loadingEvents.set(true);
    this.matchEventService.getByPlayer(playerId).subscribe({
      next: (data) => {
        this.matchEvents.set(data);
        this.loadingEvents.set(false);
      },
      error: (err) => {
        console.error('Failed to load match events:', err);
        this.loadingEvents.set(false);
      },
    });
  }

  loadSuspensions(playerId: string) {
    this.loadingSuspensions.set(true);
    this.suspensionService.getPlayerHistory(playerId).subscribe({
      next: (data) => {
        this.suspensions.set(data);
        this.loadingSuspensions.set(false);
      },
      error: (err) => {
        console.error('Failed to load suspensions:', err);
        this.loadingSuspensions.set(false);
      },
    });
  }

  setTab(tab: 'events' | 'suspensions') {
    this.activeTab.set(tab);
  }
}
