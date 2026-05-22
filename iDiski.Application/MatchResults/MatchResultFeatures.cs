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
    Guid        HomeTeamId,
    string      HomeTeamName,
    string?     HomeTeamLogo,
    string      HomeTeamShortCode,
    Guid        AwayTeamId,
    string      AwayTeamName,
    string?     AwayTeamLogo,
    string      AwayTeamShortCode,
    string?     Notes
);

// ═════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Returns a paginated fixture/results list, optionally filtered.</summary>
public sealed record GetFixturesQuery(
    int     Season,
    int?    Matchweek   = null,
    Guid?   TeamId      = null,
    MatchStatus? Status = null,
    int     PageNumber  = 1,
    int     PageSize    = 20
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
                m.HomeTeamId, m.HomeTeam.Name, m.HomeTeam.LogoUrl, m.HomeTeam.ShortCode,
                m.AwayTeamId, m.AwayTeam.Name, m.AwayTeam.LogoUrl, m.AwayTeam.ShortCode,
                m.Notes));

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
                m.HomeTeamId, m.HomeTeam.Name, m.HomeTeam.LogoUrl, m.HomeTeam.ShortCode,
                m.AwayTeamId, m.AwayTeam.Name, m.AwayTeam.LogoUrl, m.AwayTeam.ShortCode,
                m.Notes))
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
