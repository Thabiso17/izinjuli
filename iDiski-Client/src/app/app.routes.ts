import { Routes } from '@angular/router';
import { adminGuard, authGuard, noAuthGuard, superAdminGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  // ── AUTHENTICATION PAGES ──────────────────────────────────────────────────
  {
    path: 'login',
    canActivate: [noAuthGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    canActivate: [noAuthGuard],
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      ),
  },
  {
    path: 'reset-password',
    canActivate: [noAuthGuard],
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      ),
  },
  // ── PUBLIC PAGES ──────────────────────────────────────────────────────────
  {
    path: 'divisions',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/divisions/divisions-list.component').then(
            (m) => m.DivisionsListComponent
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/divisions/division-detail.component').then(
            (m) => m.DivisionDetailComponent
          ),
      },
    ],
  },
  {
    // A competition has its own page: a division runs several, and one page cannot honestly
    // show a league, a cup and a sponsor's tournament at the same time.
    path: 'competitions/:id',
    loadComponent: () =>
      import('./features/competitions/competition-detail.component').then(
        (m) => m.CompetitionDetailComponent
      ),
  },
  {
    path: 'teams',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/teams/teams-list.component').then(
            (m) => m.TeamsListComponent
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/teams/team-detail.component').then(
            (m) => m.TeamDetailComponent
          ),
      },
    ],
  },
  {
    path: 'players/:id',
    loadComponent: () =>
      import('./features/players/player-detail.component').then(
        (m) => m.PlayerDetailComponent
      ),
  },
  {
    path: 'matches',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/matches/fixtures-results.component').then(
            (m) => m.FixturesResultsComponent
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/matches/match-detail.component').then(
            (m) => m.MatchDetailComponent
          ),
      },
    ],
  },
  {
    path: 'standings',
    loadComponent: () =>
      import('./features/standings/standings.component').then(
        (m) => m.StandingsComponent
      ),
  },
  {
    path: 'news/:slug',
    loadComponent: () =>
      import('./features/articles/article-detail.component').then(
        (m) => m.ArticleDetailComponent
      ),
  },
  // ── ADMIN PAGES ───────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    children: [
      {
        path: '',
        redirectTo: 'divisions',
        pathMatch: 'full',
      },
      {
        path: 'divisions',
        loadComponent: () =>
          import('./features/admin/divisions/divisions-admin.component').then(
            (m) => m.DivisionsAdminComponent
          ),
      },
      {
        path: 'teams',
        loadComponent: () =>
          import('./features/admin/teams/teams-admin.component').then(
            (m) => m.TeamsAdminComponent
          ),
      },
      {
        path: 'players',
        loadComponent: () =>
          import('./features/admin/players/players-admin.component').then(
            (m) => m.PlayersAdminComponent
          ),
      },
      {
        path: 'matches',
        loadComponent: () =>
          import('./features/admin/matches/matches-admin.component').then(
            (m) => m.MatchesAdminComponent
          ),
      },
      {
        path: 'suspensions',
        loadComponent: () =>
          import('./features/admin/suspensions/suspensions-admin.component').then(
            (m) => m.SuspensionsAdminComponent
          ),
      },
      {
        path: 'articles',
        loadComponent: () =>
          import('./features/admin/articles/articles-admin.component').then(
            (m) => m.ArticlesAdminComponent
          ),
      },
      {
        path: 'videos',
        loadComponent: () =>
          import('./features/admin/videos/videos-admin.component').then(
            (m) => m.VideosAdminComponent
          ),
      },
      {
        path: 'sponsors',
        loadComponent: () =>
          import('./features/admin/sponsors/sponsors-admin.component').then(
            (m) => m.SponsorsAdminComponent
          ),
      },
      {
        path: 'layout',
        loadComponent: () =>
          import('./features/admin/layout-editor/layout-editor.component').then(
            (m) => m.LayoutEditorComponent
          ),
      },
      {
        // Competitions are not nested under anything: a division holds clubs, and what those
        // clubs play stands on its own.
        path: 'competitions',
        loadComponent: () =>
          import('./features/admin/competitions/competitions-admin.component').then(
            (m) => m.CompetitionsAdminComponent
          ),
      },
      {
        path: 'users',
        // Only a super admin manages administrators — creating one is theirs alone now, and
        // the full list of everyone's access is not a competition admin's to see.
        canActivate: [superAdminGuard],
        loadComponent: () =>
          import('./features/admin/users/users-admin.component').then(
            (m) => m.UsersAdminComponent
          ),
      },
      {
        path: 'clear-data',
        canActivate: [superAdminGuard],
        loadComponent: () =>
          import('./features/admin/clear-data/clear-data-admin.component').then(
            (m) => m.ClearDataAdminComponent
          ),
      },
    ],
  },
  {
    path: '404',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then(
        (m) => m.NotFoundComponent
      ),
  },
  {
    path: '**',
    redirectTo: '/404',
  },
];

