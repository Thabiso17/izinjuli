using MediatR;

namespace iDiski.Application.Suspensions.Commands;

public record CreateSuspensionCommand : IRequest<Guid>
{
    public Guid PlayerId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int MatchesSuspended { get; init; }
    public DateTime? StartDate { get; init; }
}
