using iDiski.Application.Common.Authorization;
using MediatR;

namespace iDiski.Application.MatchEvents.Commands;

/// <summary>
/// Scoped to the competition the fixture belongs to. The endpoint's CanManageCompetitions policy
/// asks only whether somebody is a division admin, never which divisions — and goals and cards
/// are the detail of a result, so leaving them unscoped would have handed back most of what
/// scoping the score itself took away.
/// </summary>
public record RecordMatchEventsCommand : IRequest<Unit>, IRequireMatchAccess
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
