using MediatR;

namespace iDiski.Application.MatchEvents.Commands;

public record RecordMatchEventsCommand : IRequest<Unit>
{
    public Guid MatchId { get; init; }
    public List<MatchEventInput> Events { get; init; } = new();
}

public record MatchEventInput(
    Guid PlayerId,
    string EventType,
    int Minute,
    string? AdditionalInfo
);
