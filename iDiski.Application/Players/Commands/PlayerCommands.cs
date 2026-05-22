using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Players.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// CREATE
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreatePlayerCommand(
    string         FirstName,
    string         LastName,
    string?        ProfileImageUrl,
    string?        Bio,
    DateTime       DateOfBirth,
    string?        Nationality,
    int            JerseyNumber,
    PlayerPosition Position,
    PreferredFoot  PreferredFoot,
    Guid           TeamId
) : IRequest<Guid>;

public sealed class CreatePlayerCommandValidator : AbstractValidator<CreatePlayerCommand>
{
    private readonly ILeagueDbContext _db;

    public CreatePlayerCommandValidator(ILeagueDbContext db)
    {
        _db = db;

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TeamId).NotEmpty();

        RuleFor(x => x.JerseyNumber)
            .InclusiveBetween(1, 99)
            .MustAsync(BeUniqueJerseyInTeam)
            .WithMessage("Jersey number is already taken in this team.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-14))
            .WithMessage("Player must be at least 14 years old.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000)
            .When(x => x.Bio is not null);
    }

    private async Task<bool> BeUniqueJerseyInTeam(
        CreatePlayerCommand cmd, int jersey, CancellationToken ct)
        => !await _db.Players.AnyAsync(
            p => p.TeamId == cmd.TeamId && p.JerseyNumber == jersey && p.IsActive, ct);
}

public sealed class CreatePlayerCommandHandler
    : IRequestHandler<CreatePlayerCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreatePlayerCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        CreatePlayerCommand request,
        CancellationToken cancellationToken)
    {
        var teamExists = await _db.Teams
            .AnyAsync(t => t.Id == request.TeamId, cancellationToken);

        if (!teamExists)
            throw new NotFoundException(nameof(Team), request.TeamId);

        var player = new Player
        {
            FirstName       = request.FirstName,
            LastName        = request.LastName,
            ProfileImageUrl = request.ProfileImageUrl,
            Bio             = request.Bio,
            DateOfBirth     = request.DateOfBirth,
            Nationality     = request.Nationality,
            JerseyNumber    = request.JerseyNumber,
            Position        = request.Position,
            PreferredFoot   = request.PreferredFoot,
            TeamId          = request.TeamId,
            IsActive        = true
        };

        _db.Players.Add(player);
        await _db.SaveChangesAsync(cancellationToken);

        return player.Id;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UPDATE
// ═════════════════════════════════════════════════════════════════════════════

public sealed record UpdatePlayerCommand(
    Guid           Id,
    string         FirstName,
    string         LastName,
    string?        ProfileImageUrl,
    string?        Bio,
    string?        Nationality,
    int            JerseyNumber,
    PlayerPosition Position,
    PreferredFoot  PreferredFoot,
    Guid           TeamId,
    bool           IsActive
) : IRequest;

public sealed class UpdatePlayerCommandValidator : AbstractValidator<UpdatePlayerCommand>
{
    public UpdatePlayerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JerseyNumber).InclusiveBetween(1, 99);

        RuleFor(x => x.Bio)
            .MaximumLength(2000)
            .When(x => x.Bio is not null);
    }
}

public sealed class UpdatePlayerCommandHandler : IRequestHandler<UpdatePlayerCommand>
{
    private readonly ILeagueDbContext _db;

    public UpdatePlayerCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await _db.Players.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Player), request.Id);

        player.FirstName       = request.FirstName;
        player.LastName        = request.LastName;
        player.ProfileImageUrl = request.ProfileImageUrl;
        player.Bio             = request.Bio;
        player.Nationality     = request.Nationality;
        player.JerseyNumber    = request.JerseyNumber;
        player.Position        = request.Position;
        player.PreferredFoot   = request.PreferredFoot;
        player.TeamId          = request.TeamId;
        player.IsActive        = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// TRANSFER (move player to different team)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record TransferPlayerCommand(
    Guid PlayerId,
    Guid NewTeamId,
    int  NewJerseyNumber
) : IRequest;

public sealed class TransferPlayerCommandValidator : AbstractValidator<TransferPlayerCommand>
{
    private readonly ILeagueDbContext _db;

    public TransferPlayerCommandValidator(ILeagueDbContext db)
    {
        _db = db;

        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.NewTeamId).NotEmpty();
        RuleFor(x => x.NewJerseyNumber)
            .InclusiveBetween(1, 99)
            .MustAsync(BeUniqueJerseyInNewTeam)
            .WithMessage("Jersey number is already taken in the new team.");
    }

    private async Task<bool> BeUniqueJerseyInNewTeam(
        TransferPlayerCommand cmd, int jersey, CancellationToken ct)
    {
        // Check if any active player in the new team already has this jersey number
        // Exclude the player being transferred
        var exists = await _db.Players.AnyAsync(
            p => p.TeamId == cmd.NewTeamId
                && p.JerseyNumber == jersey
                && p.IsActive
                && p.Id != cmd.PlayerId,
            ct);

        return !exists;
    }
}

public sealed class TransferPlayerCommandHandler : IRequestHandler<TransferPlayerCommand>
{
    private readonly ILeagueDbContext _db;

    public TransferPlayerCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(TransferPlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await _db.Players.FindAsync([request.PlayerId], cancellationToken)
            ?? throw new NotFoundException(nameof(Player), request.PlayerId);

        var newTeam = await _db.Teams.FindAsync([request.NewTeamId], cancellationToken)
            ?? throw new NotFoundException(nameof(Team), request.NewTeamId);

        // Update player's team and jersey number
        player.TeamId = request.NewTeamId;
        player.JerseyNumber = request.NewJerseyNumber;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DELETE (soft-delete via IsActive flag)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record DeletePlayerCommand(Guid Id) : IRequest;

public sealed class DeletePlayerCommandHandler : IRequestHandler<DeletePlayerCommand>
{
    private readonly ILeagueDbContext _db;

    public DeletePlayerCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task Handle(DeletePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await _db.Players.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException(nameof(Player), request.Id);

        // Soft-delete: preserve historical stats
        player.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
