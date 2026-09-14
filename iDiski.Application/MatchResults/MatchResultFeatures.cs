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
    // The client has always declared these two and the projection never supplied them, so the
    // division badge on the fixtures list had nothing to render. Required rather than
    // defaulted: an optional argument cannot be omitted inside an expression tree, and a
    // projection that tried would not compile — which is how the last one of these was found.
    Guid?       DivisionId,
    string?     DivisionName,
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
/// Filter to one division. The matches admin page has always sent this, but neither the
/// endpoint nor this query accepted it, so choosing a division changed nothing on screen.
/// </param>
public sealed record GetFixturesQuery(
    int     Season,
    int?    Matchweek   = null,
    Guid?   TeamId      = null,
    MatchStatus? Status = null,
    int     PageNumber  = 1,
    int     PageSize    = 20,
    Guid?   DivisionId  = null
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

        if (request.DivisionId.HasValue)
            query = query.Where(m => m.DivisionId == request.DivisionId.Value);

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
                m.DivisionId,
                m.Division != null ? m.Division.Name : null,
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
                m.DivisionId,
                m.Division != null ? m.Division.Name : null,
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
/// Scoped to the home club's division. [Authorize(Policy = "CanManageDivisions")] on the
/// endpoint only asks whether the requester is a division admin at all — it does not ask
/// *which* divisions — so without this any division admin could put a fixture into anybody's
/// competition. Checking the home club is enough: the handler refuses a fixture whose two
/// clubs are in different divisions, so either the away club is in the same division or there
/// is no fixture to authorise.
/// </summary>
public sealed record CreateMatchResultCommand(
    DateTime MatchDate,
    int      MatchweekNumber,
    int      Season,
    Guid     HomeTeamId,
    Guid     AwayTeamId,
    string?  Venue,
    string?  Referee
) : IRequest<Guid>, IRequireTeamAccess
{
    Guid IRequireTeamAccess.TeamId => HomeTeamId;
}

public sealed class CreateMatchResultCommandValidator
    : AbstractValidator<CreateMatchResultCommand>
{
    public CreateMatchResultCommandValidator()
    {
        RuleFor(x => x.HomeTeamId).NotEmpty();
        RuleFor(x => x.AwayTeamId).NotEmpty();
        RuleFor(x => x.HomeTeamId)
            .NotEqual(x => x.AwayTeamId)
            .WithMessage("Home and away team cannot be the same.");
        RuleFor(x => x.MatchweekNumber).GreaterThan(0);
        RuleFor(x => x.Season).InclusiveBetween(2000, DateTime.UtcNow.Year + 1);
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
        var home = await _db.Teams
            .Include(t => t.Division)
            .FirstOrDefaultAsync(t => t.Id == request.HomeTeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.HomeTeamId);

        var away = await _db.Teams
            .Include(t => t.Division)
            .FirstOrDefaultAsync(t => t.Id == request.AwayTeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.AwayTeamId);

        // A fixture belongs to the competition both clubs play in. Nothing set this before, so
        // a match made by hand had no division at all: it never appeared in a division's
        // fixture list and never counted towards its table, while looking perfectly saved.
        if (home.DivisionId is null || away.DivisionId is null)
        {
            throw new InvalidOperationException(
                "A club that is not in a division has nobody to play. Put both clubs in a "
                + "division first.");
        }

        if (home.DivisionId != away.DivisionId)
        {
            // Gender is carried by the division, so clubs from different divisions can be of
            // different genders — and a fixture between them is the one mistake here that
            // nobody would want to explain afterwards. It gets said plainly.
            var homeGender = home.Division?.Gender;
            var awayGender = away.Division?.Gender;

            if (homeGender != awayGender)
            {
                throw new InvalidOperationException(
                    $"{home.Name} plays in a {homeGender?.ToString().ToLowerInvariant()} "
                    + $"division and {away.Name} in a {awayGender?.ToString().ToLowerInvariant()} "
                    + "one. They cannot be drawn against each other.");
            }

            throw new InvalidOperationException(
                $"{home.Name} and {away.Name} are in different divisions, so there is no "
                + "competition this fixture belongs to.");
        }

        var match = new MatchResult
        {
            MatchDate       = request.MatchDate,
            MatchweekNumber = request.MatchweekNumber,
            Season          = request.Season,
            DivisionId      = home.DivisionId,
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
