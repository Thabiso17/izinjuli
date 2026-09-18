using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Models;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.MatchResults;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed record MatchResultDto(
    Guid        Id,
    DateTime    MatchDate,
    int         MatchweekNumber,
    int         Season,
    string?     Venue,
    string?     Referee,
    MatchStatus Status,
    string      ScoreDisplay,
    int         HomeScore,
    int         AwayScore,
    // Nullable throughout, because a knockout fixture exists before its teams do: the
    // semi-final is scheduled while the quarter-finals are still being played.
    Guid?       HomeTeamId,
    string?     HomeTeamName,
    string?     HomeTeamLogo,
    string?     HomeTeamShortCode,
    Guid?       AwayTeamId,
    string?     AwayTeamName,
    string?     AwayTeamLogo,
    string?     AwayTeamShortCode,
    string?     Notes,
    // What this fixture is part of, and the only thing it belongs to. There is no division
    // here: a cup tie between a Division 1 club and a Division 3 club is played in neither,
    // and the two clubs below say where each of them comes from.
    Guid?       CompetitionId,
    string?     CompetitionName,
    // Where this fixture sits in its competition.
    MatchStage  Stage,
    string?     GroupName,
    // The round's name is left to the reader rather than built here. Naming it in the
    // projection means a CASE plus a string concatenation of an integer, which is the kind of
    // expression that translates on one provider and throws on another — and it would throw
    // while loading the whole fixtures list.
    int?        KnockoutRoundSize,
    // A shootout was already being recorded and never read back, so once entered it was
    // invisible: the fixtures list could not show it and reopening the fixture lost it.
    int?        HomePenalties,
    int?        AwayPenalties
);

// ═════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Returns a paginated fixture/results list, optionally filtered.</summary>
/// <param name="DivisionId">
/// Narrow to the fixtures a division's clubs are playing — in anything, including a cup they
/// contest against clubs from elsewhere. A fixture has no division of its own to filter on.
/// </param>
public sealed record GetFixturesQuery(
    int     Season,
    int?    Matchweek   = null,
    Guid?   TeamId      = null,
    MatchStatus? Status = null,
    int     PageNumber  = 1,
    int     PageSize    = 20,
    Guid?   DivisionId  = null,
    // Narrow to one competition — the league, or the cup, rather than everything at once.
    Guid?   CompetitionId = null
) : IRequest<PaginatedList<MatchResultDto>>;

public sealed class GetFixturesQueryHandler
    : IRequestHandler<GetFixturesQuery, PaginatedList<MatchResultDto>>
{
    private readonly ILeagueDbContext _db;

    public GetFixturesQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<PaginatedList<MatchResultDto>> Handle(
        GetFixturesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.MatchResults
            .AsNoTracking()
            .Where(m => m.Season == request.Season);

        if (request.Matchweek.HasValue)
            query = query.Where(m => m.MatchweekNumber == request.Matchweek.Value);

        if (request.TeamId.HasValue)
            query = query.Where(m =>
                m.HomeTeamId == request.TeamId.Value ||
                m.AwayTeamId == request.TeamId.Value);

        if (request.Status.HasValue)
            query = query.Where(m => m.Status == request.Status.Value);

        // A fixture has no division of its own. Asking for a division's fixtures means the
        // matches its clubs are playing — which is what the filter was always used to mean,
        // and which now also finds them in a cup contested across several divisions.
        if (request.DivisionId.HasValue)
        {
            query = query.Where(m =>
                (m.HomeTeam != null && m.HomeTeam.DivisionId == request.DivisionId.Value) ||
                (m.AwayTeam != null && m.AwayTeam.DivisionId == request.DivisionId.Value));
        }

        if (request.CompetitionId.HasValue)
            query = query.Where(m => m.CompetitionId == request.CompetitionId.Value);

        var projected = query
            .OrderBy(m => m.MatchDate)
            .Select(m => new MatchResultDto(
                m.Id,
                m.MatchDate,
                m.MatchweekNumber,
                m.Season,
                m.Venue,
                m.Referee,
                m.Status,
                m.Status == MatchStatus.Scheduled ? "vs" : $"{m.HomeScore} – {m.AwayScore}",
                m.HomeScore,
                m.AwayScore,
                m.HomeTeamId,
                m.HomeTeam != null ? m.HomeTeam.Name : null,
                m.HomeTeam != null ? m.HomeTeam.LogoUrl : null,
                m.HomeTeam != null ? m.HomeTeam.ShortCode : null,
                m.AwayTeamId,
                m.AwayTeam != null ? m.AwayTeam.Name : null,
                m.AwayTeam != null ? m.AwayTeam.LogoUrl : null,
                m.AwayTeam != null ? m.AwayTeam.ShortCode : null,
                m.Notes,
                m.CompetitionId,
                m.Competition != null ? m.Competition.Name : null,
                m.Stage,
                m.GroupName,
                m.KnockoutRoundSize,
                m.HomePenalties,
                m.AwayPenalties));

        return await PaginatedList<MatchResultDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public sealed record GetMatchByIdQuery(Guid Id) : IRequest<MatchResultDto>;

public sealed class GetMatchByIdQueryHandler
    : IRequestHandler<GetMatchByIdQuery, MatchResultDto>
{
    private readonly ILeagueDbContext _db;

    public GetMatchByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<MatchResultDto> Handle(
        GetMatchByIdQuery request,
        CancellationToken cancellationToken)
    {
        var match = await _db.MatchResults
            .AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.Id == request.Id)
            .Select(m => new MatchResultDto(
                m.Id, m.MatchDate, m.MatchweekNumber, m.Season,
                m.Venue, m.Referee, m.Status,
                m.Status == MatchStatus.Scheduled ? "vs" : $"{m.HomeScore} – {m.AwayScore}",
                m.HomeScore, m.AwayScore,
                m.HomeTeamId,
                m.HomeTeam != null ? m.HomeTeam.Name : null,
                m.HomeTeam != null ? m.HomeTeam.LogoUrl : null,
                m.HomeTeam != null ? m.HomeTeam.ShortCode : null,
                m.AwayTeamId,
                m.AwayTeam != null ? m.AwayTeam.Name : null,
                m.AwayTeam != null ? m.AwayTeam.LogoUrl : null,
                m.AwayTeam != null ? m.AwayTeam.ShortCode : null,
                m.Notes,
                m.CompetitionId,
                m.Competition != null ? m.Competition.Name : null,
                m.Stage,
                m.GroupName,
                m.KnockoutRoundSize,
                m.HomePenalties,
                m.AwayPenalties))
            .FirstOrDefaultAsync(cancellationToken);

        return match ?? throw new NotFoundException(nameof(MatchResult), request.Id);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Adds a fixture to a competition by hand — this club plays that one, on this date.
///
/// Drawing a competition up is the organiser's job, so this is scoped to the competition
/// itself: whoever was assigned to run it, and a SuperAdmin. Not to either club — deciding
/// who meets whom is running the competition, not administering the sides.
///
/// The season is the competition's rather than an argument: a fixture cannot belong to a
/// different year from the competition it is part of.
/// </summary>
public sealed record CreateMatchResultCommand(
    Guid     CompetitionId,
    DateTime MatchDate,
    int      MatchweekNumber,
    Guid     HomeTeamId,
    Guid     AwayTeamId,
    string?  Venue,
    string?  Referee
) : IRequest<Guid>, IRequireCompetitionAccess;

public sealed class CreateMatchResultCommandValidator
    : AbstractValidator<CreateMatchResultCommand>
{
    public CreateMatchResultCommandValidator()
    {
        RuleFor(x => x.CompetitionId).NotEmpty();
        RuleFor(x => x.HomeTeamId).NotEmpty();
        RuleFor(x => x.AwayTeamId).NotEmpty();
        RuleFor(x => x.HomeTeamId)
            .NotEqual(x => x.AwayTeamId)
            .WithMessage("Home and away team cannot be the same.");
        RuleFor(x => x.MatchweekNumber).GreaterThan(0);
        RuleFor(x => x.MatchDate).GreaterThan(DateTime.UtcNow.AddYears(-10));
    }
}

public sealed class CreateMatchResultCommandHandler
    : IRequestHandler<CreateMatchResultCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateMatchResultCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        CreateMatchResultCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _db.Competitions
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Competition), request.CompetitionId);

        var home = await _db.Teams
            .Include(t => t.Division)
            .FirstOrDefaultAsync(t => t.Id == request.HomeTeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.HomeTeamId);

        var away = await _db.Teams
            .Include(t => t.Division)
            .FirstOrDefaultAsync(t => t.Id == request.AwayTeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.AwayTeamId);

        // Both clubs have to be in this competition. That is the rule now, and it is a better
        // one than "in the same division": a cup invited from elsewhere is a real competition
        // with real entrants, while two clubs who happen to share a division but were never
        // entered still have no business being drawn against each other here.
        var entered = await _db.CompetitionEntries
            .Where(e => e.CompetitionId == competition.Id)
            .Select(e => e.TeamId)
            .ToListAsync(cancellationToken);

        var missing = new[] { home, away }.Where(t => !entered.Contains(t.Id)).ToList();

        if (missing.Count > 0)
        {
            var names = string.Join(" and ", missing.Select(t => t.Name));

            throw new InvalidOperationException(
                $"{names} {(missing.Count == 1 ? "is" : "are")} not entered in "
                + $"{competition.Name}. Enter them before drawing this fixture.");
        }

        // A backstop rather than the main guard: entering a club of the wrong gender is already
        // refused, so reaching here would mean that check had been got around. This is the one
        // mistake nobody would want to explain afterwards, so it is worth checking twice.
        if (home.Division?.Gender != away.Division?.Gender)
        {
            throw new InvalidOperationException(
                $"{home.Name} plays in a {home.Division?.Gender?.ToString().ToLowerInvariant()} "
                + $"division and {away.Name} in a "
                + $"{away.Division?.Gender?.ToString().ToLowerInvariant()} one. "
                + "They cannot be drawn against each other.");
        }

        var match = new MatchResult
        {
            MatchDate       = request.MatchDate,
            MatchweekNumber = request.MatchweekNumber,
            Season          = competition.Season,
            CompetitionId   = competition.Id,
            HomeTeamId      = request.HomeTeamId,
            AwayTeamId      = request.AwayTeamId,
            Venue           = request.Venue,
            Referee         = request.Referee,
            Status          = MatchStatus.Scheduled
        };

        _db.MatchResults.Add(match);
        await _db.SaveChangesAsync(cancellationToken);

        return match.Id;
    }
}

/// <summary>
/// Updates the score and status of a match — used when submitting final results.
///
/// Scoped to the division the fixture belongs to. This is the one that mattered most: entering
/// a result is not just a row, it moves a table and, in a knockout, puts a club into the next
/// round. Any division admin could previously do that to any competition in the league.
/// </summary>
/// <param name="HomePenalties">
/// Shootout score, for a knockout tie level after ninety minutes. A league match is happy to
/// end in a draw; a bracket fixture has to send somebody through.
/// </param>
public sealed record UpdateMatchScoreCommand(
    Guid        Id,
    int         HomeScore,
    int         AwayScore,
    MatchStatus Status,
    string?     Notes,
    int?        HomePenalties = null,
    int?        AwayPenalties = null
) : IRequest, IRequireMatchAccess
{
    Guid IRequireMatchAccess.MatchId => Id;
}

public sealed class UpdateMatchScoreCommandValidator
    : AbstractValidator<UpdateMatchScoreCommand>
{
    public UpdateMatchScoreCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.HomeScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AwayScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status)
            .Must(s => s != MatchStatus.Scheduled)
            .WithMessage("Use CreateMatch to schedule. This endpoint updates results only.");
    }
}

public sealed class UpdateMatchScoreCommandHandler
    : IRequestHandler<UpdateMatchScoreCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdateMatchScoreCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UpdateMatchScoreCommand request, CancellationToken cancellationToken)
    {
        var match = await _db.MatchResults.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(MatchResult), request.Id);

        match.HomeScore     = request.HomeScore;
        match.AwayScore     = request.AwayScore;
        match.Status        = request.Status;
        match.Notes         = request.Notes;
        match.HomePenalties = request.HomePenalties;
        match.AwayPenalties = request.AwayPenalties;

        if (match.Stage == MatchStage.Knockout && request.Status == MatchStatus.Completed)
        {
            await AdvanceWinnerAsync(match, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Puts the winner into the fixture they have earned.
    ///
    /// Without this a bracket is a list of fixtures rather than a tournament: the semi-final
    /// would sit empty however many quarter-finals were played. Entering the result is the only
    /// moment the next round can be filled in, so it happens here rather than being left to
    /// somebody to do by hand.
    /// </summary>
    private async Task AdvanceWinnerAsync(MatchResult match, CancellationToken cancellationToken)
    {
        if (match.HomeTeamId is null || match.AwayTeamId is null)
        {
            throw new InvalidOperationException(
                "This fixture is still waiting on the round before it, so it cannot have a "
                + "result yet.");
        }

        var winner = WinnerOf(match)
            ?? throw new InvalidOperationException(
                "A knockout tie has to produce a winner. Record the shootout score, or the "
                + "result of extra time, so it is clear who goes through.");

        if (match.NextMatchId is null) return; // The final: nothing beyond it.

        var next = await _db.MatchResults.FindAsync([match.NextMatchId.Value], cancellationToken)
            ?? throw new NotFoundException(nameof(MatchResult), match.NextMatchId.Value);

        // Re-entering a result replaces the team it put through last time rather than adding
        // to it, so correcting a mistyped score does not leave the wrong club in the next round.
        if (match.NextMatchSlot == MatchSlot.Away) next.AwayTeamId = winner;
        else next.HomeTeamId = winner;
    }

    /// <summary>
    /// Who goes through, or null if the tie is still level. Ninety minutes first, then the
    /// shootout — a side that lost on the day but won on penalties is the one that advances.
    /// </summary>
    private static Guid? WinnerOf(MatchResult match)
    {
        if (match.HomeScore != match.AwayScore)
            return match.HomeScore > match.AwayScore ? match.HomeTeamId : match.AwayTeamId;

        if (match.HomePenalties is null || match.AwayPenalties is null) return null;
        if (match.HomePenalties == match.AwayPenalties) return null;

        return match.HomePenalties > match.AwayPenalties ? match.HomeTeamId : match.AwayTeamId;
    }
}
