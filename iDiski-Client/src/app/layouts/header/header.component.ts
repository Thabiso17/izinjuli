import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary sticky-top shadow">
      <div class="container-fluid">
        <!-- Brand -->
        <a class="navbar-brand d-flex align-items-center" routerLink="/">
          <i class="bi bi-shield-fill-check me-2" style="font-size: 1.5rem;"></i>
          <span class="fw-bold">izinjuli league</span>
        </a>

        <!-- Mobile Toggle -->
        <button
          class="navbar-toggler"
          type="button"
          (click)="toggleMenu()"
          [attr.aria-expanded]="isMenuOpen()"
        >
          <span class="navbar-toggler-icon"></span>
        </button>

        <!-- Nav Links -->
        <div class="collapse navbar-collapse" [class.show]="isMenuOpen()">
          <ul class="navbar-nav me-auto mb-2 mb-lg-0">
            <!-- Home -->
            <li class="nav-item">
              <a
                class="nav-link"
                routerLink="/"
                routerLinkActive="active"
                [routerLinkActiveOptions]="{ exact: true }"
                (click)="closeMenu()"
              >
                <i class="bi bi-house-fill me-1"></i>
                Home
              </a>
            </li>

            <!-- Teams -->
            <li class="nav-item">
              <a
                class="nav-link"
                routerLink="/teams"
                routerLinkActive="active"
                (click)="closeMenu()"
              >
                <i class="bi bi-shield-fill me-1"></i>
                Teams
              </a>
            </li>

            <!-- Matches -->
            <li class="nav-item">
              <a
                class="nav-link"
                routerLink="/matches"
                routerLinkActive="active"
                (click)="closeMenu()"
              >
                <i class="bi bi-calendar-event-fill me-1"></i>
                Matches
              </a>
            </li>

            <!-- Standings -->
            <li class="nav-item">
              <a
                class="nav-link"
                routerLink="/standings"
                routerLinkActive="active"
                (click)="closeMenu()"
              >
                <i class="bi bi-trophy-fill me-1"></i>
                Standings
              </a>
            </li>

            <!-- Admin Dropdown (Only if logged in) -->
            @if (authService.isAuthenticated()) {
              <li class="nav-item dropdown" [class.show]="isAdminOpen()">
                <a
                  class="nav-link dropdown-toggle"
                  href="#"
                  role="button"
                  (click)="toggleAdmin($event)"
                  [class.active]="isAdminRoute()"
                >
                  <i class="bi bi-gear-fill me-1"></i>
                  Admin
                </a>
                <ul class="dropdown-menu" [class.show]="isAdminOpen()">
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/divisions"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-trophy me-2"></i>Divisions
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/teams"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-shield me-2"></i>Teams
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/players"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-person me-2"></i>Players
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/matches"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-calendar-event me-2"></i>Matches
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/suspensions"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-exclamation-triangle me-2"></i>Suspensions
                    </a>
                  </li>
                  <li><hr class="dropdown-divider" /></li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/articles"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-newspaper me-2"></i>Articles
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/videos"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-camera-video me-2"></i>Videos
                    </a>
                  </li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/sponsors"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-tag me-2"></i>Sponsors
                    </a>
                  </li>
                  <li><hr class="dropdown-divider" /></li>
                  <li>
                    <a
                      class="dropdown-item"
                      routerLink="/admin/layout"
                      routerLinkActive="active"
                      (click)="closeMenu()"
                    >
                      <i class="bi bi-layout-text-window me-2"></i>Layout Editor
                    </a>
                  </li>
                </ul>
              </li>
            }
          </ul>

          <!-- Auth Buttons -->
          <div class="d-flex gap-2 align-items-center">
            <button class="btn btn-outline-light" type="button" disabled>
              <i class="bi bi-search me-1"></i>
              <span class="d-none d-md-inline">Search</span>
            </button>

            @if (authService.isAuthenticated()) {
              <!-- User Info + Logout Dropdown -->
              <div class="nav-item dropdown">
                <button
                  class="btn btn-outline-light dropdown-toggle"
                  type="button"
                  (click)="toggleUserMenu($event)"
                  [class.show]="isUserMenuOpen()"
                >
                  <i class="bi bi-person-circle me-1"></i>
                  <span class="d-none d-md-inline">{{ authService.currentUser()?.firstName }}</span>
                </button>
                <ul class="dropdown-menu dropdown-menu-end" [class.show]="isUserMenuOpen()">
                  <li>
                    <span class="dropdown-item-text small text-muted">
                      {{ authService.currentUser()?.email }}
                    </span>
                  </li>
                  <li><hr class="dropdown-divider" /></li>
                  <li>
                    <a class="dropdown-item" href="#" (click)="logout($event)">
                      <i class="bi bi-box-arrow-right me-2"></i>Logout
                    </a>
                  </li>
                </ul>
              </div>
            } @else {
              <!-- Login Button -->
              <a routerLink="/login" class="btn btn-light">
                <i class="bi bi-box-arrow-in-right me-1"></i>
                Login
              </a>
            }
          </div>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      z-index: 1030;
    }

    .navbar-brand {
      font-size: 1.25rem;
      transition: transform 0.2s;
    }

    .navbar-brand:hover {
      transform: scale(1.05);
    }

    .nav-link {
      transition: all 0.2s;
      border-radius: 0.25rem;
      margin: 0 0.25rem;
    }

    .nav-link:hover {
      background-color: rgba(255, 255, 255, 0.1);
    }

    .nav-link.active {
      background-color: rgba(255, 255, 255, 0.2);
      font-weight: 600;
    }

    .dropdown-menu {
      border: none;
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
    }

    .dropdown-item {
      transition: background-color 0.2s;
    }

    .dropdown-item:hover {
      background-color: #0d6efd;
      color: white;
    }

    .dropdown-item.active {
      background-color: #0d6efd;
      font-weight: 600;
    }

    .dropdown-menu-end {
      right: 0;
      left: auto;
    }

    .dropdown-item-text {
      padding: 0.25rem 1rem;
      white-space: normal;
    }

    /* Mobile Adjustments */
    @media (max-width: 991px) {
      .navbar-collapse {
        background-color: rgba(0, 0, 0, 0.05);
        padding: 1rem;
        border-radius: 0.5rem;
        margin-top: 1rem;
      }

      .nav-link {
        padding: 0.75rem 1rem;
      }

      .dropdown-menu {
        background-color: rgba(255, 255, 255, 0.1);
        border: none;
        box-shadow: none;
        padding-left: 1rem;
      }

      .dropdown-item {
        color: white;
        padding: 0.5rem 1rem;
      }

      .dropdown-item:hover {
        background-color: rgba(255, 255, 255, 0.1);
      }
    }
  `],
})
export class HeaderComponent {
  isMenuOpen = signal(false);
  isAdminOpen = signal(false);
  isUserMenuOpen = signal(false);

  constructor(
    public authService: AuthService,
    private router: Router
  ) {}

  toggleMenu() {
    this.isMenuOpen.update(v => !v);
  }

  closeMenu() {
    this.isMenuOpen.set(false);
    this.isAdminOpen.set(false);
    this.isUserMenuOpen.set(false);
  }

  toggleAdmin(event: Event) {
    event.preventDefault();
    this.isAdminOpen.update(v => !v);
  }

  toggleUserMenu(event: Event) {
    event.preventDefault();
    this.isUserMenuOpen.update(v => !v);
  }

  logout(event: Event) {
    event.preventDefault();
    this.authService.logout();
    this.closeMenu();
    this.router.navigate(['/']);
  }

  isAdminRoute(): boolean {
    return window.location.pathname.startsWith('/admin');
  }
}
