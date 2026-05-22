using MediatR;

namespace iDiski.Application.MatchEvents.Queries;

public record GetPlayerMatchEventsQuery(Guid PlayerId, int? Season = null) : IRequest<IReadOnlyList<MatchEventDto>>;
