using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Suspensions;

// ══════════════════════════════════════════════════════════════════════════════
// CREATE SUSPENSION (Manual)
// ══════════════════════════════════════════════════════════════════════════════

public sealed class CreateSuspensionCommandValidator : AbstractValidator<Commands.CreateSuspensionCommand>
{
    public CreateSuspensionCommandValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MatchesSuspended).GreaterThan(0);
    }
}

public sealed class CreateSuspensionCommandHandler
    : IRequestHandler<Commands.CreateSuspensionCommand, Guid>
{
    private readonly ILeagueDbContext _db;

    public CreateSuspensionCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<Guid> Handle(
        Commands.CreateSuspensionCommand request,
        CancellationToken cancellationToken)
    {
        var player = await _db.Players.FindAsync(new object[] { request.PlayerId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Player with ID {request.PlayerId} not found.");

        var startDate = request.StartDate ?? DateTime.UtcNow;
        var endDate = startDate.AddDays(request.MatchesSuspended * 7); // Rough estimate: 7 days per match

        var suspension = new Suspension
        {
            PlayerId = request.PlayerId,
            Reason = request.Reason,
            StartDate = startDate,
            EndDate = endDate,
            MatchesSuspended = request.MatchesSuspended,
            IsActive = true,
            AppliedByUser = "Admin" // TODO: Get from authenticated user
        };

        _db.Suspensions.Add(suspension);
        await _db.SaveChangesAsync(cancellationToken);

        return suspension.Id;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET ACTIVE SUSPENSIONS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetActiveSuspensionsQueryHandler
    : IRequestHandler<Queries.GetActiveSuspensionsQuery, IReadOnlyList<SuspensionDto>>
{
    private readonly ILeagueDbContext _db;

    public GetActiveSuspensionsQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<SuspensionDto>> Handle(
        Queries.GetActiveSuspensionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Suspensions
            .Where(s => s.IsActive && s.EndDate > DateTime.UtcNow);

        if (request.DivisionId.HasValue)
        {
            query = query.Where(s => s.Player.Team.DivisionId == request.DivisionId.Value);
        }

        return await query
            .OrderBy(s => s.EndDate)
            .Select(s => new SuspensionDto(
                s.Id,
                s.PlayerId,
                s.Player.FirstName + " " + s.Player.LastName,
                s.Player.Team.Name,
                s.Reason,
                s.StartDate,
                s.EndDate,
                s.MatchesSuspended,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET PLAYER SUSPENSION HISTORY
// ══════════════════════════════════════════════════════════════════════════════

public sealed class GetPlayerSuspensionHistoryQueryHandler
    : IRequestHandler<Queries.GetPlayerSuspensionHistoryQuery, IReadOnlyList<SuspensionDto>>
{
    private readonly ILeagueDbContext _db;

    public GetPlayerSuspensionHistoryQueryHandler(ILeagueDbContext db) => _db = db;

    public async Task<IReadOnlyList<SuspensionDto>> Handle(
        Queries.GetPlayerSuspensionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Suspensions
            .Where(s => s.PlayerId == request.PlayerId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SuspensionDto(
                s.Id,
                s.PlayerId,
                s.Player.FirstName + " " + s.Player.LastName,
                s.Player.Team.Name,
                s.Reason,
                s.StartDate,
                s.EndDate,
                s.MatchesSuspended,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}
