using iDiski.Application.Common.Authorization;
using MediatR;

namespace iDiski.Application.Suspensions.Commands;

/// <summary>
/// Scoped to the player's team, and so to the division that team plays in. A suspension stops
/// somebody playing, which makes an unscoped one the most consequential write in this set: any
/// division admin could ban any player in the league.
/// </summary>
public record CreateSuspensionCommand : IRequest<Guid>, IRequirePlayerAccess
{
    public Guid PlayerId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int MatchesSuspended { get; init; }
    public DateTime? StartDate { get; init; }
}
