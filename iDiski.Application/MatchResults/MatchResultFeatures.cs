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
    int?        KnockoutRoundSize,
    string?     RoundName
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
                m.KnockoutRoundSize == null
                    ? null
                    : m.KnockoutRoundSize == 2
                        ? "Final"
                        : m.KnockoutRoundSize == 4
                            ? "Semi-final"
                            : m.KnockoutRoundSize == 8
                                ? "Quarter-final"
                                : "Round of " + m.KnockoutRoundSize));

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
                m.KnockoutRoundSize == null
                    ? null
                    : m.KnockoutRoundSize == 2
                        ? "Final"
                        : m.KnockoutRoundSize == 4
                            ? "Semi-final"
                            : m.KnockoutRoundSize == 8
                                ? "Quarter-final"
                                : "Round of " + m.KnockoutRoundSize)))
            .FirstOrDefaultAsync(cancellationToken);

        return match ?? throw new NotFoundException(nameof(MatchResult), request.Id);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreateMatchResultCommand(
    DateTime MatchDate,
    int      MatchweekNumber,
    int      Season,
    Guid     HomeTeamId,
    Guid     AwayTeamId,
    string?  Venue,
    string?  Referee
) : IRequest<Guid>;

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
        var match = new MatchResult
        {
            MatchDate       = request.MatchDate,
            MatchweekNumber = request.MatchweekNumber,
            Season          = request.Season,
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

/// <summary>Updates the score and status of a match — used when submitting final results.</summary>
public sealed record UpdateMatchScoreCommand(
    Guid        Id,
    int         HomeScore,
    int         AwayScore,
    MatchStatus Status,
    string?     Notes
) : IRequest;

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

        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.Status    = request.Status;
        match.Notes     = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
