# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**iDiski** is a football league management system with:
- **Backend**: .NET 9 Web API using Clean Architecture (Domain, Application, Infrastructure, API layers)
- **Frontend**: Angular 21.1.3 client (in `iDiski-Client/`)
- **Database**: PostgreSQL 
- **Architecture**: CQRS with MediatR, FluentValidation, EF Core

## Architecture

### Clean Architecture Layers

The solution follows strict dependency flow: **Domain** ← **Application** ← **Infrastructure** ← **API**

```
iDiski.Domain/
  └─ Core business entities (Team, Player, MatchResult, Article, Division, MatchEvent, Suspension)
  └─ Domain services (StandingsCalculator, SlugGenerator) - pure logic, no dependencies

iDiski.Application/
  └─ CQRS Commands & Queries using MediatR
  └─ DTOs and feature folders (Teams/, Players/, Articles/, etc.)
  └─ Common/Interfaces/ILeagueDbContext.cs - EF abstraction
  └─ Common/Behaviours/ - MediatR pipeline (ValidationBehaviour, LoggingBehaviour)

iDiski.Infrastructure/
  └─ Persistence/LeagueDbContext.cs - EF Core DbContext implementation
  └─ Migrations/ - EF Core database migrations
  └─ Implements ILeagueDbContext from Application layer

iDiski.Api/
  └─ Controllers/ - thin controllers that dispatch to MediatR
  └─ Program.cs - DI registration, CORS for Angular (localhost:4200)
```

**Key principle**: Application layer never references Infrastructure. It depends only on `ILeagueDbContext` interface. Infrastructure implements the interface and is injected at runtime in `Program.cs`.

### CQRS Pattern

All business logic uses MediatR request/response pattern:
- **Commands** (Create/Update/Delete) → return `Task<Guid>` or `Task<Unit>`
- **Queries** (Get/List) → return DTOs
- Each command/query has a dedicated handler in the same file
- FluentValidation validators run automatically via `ValidationBehaviour<TRequest, TResponse>` pipeline

Example structure (see `iDiski.Application/Teams/Commands/CreateTeamCommand.cs`):
```csharp
public sealed record CreateTeamCommand(...) : IRequest<Guid>;
public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand> { }
public sealed class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Guid> { }
```

### Controllers

All controllers inherit from `BaseApiController` which provides `ISender Sender` (MediatR). Controllers are thin wrappers:
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateTeamCommand command)
    => Ok(await Sender.Send(command));
```

## Development Commands

### Backend (.NET API)

```bash
# Build solution
dotnet build iDiski.sln

# Run API (starts on https://localhost:5001)
dotnet run --project iDiski.Api

# Run tests (if test project exists)
dotnet test

# Entity Framework migrations
dotnet ef migrations add MigrationName --project iDiski.Infrastructure --startup-project iDiski.Api
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api

# Restore packages
dotnet restore
```

**Note**: Migrations are stored in `iDiski.Infrastructure/Migrations/` but must specify `--startup-project iDiski.Api` because the connection string is in `iDiski.Api/appsettings.json`.

### Frontend (Angular)

```bash
cd iDiski-Client

# Start dev server (http://localhost:4200)
ng serve

# Build for production
ng build

# Run unit tests (Vitest)
ng test

# Generate new component
ng generate component component-name

# Generate other schematics
ng generate --help
```

## Database Configuration

Connection string in `iDiski.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=idiski_db;Username=postgres;Password=..."
}
```

The DbContext is registered in `Program.cs` with:
```csharp
builder.Services.AddDbContext<LeagueDbContext>(options =>
    options.UseNpgsql(connectionString, 
        b => b.MigrationsAssembly("iDiski.Infrastructure")));
```

## Key Domain Entities

- **Team**: ShortCode (unique), Division FK, colors, logo
- **Player**: JerseyNumber (unique per team), Position enum, Team FK
- **MatchResult**: HomeTeam/AwayTeam FKs, Division FK, Season, Matchweek, scores, Status enum
- **Division**: Season + ShortCode composite unique index, Gender enum, AgeGroup
- **MatchEvent**: EventType enum (Goal/YellowCard/RedCard/Substitution), Minute (1-120), Match/Player FKs
- **Suspension**: Player FK, StartDate/EndDate (validated), MatchesSuspended
- **Article**: Slug (unique), Tags (PostgreSQL text[]), PublishedAt
- **Sponsor**: Tier/Placement enums, DisplayOrder, IsActive
- **PageLayoutConfig**: PageName + ComponentName composite unique, ConfigJson (jsonb)

All entities inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt). `LeagueDbContext.SaveChangesAsync` auto-updates `UpdatedAt`.

## Domain Services

**StandingsCalculator**: Pure static class in Domain layer. Computes league table from matches. Tie-break order: Points → GD → GF → Name. Returns `TeamStanding` records with position, points, form (last 5 results as "W,D,L,W,W").

**SlugGenerator**: Creates URL-safe slugs from article titles.

Both services have zero dependencies and are fully unit-testable.

## CORS Configuration

API allows requests from Angular dev server:
```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

## Validation

FluentValidation is wired into MediatR pipeline. Validators are auto-discovered from Application assembly. Validation failures throw `Application.Common.Exceptions.ValidationException` which should be caught by global exception middleware (if implemented) and returned as HTTP 422.

## API Documentation

Swagger UI available in Development mode at `/openapi/v1.json` via SwaggerUI endpoint.

## Common Patterns

### Adding a new entity
1. Create entity in `iDiski.Domain/Entities/`
2. Add `DbSet<T>` to `ILeagueDbContext` and `LeagueDbContext`
3. Configure entity in `LeagueDbContext.OnModelCreating`
4. Create migration: `dotnet ef migrations add Add{Entity} --project iDiski.Infrastructure --startup-project iDiski.Api`
5. Add DTOs, Commands, Queries in `iDiski.Application/{Entity}/`
6. Create controller in `iDiski.Api/Controllers/`

### Creating a new feature
1. Define Command/Query record in `iDiski.Application/{Feature}/Commands/` or `Queries/`
2. Add FluentValidation validator class if needed
3. Add handler implementing `IRequestHandler<TRequest, TResponse>`
4. Create controller endpoint that dispatches via `Sender.Send(command)`

### Testing business logic
- Domain services are pure functions - test directly
- Application handlers can be tested by mocking `ILeagueDbContext`
- Integration tests should hit LeagueDbContext with in-memory or test PostgreSQL database
