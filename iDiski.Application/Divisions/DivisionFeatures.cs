using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Divisions;

// ══════════════════════════════════════════════════════════════════════════════
// CREATE DIVISION
// ══════════════════════════════════════════════════════════════════════════════

public sealed class CreateDivisionCommandValidator : AbstractValidator<Commands.CreateDivisionCommand>
{
    private readonly ILeagueDbContext _db;

    public CreateDivisionCommandValidator(ILeagueDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Z0-9]+$").WithMessage("ShortCode must be uppercase letters or digits.")
            .MustAsync(BeUniqueShortCode).WithMessage("ShortCode is already taken for this season.");

        RuleFor(x => x.Season)
            .InclusiveBetween(2020, 2099);

        RuleFor(x => x.Gender)
            .Must(g => g == null || g == "Male" || g == "Female" || g == "Mixed")
            .When(x => x.Gender is not null)
            .WithMessage("Gender must be 'Male', 'Female', or 'Mixed'.");
    }

    private async Task<bool> BeUniqueShortCode(
        Commands.CreateDivisionCommand command,
        string shortCode,
        CancellationToken cancellationToken)
        => !await _db.Divisions.AnyAsync(
            d => d.Season == command.Season && d.ShortCode == shortCode,
            cancellationToken);
}

public sealed class CreateDivisionCommandHandler
    : IRequestHandler<Commands.CreateDivisionCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateDivisionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        Commands.CreateDivisionCommand request,
        CancellationToken cancellationToken)
    {
        Gender? gender = request.Gender switch
        {
            "Male" => Gender.Male,
            "Female" => Gender.Female,
            "Mixed" => Gender.Mixed,
            _ => null
        };

        var division = new Division
        {
            Name = request.Name,
            ShortCode = request.ShortCode.ToUpperInvariant(),
            Season = request.Season,
            AgeGroup = request.AgeGroup,
            Gender = gender,
            StartDate = request.StartDate.HasValue ? DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc) : null,
            EndDate = request.EndDate.HasValue ? DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc) : null,
            Description = request.Description
        };

        _db.Divisions.Add(division);
        await _db.SaveChangesAsync(cancellationToken);

        return division.Id;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// UPDATE DIVISION
// ══════════════════════════════════════════════════════════════════════════════

public sealed class UpdateDivisionCommandValidator : AbstractValidator<Commands.UpdateDivisionCommand>
{
    public UpdateDivisionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Z0-9]+$").WithMessage("ShortCode must be uppercase letters or digits.");

        RuleFor(x => x.Season)
            .InclusiveBetween(2020, 2099);

        RuleFor(x => x.Gender)
            .Must(g => g == null || g == "Male" || g == "Female" || g == "Mixed")
            .When(x => x.Gender is not null)
            .WithMessage("Gender must be 'Male', 'Female', or 'Mixed'.");
    }
}

public sealed class UpdateDivisionCommandHandler
    : IRequestHandler<Commands.UpdateDivisionCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public UpdateDivisionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.UpdateDivisionCommand request,
        CancellationToken cancellationToken)
    {
        var division = await _db.Divisions.FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Division with ID {request.Id} not found.");

        Gender? gender = request.Gender switch
        {
            "Male" => Gender.Male,
            "Female" => Gender.Female,
            "Mixed" => Gender.Mixed,
            _ => null
        };

        // Changing the format once fixtures exist would leave them describing a competition
        // that is no longer being played — league fixtures in a bracket with nothing linking
        // them, or a bracket in a league table. The fixtures have to go first.
        division.AgeGroup = request.AgeGroup;
        division.Gender = gender;
        division.IsActive = request.IsActive;
        division.StartDate = request.StartDate.HasValue ? DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc) : null;
        division.EndDate = request.EndDate.HasValue ? DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc) : null;
        division.Description = request.Description;

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// DELETE DIVISION
// ══════════════════════════════════════════════════════════════════════════════

public sealed class DeleteDivisionCommandHandler
    : IRequestHandler<Commands.DeleteDivisionCommand, Unit>
{
    private readonly ILeagueDbContext _db;

    public DeleteDivisionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Unit> Handle(
        Commands.DeleteDivisionCommand request,
        CancellationToken cancellationToken)
    {
        var division = await _db.Divisions
            .Include(d => d.Teams)
            .Include(d => d.Matches)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Division with ID {request.Id} not found.");

        if (division.Teams.Any() || division.Matches.Any())
        {
            throw new InvalidOperationException(
                "Cannot delete division with assigned teams or matches.");
        }

        // A division running a competition is not empty, even with no teams left in it: the
        // competition may be fielding clubs invited from elsewhere.
        var competitions = await _db.Competitions
            .CountAsync(c => c.DivisionId == division.Id, cancellationToken);

        if (competitions > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete a division running {competitions} "
                + (competitions == 1 ? "competition" : "competitions")
                + ". Delete those first.");
        }

        _db.Divisions.Remove(division);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET DIVISIONS (LIST)
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetDivisionsQueryHandler
    : IRequestHandler<Queries.GetDivisionsQuery, IReadOnlyList<DivisionDto>>
{
    private readonly ILeagueDbContext _db;

    public GetDivisionsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<DivisionDto>> Handle(
        Queries.GetDivisionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Divisions.AsQueryable();

        if (request.Season.HasValue)
            query = query.Where(d => d.Season == request.Season.Value);

        if (request.IsActive.HasValue)
            query = query.Where(d => d.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(d => d.Season)
            .ThenBy(d => d.Name)
            .Select(d => new DivisionDto(
                d.Id,
                d.Name,
                d.ShortCode,
                d.Season,
                d.AgeGroup,
                d.Gender.HasValue ? d.Gender.Value.ToString() : null,
                d.IsActive,
                d.StartDate,
                d.EndDate,
                d.Description,
                d.Teams.Count,
                d.Matches.Count,
                // What is being played here. A division no longer has a status of its own —
                // each competition has one, and they disagree by design.
                d.Competitions.Count
            ))
            .ToListAsync(cancellationToken);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET DIVISION BY ID
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetDivisionByIdQueryHandler
    : IRequestHandler<Queries.GetDivisionByIdQuery, DivisionDto?>
{
    private readonly ILeagueDbContext _db;

    public GetDivisionByIdQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<DivisionDto?> Handle(
        Queries.GetDivisionByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Divisions
            .Where(d => d.Id == request.Id)
            .Select(d => new DivisionDto(
                d.Id,
                d.Name,
                d.ShortCode,
                d.Season,
                d.AgeGroup,
                d.Gender.HasValue ? d.Gender.Value.ToString() : null,
                d.IsActive,
                d.StartDate,
                d.EndDate,
                d.Description,
                d.Teams.Count,
                d.Matches.Count,
                // What is being played here. A division no longer has a status of its own —
                // each competition has one, and they disagree by design.
                d.Competitions.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
