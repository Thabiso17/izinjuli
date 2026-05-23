import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  // ── PUBLIC PAGES ──────────────────────────────────────────────────────────
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
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
