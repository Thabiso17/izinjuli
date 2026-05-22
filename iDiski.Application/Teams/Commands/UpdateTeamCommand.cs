using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using MediatR;
using FluentValidation;

namespace iDiski.Application.Teams.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record UpdateTeamCommand(
    Guid    Id,
    string  Name,
    string? LogoUrl,
    int     Founded,
    string? HomeGround,
    string? City,
    string? PrimaryColour,
    string? SecondaryColour
) : IRequest;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Founded)
            .InclusiveBetween(1800, DateTime.UtcNow.Year);

        RuleFor(x => x.PrimaryColour)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Must be a valid hex colour.")
            .When(x => x.PrimaryColour is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdateTeamCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(
        UpdateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Team), request.Id);

        team.Name            = request.Name;
        team.LogoUrl         = request.LogoUrl;
        team.Founded         = request.Founded;
        team.HomeGround      = request.HomeGround;
        team.City            = request.City;
        team.PrimaryColour   = request.PrimaryColour;
        team.SecondaryColour = request.SecondaryColour;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
