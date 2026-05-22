using MediatR;

namespace iDiski.Application.MatchEvents.Queries;

public record GetMatchEventsQuery(Guid MatchId) : IRequest<IReadOnlyList<MatchEventDto>>;
