using System.Linq.Expressions;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Competitions.Queries;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Competitions;

// ═════════════════════════════════════════════════════════════════════════════
// VALIDATION
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CreateCompetitionCommandValidator
    : AbstractValidator<Commands.CreateCompetitionCommand>
{
    public CreateCompetitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShortCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Season).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Description).MaximumLength(1000);

        // Two is the smallest thing that can be played at all; the ceiling is only there to
        // catch a typo that would otherwise become a bracket of ten thousand.
        RuleFor(x => x.MaxTeams)
            .InclusiveBetween(2, 512)
            .When(x => x.MaxTeams.HasValue)
            .WithMessage("A competition holds between 2 and 512 clubs.");
    }
}

public sealed class UpdateCompetitionCommandValidator
    : AbstractValidator<Commands.UpdateCompetitionCommand>
{
    public UpdateCompetitionCommandValidator()
    {
        RuleFor(x => x.CompetitionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShortCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(1000);

        // Two is the smallest thing that can be played at all; the ceiling is only there to
        // catch a typo that would otherwise become a bracket of ten thousand.
        RuleFor(x => x.MaxTeams)
            .InclusiveBetween(2, 512)
            .When(x => x.MaxTeams.HasValue)
            .WithMessage("A competition holds between 2 and 512 clubs.");
    }
}

/// <summary>
/// How a gender reads in a refusal. Shared because two places turn a club away over it: adding
/// one by hand, and starting a competition off with a whole division's clubs.
/// </summary>
internal static class GenderWords
{
    public static string Describe(Gender gender) => gender.ToString().ToLowerInvariant();
}

// ═════════════════════════════════════════════════════════════════════════════
// CREATE
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CreateCompetitionCommandHandler
    : IRequestHandler<Commands.CreateCompetitionCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateCompetitionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        Commands.CreateCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var shortCode = request.ShortCode.Trim().ToUpperInvariant();

        // Unique across the season. Nothing runs a competition, so there is no division to be
        // unique within any more.
        var clash = await _db.Competitions.AnyAsync(
            c => c.Season == request.Season && c.ShortCode == shortCode,
            cancellationToken);

        if (clash)
        {
            throw new InvalidOperationException(
                $"A competition with the short code {shortCode} is already being played in "
                + $"{request.Season}.");
        }

        // Optionally started off with a whole division's clubs. Loaded up front so both the
        // gender rule and the size limit are answered before anything is written.
        var starters = new List<Team>();

        if (request.EnterTeamsFromDivisionId.HasValue)
        {
            var division = await _db.Divisions
                .Include(d => d.Teams)
                .FirstOrDefaultAsync(
                    d => d.Id == request.EnterTeamsFromDivisionId.Value, cancellationToken)
                ?? throw new NotFoundException(
                    nameof(Division), request.EnterTeamsFromDivisionId.Value);

            // Same rule as entering a club by hand: a recorded contradiction is refused, an
            // unrecorded gender is not.
            if (division.Gender is Gender startersGender && startersGender != request.Gender)
            {
                throw new InvalidOperationException(
                    $"{division.Name} is a {GenderWords.Describe(startersGender)} division "
                    + $"and this is a {GenderWords.Describe(request.Gender)} competition. Its "
                    + "clubs cannot be entered.");
            }

            // Entering everybody into a competition that holds fewer would mean choosing which
            // clubs to drop, and that is the organiser's decision rather than ours.
            if (request.MaxTeams.HasValue && division.Teams.Count > request.MaxTeams.Value)
            {
                throw new InvalidOperationException(
                    $"{division.Name} has {division.Teams.Count} clubs and this competition "
                    + $"holds {request.MaxTeams.Value}. Create it empty and choose who plays.");
            }

            starters.AddRange(division.Teams);
        }

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ShortCode = shortCode,
            Season = request.Season,
            Format = request.Format,
            Gender = request.Gender,
            AgeGroup = request.AgeGroup?.Trim(),
            MaxTeams = request.MaxTeams,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Competitions.Add(competition);

        foreach (var team in starters)
        {
            _db.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = competition.Id,
                TeamId = team.Id,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return competition.Id;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UPDATE
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UpdateCompetitionCommandHandler
    : IRequestHandler<Commands.UpdateCompetitionCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public UpdateCompetitionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.UpdateCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _db.Competitions
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Competition), request.CompetitionId);

        var shortCode = request.ShortCode.Trim().ToUpperInvariant();

        if (shortCode != competition.ShortCode)
        {
            var clash = await _db.Competitions.AnyAsync(
                c => c.Season == competition.Season
                     && c.ShortCode == shortCode
                     && c.Id != competition.Id,
                cancellationToken);

            if (clash)
            {
                throw new InvalidOperationException(
                    $"Another competition already uses the short code {shortCode} in "
                    + $"{competition.Season}.");
            }
        }

        // Changing the shape of something already drawn would leave the fixtures describing a
        // competition that no longer exists — a bracket in a league, or a table nothing feeds.
        if (request.Format != competition.Format)
        {
            var drawn = await _db.MatchResults
                .AnyAsync(m => m.CompetitionId == competition.Id, cancellationToken);

            if (drawn)
            {
                throw new InvalidOperationException(
                    $"{competition.Name} has already been drawn up, so how it is played cannot "
                    + "change. Delete its fixtures first.");
            }

            competition.Format = request.Format;
        }

        // Shrinking a competition below the clubs already in it would leave entrants with no
        // place, so the organiser withdraws somebody first and decides who.
        if (request.MaxTeams.HasValue)
        {
            var entered = await _db.CompetitionEntries
                .CountAsync(e => e.CompetitionId == competition.Id, cancellationToken);

            if (entered > request.MaxTeams.Value)
            {
                throw new InvalidOperationException(
                    $"{competition.Name} already has {entered} clubs entered, so it cannot be "
                    + $"cut to {request.MaxTeams.Value}. Withdraw somebody first.");
            }
        }

        // Changing who a competition is for, with clubs already entered, would leave sides in
        // something they are not eligible for.
        if (request.Gender != competition.Gender)
        {
            var entered = await _db.CompetitionEntries
                .CountAsync(e => e.CompetitionId == competition.Id, cancellationToken);

            if (entered > 0)
            {
                throw new InvalidOperationException(
                    $"{competition.Name} already has {entered} clubs entered, so who it is for "
                    + "cannot change. Withdraw them first.");
            }

            competition.Gender = request.Gender;
        }

        competition.Name = request.Name.Trim();
        competition.AgeGroup = request.AgeGroup?.Trim();
        competition.MaxTeams = request.MaxTeams;
        competition.ShortCode = shortCode;
        competition.StartDate = request.StartDate;
        competition.EndDate = request.EndDate;
        competition.Description = request.Description;
        competition.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DELETE
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DeleteCompetitionCommandHandler
    : IRequestHandler<Commands.DeleteCompetitionCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public DeleteCompetitionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.DeleteCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _db.Competitions
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Competition), request.CompetitionId);

        var fixtures = await _db.MatchResults
            .CountAsync(m => m.CompetitionId == competition.Id, cancellationToken);

        // Refused here rather than left to the foreign key, so the reason is a sentence rather
        // than a constraint violation.
        if (fixtures > 0)
        {
            throw new InvalidOperationException(
                $"{competition.Name} has {fixtures} fixtures. Delete those first if this "
                + "competition really is being abandoned.");
        }

        // Entries go with it: they describe a place in this competition and nothing else.
        var entries = await _db.CompetitionEntries
            .Where(e => e.CompetitionId == competition.Id)
            .ToListAsync(cancellationToken);

        _db.CompetitionEntries.RemoveRange(entries);
        _db.Competitions.Remove(competition);

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// ENTERING AND WITHDRAWING
// ═════════════════════════════════════════════════════════════════════════════

public sealed class EnterTeamCommandHandler : IRequestHandler<Commands.EnterTeamCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public EnterTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.EnterTeamCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _db.Competitions
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Competition), request.CompetitionId);

        var team = await _db.Teams
            .Include(t => t.Division)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.TeamId);

        // ── The one entry that is never allowed ───────────────────────────────
        //
        // A club from another division is welcome — a sponsor's cup is meant to invite them.
        // A club of another gender is not, and it is refused here rather than later at the
        // draw, because by the time two sides are staring at a fixture list somebody has
        // already been told they are playing.
        //
        // What is refused is a recorded contradiction, not an unanswered question. A
        // division's gender is optional and every division written before this rule existed
        // has it empty; refusing those would leave a whole league unable to enter the
        // competitions it was already playing. The division form asks for it now, so the
        // silence is historical rather than something new being created.
        if (team.Division?.Gender is Gender divisionGender
            && divisionGender != competition.Gender)
        {
            throw new InvalidOperationException(
                $"{team.Name} plays in a "
                + $"{GenderWords.Describe(divisionGender)} division and {competition.Name} is a "
                + $"{GenderWords.Describe(competition.Gender)} competition. "
                + "They cannot be entered into it.");
        }

        var already = await _db.CompetitionEntries.AnyAsync(
            e => e.CompetitionId == competition.Id && e.TeamId == team.Id,
            cancellationToken);

        // Entering twice would give them two places in the draw and two rows in the table.
        // Saying so beats a unique-index violation.
        if (already)
        {
            throw new InvalidOperationException(
                $"{team.Name} is already entered in {competition.Name}.");
        }

        // Full is full. A top-eight cup that quietly accepts a ninth club is a bracket built
        // around a number nobody meant.
        if (competition.MaxTeams.HasValue)
        {
            var entered = await _db.CompetitionEntries
                .CountAsync(e => e.CompetitionId == competition.Id, cancellationToken);

            if (entered >= competition.MaxTeams.Value)
            {
                throw new InvalidOperationException(
                    $"{competition.Name} holds {competition.MaxTeams.Value} clubs and already "
                    + $"has {entered}. Withdraw somebody, or raise the number of teams.");
            }
        }

        // Adding an entrant to a competition already drawn would leave them with a place and
        // no fixtures, which reads as a bug to everybody who sees it.
        var drawn = await _db.MatchResults
            .AnyAsync(m => m.CompetitionId == competition.Id, cancellationToken);

        if (drawn)
        {
            throw new InvalidOperationException(
                $"{competition.Name} has already been drawn up. Entering {team.Name} now would "
                + "leave them with a place and no fixtures — delete the fixtures and draw it "
                + "again if the entry list really has changed.");
        }

        _db.CompetitionEntries.Add(new CompetitionEntry
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            TeamId = team.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

}

public sealed class WithdrawTeamCommandHandler : IRequestHandler<Commands.WithdrawTeamCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public WithdrawTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.WithdrawTeamCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _db.CompetitionEntries
            .FirstOrDefaultAsync(
                e => e.CompetitionId == request.CompetitionId && e.TeamId == request.TeamId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(CompetitionEntry), request.TeamId);

        // Withdrawing after the draw would leave fixtures against a club with no place in the
        // competition, and a table counting results from somebody who is not in it.
        var drawn = await _db.MatchResults.AnyAsync(
            m => m.CompetitionId == request.CompetitionId
                 && (m.HomeTeamId == request.TeamId || m.AwayTeamId == request.TeamId),
            cancellationToken);

        if (drawn)
        {
            throw new InvalidOperationException(
                "This club already has fixtures in this competition, so they cannot simply be "
                + "withdrawn. Delete the fixtures and draw it again.");
        }

        _db.CompetitionEntries.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═════════════════════════════════════════════════════════════════════════════

public sealed class GetCompetitionsQueryHandler
    : IRequestHandler<GetCompetitionsQuery, IReadOnlyList<CompetitionDto>>
{
    private readonly ILeagueDbContext _db;

    public GetCompetitionsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<CompetitionDto>> Handle(
        GetCompetitionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Competitions.AsNoTracking();

        // A division no longer owns competitions, so this asks the only question left that
        // means anything: which ones are this division's clubs playing in?
        if (request.DivisionId.HasValue)
        {
            query = query.Where(
                c => c.Entries.Any(e => e.Team.DivisionId == request.DivisionId.Value));
        }

        if (request.Season.HasValue)
            query = query.Where(c => c.Season == request.Season.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(c => c.Season)
            .ThenBy(c => c.Name)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Shared with the single-competition query so the list and the detail cannot drift — the
    /// kind of disagreement that shows one status on a card and another on the page behind it.
    ///
    /// An expression rather than a method, because a method call inside a projection is not
    /// something EF can translate: it would compile and then fail at the first request.
    /// </summary>
    internal static readonly Expression<Func<Competition, CompetitionDto>> Projection = c =>
        new CompetitionDto(
        c.Id,
        c.Name,
        c.ShortCode,
        c.Season,
        c.Format,
        c.Gender,
        c.AgeGroup,
        c.MaxTeams,
        c.StartDate,
        c.EndDate,
        c.Description,
        c.IsActive,
        c.Entries.Count,
        c.Entries.Select(e => e.Team.DivisionId).Distinct().Count(),
        c.Matches.Count,
        c.Matches.Count(m => m.Status == MatchStatus.Completed),
        c.Matches.Count(m =>
            m.Status == MatchStatus.Scheduled ||
            m.Status == MatchStatus.InProgress ||
            m.Status == MatchStatus.Postponed),
        c.Matches.Any(m =>
            m.Stage == MatchStage.Knockout &&
            m.NextMatchId == null &&
            m.Status == MatchStatus.Completed)
    );
}

public sealed class GetCompetitionByIdQueryHandler
    : IRequestHandler<GetCompetitionByIdQuery, CompetitionDto?>
{
    private readonly ILeagueDbContext _db;

    public GetCompetitionByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<CompetitionDto?> Handle(
        GetCompetitionByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Competitions
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(GetCompetitionsQueryHandler.Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class GetCompetitionEntrantsQueryHandler
    : IRequestHandler<GetCompetitionEntrantsQuery, IReadOnlyList<CompetitionEntrantDto>>
{
    private readonly ILeagueDbContext _db;

    public GetCompetitionEntrantsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<CompetitionEntrantDto>> Handle(
        GetCompetitionEntrantsQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.CompetitionEntries
            .AsNoTracking()
            .Where(e => e.CompetitionId == request.CompetitionId)
            .OrderBy(e => e.Team.Name)
            .Select(e => new CompetitionEntrantDto(
                e.TeamId,
                e.Team.Name,
                e.Team.ShortCode,
                e.Team.LogoUrl,
                e.Team.DivisionId,
                e.Team.Division != null ? e.Team.Division.Name : null
            ))
            .ToListAsync(cancellationToken);
    }
}
