using MediatR;

namespace iDiski.Application.Suspensions.Queries;

/// <param name="DivisionId">Filter to suspensions of players in this division.</param>
/// <param name="TeamId">
/// Filter to one team. A league-wide list of everyone currently suspended is unusable once
/// there is more than a handful of divisions, so the dashboard narrows by team as well.
/// </param>
public record GetActiveSuspensionsQuery(
    Guid? DivisionId = null,
    Guid? TeamId = null
) : IRequest<IReadOnlyList<SuspensionDto>>;
