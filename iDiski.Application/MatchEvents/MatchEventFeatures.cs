using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.MatchEvents;

// ══════════════════════════════════════════════════════════════════════════════
// RECORD MATCH EVENTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class RecordMatchEventsCommandValidator : AbstractValidator<Commands.RecordMatchEventsCommand>
{
    public RecordMatchEventsCommandValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();

        RuleForEach(x => x.Events).ChildRules(events =>
        {
            events.RuleFor(e => e.PlayerId).NotEmpty();
            events.RuleFor(e => e.EventType)
                .Must(type => Enum.TryParse<EventType>(type, out _))
                .WithMessage("EventType must be: Goal, Assist, YellowCard, RedCard, OwnGoal, or Substitution");
            events.RuleFor(e => e.Minute)
                .InclusiveBetween(1, 120)
                .WithMessage("Minute must be between 1 and 120");
        });
    }
}

public sealed class RecordMatchEventsCommandHandler
    : IRequestHandler<Commands.RecordMatchEventsCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public RecordMatchEventsCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.RecordMatchEventsCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify match exists and get team IDs
        var match = await _db.MatchResults
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Match with ID {request.MatchId} not found.");

        // 2. Delete existing events for this match (allows re-entry)
        var existingEvents = await _db.MatchEvents
            .Where(e => e.MatchId == request.MatchId)
            .ToListAsync(cancellationToken);

        if (existingEvents.Any())
        {
            // Reverse the stats from existing events before deleting
            await ReversePlayerStats(existingEvents, cancellationToken);
            _db.MatchEvents.RemoveRange(existingEvents);
        }

        // 3. Validate all players belong to either home or away team
        var playerIds = request.Events.Select(e => e.PlayerId).Distinct().ToList();
        var players = await _db.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var eventInput in request.Events)
        {
            var player = players.FirstOrDefault(p => p.Id == eventInput.PlayerId)
                ?? throw new KeyNotFoundException($"Player with ID {eventInput.PlayerId} not found.");

            if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
            {
                throw new InvalidOperationException(
                    $"Player {player.FirstName} {player.LastName} does not belong to either team in this match.");
            }
        }

        // 4. Create new match events
        foreach (var eventInput in request.Events)
        {
            if (!Enum.TryParse<EventType>(eventInput.EventType, out var eventType))
                continue;

            var matchEvent = new MatchEvent
            {
                MatchId = request.MatchId,
                PlayerId = eventInput.PlayerId,
                EventType = eventType,
                Minute = eventInput.Minute,
                AdditionalInfo = eventInput.AdditionalInfo
            };

            _db.MatchEvents.Add(matchEvent);
        }

        // 5. Update player cumulative stats
        await UpdatePlayerStats(request.Events, players, cancellationToken);

        // 6. Check and create suspensions based on card rules
        await CheckAndCreateSuspensions(request.Events, players, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task ReversePlayerStats(
        List<MatchEvent> events,
        CancellationToken cancellationToken)
    {
        var playerIds = events.Select(e => e.PlayerId).Distinct();
        var players = await _db.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var matchEvent in events)
        {
            var player = players.FirstOrDefault(p => p.Id == matchEvent.PlayerId);
            if (player == null) continue;

            switch (matchEvent.EventType)
            {
                case EventType.Goal:
                    player.Goals = Math.Max(0, player.Goals - 1);
                    break;
                case EventType.Assist:
                    player.Assists = Math.Max(0, player.Assists - 1);
                    break;
                case EventType.YellowCard:
                    player.YellowCards = Math.Max(0, player.YellowCards - 1);
                    break;
                case EventType.RedCard:
                    player.RedCards = Math.Max(0, player.RedCards - 1);
                    break;
            }
        }
    }

    private async Task UpdatePlayerStats(
        List<Commands.MatchEventInput> events,
        List<Player> players,
        CancellationToken cancellationToken)
    {
        foreach (var eventInput in events)
        {
            var player = players.FirstOrDefault(p => p.Id == eventInput.PlayerId);
            if (player == null) continue;

            if (!Enum.TryParse<EventType>(eventInput.EventType, out var eventType))
                continue;

            switch (eventType)
            {
                case EventType.Goal:
                    player.Goals++;
                    break;
                case EventType.Assist:
                    player.Assists++;
                    break;
                case EventType.YellowCard:
                    player.YellowCards++;
                    break;
                case EventType.RedCard:
                    player.RedCards++;
                    break;
            }
        }

        await Task.CompletedTask;
    }

    private async Task CheckAndCreateSuspensions(
        List<Commands.MatchEventInput> events,
        List<Player> players,
        CancellationToken cancellationToken)
    {
        // Hardcoded suspension rules (can be made configurable later)
        // 5 yellow cards = 1 match suspension
        // 10 yellow cards = 2 match suspension
        // 1 red card = 3 match suspension

        foreach (var player in players)
        {
            var yellowCards = player.YellowCards;
            var redCards = player.RedCards;

            // Check for yellow card thresholds
            if (yellowCards == 5)
            {
                await CreateSuspension(player, "5 yellow cards", 1, cancellationToken);
            }
            else if (yellowCards == 10)
            {
                await CreateSuspension(player, "10 yellow cards", 2, cancellationToken);
            }

            // Check for red card
            var redCardEvents = events.Count(e =>
                e.PlayerId == player.Id &&
                e.EventType == EventType.RedCard.ToString());

            if (redCardEvents > 0)
            {
                await CreateSuspension(player, "Red card", 3, cancellationToken);
            }
        }

        await Task.CompletedTask;
    }

    private async Task CreateSuspension(
        Player player,
        string reason,
        int matchesSuspended,
        CancellationToken cancellationToken)
    {
        // Check if suspension already exists for this reason recently
        var existingSuspension = await _db.Suspensions
            .Where(s => s.PlayerId == player.Id &&
                       s.Reason == reason &&
                       s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSuspension != null)
            return; // Already suspended for this reason

        var suspension = new Suspension
        {
            PlayerId = player.Id,
            Reason = reason,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(matchesSuspended * 7), // Rough estimate
            MatchesSuspended = matchesSuspended,
            IsActive = true,
            AppliedByUser = "System"
        };

        _db.Suspensions.Add(suspension);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET MATCH EVENTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetMatchEventsQueryHandler
    : IRequestHandler<Queries.GetMatchEventsQuery, IReadOnlyList<MatchEventDto>>
{
    private readonly ILeagueDbContext _db;

    public GetMatchEventsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<MatchEventDto>> Handle(
        Queries.GetMatchEventsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.MatchEvents
            .Where(e => e.MatchId == request.MatchId)
            .OrderBy(e => e.Minute)
            .Select(e => new MatchEventDto(
                e.Id,
                e.MatchId,
                e.PlayerId,
                e.Player.FirstName + " " + e.Player.LastName,
                e.Player.Team.Name,
                e.EventType.ToString(),
                e.Minute,
                e.AdditionalInfo
            ))
            .ToListAsync(cancellationToken);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET PLAYER MATCH EVENTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetPlayerMatchEventsQueryHandler
    : IRequestHandler<Queries.GetPlayerMatchEventsQuery, IReadOnlyList<MatchEventDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPlayerMatchEventsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<MatchEventDto>> Handle(
        Queries.GetPlayerMatchEventsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.MatchEvents
            .Where(e => e.PlayerId == request.PlayerId);

        if (request.Season.HasValue)
        {
            query = query.Where(e => e.Match.Season == request.Season.Value);
        }

        return await query
            .OrderByDescending(e => e.Match.MatchDate)
            .ThenBy(e => e.Minute)
            .Select(e => new MatchEventDto(
                e.Id,
                e.MatchId,
                e.PlayerId,
                e.Player.FirstName + " " + e.Player.LastName,
                e.Player.Team.Name,
                e.EventType.ToString(),
                e.Minute,
                e.AdditionalInfo
            ))
            .ToListAsync(cancellationToken);
    }
}
