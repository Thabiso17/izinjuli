using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Teams.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CreateTeamCommand(
    string  Name,
    string  ShortCode,
    string? LogoUrl,
    int     Founded,
    string? HomeGround,
    string? City,
    string? PrimaryColour,
    string? SecondaryColour
) : IRequest<Guid>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    private readonly ILeagueDbContext _db;

    public CreateTeamCommandValidator(ILeagueDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[A-Z0-9]+$").WithMessage("ShortCode must be uppercase letters or digits.")
            .MustAsync(BeUniqueShortCode).WithMessage("ShortCode is already taken.");

        RuleFor(x => x.Founded)
            .InclusiveBetween(1800, DateTime.UtcNow.Year);

        RuleFor(x => x.PrimaryColour)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Must be a valid hex colour, e.g. #FF0000.")
            .When(x => x.PrimaryColour is not null);
    }

    private async Task<bool> BeUniqueShortCode(
        string shortCode, CancellationToken cancellationToken)
        => !await _db.Teams.AnyAsync(t => t.ShortCode == shortCode, cancellationToken);
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateTeamCommandHandler
    : IRequestHandler<CreateTeamCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        CreateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var team = new Team
        {
            Name            = request.Name,
            ShortCode       = request.ShortCode.ToUpperInvariant(),
            LogoUrl         = request.LogoUrl,
            Founded         = request.Founded,
            HomeGround      = request.HomeGround,
            City            = request.City,
            PrimaryColour   = request.PrimaryColour,
            SecondaryColour = request.SecondaryColour
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync(cancellationToken);

        return team.Id;
    }
}
