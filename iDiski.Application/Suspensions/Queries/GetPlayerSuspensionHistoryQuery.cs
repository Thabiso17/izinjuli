using MediatR;

namespace iDiski.Application.Suspensions.Queries;

public record GetPlayerSuspensionHistoryQuery(Guid PlayerId) : IRequest<IReadOnlyList<SuspensionDto>>;
