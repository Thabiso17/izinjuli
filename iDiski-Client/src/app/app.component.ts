import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layouts/header/header.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent],
  template: `
    <div class="min-vh-100 bg-light">
      <app-header />
      <main>
        <router-outlet />
      </main>
      <footer class="bg-dark text-white py-4 mt-5">
        <div class="container">
          <div class="row">
            <div class="col-md-6">
              <h5 class="fw-bold mb-3">
                <i class="bi bi-shield-fill-check me-2"></i>iDiski League
              </h5>
              <p class="text-muted">
                Professional football league management system. Track teams, players, matches, and standings in real-time.
              </p>
            </div>
            <div class="col-md-3">
              <h6 class="fw-bold mb-3">Quick Links</h6>
              <ul class="list-unstyled">
                <li class="mb-2">
                  <a href="/teams" class="text-white-50 text-decoration-none">Teams</a>
                </li>
                <li class="mb-2">
                  <a href="/matches" class="text-white-50 text-decoration-none">Fixtures</a>
                </li>
                <li class="mb-2">
                  <a href="/standings" class="text-white-50 text-decoration-none">Standings</a>
                </li>
              </ul>
            </div>
            <div class="col-md-3">
              <h6 class="fw-bold mb-3">Admin</h6>
              <ul class="list-unstyled">
                <li class="mb-2">
                  <a href="/admin/teams" class="text-white-50 text-decoration-none">Manage Teams</a>
                </li>
                <li class="mb-2">
                  <a href="/admin/players" class="text-white-50 text-decoration-none">Manage Players</a>
                </li>
                <li class="mb-2">
                  <a href="/admin/matches" class="text-white-50 text-decoration-none">Manage Matches</a>
                </li>
              </ul>
            </div>
          </div>
          <hr class="my-4 bg-secondary" />
          <div class="text-center text-muted">
            <small>
              &copy; {{ currentYear }} iDiski League. Built with Angular 21 & .NET 9.
            </small>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    main {
      min-height: calc(100vh - 200px);
    }

    footer a:hover {
      color: white !important;
      text-decoration: underline !important;
    }
  `],
})
export class AppComponent {
  currentYear = new Date().getFullYear();
}
