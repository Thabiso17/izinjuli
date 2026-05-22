# Phase 3: Services - COMPLETE ✅

**Duration**: 20 minutes (as planned)

## Summary

All Angular services have been created/updated to communicate with the .NET API backend. Services follow best practices with modern Angular standalone patterns using `inject()` and `providedIn: 'root'`.

## Completed Services

### Core League Services

#### 1. **TeamService** (`team.service.ts`)
- ✅ `getAll()` - List all teams
- ✅ `getById(id)` - Get single team
- ✅ `create(command)` - Create new team
- ✅ `update(id, command)` - Update team
- ✅ `delete(id)` - Delete team

#### 2. **PlayerService** (`player.service.ts`)
- ✅ `getAll(teamId?, activeOnly?)` - List players with filters
- ✅ `getById(id)` - Get single player *(newly added)*
- ✅ `create(command)` - Create new player
- ✅ `update(id, command)` - Update player
- ✅ `delete(id)` - Soft delete player

#### 3. **DivisionService** (`division.service.ts`)
- ✅ `getAll(season?, isActive?)` - List divisions
- ✅ `getById(id)` - Get single division
- ✅ `create(command)` - Create division
- ✅ `update(command)` - Update division
- ✅ `delete(id)` - Delete division

#### 4. **MatchService** (`match.service.ts`)
- ✅ `getAll(...)` - Paginated fixtures with filters
- ✅ `getById(id)` - Get single match
- ✅ `create(command)` - Schedule new match
- ✅ `updateScore(id, command)` - Update match score/status
- ✅ `delete(id)` - Delete match *(newly added)*

#### 5. **MatchEventService** (`match-event.service.ts`)
- ✅ `getByMatch(matchId)` - Get events for a match
- ✅ `getByPlayer(playerId, season?)` - Get events for a player
- ✅ `recordEvents(command)` - Batch record match events

#### 6. **SuspensionService** (`suspension.service.ts`)
- ✅ `getActive(divisionId?)` - Get active suspensions
- ✅ `getPlayerHistory(playerId)` - Get player's suspension history
- ✅ `create(command)` - Create suspension

#### 7. **StandingsService** (`standings.service.ts`) ⭐ *NEW*
- ✅ `getLeagueTable(season, divisionId?, upToMatchweek?)` - League table
- ✅ `getTopScorers(season, topN?, divisionId?)` - Top scorers leaderboard
- ✅ `getHeadToHead(teamAId, teamBId)` - H2H stats

### Content & Configuration Services

#### 8. **ArticleService** (`article.service.ts`)
- ✅ Public: `getPublished()`, `getBySlug()`
- ✅ Admin: `getAllAdmin()`, `create()`, `update()`, `publish()`, `unpublish()`, `delete()`

#### 9. **SponsorService** (`sponsor.service.ts`)
- ✅ `getByPlacement(placement)` - Get sponsors for ad placement

#### 10. **PageLayoutService** (`page-layout.service.ts`)
- ✅ `getLayout(pageName)` - Get page component layout
- ✅ `upsert(command)` - Upsert single component
- ✅ `bulkUpdate(command)` - Bulk update layout order

#### 11. **LeagueService** (`league.service.ts`)
- ✅ Legacy service - contains combined endpoints (overlaps with newer services)
- ✅ Can be refactored later or kept for backward compatibility

## Service Architecture

### Pattern Used
```typescript
@Injectable({ providedIn: 'root' })
export class ExampleService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/endpoint`;
  
  // Methods use HttpClient + RxJS Observables
}
```

### Key Features
- ✅ **Dependency Injection**: Modern `inject()` syntax
- ✅ **Type Safety**: Full TypeScript typing with DTOs
- ✅ **RxJS Observables**: Async data streams
- ✅ **Environment Config**: API URLs from `environment.ts`
- ✅ **HTTP Params**: Proper query string handling
- ✅ **Error Handling**: Let interceptors handle globally

## Environment Configuration

### Development (`environment.ts` & `environment.development.ts`)
```typescript
{
  production: false,
  apiBaseUrl: 'http://localhost:5207/api'
}
```

### Production (`environment.production.ts`)
```typescript
{
  production: true,
  apiBaseUrl: 'https://your-production-api.com/api' // TODO: Update before deploy
}
```

## Updates Made

1. ✅ Created `StandingsService` (was missing)
2. ✅ Added `getById()` to `PlayerService`
3. ✅ Added `delete()` to `MatchService`
4. ✅ Fixed `UpdateMatchScoreCommand` model (added `id` property)
5. ✅ Fixed `CreateMatchCommand` model (removed unnecessary fields)
6. ✅ Updated service barrel export (`index.ts`) to include all services
7. ✅ Created `environment.development.ts` for consistency

## API Endpoints Coverage

| Controller | Service | Status |
|------------|---------|--------|
| TeamsController | TeamService | ✅ Complete |
| PlayersController | PlayerService | ✅ Complete |
| DivisionsController | DivisionService | ✅ Complete |
| MatchResultsController | MatchService | ✅ Complete |
| MatchEventsController | MatchEventService | ✅ Complete |
| SuspensionsController | SuspensionService | ✅ Complete |
| StandingsController | StandingsService | ✅ Complete |
| ArticlesController | ArticleService | ✅ Complete |
| SponsorsController | SponsorService | ✅ Complete |
| PageLayoutConfigsController | PageLayoutService | ✅ Complete |

## Next Steps: Phase 4

**Phase 4: Directory Setup** (5 minutes)
- Create feature module directories
- Set up admin vs public page structure
- Organize component scaffolding

## Usage Example

```typescript
// In a component
import { TeamService } from '@core/services';

export class TeamsListComponent {
  private teamService = inject(TeamService);
  teams$ = this.teamService.getAll();
}
```

```html
<!-- In template -->
@if (teams$ | async; as teams) {
  @for (team of teams; track team.id) {
    <div>{{ team.name }}</div>
  }
}
```

---

**Phase 3 Status**: ✅ COMPLETE  
**Time Spent**: ~20 minutes  
**Ready for**: Phase 4 (Directory Setup)
